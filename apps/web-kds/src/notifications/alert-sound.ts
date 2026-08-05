/**
 * US-031 §10: "chegada de pedido no KDS com sinal sonoro configurável — a cozinha não fica olhando
 * a tela". Cópia deliberada de `apps/web-pos/src/notifications/alert-sound.ts` (US-025 §10, mesmo
 * motivo ali documentado) — cada app do monorepo é um pacote próprio sem alias entre `apps/*`, e o
 * arquivo é pequeno e autocontido o bastante para não justificar mover para `packages/ui` (que os
 * outros dois times desta mesma onda também tocam, ver limites de arquivo da tarefa).
 *
 * US-045 (Alerta sonoro de pedido novo e de atraso) estende este módulo por baixo dos dois
 * exports que `kds-queue-page.tsx` já chamava sem argumento nenhum (`playAlertChime()`,
 * `vibrateAlert()`) — esse arquivo é INTOCÁVEL nesta história (outro time mexe nele em paralelo),
 * então volume/timbre/mudo/agrupamento em rajada viram config de MÓDULO aqui
 * (`configureAlertSound`), lida a cada chamada dessas duas funções. `useSoundAlerts`
 * (`../kds/sound-preferences.tsx`) mantém essa config sincronizada com as preferências carregadas
 * de `PATCH /v1/devices/{id}/preferences` — sem precisar tocar em `kds-queue-page.tsx`.
 */

export type AlertTone = 'CHIME' | 'ALERT';

/** CHIME (pedido novo) e ALERT (atraso crítico) precisam soar claramente diferentes (US-045 §10: "não podem soar parecido") — frequências e forma de onda distintas, não só volume. */
const TONE_PROFILE: Record<AlertTone, { readonly frequencies: readonly number[]; readonly oscillatorType: OscillatorType }> = {
  CHIME: { frequencies: [880, 1046.5], oscillatorType: 'sine' },
  ALERT: { frequencies: [523.25, 392, 523.25], oscillatorType: 'square' },
};

const DEFAULT_VOLUME = 0.15;

interface AlertSoundConfig {
  readonly muted: boolean;
  readonly volume: number;
  readonly newOrderTone: AlertTone;
  readonly lateTone: AlertTone;
}

const DEFAULT_CONFIG: AlertSoundConfig = {
  muted: false,
  volume: DEFAULT_VOLUME,
  newOrderTone: 'CHIME',
  lateTone: 'ALERT',
};

let currentConfig: AlertSoundConfig = { ...DEFAULT_CONFIG };

/**
 * US-045 §3.1/§7 — aplica as preferências de som do dispositivo (`kds.sound`, mescladas por
 * `PATCH /v1/devices/{id}/preferences`) a cada alerta sonoro do KDS, inclusive as chamadas sem
 * argumento que `kds-queue-page.tsx` já fazia antes desta história. Parcial de propósito: chamador
 * (`useSoundAlerts`) só passa o que já resolveu.
 */
export function configureAlertSound(config: Partial<AlertSoundConfig>): void {
  currentConfig = { ...currentConfig, ...config };
}

/** Só para teste: volta a config e o agrupamento em rajada ao estado inicial (o módulo é singleton entre casos). */
export function resetAlertSoundConfigForTests(): void {
  currentConfig = { ...DEFAULT_CONFIG };
  lastPlayedAtByTone.clear();
}

/** Padrão curto (200ms liga, 100ms pausa, 200ms liga) — perceptível sem ser incômodo. Respeita o modo silencioso (US-045 §3.1: "nenhum som deve tocar" cobre também a vibração). */
export function vibrateAlert(navigatorRef: Navigator | undefined = typeof navigator === 'undefined' ? undefined : navigator): void {
  if (currentConfig.muted) return;
  try {
    navigatorRef?.vibrate?.([200, 100, 200]);
  } catch {
    // Alguns navegadores lançam se chamado fora de um gesto do usuário — ignora, é só reforço.
  }
}

/** Padrão mais longo e com mais pulsos que `vibrateAlert` — atraso crítico precisa se distinguir de pedido novo também no tato. Mesma regra de modo silencioso. */
export function vibrateLateAlert(navigatorRef: Navigator | undefined = typeof navigator === 'undefined' ? undefined : navigator): void {
  if (currentConfig.muted) return;
  try {
    navigatorRef?.vibrate?.([300, 100, 300, 100, 300]);
  } catch {
    // Ver vibrateAlert.
  }
}

/** US-045 §3.1 "Agrupamento de sons em rajada": 5 pedidos em 2s tocam 1 vez, não 5. Janela por TIMBRE — pedido novo e atraso nunca suprimem um ao outro. */
const BURST_WINDOW_MS = 2000;
const lastPlayedAtByTone = new Map<AlertTone, number>();

interface PlayToneOptions {
  readonly AudioContextCtor?: typeof AudioContext | undefined;
  /** 0–1 (US-045 §7 `kds.sound.volume`); fora da faixa é grampeado. */
  readonly volume?: number | undefined;
  /** Ignora o agrupamento em rajada — só o botão "testar som" da configuração usa isto (US-045 §10: "toca o chime na hora, sem esperar evento real"). */
  readonly bypassThrottle?: boolean | undefined;
}

/**
 * Beep curto via Web Audio API — sem depender de um arquivo de áudio externo (o edge precisa
 * funcionar 100% local). `AudioContextCtor` é injetável só para teste (jsdom não implementa
 * `AudioContext`).
 */
function playTone(tone: AlertTone, options: Readonly<PlayToneOptions> = {}): void {
  const { AudioContextCtor, volume = DEFAULT_VOLUME, bypassThrottle = false } = options;

  if (!bypassThrottle) {
    const now = Date.now();
    const last = lastPlayedAtByTone.get(tone);
    if (last !== undefined && now - last < BURST_WINDOW_MS) return;
    lastPlayedAtByTone.set(tone, now);
  }

  const Ctor = AudioContextCtor ?? (typeof AudioContext !== 'undefined' ? AudioContext : undefined);
  if (!Ctor) return;
  const clampedVolume = Math.min(1, Math.max(0, volume));
  if (clampedVolume <= 0) return;

  try {
    const context = new Ctor();
    const start = context.currentTime;
    const { frequencies, oscillatorType } = TONE_PROFILE[tone];
    frequencies.forEach((frequency, index) => {
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.frequency.value = frequency;
      oscillator.type = oscillatorType;
      gain.gain.setValueAtTime(clampedVolume, start + index * 0.15);
      gain.gain.exponentialRampToValueAtTime(0.001, start + index * 0.15 + 0.14);
      oscillator.connect(gain).connect(context.destination);
      oscillator.start(start + index * 0.15);
      oscillator.stop(start + index * 0.15 + 0.15);
    });
    setTimeout(() => void context.close(), 500);
  } catch {
    // Ambiente sem suporte real a áudio (ex.: alguns testes headless) — silencioso, não é crítico.
  }
}

/**
 * Som de pedido novo. Chamado SEM argumento por `kds-queue-page.tsx` (arquivo intocável nesta
 * história) — volume/timbre/mudo efetivos vêm de `configureAlertSound`, não de parâmetro aqui.
 */
export function playAlertChime(AudioContextCtor?: typeof AudioContext): void {
  if (currentConfig.muted) return;
  playTone(currentConfig.newOrderTone, { AudioContextCtor, volume: currentConfig.volume });
}

/** Som de atraso crítico (US-045, novo nesta história) — mesma regra de config de `playAlertChime`. */
export function playLateAlertChime(AudioContextCtor?: typeof AudioContext): void {
  if (currentConfig.muted) return;
  playTone(currentConfig.lateTone, { AudioContextCtor, volume: currentConfig.volume });
}

/**
 * US-045 §10 "testar som" — ignora mudo E agrupamento em rajada de propósito: é uma ação
 * explícita da pessoa configurando o som, não um alerta de evento real.
 */
export function previewAlertTone(tone: AlertTone, volume?: number, AudioContextCtor?: typeof AudioContext): void {
  playTone(tone, { AudioContextCtor, bypassThrottle: true, ...(volume === undefined ? {} : { volume }) });
}

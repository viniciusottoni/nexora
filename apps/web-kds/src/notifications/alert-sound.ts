/**
 * US-031 §10: "chegada de pedido no KDS com sinal sonoro configurável — a cozinha não fica olhando
 * a tela". Cópia deliberada de `apps/web-pos/src/notifications/alert-sound.ts` (US-025 §10, mesmo
 * motivo ali documentado) — cada app do monorepo é um pacote próprio sem alias entre `apps/*`, e o
 * arquivo é pequeno e autocontido o bastante para não justificar mover para `packages/ui` (que os
 * outros dois times desta mesma onda também tocam, ver limites de arquivo da tarefa).
 */

/** Padrão curto (200ms liga, 100ms pausa, 200ms liga) — perceptível sem ser incômodo. */
export function vibrateAlert(navigatorRef: Navigator | undefined = typeof navigator === 'undefined' ? undefined : navigator): void {
  try {
    navigatorRef?.vibrate?.([200, 100, 200]);
  } catch {
    // Alguns navegadores lançam se chamado fora de um gesto do usuário — ignora, é só reforço.
  }
}

/**
 * Beep curto de dois tons via Web Audio API — sem depender de um arquivo de áudio externo (o
 * edge precisa funcionar 100% local). `AudioContextCtor` é injetável só para teste (jsdom não
 * implementa `AudioContext`).
 */
export function playAlertChime(AudioContextCtor?: typeof AudioContext): void {
  const Ctor = AudioContextCtor ?? (typeof AudioContext !== 'undefined' ? AudioContext : undefined);
  if (!Ctor) return;

  try {
    const context = new Ctor();
    const now = context.currentTime;
    [880, 1046.5].forEach((frequency, index) => {
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.frequency.value = frequency;
      oscillator.type = 'sine';
      gain.gain.setValueAtTime(0.15, now + index * 0.15);
      gain.gain.exponentialRampToValueAtTime(0.001, now + index * 0.15 + 0.14);
      oscillator.connect(gain).connect(context.destination);
      oscillator.start(now + index * 0.15);
      oscillator.stop(now + index * 0.15 + 0.15);
    });
    setTimeout(() => void context.close(), 500);
  } catch {
    // Ambiente sem suporte real a áudio (ex.: alguns testes headless) — silencioso, não é crítico.
  }
}

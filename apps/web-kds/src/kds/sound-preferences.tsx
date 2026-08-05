import { useCallback, useEffect, useRef, useState } from 'react';
import type { KdsDeviceSoundPreferences } from '@nexora/contracts';
import type { KdsQueueItem, KdsThresholdState } from '@nexora/contracts';
import { Button, Field, Icon, Modal, Select, Switch, type OperationalRequestIdentity } from '@nexora/ui';
import { configureAlertSound, playLateAlertChime, previewAlertTone, type AlertTone } from '../notifications/alert-sound.js';
import { DevicePreferencesApi } from './device-preferences-api.js';
import './sound-preferences.css';

/**
 * US-045 §7 (`PATCH /v1/devices/{id}/preferences`, sub-chave `kds.sound`) — mesmo default do
 * exemplo do contrato de API na história: som ativo, volume alto o bastante pra vencer a coifa,
 * timbres distintos entre pedido novo e atraso, repetição a cada 60s enquanto o item continuar
 * crítico.
 */
export const DEFAULT_SOUND_PREFERENCES: Readonly<KdsDeviceSoundPreferences> = {
  enabled: true,
  volume: 0.8,
  newOrderTone: 'CHIME',
  lateTone: 'ALERT',
  lateRepeatSeconds: 60,
};

/** Classe aplicada ao cartão do pedido enquanto o reforço visual do modo silencioso estiver ativo — ver `sound-preferences.css`. Reutilize esta constante no lugar de escrever a string solta. */
export const SILENT_ALERT_FLASH_CLASS_NAME = 'kds-sound-alert-flash';

/** Quanto tempo a classe de reforço visual fica aplicada a cada disparo — pulso, não permanente (o cartão já fica vermelho por conta própria via `db-order-ticket--late`, isto é reforço ADICIONAL). */
const SILENT_FLASH_DURATION_MS = 4000;

function storageKey(deviceId: string): string {
  return `nexora:kds:sound-preferences:${deviceId}`;
}

function readCachedPreferences(deviceId: string | undefined): KdsDeviceSoundPreferences {
  if (!deviceId) return DEFAULT_SOUND_PREFERENCES;
  try {
    const raw = globalThis.localStorage?.getItem(storageKey(deviceId));
    if (!raw) return DEFAULT_SOUND_PREFERENCES;
    const parsed = JSON.parse(raw) as Partial<KdsDeviceSoundPreferences>;
    return { ...DEFAULT_SOUND_PREFERENCES, ...parsed };
  } catch {
    // Cache é só conveniência (evita depender de um GET que a API não expõe) — corrompido ou
    // indisponível (modo privado, quota), volta ao padrão sem quebrar a tela.
    return DEFAULT_SOUND_PREFERENCES;
  }
}

function writeCachedPreferences(deviceId: string | undefined, preferences: KdsDeviceSoundPreferences): void {
  if (!deviceId) return;
  try {
    globalThis.localStorage?.setItem(storageKey(deviceId), JSON.stringify(preferences));
  } catch {
    // Ver readCachedPreferences.
  }
}

export interface UseDeviceSoundPreferencesResult {
  readonly preferences: KdsDeviceSoundPreferences;
  /** Mescla o patch localmente (otimista) E envia `PATCH /v1/devices/{id}/preferences` com só a fatia `sound` que mudou — o servidor faz a mescla profunda, então nunca reenvia o objeto inteiro. */
  readonly updatePreferences: (patch: Partial<KdsDeviceSoundPreferences>) => Promise<void>;
  readonly saving: boolean;
  readonly error: string | undefined;
}

/**
 * Carrega/persiste `kds.sound` (US-045 §7) e mantém `alert-sound.ts` sincronizado via
 * `configureAlertSound` — é assim que `playAlertChime()`/`vibrateAlert()`, já chamados sem
 * argumento por `kds-queue-page.tsx`, passam a respeitar volume/timbre/mudo sem que este arquivo
 * intocável precise mudar. Não existe `GET` de preferências no backend (só o `PATCH` devolve o
 * estado mesclado) — por isso o cache local em `localStorage` é a fonte de verdade entre cargas de
 * página, e o `PATCH` é o que mantém o servidor (e outros dispositivos administrando este, se
 * algum dia existir) a par da última escolha.
 */
export function useDeviceSoundPreferences(
  identity: Readonly<OperationalRequestIdentity> | undefined,
  api: DevicePreferencesApi = new DevicePreferencesApi(),
): UseDeviceSoundPreferencesResult {
  const [preferences, setPreferences] = useState<KdsDeviceSoundPreferences>(() =>
    readCachedPreferences(identity?.deviceId),
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    setPreferences(readCachedPreferences(identity?.deviceId));
  }, [identity?.deviceId]);

  useEffect(() => {
    configureAlertSound({
      muted: !preferences.enabled,
      volume: preferences.volume,
      newOrderTone: preferences.newOrderTone ?? 'CHIME',
      lateTone: preferences.lateTone ?? 'ALERT',
    });
  }, [preferences.enabled, preferences.volume, preferences.newOrderTone, preferences.lateTone]);

  const updatePreferences = useCallback(
    async (patch: Partial<KdsDeviceSoundPreferences>) => {
      const next: KdsDeviceSoundPreferences = { ...preferences, ...patch };
      setPreferences(next);
      writeCachedPreferences(identity?.deviceId, next);
      if (!identity) return;

      setSaving(true);
      setError(undefined);
      try {
        await api.updateKdsPreferences(identity, { sound: patch });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Não foi possível salvar a preferência de som.');
      } finally {
        setSaving(false);
      }
    },
    [api, identity, preferences],
  );

  return { preferences, updatePreferences, saving, error };
}

export interface SoundAlertsState {
  /** `orderItemId` dos itens com reforço visual ativo agora — SÓ preenchido em modo silencioso (US-045 §3.1 "Modo silencioso": "nenhum som deve tocar E o sinal visual deve ser reforçado"). Aplique `SILENT_ALERT_FLASH_CLASS_NAME` no cartão que contém o item. */
  readonly silentFlashItemIds: ReadonlySet<string>;
}

/**
 * US-045 — decide QUANDO tocar o alerta de atraso e repete a cada `lateRepeatSeconds` enquanto o
 * item continuar `CRITICAL` (nunca contínuo). O som/vibração de PEDIDO NOVO já existe em
 * `kds-queue-page.tsx` e não é duplicado aqui — este hook só adiciona o reforço visual do modo
 * silencioso para pedido novo (comparando a mesma lista de itens que a página já busca) e cuida
 * inteiramente do alerta de atraso, que não existia antes desta história.
 *
 * `items` é a MESMA referência que `kds-queue-page.tsx` mantém em `state` (nova a cada
 * `refresh()`/evento SignalR) — a cada mudança, comparamos o `thresholdState` de cada item com o
 * da rodada anterior para detectar quem ACABOU de cruzar para `CRITICAL`.
 */
export function useSoundAlerts(
  items: readonly KdsQueueItem[],
  preferences: Readonly<KdsDeviceSoundPreferences>,
): SoundAlertsState {
  const knownItemIdsRef = useRef<Set<string>>(new Set());
  const previousThresholdRef = useRef<Map<string, KdsThresholdState>>(new Map());
  const lastLateAlertAtRef = useRef<Map<string, number>>(new Map());
  const hasLoadedOnceRef = useRef(false);
  const flashTimeoutsRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());
  const [silentFlashItemIds, setSilentFlashItemIds] = useState<ReadonlySet<string>>(new Set());

  useEffect(
    () => () => {
      for (const timeout of flashTimeoutsRef.current.values()) clearTimeout(timeout);
    },
    [],
  );

  const flashItem = useCallback((itemId: string) => {
    setSilentFlashItemIds((prev) => {
      const next = new Set(prev);
      next.add(itemId);
      return next;
    });
    const existingTimeout = flashTimeoutsRef.current.get(itemId);
    if (existingTimeout) clearTimeout(existingTimeout);
    flashTimeoutsRef.current.set(
      itemId,
      setTimeout(() => {
        flashTimeoutsRef.current.delete(itemId);
        setSilentFlashItemIds((prev) => {
          if (!prev.has(itemId)) return prev;
          const next = new Set(prev);
          next.delete(itemId);
          return next;
        });
      }, SILENT_FLASH_DURATION_MS),
    );
  }, []);

  const triggerLateAlert = useCallback(
    (itemId: string, now: number) => {
      lastLateAlertAtRef.current.set(itemId, now);
      if (preferences.enabled) {
        playLateAlertChime();
      } else {
        flashItem(itemId);
      }
    },
    [preferences.enabled, flashItem],
  );

  // Detecta pedido novo (reforço visual só) e transição para CRITICAL (dispara o alerta de atraso).
  useEffect(() => {
    const now = Date.now();
    const currentIds = new Set<string>();

    for (const item of items) {
      currentIds.add(item.orderItemId);

      if (hasLoadedOnceRef.current && !knownItemIdsRef.current.has(item.orderItemId) && !preferences.enabled) {
        flashItem(item.orderItemId);
      }

      const previousState = previousThresholdRef.current.get(item.orderItemId);
      if (item.thresholdState === 'CRITICAL' && previousState !== 'CRITICAL') {
        triggerLateAlert(item.orderItemId, now);
      }
      previousThresholdRef.current.set(item.orderItemId, item.thresholdState);
    }

    knownItemIdsRef.current = currentIds;
    hasLoadedOnceRef.current = true;

    for (const id of Array.from(previousThresholdRef.current.keys())) {
      if (!currentIds.has(id)) {
        previousThresholdRef.current.delete(id);
        lastLateAlertAtRef.current.delete(id);
      }
    }
  }, [items, preferences.enabled, flashItem, triggerLateAlert]);

  // Repetição do alerta de atraso — verifica a cada 1s se algum item CRITICAL já passou de
  // `lateRepeatSeconds` desde o último disparo. Desacoplado do ciclo de poll de `kds-queue-page.tsx`
  // (5s) para o intervalo configurado ser respeitado com precisão, inclusive em teste com timers falsos.
  useEffect(() => {
    const repeatMs = (preferences.lateRepeatSeconds ?? 60) * 1000;
    const interval = setInterval(() => {
      const now = Date.now();
      for (const item of items) {
        if (item.thresholdState !== 'CRITICAL') continue;
        const last = lastLateAlertAtRef.current.get(item.orderItemId);
        if (last !== undefined && now - last >= repeatMs) {
          triggerLateAlert(item.orderItemId, now);
        }
      }
    }, 1000);
    return () => clearInterval(interval);
  }, [items, preferences.lateRepeatSeconds, triggerLateAlert]);

  return { silentFlashItemIds };
}

const TONE_OPTIONS: ReadonlyArray<{ readonly value: AlertTone; readonly label: string }> = [
  { value: 'CHIME', label: 'Chime (agudo, curto)' },
  { value: 'ALERT', label: 'Alerta (grave, insistente)' },
];

export interface SoundSettingsPanelProps {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly preferences: Readonly<KdsDeviceSoundPreferences>;
  readonly onChange: (patch: Partial<KdsDeviceSoundPreferences>) => void | Promise<void>;
  readonly saving?: boolean;
  readonly error?: string | undefined;
}

/**
 * Painel de configuração de som do KDS (US-045 §3.1/§10) — volume, timbre de pedido novo e de
 * atraso, intervalo de repetição do atraso e modo silencioso, com "testar som" que toca na hora
 * (`previewAlertTone`, ignora mudo e agrupamento em rajada de propósito).
 */
export function SoundSettingsPanel({
  open,
  onClose,
  preferences,
  onChange,
  saving = false,
  error,
}: Readonly<SoundSettingsPanelProps>) {
  const newOrderTone = preferences.newOrderTone ?? 'CHIME';
  const lateTone = preferences.lateTone ?? 'ALERT';
  const lateRepeatSeconds = preferences.lateRepeatSeconds ?? 60;

  return (
    <Modal
      open={open}
      onClose={onClose}
      eyebrow="Configuração do dispositivo"
      title="Som da cozinha"
      actions={
        <Button type="button" size="touch" onClick={onClose}>
          Fechar
        </Button>
      }
    >
      <div className="kds-sound-panel">
        <Switch
          label="Som ativado"
          description="Desativar entra em modo silencioso — os cartões continuam avisando por reforço visual."
          checked={preferences.enabled}
          onChange={(event) => void onChange({ enabled: event.target.checked })}
        />

        <Field label="Volume" hint={`${Math.round(preferences.volume * 100)}%`}>
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={preferences.volume}
            disabled={!preferences.enabled}
            onChange={(event) => void onChange({ volume: Number(event.target.value) })}
            aria-label="Volume do som do KDS"
          />
        </Field>

        <Field label="Som de pedido novo">
          <div className="kds-sound-panel__row">
            <Select
              size="lg"
              value={newOrderTone}
              onChange={(event) => void onChange({ newOrderTone: event.target.value as AlertTone })}
              options={TONE_OPTIONS.map((option) => ({ value: option.value, label: option.label }))}
              aria-label="Timbre do som de pedido novo"
            />
            <Button
              type="button"
              variant="secondary"
              size="touch"
              onClick={() => previewAlertTone(newOrderTone, preferences.volume)}
            >
              <Icon name="play_circle" size={20} />
              Testar
            </Button>
          </div>
        </Field>

        <Field label="Som de atraso crítico">
          <div className="kds-sound-panel__row">
            <Select
              size="lg"
              value={lateTone}
              onChange={(event) => void onChange({ lateTone: event.target.value as AlertTone })}
              options={TONE_OPTIONS.map((option) => ({ value: option.value, label: option.label }))}
              aria-label="Timbre do som de atraso crítico"
            />
            <Button
              type="button"
              variant="secondary"
              size="touch"
              onClick={() => previewAlertTone(lateTone, preferences.volume)}
            >
              <Icon name="play_circle" size={20} />
              Testar
            </Button>
          </div>
        </Field>

        <Field label="Repetir alerta de atraso a cada (segundos)" hint="Enquanto o item continuar em atraso crítico — nunca contínuo.">
          <input
            type="number"
            min={10}
            step={5}
            value={lateRepeatSeconds}
            onChange={(event) => {
              const parsed = Number(event.target.value);
              if (Number.isFinite(parsed) && parsed > 0) void onChange({ lateRepeatSeconds: Math.round(parsed) });
            }}
            aria-label="Intervalo de repetição do alerta de atraso, em segundos"
          />
        </Field>

        {saving ? (
          <p className="kds-sound-panel__status" role="status">
            Salvando…
          </p>
        ) : null}
        {error ? (
          <p className="kds-sound-panel__error nx-anim-in" role="alert">
            {error}
          </p>
        ) : null}
      </div>
    </Modal>
  );
}

import { useCallback, useEffect, useMemo, useState } from 'react';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { DevicePreferencesApi } from './device-preferences-api.js';
import { DEFAULT_PEAK_MODE_THRESHOLDS, resolvePeakMode, type PeakModeThresholds } from './peak-mode.js';

export interface UsePeakModeOptions {
  /** Contagem de CARTÕES/pedidos na fila (`orderGroups.length` de `groupItemsByOrder`), não de itens — US-047 §2. */
  readonly orderCount: number;
  /** `undefined` antes do login — mesmo padrão de `UseNotificationCenterOptions.identity`: o hook fica ocioso (sem PATCH) até existir. */
  readonly identity: Readonly<OperationalRequestIdentity> | undefined;
  readonly baseUrl?: string;
  readonly api?: DevicePreferencesApi;
  readonly thresholds?: PeakModeThresholds;
}

export interface UsePeakModeResult {
  readonly active: boolean;
  /** `true` quando a última decisão do operador foi DESLIGAR manualmente — usado pelo banner para oferecer "reativar". */
  readonly manuallyDisabled: boolean;
  readonly thresholdItems: number;
  readonly hysteresisItems: number;
  /** Liga/desliga manualmente — SEMPRE tem prioridade sobre o cálculo automático (§10, cenário "Sobreposição manual"). */
  readonly toggle: () => void;
}

/**
 * US-047 — combina o cálculo automático de `resolvePeakMode` (histerese) com a ativação/
 * desativação MANUAL do operador, que TEM PRIORIDADE sobre o automático (§10, cenário
 * "Sobreposição manual": "deve permanecer desativado até o fim do turno"). Enquanto o operador não
 * mexeu em nada (`manualOverride === null`), quem decide é o automático; a partir do primeiro
 * toque em "ligar"/"desligar", a decisão manual vence — mesmo que a fila continue grande — até o
 * operador tocar de novo.
 *
 * [LIMITAÇÃO CONHECIDA] A escolha manual é persistida em `PATCH /v1/devices/{id}/preferences`
 * (`kds.peakMode.manuallyDisabled`, via `DevicePreferencesApi`) só como melhor-esforço — não existe
 * hoje um `GET` de preferência de UM dispositivo no contrato (a listagem não devolve
 * `preferences`, e o único retorno de preferências é a resposta do próprio `PATCH`). Por isso a
 * sobreposição manual dura a SESSÃO deste terminal (recarregar a página volta ao automático), não
 * literalmente "até o fim do turno" persistido no servidor — comportamento aceitável dado que
 * "recarregar = nova sessão" já é o limite de turno usado em outros pontos do KDS (ex.:
 * `hasLoadedOnceRef` em `kds-queue-page.tsx`). Ver relatório da US-047 para o detalhe.
 */
export function usePeakMode({
  orderCount,
  identity,
  baseUrl = '',
  api,
  thresholds = DEFAULT_PEAK_MODE_THRESHOLDS,
}: UsePeakModeOptions): UsePeakModeResult {
  const preferencesApi = useMemo(() => api ?? new DevicePreferencesApi(baseUrl), [api, baseUrl]);
  const [autoActive, setAutoActive] = useState(false);
  // `null` = nenhuma decisão manual tomada nesta sessão ainda (o automático manda). `true`/`false`
  // = o operador já decidiu, e essa decisão vence o automático até a próxima decisão manual.
  const [manualOverride, setManualOverride] = useState<boolean | null>(null);

  useEffect(() => {
    setAutoActive((current) =>
      resolvePeakMode(current, orderCount, thresholds.thresholdItems, thresholds.hysteresisItems),
    );
  }, [orderCount, thresholds.thresholdItems, thresholds.hysteresisItems]);

  const active = manualOverride ?? autoActive;

  const toggle = useCallback(() => {
    const next = !active;
    setManualOverride(next);
    if (identity) {
      void preferencesApi
        .updateKdsPreferences(identity, {
          peakMode: {
            auto: true,
            thresholdItems: thresholds.thresholdItems,
            hysteresisItems: thresholds.hysteresisItems,
            manuallyDisabled: !next,
          },
        })
        .catch(() => {
          // Melhor-esforço: US-047 §9 ("comportamento integralmente do cliente; não depende de
          // rede") — o estado local (`manualOverride`) já reflete a decisão do operador
          // imediatamente, a falha de rede só significa que a próxima sessão não vai lembrar dela.
        });
    }
  }, [active, identity, preferencesApi, thresholds.thresholdItems, thresholds.hysteresisItems]);

  return {
    active,
    manuallyDisabled: manualOverride === false,
    thresholdItems: thresholds.thresholdItems,
    hysteresisItems: thresholds.hysteresisItems,
    toggle,
  };
}

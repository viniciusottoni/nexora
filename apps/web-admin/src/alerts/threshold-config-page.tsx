import { useEffect, useId, useMemo, useState, type ReactNode } from 'react';
import { AlertBanner, Button, Card, Field, Input, centsToMoney, moneyToCents } from '@nexora/ui';
import type { TenantThresholds, UpdateTenantThresholdsRequest } from '@nexora/contracts';
import { ThresholdsApi } from './thresholds-api.js';
import './alerts.css';

/**
 * US-080 (Motor de alertas com limiares configuráveis) §10 — "tela de configuração de limiares
 * com explicação em linguagem de negócio de cada um". Auto-suficiente (busca os próprios dados
 * via `ThresholdsApi`, não recebe lista pronta em prop) — mesmo padrão de `AuditLogPage`: os 14
 * campos não são uma lista pequena pré-carregada no boot do `CloudAdmin`, são configuração de uma
 * única tela.
 *
 * `cashDivergenceAlert` é o único campo monetário (ADR-017: string decimal no contrato) — segue a
 * mesma máscara de centavos duplicada em `pricing/price-table-page.tsx` e `audit/audit-log-page.tsx`
 * (não há módulo de dinheiro compartilhado em `apps/web-admin/src`), mas usa `moneyToCents`/
 * `centsToMoney` de `@nexora/ui` para a conversão de/para a string do contrato.
 */

type NumericKey = Exclude<keyof TenantThresholds, 'cashDivergenceAlert'>;

type ThresholdGroup = 'order' | 'avg' | 'cash' | 'sync' | 'discipline';

interface NumericFieldConfig {
  readonly key: NumericKey;
  readonly group: ThresholdGroup;
  readonly label: string;
  readonly hint: string;
  readonly suffix?: string;
}

const GROUPS: readonly ThresholdGroup[] = ['order', 'avg', 'cash', 'sync', 'discipline'];

const GROUP_META: Record<ThresholdGroup, { readonly title: string; readonly subtitle: string }> = {
  order: {
    title: 'Tempo de pedido',
    subtitle: 'Quando um pedido, um item ou uma mesa passam a chamar atenção.',
  },
  avg: {
    title: 'Tempo médio de atendimento',
    subtitle: 'Quando o ritmo geral da operação foge do combinado.',
  },
  cash: {
    title: 'Caixa',
    subtitle: 'Quando o fechamento de caixa ou o custo da mercadoria vendida fogem do esperado.',
  },
  sync: {
    title: 'Sincronização',
    subtitle: 'Quando a loja fica tempo demais sem falar com a nuvem.',
  },
  discipline: {
    title: 'Cancelamento e desconto',
    subtitle: 'Quando o padrão de cancelamentos ou descontos foge do esperado.',
  },
};

const NUMERIC_FIELDS: readonly NumericFieldConfig[] = [
  {
    key: 'orderWarnMinutes',
    group: 'order',
    label: 'Aviso de pedido demorado',
    hint: 'A partir de quantos minutos um pedido entra em atenção — antes de virar crítico.',
    suffix: 'min',
  },
  {
    key: 'orderCriticalMinutes',
    group: 'order',
    label: 'Pedido atrasado (crítico)',
    hint: 'A partir de quantos minutos um pedido é considerado atrasado e gera alerta de severidade alta.',
    suffix: 'min',
  },
  {
    key: 'itemInWindowMinutes',
    group: 'order',
    label: 'Item parado na expedição',
    hint: 'Quanto tempo um item pronto pode esperar na janela de expedição antes de virar alerta.',
    suffix: 'min',
  },
  {
    key: 'tableIdleMinutes',
    group: 'order',
    label: 'Mesa ociosa',
    hint: 'Quanto tempo uma mesa aberta pode ficar sem novo pedido antes de ser considerada ociosa.',
    suffix: 'min',
  },
  {
    key: 'dineInPromiseMinutes',
    group: 'order',
    label: 'Promessa de tempo no salão',
    hint: 'Tempo prometido para o pedido chegar à mesa — referência para o alerta de atraso do salão.',
    suffix: 'min',
  },
  {
    key: 'deliveryPromiseMinutes',
    group: 'order',
    label: 'Promessa de tempo no delivery',
    hint: 'Tempo prometido para o pedido chegar ao cliente do delivery.',
    suffix: 'min',
  },
  {
    key: 'avgTimeAboveTargetPercent',
    group: 'avg',
    label: 'Tempo médio acima da meta',
    hint: 'Percentual acima da meta de tempo médio de atendimento que dispara alerta.',
    suffix: '%',
  },
  {
    key: 'cmvDivergencePercent',
    group: 'cash',
    label: 'Divergência de CMV',
    hint: 'Percentual de divergência do custo da mercadoria vendida que dispara alerta.',
    suffix: '%',
  },
  {
    key: 'syncDelayMinutes',
    group: 'sync',
    label: 'Atraso de sincronização',
    hint: 'Quanto tempo sem sincronizar com a nuvem antes de avisar o gestor.',
    suffix: 'min',
  },
  {
    key: 'cancellationCountThreshold',
    group: 'discipline',
    label: 'Cancelamentos na janela',
    hint: 'Quantidade de cancelamentos na janela abaixo que dispara alerta de padrão de cancelamento.',
    suffix: 'cancelamentos',
  },
  {
    key: 'cancellationWindowMinutes',
    group: 'discipline',
    label: 'Janela de cancelamento',
    hint: 'Período, em minutos, usado para contar os cancelamentos acima.',
    suffix: 'min',
  },
  {
    key: 'discountAboveThresholdPercent',
    group: 'discipline',
    label: 'Desconto acima do padrão',
    hint: 'Percentual de desconto acima do qual o alerta é disparado.',
    suffix: '%',
  },
  {
    key: 'discountWindowMinutes',
    group: 'discipline',
    label: 'Janela de desconto',
    hint: 'Período, em minutos, usado para avaliar descontos acima do padrão.',
    suffix: 'min',
  },
];

function toFormValues(thresholds: TenantThresholds): Record<NumericKey, string> {
  const result = {} as Record<NumericKey, string>;
  for (const field of NUMERIC_FIELDS) result[field.key] = String(thresholds[field.key]);
  return result;
}

export interface ThresholdConfigPageProps {
  /** Injetável para teste — padrão `new ThresholdsApi()`. */
  readonly thresholdsApi?: ThresholdsApi;
}

export function ThresholdConfigPage({
  thresholdsApi = new ThresholdsApi(),
}: Readonly<ThresholdConfigPageProps>) {
  const [baseline, setBaseline] = useState<TenantThresholds>();
  const [values, setValues] = useState<Record<NumericKey, string>>();
  const [cashCents, setCashCents] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(undefined);
    thresholdsApi
      .get()
      .then((thresholds) => {
        if (!active) return;
        setBaseline(thresholds);
        setValues(toFormValues(thresholds));
        setCashCents(moneyToCents(thresholds.cashDivergenceAlert));
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [thresholdsApi]);

  const dirtyKeys = useMemo(() => {
    if (!baseline || !values) return [];
    return NUMERIC_FIELDS.filter((field) => values[field.key] !== String(baseline[field.key])).map(
      (field) => field.key,
    );
  }, [baseline, values]);

  const cashDirty = baseline ? cashCents !== moneyToCents(baseline.cashDivergenceAlert) : false;
  const dirtyCount = dirtyKeys.length + (cashDirty ? 1 : 0);

  async function save() {
    if (!baseline || !values || dirtyCount === 0) return;
    setBusy(true);
    setError(undefined);
    try {
      const numericPatch = Object.fromEntries(
        dirtyKeys.map((key) => [key, Number(values[key])]),
      ) as Partial<Record<NumericKey, number>>;
      const patch: UpdateTenantThresholdsRequest = {
        ...numericPatch,
        ...(cashDirty ? { cashDivergenceAlert: centsToMoney(cashCents) } : {}),
      };
      const updated = await thresholdsApi.update(patch);
      setBaseline(updated);
      setValues(toFormValues(updated));
      setCashCents(moneyToCents(updated.cashDivergenceAlert));
      setNotice('Limiares de alerta atualizados.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="thresholds-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Alertas</p>
          <h1 className="db-page__title" id="thresholds-title">
            Limiares de alerta
          </h1>
          <p className="db-page__lead">
            Defina o que conta como atraso, divergência ou padrão fora do esperado para o seu
            estabelecimento — o motor de alertas (US-080) avalia cada pedido, mesa e fechamento de
            caixa contra esses números. Valores em branco não existem: todo campo já vem com um
            padrão sensato.
          </p>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Limiares salvos">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? (
        <AlertBanner tone="danger" title="Falha ao carregar limiares">
          {error}
        </AlertBanner>
      ) : null}

      {loading ? (
        <p className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando limiares…
        </p>
      ) : values ? (
        <div className="db-stack">
          {GROUPS.map((group) => (
            <ThresholdGroupCard
              key={group}
              title={GROUP_META[group].title}
              subtitle={GROUP_META[group].subtitle}
              fields={NUMERIC_FIELDS.filter((field) => field.group === group)}
              values={values}
              onChange={(key, value) =>
                setValues((current) => (current ? { ...current, [key]: value } : current))
              }
              extra={
                group === 'cash' ? (
                  <CashDivergenceField cents={cashCents} onChange={setCashCents} />
                ) : null
              }
            />
          ))}

          <div className="db-editor__footer">
            <p className="db-hint">
              {dirtyCount > 0
                ? `${dirtyCount} limiar(es) alterado(s)`
                : 'Nenhuma alteração pendente'}
            </p>
            <Button
              type="button"
              busy={busy}
              disabled={dirtyCount === 0}
              onClick={() => void save()}
            >
              Salvar limiares
            </Button>
          </div>
        </div>
      ) : null}
    </main>
  );
}

interface ThresholdGroupCardProps {
  readonly title: string;
  readonly subtitle: string;
  readonly fields: readonly NumericFieldConfig[];
  readonly values: Record<NumericKey, string>;
  readonly onChange: (key: NumericKey, value: string) => void;
  readonly extra?: ReactNode;
}

function ThresholdGroupCard({
  title,
  subtitle,
  fields,
  values,
  onChange,
  extra,
}: Readonly<ThresholdGroupCardProps>) {
  return (
    <Card title={title} subtitle={subtitle} className="db-form-card">
      {extra}
      {chunkPairs(fields).map((pair, index) => (
        <div className="db-form-row" key={pair.map((field) => field.key).join('-') || index}>
          {pair.map((field) => (
            <ThresholdField
              key={field.key}
              field={field}
              value={values[field.key]}
              onChange={(value) => onChange(field.key, value)}
            />
          ))}
        </div>
      ))}
    </Card>
  );
}

interface ThresholdFieldProps {
  readonly field: NumericFieldConfig;
  readonly value: string;
  readonly onChange: (value: string) => void;
}

function ThresholdField({ field, value, onChange }: Readonly<ThresholdFieldProps>) {
  const id = useId();
  return (
    <Field label={field.label} htmlFor={id} hint={field.hint}>
      <Input
        id={id}
        type="number"
        min={0}
        numeric
        suffix={field.suffix}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </Field>
  );
}

interface CashDivergenceFieldProps {
  readonly cents: number;
  readonly onChange: (cents: number) => void;
}

function CashDivergenceField({ cents, onChange }: Readonly<CashDivergenceFieldProps>) {
  const id = useId();
  return (
    <Field
      label="Divergência de caixa"
      htmlFor={id}
      hint="A partir de qual valor de diferença no fechamento de caixa o gestor é avisado."
    >
      <Input
        id={id}
        numeric
        inputMode="numeric"
        prefix="R$"
        value={centsToDisplay(cents)}
        onChange={(event) => onChange(digitsToCents(event.target.value))}
      />
    </Field>
  );
}

/** Agrupa os campos de um card em pares para caber em `db-form-row` (grid de 2 colunas). */
function chunkPairs<T>(items: readonly T[]): T[][] {
  const pairs: T[][] = [];
  for (let index = 0; index < items.length; index += 2) {
    pairs.push(items.slice(index, index + 2));
  }
  return pairs;
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'Não foi possível carregar os limiares de alerta.';
}

// --- máscara de moeda (duplicada de propósito — mesmo comentário de price-table-page.tsx e
// audit-log-page.tsx: não há módulo de dinheiro compartilhado em apps/web-admin/src) ---

function centsToDisplay(cents: number): string {
  const intPart = String(Math.floor(cents / 100)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const decPart = String(cents % 100).padStart(2, '0');
  return `${intPart},${decPart}`;
}

function digitsToCents(rawInput: string): number {
  const digits = rawInput.replace(/\D/g, '');
  return digits === '' ? 0 : Number(digits);
}

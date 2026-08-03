import { useId, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Card, Field, Input, Select } from '@nexora/ui';
import type {
  BulkAdjustPricesRequest,
  BulkAdjustPricesResponse,
  PricingChannel,
  SetVariantChannelPriceRequest,
  VariantChannelPriceRow,
  VariantPriceTableResponse,
} from '@nexora/contracts';
import './pricing.css';

/**
 * US-014 (Preço por canal de venda) — tabela de preço por canal de uma variante, editada em linha
 * (§10: "Tabela de preços por canal editável em linha, sem navegação"), e reajuste em massa por
 * categoria com pré-visualização do antes/depois antes de confirmar.
 *
 * NOTA DE INTEGRAÇÃO: este worktree ainda não tinha `apps/web-admin/src/catalog/*` (US-010/US-011
 * não tinham frontend aqui) nem uma seção de catálogo em `app.tsx` (arquivo proibido de editar) —
 * este componente é autocontido e não está plugado em nenhuma navegação ainda. Ver relatório da
 * tarefa para o que falta plugar (rota/seção do painel, API real por trás das props de callback).
 */

const CHANNEL_ORDER: readonly PricingChannel[] = ['DineIn', 'Delivery', 'Takeout', 'Marketplace'];

/** Rótulo em português de cada canal — função (não `Record`) para não sofrer com `noUncheckedIndexedAccess` do tsconfig base. */
function channelLabel(channel: PricingChannel): string {
  switch (channel) {
    case 'DineIn':
      return 'Salão';
    case 'Delivery':
      return 'Delivery';
    case 'Takeout':
      return 'Balcão';
    case 'Marketplace':
      return 'Marketplace';
    default:
      // Defensivo: também evita que o TS acuse "falta de return" quando `PricingChannel` cai
      // para `any` por causa do import de @nexora/contracts ainda não estar exportado (ver nota
      // de integração no topo do arquivo) — nesse cenário o switch deixa de ser reconhecido como
      // exaustivo pelo compilador.
      return channel;
  }
}

/** Preço vigente de uma variação da categoria no canal selecionado — usado para montar a pré-visualização do reajuste em massa. */
export interface CategoryPriceSnapshotItem {
  readonly variantId: string;
  readonly variantName: string;
  readonly currentAmount: string;
}

export interface PriceTablePageCategoryOption {
  readonly id: string;
  readonly name: string;
}

export interface PriceTablePageProps {
  readonly variantId: string;
  readonly variantName: string;
  readonly channels: readonly VariantChannelPriceRow[];
  readonly categories: readonly PriceTablePageCategoryOption[];
  readonly onSaveChannelPrices: (
    variantId: string,
    input: SetVariantChannelPriceRequest,
  ) => Promise<VariantPriceTableResponse>;
  readonly onLoadCategoryPriceSnapshot: (
    categoryId: string,
    channel: PricingChannel,
  ) => Promise<readonly CategoryPriceSnapshotItem[]>;
  readonly onBulkAdjust: (input: BulkAdjustPricesRequest) => Promise<BulkAdjustPricesResponse>;
}

export function PriceTablePage({
  variantId,
  variantName,
  channels,
  categories,
  onSaveChannelPrices,
  onLoadCategoryPriceSnapshot,
  onBulkAdjust,
}: Readonly<PriceTablePageProps>) {
  return (
    <div className="db-stack">
      <ChannelPriceTable
        variantId={variantId}
        variantName={variantName}
        channels={channels}
        onSave={onSaveChannelPrices}
      />

      <BulkAdjustSection
        categories={categories}
        onLoadSnapshot={onLoadCategoryPriceSnapshot}
        onConfirm={onBulkAdjust}
      />
    </div>
  );
}

interface ChannelPriceTableProps {
  readonly variantId: string;
  readonly variantName: string;
  readonly channels: readonly VariantChannelPriceRow[];
  readonly onSave: (
    variantId: string,
    input: SetVariantChannelPriceRequest,
  ) => Promise<VariantPriceTableResponse>;
}

function ChannelPriceTable({
  variantId,
  variantName,
  channels,
  onSave,
}: Readonly<ChannelPriceTableProps>) {
  const [baselineChannels, setBaselineChannels] = useState(channels);
  const byChannel = useMemo(() => {
    const map = new Map<PricingChannel, VariantChannelPriceRow>();
    for (const row of baselineChannels) map.set(row.channel, row);
    return map;
  }, [baselineChannels]);

  const [centsByChannel, setCentsByChannel] = useState<Record<PricingChannel, number>>(() =>
    initialCents(byChannel),
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();

  const dineInCents = centsByChannel.DineIn ?? 0;
  const deliveryCents = centsByChannel.Delivery ?? 0;
  const deliveryCheaperWarning =
    deliveryCents > 0 && dineInCents > 0 && deliveryCents < dineInCents;

  const dirtyChannels = CHANNEL_ORDER.filter((channel) => {
    const row = byChannel.get(channel);
    const original = row?.amount ? decimalStringToCents(row.amount) : 0;
    return original !== (centsByChannel[channel] ?? 0);
  });

  async function save() {
    if (dirtyChannels.length === 0) return;
    setError(undefined);
    setBusy(true);
    try {
      const response = await onSave(variantId, {
        prices: dirtyChannels.map((channel) => ({
          channel,
          amount: centsToDecimalString(centsByChannel[channel] ?? 0),
        })),
      });
      const savedByChannel = new Map(
        response.channels.map((row: VariantChannelPriceRow) => [row.channel, row] as const),
      );
      setBaselineChannels(response.channels);
      setCentsByChannel(initialCents(savedByChannel));
      setNotice(
        'Preços atualizados. O preço anterior de cada canal alterado continua historizado.',
      );
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card
      title="Tabela de preços por canal"
      subtitle={`${variantName} — a taxa do delivery ou do marketplace não precisa comer a margem do salão.`}
      className="db-form-card"
    >
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
      {notice ? (
        <AlertBanner tone="success" title="Preços atualizados">
          {notice}
        </AlertBanner>
      ) : null}

      {deliveryCheaperWarning ? (
        <AlertBanner tone="warning" title="Delivery mais barato que o salão">
          Confira se não é engano — geralmente o delivery custa mais por causa da embalagem, do
          entregador e da comissão do marketplace (US-014 §10).
        </AlertBanner>
      ) : null}

      <div className="db-table-wrap">
        <table className="db-table pricing-channel-table">
          <thead>
            <tr>
              <th scope="col">Canal</th>
              <th scope="col">Preço</th>
              <th scope="col">Origem</th>
            </tr>
          </thead>
          <tbody>
            {CHANNEL_ORDER.map((channel) => {
              const row = byChannel.get(channel);
              return (
                <ChannelPriceRow
                  key={channel}
                  channel={channel}
                  cents={centsByChannel[channel] ?? 0}
                  isInherited={row?.isInherited ?? false}
                  onChange={(cents) =>
                    setCentsByChannel((current) => ({ ...current, [channel]: cents }))
                  }
                />
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="db-editor__footer">
        <p className="db-hint">
          {dirtyChannels.length > 0
            ? `${dirtyChannels.length} canal(is) alterado(s)`
            : 'Nenhuma alteração pendente'}
        </p>
        <Button
          type="button"
          busy={busy}
          disabled={dirtyChannels.length === 0}
          onClick={() => void save()}
        >
          Salvar preços
        </Button>
      </div>
    </Card>
  );
}

interface ChannelPriceRowProps {
  readonly channel: PricingChannel;
  readonly cents: number;
  readonly isInherited: boolean;
  readonly onChange: (cents: number) => void;
}

function ChannelPriceRow({
  channel,
  cents,
  isInherited,
  onChange,
}: Readonly<ChannelPriceRowProps>) {
  return (
    <tr className="pricing-channel-row">
      <td>{channelLabel(channel)}</td>
      <td>
        <Input
          aria-label={`Preço do canal ${channelLabel(channel)}`}
          numeric
          inputMode="numeric"
          prefix="R$"
          value={centsToDisplay(cents)}
          onChange={(event) => onChange(digitsToCents(event.target.value))}
        />
      </td>
      <td>
        {isInherited ? (
          <Badge tone="neutral">Herdado do salão</Badge>
        ) : (
          <Badge tone="brand">Próprio</Badge>
        )}
      </td>
    </tr>
  );
}

interface BulkAdjustSectionProps {
  readonly categories: readonly PriceTablePageCategoryOption[];
  readonly onLoadSnapshot: (
    categoryId: string,
    channel: PricingChannel,
  ) => Promise<readonly CategoryPriceSnapshotItem[]>;
  readonly onConfirm: (input: BulkAdjustPricesRequest) => Promise<BulkAdjustPricesResponse>;
}

function BulkAdjustSection({
  categories,
  onLoadSnapshot,
  onConfirm,
}: Readonly<BulkAdjustSectionProps>) {
  const categoryFieldId = useId();
  const channelFieldId = useId();
  const percentFieldId = useId();

  const [categoryId, setCategoryId] = useState(categories[0]?.id ?? '');
  const [channel, setChannel] = useState<PricingChannel>('Delivery');
  const [percent, setPercent] = useState(0);
  const [preview, setPreview] = useState<readonly CategoryPriceSnapshotItem[]>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [result, setResult] = useState<BulkAdjustPricesResponse>();

  async function loadPreview() {
    if (!categoryId) return;
    setError(undefined);
    setResult(undefined);
    setBusy(true);
    try {
      setPreview(await onLoadSnapshot(categoryId, channel));
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function confirm() {
    if (!categoryId) return;
    setError(undefined);
    setBusy(true);
    try {
      const response = await onConfirm({ categoryId, channel, percent });
      setResult(response);
      setPreview(undefined);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card
      title="Reajuste em massa por categoria"
      subtitle="Aplica um percentual em um canal para todas as variações ativas da categoria"
      className="db-form-card"
    >
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
      {result ? (
        <AlertBanner tone="success" title="Reajuste aplicado">
          {result.updated} preço(s) atualizado(s), vigentes a partir de{' '}
          {new Date(result.effectiveFrom).toLocaleString('pt-BR')}.
        </AlertBanner>
      ) : null}

      <div className="pricing-bulk-fields">
        <Field label="Categoria" htmlFor={categoryFieldId}>
          <Select
            id={categoryFieldId}
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setPreview(undefined);
            }}
            options={categories.map((category) => ({ value: category.id, label: category.name }))}
          />
        </Field>
        <Field label="Canal" htmlFor={channelFieldId}>
          <Select
            id={channelFieldId}
            value={channel}
            onChange={(event) => {
              setChannel(event.target.value as PricingChannel);
              setPreview(undefined);
            }}
            options={CHANNEL_ORDER.map((value) => ({ value, label: channelLabel(value) }))}
          />
        </Field>
        <Field label="Percentual" htmlFor={percentFieldId} hint="Ex.: 8 para +8%, -5 para -5%">
          <Input
            id={percentFieldId}
            type="number"
            step="0.01"
            value={percent}
            onChange={(event) => {
              setPercent(Number(event.target.value));
              setPreview(undefined);
            }}
          />
        </Field>
      </div>

      <div className="db-page__actions">
        <Button
          type="button"
          variant="ghost"
          busy={busy}
          disabled={!categoryId}
          onClick={() => void loadPreview()}
        >
          Pré-visualizar
        </Button>
        <Button
          type="button"
          busy={busy}
          disabled={!preview || preview.length === 0}
          onClick={() => void confirm()}
        >
          Confirmar reajuste
        </Button>
      </div>

      {preview ? (
        preview.length === 0 ? (
          <p className="db-hint">Nenhuma variação ativa nesta categoria e canal.</p>
        ) : (
          <div className="db-table-wrap">
            <table className="db-table db-table--compact pricing-preview-table">
              <thead>
                <tr>
                  <th scope="col">Variação</th>
                  <th scope="col">Antes</th>
                  <th scope="col">Depois</th>
                </tr>
              </thead>
              <tbody>
                {preview.map((item) => (
                  <tr key={item.variantId}>
                    <td>{item.variantName}</td>
                    <td>{centsToDisplay(decimalStringToCents(item.currentAmount))}</td>
                    <td>
                      {centsToDisplay(
                        applyPercentCents(decimalStringToCents(item.currentAmount), percent),
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      ) : null}
    </Card>
  );
}

function initialCents(
  byChannel: Map<PricingChannel, VariantChannelPriceRow>,
): Record<PricingChannel, number> {
  const result = {} as Record<PricingChannel, number>;
  for (const channel of CHANNEL_ORDER) {
    const row = byChannel.get(channel);
    result[channel] = row?.amount ? decimalStringToCents(row.amount) : 0;
  }
  return result;
}

/**
 * Prévia client-side do reajuste percentual (US-014 §10, "pré-visualização do antes e depois
 * antes de confirmar"). Aritmética inteira sobre centavos, arredondamento half-up — mesmo espírito
 * de `BulkPriceAdjustmentCalculator` no backend (`Math.round` do JS já arredonda .5 para cima em
 * valores positivos, equivalente a `MidpointRounding.AwayFromZero` do C#). O valor final e
 * autoritativo é sempre o devolvido pela API depois de confirmado — esta função é só para a
 * pré-visualização.
 */
function applyPercentCents(cents: number, percent: number): number {
  const factor = 1 + percent / 100;
  return Math.max(0, Math.round(cents * factor));
}

// --- máscara de moeda (duplicada de propósito — apps/web-admin/src/catalog/money.ts não existia
// neste worktree e, mesmo se existisse, é pasta proibida de tocar nesta tarefa) ---

function decimalStringToCents(value: string): number {
  const negative = value.trim().startsWith('-');
  const [rawInt, rawDec = ''] = value.trim().replace('-', '').split('.');
  const intPart = rawInt === '' ? 0 : Number(rawInt);
  const decPart = (rawDec + '00').slice(0, 2);
  const cents = intPart * 100 + Number(decPart || '0');
  return negative ? -cents : cents;
}

function centsToDecimalString(cents: number): string {
  const negative = cents < 0;
  const absolute = Math.abs(Math.trunc(cents));
  const intPart = Math.floor(absolute / 100);
  const decPart = String(absolute % 100).padStart(2, '0');
  return `${negative ? '-' : ''}${intPart}.${decPart}`;
}

function centsToDisplay(cents: number): string {
  const negative = cents < 0;
  const absolute = Math.abs(Math.trunc(cents));
  const intPart = String(Math.floor(absolute / 100)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const decPart = String(absolute % 100).padStart(2, '0');
  return `${negative ? '-' : ''}${intPart},${decPart}`;
}

function digitsToCents(rawInput: string): number {
  const digits = rawInput.replace(/\D/g, '');
  return digits === '' ? 0 : Number(digits);
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

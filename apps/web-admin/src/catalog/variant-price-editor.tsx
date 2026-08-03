import { useEffect, useId, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Field, Input } from '@nexora/ui';
import type {
  CreateVariantRequest,
  PriceDto,
  SetVariantPriceRequest,
  UpdateVariantRequest,
  VariantDto,
} from '@nexora/contracts';
import {
  centsToDecimalString,
  centsToDisplay,
  decimalStringToCents,
  digitsToCents,
} from './money.js';

export interface VariantPriceEditorProps {
  readonly variants: readonly VariantDto[];
  readonly loading: boolean;
  readonly onCreate: (input: CreateVariantRequest) => Promise<VariantDto>;
  readonly onUpdate: (id: string, input: UpdateVariantRequest) => Promise<VariantDto>;
  readonly onSetPrice: (id: string, input: SetVariantPriceRequest) => Promise<PriceDto>;
  readonly onActivate: (id: string) => Promise<VariantDto>;
  readonly onDeactivate: (id: string) => Promise<VariantDto>;
  readonly onMarkDefault: (id: string) => Promise<VariantDto>;
}

/**
 * Variações (tamanhos) e preço do produto selecionado, editadas em linha na mesma tela do
 * produto — não em uma tela separada (US-011 §10). Cada linha é a `product_variant`, a unidade
 * real de venda/preço (US-011 §2): nome, `sizeCode`/SKU editáveis e o preço base em um único
 * canal (`DineIn`, US-011 §"Esta US só precisa entregar UM preço 'base'" — a tabela de preço por
 * canal completa é US-014). Não existe exclusão física de variação (US-011 §3.1, cenário
 * "Exclusão com histórico") — só desativar/reativar.
 */
export function VariantPriceEditor({
  variants,
  loading,
  onCreate,
  onUpdate,
  onSetPrice,
  onActivate,
  onDeactivate,
  onMarkDefault,
}: Readonly<VariantPriceEditorProps>) {
  const [busyId, setBusyId] = useState<string>();
  const [error, setError] = useState<string>();
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState('');
  const [newSizeCode, setNewSizeCode] = useState('');
  const [newPriceCents, setNewPriceCents] = useState(0);

  const newNameFieldId = useId();
  const newSizeCodeFieldId = useId();
  const newPriceFieldId = useId();

  const duplicateSizeCodes = useMemo(() => {
    const seen = new Map<string, number>();
    for (const variant of variants) {
      const key = variant.sizeCode?.trim().toUpperCase();
      if (!key) continue;
      seen.set(key, (seen.get(key) ?? 0) + 1);
    }
    return new Set([...seen.entries()].filter(([, count]) => count > 1).map(([key]) => key));
  }, [variants]);

  async function createVariant() {
    if (!newName.trim()) return;
    setError(undefined);
    setBusyId('__new__');
    try {
      await onCreate({
        name: newName.trim(),
        sizeCode: newSizeCode.trim() || undefined,
        isDefault: variants.length === 0,
        basePrice: centsToDecimalString(newPriceCents),
      });
      setAdding(false);
      setNewName('');
      setNewSizeCode('');
      setNewPriceCents(0);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusyId(undefined);
    }
  }

  return (
    <div className="catalog-variant-editor">
      <div className="catalog-variant-editor__header">
        <h3>Variações e preço</h3>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => setAdding(true)}
          disabled={adding}
        >
          Nova variação
        </Button>
      </div>

      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {loading ? (
        <p className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando variações…
        </p>
      ) : variants.length === 0 && !adding ? (
        <p className="db-hint">Nenhuma variação cadastrada ainda.</p>
      ) : (
        <div className="db-table-wrap">
          <table className="db-table db-table--compact">
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">Tamanho</th>
                <th scope="col">Preço</th>
                <th scope="col">Padrão</th>
                <th scope="col">Situação</th>
                <th scope="col" />
              </tr>
            </thead>
            <tbody>
              {variants.map((variant) => (
                <VariantRow
                  key={variant.id}
                  variant={variant}
                  busy={busyId === variant.id}
                  duplicateSizeCode={Boolean(
                    variant.sizeCode &&
                    duplicateSizeCodes.has(variant.sizeCode.trim().toUpperCase()),
                  )}
                  onSave={async (input, priceCents) => {
                    setError(undefined);
                    setBusyId(variant.id);
                    try {
                      await onUpdate(variant.id, input);
                      const nextPrice = centsToDecimalString(priceCents);
                      if (nextPrice !== variant.currentPrice) {
                        await onSetPrice(variant.id, { amount: nextPrice });
                      }
                    } catch (reason) {
                      setError(toMessage(reason));
                    } finally {
                      setBusyId(undefined);
                    }
                  }}
                  onToggleActive={async () => {
                    setError(undefined);
                    setBusyId(variant.id);
                    try {
                      if (variant.isActive) {
                        await onDeactivate(variant.id);
                      } else {
                        await onActivate(variant.id);
                      }
                    } catch (reason) {
                      setError(toMessage(reason));
                    } finally {
                      setBusyId(undefined);
                    }
                  }}
                  onMarkDefault={async () => {
                    setError(undefined);
                    setBusyId(variant.id);
                    try {
                      await onMarkDefault(variant.id);
                    } catch (reason) {
                      setError(toMessage(reason));
                    } finally {
                      setBusyId(undefined);
                    }
                  }}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {adding ? (
        <div className="catalog-variant-new-row">
          <Field label="Nome da variação" htmlFor={newNameFieldId}>
            <Input
              id={newNameFieldId}
              value={newName}
              onChange={(event) => setNewName(event.target.value)}
            />
          </Field>
          <Field label="Tamanho" htmlFor={newSizeCodeFieldId} hint="Ex.: P, M, G">
            <Input
              id={newSizeCodeFieldId}
              value={newSizeCode}
              onChange={(event) => setNewSizeCode(event.target.value)}
            />
          </Field>
          <Field label="Preço" htmlFor={newPriceFieldId}>
            <Input
              id={newPriceFieldId}
              numeric
              inputMode="numeric"
              prefix="R$"
              value={centsToDisplay(newPriceCents)}
              onChange={(event) => setNewPriceCents(digitsToCents(event.target.value))}
            />
          </Field>
          <div className="catalog-variant-new-row__actions">
            <Button type="button" variant="ghost" size="sm" onClick={() => setAdding(false)}>
              Cancelar
            </Button>
            <Button
              type="button"
              size="sm"
              busy={busyId === '__new__'}
              onClick={() => void createVariant()}
            >
              Adicionar variação
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

interface VariantRowProps {
  readonly variant: VariantDto;
  readonly busy: boolean;
  readonly duplicateSizeCode: boolean;
  readonly onSave: (input: UpdateVariantRequest, priceCents: number) => Promise<void>;
  readonly onToggleActive: () => Promise<void>;
  readonly onMarkDefault: () => Promise<void>;
}

function VariantRow({
  variant,
  busy,
  duplicateSizeCode,
  onSave,
  onToggleActive,
  onMarkDefault,
}: Readonly<VariantRowProps>) {
  const [name, setName] = useState(variant.name);
  const [sizeCode, setSizeCode] = useState(variant.sizeCode ?? '');
  const [priceCents, setPriceCents] = useState(() =>
    variant.currentPrice ? decimalStringToCents(variant.currentPrice) : 0,
  );

  useEffect(() => {
    setName(variant.name);
    setSizeCode(variant.sizeCode ?? '');
    setPriceCents(variant.currentPrice ? decimalStringToCents(variant.currentPrice) : 0);
  }, [variant.id]);

  const dirty =
    name !== variant.name ||
    sizeCode !== (variant.sizeCode ?? '') ||
    (variant.currentPrice ? decimalStringToCents(variant.currentPrice) : 0) !== priceCents;

  return (
    <tr className="catalog-variant-row">
      <td>
        <Input
          aria-label={`Nome da variação ${variant.name}`}
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </td>
      <td>
        <Input
          aria-label={`Tamanho da variação ${variant.name}`}
          value={sizeCode}
          onChange={(event) => setSizeCode(event.target.value)}
        />
        {duplicateSizeCode ? (
          <Badge tone="warning" size="sm">
            Tamanho repetido
          </Badge>
        ) : null}
      </td>
      <td>
        <Input
          aria-label={`Preço da variação ${variant.name}`}
          numeric
          inputMode="numeric"
          prefix="R$"
          value={centsToDisplay(priceCents)}
          onChange={(event) => setPriceCents(digitsToCents(event.target.value))}
        />
      </td>
      <td>
        {variant.isDefault ? (
          <Badge tone="brand">Padrão</Badge>
        ) : (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            busy={busy}
            onClick={() => void onMarkDefault()}
          >
            Tornar padrão
          </Button>
        )}
      </td>
      <td>
        {variant.isActive ? (
          <Badge tone="success">Ativa</Badge>
        ) : (
          <Badge tone="neutral">Inativa</Badge>
        )}
      </td>
      <td className="catalog-variant-row__actions">
        <Button
          type="button"
          size="sm"
          disabled={!dirty}
          busy={busy}
          onClick={() =>
            void onSave(
              {
                name: name.trim(),
                sizeCode: sizeCode.trim() || undefined,
                sku: variant.sku ?? undefined,
              },
              priceCents,
            )
          }
        >
          Salvar
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          busy={busy}
          onClick={() => void onToggleActive()}
        >
          {variant.isActive ? 'Desativar' : 'Reativar'}
        </Button>
      </td>
    </tr>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

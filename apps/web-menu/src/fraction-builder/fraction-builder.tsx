import { useEffect, useMemo, useRef, useState } from 'react';
import type {
  PreviewFractionPricingRequest,
  PreviewFractionPricingResponse,
} from '@nexora/contracts';
import { Badge, Card, SegmentedControl } from '@nexora/ui';
import './fraction-builder.css';

/** Um sabor (variante) candidato a compor um meio a meio — já resolvido pelo cardápio (produto + variante + disponibilidade). */
export interface FractionFlavorOption {
  readonly variantId: string;
  /** Nome do produto/sabor (ex.: "Mussarela") — usado na descrição composta e no rótulo do botão. */
  readonly productName: string;
  readonly sizeCode: string;
  /** `Product.FractionGroup` — só sabores do mesmo grupo podem ser combinados (US-013 §4, "Grupos de fração distintos"). */
  readonly fractionGroup: string;
  readonly available: boolean;
  /** Motivo da indisponibilidade (US-013 §10: "sabores indisponíveis exibidos como bloqueados, com o motivo"). */
  readonly unavailableReason?: string;
}

/** Porta mínima consumida pelo componente — `FractionPricingApi` (fraction-pricing-api.ts) satisfaz esta interface; testes injetam um duplo. */
export interface FractionPricingApiLike {
  preview(input: PreviewFractionPricingRequest): Promise<PreviewFractionPricingResponse>;
}

export interface FractionBuilderProps {
  /** Todos os sabores candidatos, de todos os tamanhos — o componente agrupa por `sizeCode` na Etapa 1. */
  readonly flavors: readonly FractionFlavorOption[];
  /** Limite de sabores por item (`Product.MaxFractions`, US-013 §3.1) — normalmente 2, mas o modelo permite mais. */
  readonly maxFractions: number;
  readonly api: FractionPricingApiLike;
  readonly channel?: PreviewFractionPricingRequest['channel'];
  /** Formatação de moeda (padrão: `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`). */
  readonly formatPrice?: (amount: number) => string;
  readonly onPriced?: (pricing: PreviewFractionPricingResponse) => void;
}

const priceFormatter = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const defaultFormatPrice = (amount: number) => priceFormatter.format(amount);

/**
 * Divide 1,0 em <paramref>count</paramref> pesos iguais, truncados em 4 casas (`fraction_weight`
 * é `NUMERIC(5,4)`) — a sobra da divisão vai para a PRIMEIRA parcela (ADR-017: "toda divisão
 * concilia, a sobra vai para a primeira parcela"), garantindo soma exatamente 1,0 mesmo quando
 * `1 / count` não é uma dízima finita (ex.: 3 sabores -> 0,3334/0,3333/0,3333).
 */
export function splitEqualWeights(count: number): number[] {
  if (count <= 0) return [];
  const base = Math.floor((1 / count) * 10000) / 10000;
  const weights = new Array<number>(count).fill(base);
  const remainder = Number((1 - base * count).toFixed(4));
  weights[0] = Number((weights[0]! + remainder).toFixed(4));
  return weights;
}

/**
 * Montagem de item meio a meio (US-013) — duas etapas visuais (§10 do documento): escolher o
 * tamanho, depois escolher os sabores compatíveis. O preço é recalculado (via
 * `POST /v1/catalog/fraction-pricing/preview`) a cada escolha, sempre visível antes de qualquer
 * confirmação. Componente isolado, sem integração a roteamento/tela maior — `web-menu` ainda não
 * tem app de cardápio do cliente construído (ver relatório da tarefa); pronto para ser plugado
 * quando existir.
 */
export function FractionBuilder({
  flavors,
  maxFractions,
  api,
  channel,
  formatPrice = defaultFormatPrice,
  onPriced,
}: Readonly<FractionBuilderProps>) {
  const sizes = useMemo(() => Array.from(new Set(flavors.map((f) => f.sizeCode))), [flavors]);
  const [selectedSize, setSelectedSize] = useState<string | undefined>(sizes[0]);
  const [selectedVariantIds, setSelectedVariantIds] = useState<string[]>([]);
  const [pricing, setPricing] = useState<PreviewFractionPricingResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Destaque breve (nx-anim-flash) quando o preço recalculado chega com um valor DIFERENTE do
  // anterior — nunca no primeiro cálculo, e nunca quando o preview repete o mesmo valor.
  const previousUnitPriceRef = useRef<number | null>(null);
  const [priceFlashKey, setPriceFlashKey] = useState(0);

  useEffect(() => {
    if (selectedSize !== undefined && sizes.includes(selectedSize)) return;

    setSelectedSize(sizes[0]);
    setSelectedVariantIds([]);
    setPricing(null);
    setError(null);
  }, [selectedSize, sizes]);

  const flavorsForSize = useMemo(
    () => flavors.filter((f) => f.sizeCode === selectedSize),
    [flavors, selectedSize],
  );

  const selectedFlavors = useMemo(
    () =>
      selectedVariantIds
        .map((id) => flavorsForSize.find((f) => f.variantId === id))
        .filter((f): f is FractionFlavorOption => Boolean(f)),
    [selectedVariantIds, flavorsForSize],
  );

  // Uma vez que o primeiro sabor é escolhido, só sabores do MESMO fraction_group aparecem
  // habilitados (US-013 §4, "Grupos de fração distintos") — checado no cliente antes mesmo de
  // chamar o preview, para dar feedback imediato (o preview ainda valida de novo no servidor).
  const requiredGroup = selectedFlavors[0]?.fractionGroup;

  function blockReason(flavor: FractionFlavorOption): string | undefined {
    if (!flavor.available) return flavor.unavailableReason ?? 'Indisponível no momento';
    if (requiredGroup && flavor.fractionGroup !== requiredGroup)
      return 'Não combina com o sabor já escolhido';
    if (
      !selectedVariantIds.includes(flavor.variantId) &&
      selectedVariantIds.length >= maxFractions
    ) {
      return `Limite de ${maxFractions} sabores atingido`;
    }
    return undefined;
  }

  function toggleFlavor(flavor: FractionFlavorOption) {
    setSelectedVariantIds((current) => {
      if (current.includes(flavor.variantId)) {
        return current.filter((id) => id !== flavor.variantId);
      }
      if (blockReason(flavor)) return current;
      return [...current, flavor.variantId];
    });
  }

  function selectSize(size: string) {
    setSelectedSize(size);
    setSelectedVariantIds([]);
    setPricing(null);
    setError(null);
  }

  useEffect(() => {
    if (selectedVariantIds.length < 2) {
      setPricing(null);
      setError(null);
      return;
    }

    let cancelled = false;
    const weights = splitEqualWeights(selectedVariantIds.length);

    setLoading(true);
    setError(null);

    const request: PreviewFractionPricingRequest = {
      fractions: selectedVariantIds.map((variantId, index) => ({
        variantId,
        weight: weights[index]!,
      })),
      ...(channel ? { channel } : {}),
    };

    api
      .preview(request)
      .then((response) => {
        if (cancelled) return;
        setPricing(response);
        onPriced?.(response);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setPricing(null);
        setError(err instanceof Error ? err.message : 'Não foi possível calcular o preço.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [api, channel, onPriced, selectedVariantIds]);

  useEffect(() => {
    if (!pricing) return;
    if (
      previousUnitPriceRef.current !== null &&
      previousUnitPriceRef.current !== pricing.unitPrice
    ) {
      setPriceFlashKey((key) => key + 1);
    }
    previousUnitPriceRef.current = pricing.unitPrice;
  }, [pricing]);

  return (
    <Card
      className="fraction-builder"
      title="Monte seu meio a meio"
      subtitle={`Escolha o tamanho e até ${maxFractions} sabores`}
    >
      <div className="fraction-builder__step">
        <span className="fraction-builder__step-label">1. Tamanho</span>
        <SegmentedControl
          options={sizes}
          {...(selectedSize !== undefined ? { value: selectedSize } : {})}
          onChange={selectSize}
          size="lg"
          aria-label="Escolha o tamanho"
        />
      </div>

      {selectedSize ? (
        <div className="fraction-builder__step">
          <span className="fraction-builder__step-label">
            2. Sabores ({selectedVariantIds.length}/{maxFractions})
          </span>
          <div
            className="fraction-builder__flavors nx-stagger"
            role="group"
            aria-label="Escolha os sabores"
          >
            {flavorsForSize.map((flavor) => {
              const reason = blockReason(flavor);
              const selected = selectedVariantIds.includes(flavor.variantId);
              const blocked = Boolean(reason) && !selected;
              return (
                <button
                  key={flavor.variantId}
                  type="button"
                  className={[
                    'fraction-builder__flavor',
                    selected ? 'fraction-builder__flavor--selected' : '',
                    blocked ? 'fraction-builder__flavor--blocked' : '',
                  ]
                    .filter(Boolean)
                    .join(' ')}
                  aria-pressed={selected}
                  disabled={blocked}
                  title={blocked ? reason : undefined}
                  onClick={() => toggleFlavor(flavor)}
                >
                  <span className="fraction-builder__flavor-name">{flavor.productName}</span>
                  {blocked ? (
                    <span className="fraction-builder__flavor-reason">{reason}</span>
                  ) : null}
                </button>
              );
            })}
          </div>
        </div>
      ) : null}

      <div className="fraction-builder__price" aria-live="polite">
        {loading ? (
          <span className="fraction-builder__price-loading">Calculando preço…</span>
        ) : null}
        {!loading && error ? <span className="fraction-builder__price-error">{error}</span> : null}
        {!loading && !error && pricing ? (
          <>
            <span
              key={priceFlashKey}
              className="fraction-builder__price-value nx-anim-flash"
            >
              {formatPrice(pricing.unitPrice)}
            </span>
            <Badge tone="brand">{pricing.priceRule}</Badge>
            <span className="fraction-builder__price-description">{pricing.description}</span>
          </>
        ) : null}
        {!loading && !error && !pricing && selectedVariantIds.length < 2 ? (
          <span className="fraction-builder__price-hint">
            Escolha pelo menos dois sabores para ver o preço.
          </span>
        ) : null}
      </div>
    </Card>
  );
}

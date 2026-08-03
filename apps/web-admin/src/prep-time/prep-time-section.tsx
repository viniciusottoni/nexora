import { useEffect, useState } from 'react';
import type { ProductDto, StationDto } from '@nexora/contracts';
import { PrepTimePage, type PrepTimeVariantRow } from './prep-time-page.js';
import type { PrepTimeApi } from './prep-time-api.js';
import type { VariantsApi } from '../catalog/variants-api.js';

export interface PrepTimeSectionProps {
  readonly products: readonly ProductDto[];
  readonly stations: readonly StationDto[];
  readonly prepTimeApi: PrepTimeApi;
  readonly variantsApi: VariantsApi;
}

/**
 * Encaixa `PrepTimePage` (lista achatada de variações) na navegação do admin. Não há endpoint
 * `GET /v1/catalog/variants` global — a lista é montada client-side (produtos → variações de cada
 * produto → análise de tempo de preparo de cada variação, para pré-popular os limiares efetivos).
 */
export function PrepTimeSection({
  products,
  stations,
  prepTimeApi,
  variantsApi,
}: Readonly<PrepTimeSectionProps>) {
  const [rows, setRows] = useState<readonly PrepTimeVariantRow[]>();
  const [loadError, setLoadError] = useState<string>();

  useEffect(() => {
    let active = true;

    async function load() {
      setRows(undefined);
      setLoadError(undefined);
      try {
        const stationByProduct = new Map(products.map((product) => [product.id, product]));
        const rowsByProduct = await Promise.all(
          products.map(async (product) => {
            const variants = await variantsApi.listForProduct(product.id);
            const withAnalysis = await Promise.all(
              variants.items.map(async (variant) => {
                const analysis = await prepTimeApi
                  .getPrepTimeAnalysis(variant.id)
                  .catch(() => undefined);
                const owner = stationByProduct.get(product.id);
                const row: PrepTimeVariantRow = {
                  variantId: variant.id,
                  variantName: variant.name,
                  productId: product.id,
                  productName: product.name,
                  prepMinutes: variant.prepMinutes,
                  warnMinutes: analysis?.effectiveWarnMinutes ?? null,
                  criticalMinutes: analysis?.effectiveCriticalMinutes ?? null,
                  stationId: owner?.stationId ?? null,
                  stationCode: null,
                  stationName: owner?.stationName ?? null,
                };
                return row;
              }),
            );
            return withAnalysis;
          }),
        );
        if (active) setRows(rowsByProduct.flat());
      } catch (reason) {
        if (active) {
          setLoadError(
            reason instanceof Error
              ? reason.message
              : 'Não foi possível carregar os tempos de preparo.',
          );
        }
      }
    }

    void load();
    return () => {
      active = false;
    };
  }, [products, prepTimeApi, variantsApi]);

  return (
    <PrepTimePage
      variants={rows ?? []}
      loading={!rows && !loadError}
      loadError={loadError}
      stations={stations}
      onUpdatePrepTime={async (variantId, input) => {
        await prepTimeApi.updatePrepTime(variantId, input);
        setRows((current) =>
          current?.map((row) => (row.variantId === variantId ? { ...row, ...input } : row)),
        );
      }}
      onReassignStation={async (productId, stationId) => {
        const updated = await prepTimeApi.reassignStation(productId, stationId);
        setRows((current) =>
          current?.map((row) =>
            row.productId === productId
              ? { ...row, stationId: updated.stationId, stationName: updated.stationName }
              : row,
          ),
        );
      }}
      onLoadAnalysis={(variantId) => prepTimeApi.getPrepTimeAnalysis(variantId)}
    />
  );
}

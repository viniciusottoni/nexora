import { useEffect, useId, useState } from 'react';
import { Card, EmptyState, Field, Select } from '@nexora/ui';
import type { CategoryDto, PricingChannel, ProductDto, VariantDto } from '@nexora/contracts';
import {
  PriceTablePage,
  type CategoryPriceSnapshotItem,
  type PriceTablePageCategoryOption,
} from './price-table-page.js';
import type { PricingApi } from './pricing-api.js';
import type { VariantsApi } from '../catalog/variants-api.js';
import './pricing.css';

export interface PricingSectionProps {
  readonly categories: readonly CategoryDto[];
  readonly products: readonly ProductDto[];
  readonly pricingApi: PricingApi;
  readonly variantsApi: VariantsApi;
}

/**
 * Encaixa `PriceTablePage` (que opera sobre UMA variação por vez) no fluxo de navegação do
 * admin: escolher produto → escolher variação → editar a tabela de preços por canal. O preview
 * de reajuste em massa (`onLoadCategoryPriceSnapshot`) agrega client-side (produtos da categoria →
 * variações de cada produto → preço vigente de cada uma) porque não há endpoint dedicado de
 * "snapshot" no backend desta US.
 */
export function PricingSection({
  categories,
  products,
  pricingApi,
  variantsApi,
}: Readonly<PricingSectionProps>) {
  const productFieldId = useId();
  const variantFieldId = useId();

  const [productId, setProductId] = useState<string>();
  const [variants, setVariants] = useState<readonly VariantDto[]>([]);
  const [variantId, setVariantId] = useState<string>();

  useEffect(() => {
    if (!productId) {
      setVariants([]);
      setVariantId(undefined);
      return;
    }
    let active = true;
    variantsApi
      .listForProduct(productId)
      .then((result) => {
        if (!active) return;
        setVariants(result.items);
        setVariantId(result.items[0]?.id);
      })
      .catch(() => {
        if (active) setVariants([]);
      });
    return () => {
      active = false;
    };
  }, [productId, variantsApi]);

  const selectedVariant = variants.find((variant) => variant.id === variantId);

  const categoryOptions: readonly PriceTablePageCategoryOption[] = categories.map((category) => ({
    id: category.id,
    name: category.name,
  }));

  async function loadCategoryPriceSnapshot(
    categoryId: string,
    channel: PricingChannel,
  ): Promise<readonly CategoryPriceSnapshotItem[]> {
    const categoryProducts = products.filter((product) => product.categoryId === categoryId);
    const items: CategoryPriceSnapshotItem[] = [];
    for (const product of categoryProducts) {
      const productVariants = await variantsApi.listForProduct(product.id);
      for (const variant of productVariants.items) {
        const table = await pricingApi.getPriceTable(variant.id);
        const row = table.channels.find((entry) => entry.channel === channel);
        items.push({
          variantId: variant.id,
          variantName: `${product.name} · ${variant.name}`,
          currentAmount: row?.amount ?? '0.00',
        });
      }
    }
    return items;
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="pricing-section-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Cardápio · preços</p>
          <h1 className="db-page__title" id="pricing-section-title">
            Preço por canal de venda
          </h1>
          <p className="db-page__lead">
            Escolha um produto e uma variação para editar o preço por canal, ou use o reajuste em
            massa por categoria.
          </p>
        </div>
      </header>

      <Card className="db-form-card" title="Item a precificar" padding="tight">
        <div className="db-form-row">
          <Field label="Produto" htmlFor={productFieldId}>
            <Select
              id={productFieldId}
              value={productId ?? ''}
              onChange={(event) => setProductId(event.target.value || undefined)}
            >
              <option value="">Selecione um produto</option>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name}
                </option>
              ))}
            </Select>
          </Field>

          {productId ? (
            <Field label="Variação" htmlFor={variantFieldId}>
              <Select
                id={variantFieldId}
                value={variantId ?? ''}
                onChange={(event) => setVariantId(event.target.value || undefined)}
              >
                {variants.map((variant) => (
                  <option key={variant.id} value={variant.id}>
                    {variant.name}
                  </option>
                ))}
              </Select>
            </Field>
          ) : null}
        </div>
      </Card>

      {selectedVariant ? (
        <PriceTableLoader
          key={selectedVariant.id}
          variantId={selectedVariant.id}
          variantName={selectedVariant.name}
          categories={categoryOptions}
          pricingApi={pricingApi}
          onLoadCategoryPriceSnapshot={loadCategoryPriceSnapshot}
        />
      ) : (
        <Card padding="none">
          <EmptyState icon="sell" title="Selecione um produto e uma variação">
            A tabela de preços por canal aparece aqui.
          </EmptyState>
        </Card>
      )}
    </main>
  );
}

function PriceTableLoader({
  variantId,
  variantName,
  categories,
  pricingApi,
  onLoadCategoryPriceSnapshot,
}: Readonly<{
  variantId: string;
  variantName: string;
  categories: readonly PriceTablePageCategoryOption[];
  pricingApi: PricingApi;
  onLoadCategoryPriceSnapshot: (
    categoryId: string,
    channel: PricingChannel,
  ) => Promise<readonly CategoryPriceSnapshotItem[]>;
}>) {
  const [channels, setChannels] =
    useState<Awaited<ReturnType<PricingApi['getPriceTable']>>['channels']>();

  useEffect(() => {
    let active = true;
    void pricingApi
      .getPriceTable(variantId)
      .then((table) => {
        if (active) setChannels(table.channels);
      })
      .catch(() => {
        if (active) setChannels([]);
      });
    return () => {
      active = false;
    };
  }, [variantId, pricingApi]);

  if (!channels) {
    return (
      <Card role="status">
        <p className="db-loading">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando tabela de preços…
        </p>
      </Card>
    );
  }

  return (
    <PriceTablePage
      variantId={variantId}
      variantName={variantName}
      channels={channels}
      categories={categories}
      onSaveChannelPrices={(id, input) => pricingApi.setPriceTable(id, input)}
      onLoadCategoryPriceSnapshot={onLoadCategoryPriceSnapshot}
      onBulkAdjust={(input) => pricingApi.bulkAdjust(input)}
    />
  );
}

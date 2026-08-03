import { useState } from 'react';
import { SegmentedControl } from '@nexora/ui';
import type {
  CategoryDto,
  CreateCategoryRequest,
  CreateProductRequest,
  CreateVariantRequest,
  PriceDto,
  ProductDto,
  SetVariantPriceRequest,
  StationDto,
  UpdateCategoryRequest,
  UpdateProductRequest,
  UpdateVariantRequest,
  VariantDto,
  VariantListResponse,
} from '@nexora/contracts';
import { CategoryManagementPage } from './category-management-page.js';
import { ProductManagementPage } from './product-management-page.js';
import type { ProductImageContentType } from './products-api.js';
import './catalog.css';

export interface CatalogPageProps {
  readonly categories: readonly CategoryDto[];
  readonly products: readonly ProductDto[];
  readonly stations: readonly StationDto[];
  readonly onCreateCategory: (input: CreateCategoryRequest) => Promise<CategoryDto>;
  readonly onUpdateCategory: (id: string, input: UpdateCategoryRequest) => Promise<CategoryDto>;
  readonly onReorderCategories: (order: readonly string[]) => Promise<void>;
  readonly onDeactivateCategory: (id: string) => Promise<void>;
  readonly onCreateProduct: (input: CreateProductRequest) => Promise<ProductDto>;
  readonly onUpdateProduct: (id: string, input: UpdateProductRequest) => Promise<ProductDto>;
  readonly onReorderProducts: (categoryId: string, order: readonly string[]) => Promise<void>;
  readonly onActivateProduct: (id: string) => Promise<ProductDto>;
  readonly onDeactivateProduct: (id: string) => Promise<ProductDto>;
  readonly onUploadProductImage: (
    productId: string,
    blob: Blob,
    contentType: ProductImageContentType,
    dimensions: { width: number; height: number },
  ) => Promise<void>;
  /** Variações e preço (US-011) — opcionais, ver docstring de `ProductManagementPageProps`. */
  readonly onLoadVariants?: ((productId: string) => Promise<VariantListResponse>) | undefined;
  readonly onCreateVariant?:
    ((productId: string, input: CreateVariantRequest) => Promise<VariantDto>) | undefined;
  readonly onUpdateVariant?:
    ((id: string, input: UpdateVariantRequest) => Promise<VariantDto>) | undefined;
  readonly onSetVariantPrice?:
    ((id: string, input: SetVariantPriceRequest) => Promise<PriceDto>) | undefined;
  readonly onActivateVariant?: ((id: string) => Promise<VariantDto>) | undefined;
  readonly onDeactivateVariant?: ((id: string) => Promise<VariantDto>) | undefined;
  readonly onMarkVariantDefault?: ((id: string) => Promise<VariantDto>) | undefined;
}

type CatalogTab = 'categories' | 'products';

/**
 * Tela única de catálogo (US-010), com abas "Categorias"/"Produtos" em vez de duas seções de
 * navegação separadas — decisão documentada no relatório da tarefa: cadastrar um produto exige
 * ver a lista de categorias ao mesmo tempo (dropdown de categoria no formulário), então mantê-las
 * na mesma tela evita perder contexto ao alternar de seção só para conferir o nome de uma
 * categoria recém-criada.
 */
export function CatalogPage({
  categories,
  products,
  stations,
  onCreateCategory,
  onUpdateCategory,
  onReorderCategories,
  onDeactivateCategory,
  onCreateProduct,
  onUpdateProduct,
  onReorderProducts,
  onActivateProduct,
  onDeactivateProduct,
  onUploadProductImage,
  onLoadVariants,
  onCreateVariant,
  onUpdateVariant,
  onSetVariantPrice,
  onActivateVariant,
  onDeactivateVariant,
  onMarkVariantDefault,
}: Readonly<CatalogPageProps>) {
  const [tab, setTab] = useState<CatalogTab>('categories');

  return (
    <div className="catalog-tabs-shell">
      <nav aria-label="Seções do catálogo">
        <SegmentedControl
          options={[
            { value: 'categories', label: 'Categorias' },
            { value: 'products', label: 'Produtos' },
          ]}
          value={tab}
          onChange={(value) => setTab(value as CatalogTab)}
        />
      </nav>

      {tab === 'categories' ? (
        <CategoryManagementPage
          categories={categories}
          onCreate={onCreateCategory}
          onUpdate={onUpdateCategory}
          onReorder={onReorderCategories}
          onDeactivate={onDeactivateCategory}
        />
      ) : (
        <ProductManagementPage
          products={products}
          categories={categories}
          stations={stations}
          onCreate={onCreateProduct}
          onUpdate={onUpdateProduct}
          onReorder={onReorderProducts}
          onActivate={onActivateProduct}
          onDeactivate={onDeactivateProduct}
          onUploadImage={onUploadProductImage}
          onLoadVariants={onLoadVariants}
          onCreateVariant={onCreateVariant}
          onUpdateVariant={onUpdateVariant}
          onSetVariantPrice={onSetVariantPrice}
          onActivateVariant={onActivateVariant}
          onDeactivateVariant={onDeactivateVariant}
          onMarkVariantDefault={onMarkVariantDefault}
        />
      )}
    </div>
  );
}

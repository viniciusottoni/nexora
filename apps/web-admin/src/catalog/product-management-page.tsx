import { useEffect, useId, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  DataTable,
  EmptyState,
  Field,
  IconButton,
  Input,
  MenuItemCard,
  Select,
  Switch,
} from '@nexora/ui';
import type {
  CategoryDto,
  CreateProductRequest,
  CreateVariantRequest,
  PriceDto,
  ProductDto,
  SetVariantPriceRequest,
  StationDto,
  UpdateProductRequest,
  UpdateVariantRequest,
  VariantDto,
  VariantListResponse,
} from '@nexora/contracts';
import { NO_STATION_SENTINEL } from '@nexora/contracts';
import { ImageCropDialog, type ImageCropResult } from './image-crop-dialog.js';
import type { ProductImageContentType } from './products-api.js';
import { VariantPriceEditor } from './variant-price-editor.js';
import { centsToDisplay, decimalStringToCents } from './money.js';
import './catalog.css';

const NO_STATION_OPTION = '';

export interface ProductManagementPageProps {
  readonly products: readonly ProductDto[];
  readonly categories: readonly CategoryDto[];
  readonly stations: readonly StationDto[];
  readonly onCreate: (input: CreateProductRequest) => Promise<ProductDto>;
  readonly onUpdate: (id: string, input: UpdateProductRequest) => Promise<ProductDto>;
  readonly onReorder: (categoryId: string, order: readonly string[]) => Promise<void>;
  readonly onActivate: (id: string) => Promise<ProductDto>;
  readonly onDeactivate: (id: string) => Promise<ProductDto>;
  readonly onUploadImage: (
    productId: string,
    blob: Blob,
    contentType: ProductImageContentType,
    dimensions: { width: number; height: number },
  ) => Promise<void>;
  /**
   * Variações e preço (US-011) — todos opcionais para não quebrar quem ainda instancia esta
   * página sem o recurso (ex.: testes existentes de US-010). Quando `onLoadVariants` não é
   * informado, a seção "Variações e preço" simplesmente não é exibida.
   */
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

/**
 * Cadastro de produtos do cardápio (US-010) — praça via dropdown (`StationsApi` reaproveitada de
 * `apps/web-admin/src/stations`), pré-visualização lado a lado usando `MenuItemCard`
 * (`@nexora/ui`), recorte assistido de foto com proporção fixa e duplicação de produto com um
 * clique (US-010 §10).
 */
export function ProductManagementPage({
  products,
  categories,
  stations,
  onCreate,
  onUpdate,
  onReorder,
  onActivate,
  onDeactivate,
  onUploadImage,
  onLoadVariants,
  onCreateVariant,
  onUpdateVariant,
  onSetVariantPrice,
  onActivateVariant,
  onDeactivateVariant,
  onMarkVariantDefault,
}: Readonly<ProductManagementPageProps>) {
  const createNameFieldId = useId();
  const createCategoryFieldId = useId();
  const createStationFieldId = useId();
  const createDescriptionFieldId = useId();
  const createIngredientsFieldId = useId();
  const createAllergensFieldId = useId();
  const createMaxFractionsFieldId = useId();
  const createImageFieldId = useId();
  const editNameFieldId = useId();
  const editCategoryFieldId = useId();
  const editStationFieldId = useId();
  const editDescriptionFieldId = useId();
  const editIngredientsFieldId = useId();
  const editAllergensFieldId = useId();
  const editMaxFractionsFieldId = useId();

  const categoryFilterFieldId = useId();
  const [categoryFilter, setCategoryFilter] = useState<string>('');
  const [selectedId, setSelectedId] = useState<string | undefined>(products[0]?.id);
  const [creating, setCreating] = useState(false);
  const [pendingFile, setPendingFile] = useState<File>();
  const [cropTarget, setCropTarget] = useState<'create' | 'edit'>('edit');

  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();

  const filtered = categoryFilter
    ? products.filter((product) => product.categoryId === categoryFilter)
    : products;
  const ordered = [...filtered].sort((a, b) => a.position - b.position);
  const selected = products.find((product) => product.id === selectedId);

  const [name, setName] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [stationId, setStationId] = useState(NO_STATION_OPTION);
  const [description, setDescription] = useState('');
  const [ingredientsText, setIngredientsText] = useState('');
  const [allergensText, setAllergensText] = useState('');
  const [allowsFractions, setAllowsFractions] = useState(false);
  const [maxFractions, setMaxFractions] = useState('1');

  useEffect(() => {
    if (!selected) return;
    setName(selected.name);
    setCategoryId(selected.categoryId);
    setStationId(selected.stationId ?? NO_STATION_OPTION);
    setDescription(selected.description ?? '');
    setIngredientsText(selected.ingredientsText ?? '');
    setAllergensText(selected.allergens.join(', '));
    setAllowsFractions(selected.allowsFractions);
    setMaxFractions(String(selected.maxFractions));
  }, [selected?.id]);

  // US-011 (Variações de produto com preço próprio) — carregadas por produto, sob demanda, não
  // no estado global do catálogo (products/categories/stations continuam vindo inteiros por
  // props): a lista de variações só existe no back-end por `GET .../products/:id/variants`, não
  // há endpoint "todas as variações de todos os produtos".
  const [variants, setVariants] = useState<readonly VariantDto[]>([]);
  const [variantsLoading, setVariantsLoading] = useState(false);

  useEffect(() => {
    if (!selected || !onLoadVariants) {
      setVariants([]);
      return;
    }
    let active = true;
    setVariantsLoading(true);
    onLoadVariants(selected.id)
      .then((result) => {
        if (active) setVariants(result.items);
      })
      .catch(() => {
        if (active) setVariants([]);
      })
      .finally(() => {
        if (active) setVariantsLoading(false);
      });
    return () => {
      active = false;
    };
  }, [selected?.id, onLoadVariants]);

  async function refreshVariants() {
    if (!selected || !onLoadVariants) return;
    const result = await onLoadVariants(selected.id);
    setVariants(result.items);
  }

  /** Menor preço vigente entre as variações ativas — mesmo cálculo de "a partir de" do cardápio público (US-011 §4). */
  const fromPriceCents = useMemo(() => {
    const prices = variants
      .filter((variant) => variant.isActive && variant.currentPrice)
      .map((variant) => decimalStringToCents(variant.currentPrice!));
    return prices.length > 0 ? Math.min(...prices) : undefined;
  }, [variants]);

  const categoryOptions = useMemo(
    () => categories.map((category) => ({ value: category.id, label: category.name })),
    [categories],
  );
  const stationOptions = useMemo(
    () => [
      { value: NO_STATION_OPTION, label: 'Nenhuma praça' },
      ...stations.map((station) => ({ value: station.id, label: station.name })),
    ],
    [stations],
  );

  const [createName, setCreateName] = useState('');
  const [createCategoryId, setCreateCategoryId] = useState(categories[0]?.id ?? '');
  const [createStationId, setCreateStationId] = useState(NO_STATION_OPTION);
  const [createDescription, setCreateDescription] = useState('');
  const [createIngredientsText, setCreateIngredientsText] = useState('');
  const [createAllergensText, setCreateAllergensText] = useState('');
  const [createAllowsFractions, setCreateAllowsFractions] = useState(false);
  const [createMaxFractions, setCreateMaxFractions] = useState('1');
  const [createImage, setCreateImage] = useState<ImageCropResult>();

  useEffect(() => {
    if (!categories.some((category) => category.id === createCategoryId)) {
      setCreateCategoryId(categories[0]?.id ?? '');
    }
  }, [categories, createCategoryId]);

  function parseAllergens(text: string): string[] {
    return text
      .split(',')
      .map((entry) => entry.trim())
      .filter((entry) => entry.length > 0);
  }

  async function createProduct() {
    if (!createName.trim() || !createCategoryId) return;
    setBusy(true);
    setError(undefined);
    try {
      const positionInCategory = products.filter(
        (product) => product.categoryId === createCategoryId,
      ).length;
      const created = await onCreate({
        categoryId: createCategoryId,
        name: createName.trim(),
        stationId: createStationId || undefined,
        description: createDescription.trim() || undefined,
        ingredientsText: createIngredientsText.trim() || undefined,
        allergens: parseAllergens(createAllergensText),
        allowsFractions: createAllowsFractions,
        maxFractions: Number(createMaxFractions) || 1,
        position: positionInCategory,
        isActive: true,
      });
      setSelectedId(created.id);
      setCreating(false);
      setCreateName('');
      setCreateStationId(NO_STATION_OPTION);
      setCreateDescription('');
      setCreateIngredientsText('');
      setCreateAllergensText('');
      setCreateAllowsFractions(false);
      setCreateMaxFractions('1');
      const imageToUpload = createImage;
      setCreateImage(undefined);
      if (imageToUpload) {
        await onUploadImage(created.id, imageToUpload.blob, imageToUpload.contentType, {
          width: imageToUpload.width,
          height: imageToUpload.height,
        });
      }
      setNotice(imageToUpload ? 'Produto e foto criados.' : 'Produto criado.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  /** Duplicação de produto com um clique (US-010 §10) — chama `create` com os dados do produto selecionado. */
  async function duplicate() {
    if (!selected) return;
    setBusy(true);
    setError(undefined);
    try {
      const positionInCategory = products.filter(
        (product) => product.categoryId === selected.categoryId,
      ).length;
      const created = await onCreate({
        categoryId: selected.categoryId,
        name: `${selected.name} (cópia)`,
        stationId: selected.stationId ?? undefined,
        description: selected.description ?? undefined,
        ingredientsText: selected.ingredientsText ?? undefined,
        allergens: selected.allergens,
        allowsFractions: selected.allowsFractions,
        maxFractions: selected.maxFractions,
        position: positionInCategory,
        isActive: true,
      });
      setSelectedId(created.id);
      setNotice(`"${selected.name}" duplicado.`);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function save() {
    if (!selected) return;
    setBusy(true);
    setError(undefined);
    try {
      const stationChanged = stationId !== (selected.stationId ?? NO_STATION_OPTION);
      await onUpdate(selected.id, {
        name: name.trim(),
        categoryId: categoryId !== selected.categoryId ? categoryId : undefined,
        stationId: stationChanged
          ? stationId === NO_STATION_OPTION
            ? NO_STATION_SENTINEL
            : stationId
          : undefined,
        description: description.trim(),
        ingredientsText: ingredientsText.trim(),
        allergens: parseAllergens(allergensText),
        allowsFractions,
        maxFractions: Number(maxFractions) || 1,
      });
      setNotice('Alterações salvas.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function move(productId: string, direction: -1 | 1) {
    if (!categoryFilter) return;
    const index = ordered.findIndex((product) => product.id === productId);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= ordered.length) return;

    const next = [...ordered];
    [next[index], next[target]] = [next[target]!, next[index]!];

    setBusy(true);
    setError(undefined);
    try {
      await onReorder(
        categoryFilter,
        next.map((product) => product.id),
      );
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function toggleActive(product: ProductDto) {
    setBusy(true);
    setError(undefined);
    try {
      if (product.isActive) {
        await onDeactivate(product.id);
        setNotice('Produto desativado.');
      } else {
        await onActivate(product.id);
        setNotice('Produto reativado.');
      }
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function confirmCrop(result: {
    blob: Blob;
    width: number;
    height: number;
    contentType: ProductImageContentType;
  }) {
    if (cropTarget === 'create') {
      setCreateImage(result);
      setPendingFile(undefined);
      return;
    }
    if (!selected) return;
    setPendingFile(undefined);
    setBusy(true);
    setError(undefined);
    try {
      await onUploadImage(selected.id, result.blob, result.contentType, {
        width: result.width,
        height: result.height,
      });
      setNotice('Foto atualizada.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="catalog-shell" aria-labelledby="products-title">
      <header className="catalog-header">
        <div>
          <p className="catalog-eyebrow">CARDÁPIO</p>
          <h1 id="products-title">Produtos</h1>
          <p className="catalog-lead">
            Cadastre os itens vendidos, com foto, ingredientes e praça de produção — o cardápio
            digital substitui o cardápio de papel em todos os canais.
          </p>
        </div>
        <Button type="button" onClick={() => setCreating(true)} disabled={categories.length === 0}>
          Novo produto
        </Button>
      </header>

      {notice ? (
        <p className="catalog-notice" role="status">
          {notice}
        </p>
      ) : null}
      {error ? (
        <p className="catalog-error" role="alert">
          {error}
        </p>
      ) : null}

      {categories.length === 0 ? (
        <EmptyState icon="restaurant_menu" title="Cadastre uma categoria antes">
          Um produto sempre pertence a uma categoria — crie a primeira na aba Categorias.
        </EmptyState>
      ) : (
        <>
          <Field
            label="Filtrar por categoria"
            htmlFor={categoryFilterFieldId}
            className="catalog-filter"
          >
            <Select
              id={categoryFilterFieldId}
              value={categoryFilter}
              onChange={(event) => setCategoryFilter(event.target.value)}
              options={[{ value: '', label: 'Todas as categorias' }, ...categoryOptions]}
            />
          </Field>

          {ordered.length === 0 ? (
            <EmptyState icon="no_meals" title="Nenhum produto cadastrado">
              Crie o primeiro produto desta categoria.
            </EmptyState>
          ) : (
            <div className="catalog-workbench">
              <Card padding="none" className="catalog-table-card">
                <DataTable
                  rowKey="id"
                  rows={ordered}
                  onRowClick={(row) => setSelectedId(row.id)}
                  columns={[
                    {
                      key: 'reorder',
                      header: '',
                      width: '4.5rem',
                      render: (row) =>
                        categoryFilter ? (
                          <span className="catalog-reorder-controls">
                            <IconButton
                              icon="arrow_upward"
                              label={`Mover ${row.name} para cima`}
                              size="sm"
                              onClick={(event) => {
                                event.stopPropagation();
                                void move(row.id, -1);
                              }}
                            />
                            <IconButton
                              icon="arrow_downward"
                              label={`Mover ${row.name} para baixo`}
                              size="sm"
                              onClick={(event) => {
                                event.stopPropagation();
                                void move(row.id, 1);
                              }}
                            />
                          </span>
                        ) : null,
                    },
                    {
                      key: 'imageUrl',
                      header: '',
                      width: '2.5rem',
                      render: (row) =>
                        row.imageUrl ? (
                          <img src={row.imageUrl} alt="" className="catalog-product-thumb" />
                        ) : (
                          <span
                            className="catalog-product-thumb catalog-product-thumb--empty"
                            aria-hidden="true"
                          />
                        ),
                    },
                    { key: 'name', header: 'Nome' },
                    { key: 'categoryName', header: 'Categoria' },
                    {
                      key: 'stationName',
                      header: 'Praça',
                      render: (row) => row.stationName ?? '—',
                    },
                    {
                      key: 'isActive',
                      header: 'Situação',
                      render: (row) =>
                        row.isActive ? (
                          <Badge tone="success">Ativo</Badge>
                        ) : (
                          <Badge tone="neutral">Inativo</Badge>
                        ),
                    },
                  ]}
                />
              </Card>

              {selected ? (
                <div className="catalog-editor-column">
                  <Card
                    className="catalog-editor"
                    title={selected.name}
                    subtitle={selected.categoryName}
                  >
                    <Field label="Nome do produto" htmlFor={editNameFieldId}>
                      <Input
                        id={editNameFieldId}
                        value={name}
                        onChange={(event) => setName(event.target.value)}
                      />
                    </Field>

                    <div className="catalog-editor__row">
                      <Field label="Categoria" htmlFor={editCategoryFieldId}>
                        <Select
                          id={editCategoryFieldId}
                          value={categoryId}
                          onChange={(event) => setCategoryId(event.target.value)}
                          options={categoryOptions}
                        />
                      </Field>
                      <Field label="Praça de produção" htmlFor={editStationFieldId}>
                        <Select
                          id={editStationFieldId}
                          value={stationId}
                          onChange={(event) => setStationId(event.target.value)}
                          options={stationOptions}
                        />
                      </Field>
                    </div>

                    <Field label="Descrição" htmlFor={editDescriptionFieldId}>
                      <Input
                        id={editDescriptionFieldId}
                        value={description}
                        onChange={(event) => setDescription(event.target.value)}
                      />
                    </Field>
                    <Field
                      label="Ingredientes"
                      htmlFor={editIngredientsFieldId}
                      hint="Visível ao cliente — ex.: molho, mussarela, orégano"
                    >
                      <Input
                        id={editIngredientsFieldId}
                        value={ingredientsText}
                        onChange={(event) => setIngredientsText(event.target.value)}
                      />
                    </Field>
                    <Field
                      label="Alérgenos"
                      htmlFor={editAllergensFieldId}
                      hint="Separados por vírgula"
                    >
                      <Input
                        id={editAllergensFieldId}
                        value={allergensText}
                        onChange={(event) => setAllergensText(event.target.value)}
                      />
                    </Field>

                    <Switch
                      label="Permite fracionamento (ex.: pizza meio a meio)"
                      checked={allowsFractions}
                      onChange={(event) => setAllowsFractions(event.target.checked)}
                    />
                    {allowsFractions ? (
                      <Field label="Máximo de frações" htmlFor={editMaxFractionsFieldId}>
                        <Input
                          id={editMaxFractionsFieldId}
                          type="number"
                          min={1}
                          numeric
                          value={maxFractions}
                          onChange={(event) => setMaxFractions(event.target.value)}
                        />
                      </Field>
                    ) : null}

                    <Field
                      label="Foto do produto"
                      hint="PNG, JPEG, WebP ou HEIC — recorte assistido em proporção 1:1"
                    >
                      <input
                        type="file"
                        accept="image/png,image/jpeg,image/webp,image/heic,image/heif,.heic,.heif"
                        aria-label="Selecionar foto do produto"
                        onChange={(event) => {
                          const file = event.target.files?.[0];
                          if (file) {
                            setCropTarget('edit');
                            setPendingFile(file);
                          }
                          event.target.value = '';
                        }}
                      />
                    </Field>

                    <div className="catalog-editor__footer">
                      <span className="catalog-editor__footer-secondary">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => void duplicate()}
                          busy={busy}
                        >
                          Duplicar produto
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => void toggleActive(selected)}
                          busy={busy}
                        >
                          {selected.isActive ? 'Desativar produto' : 'Reativar produto'}
                        </Button>
                      </span>
                      <Button type="button" busy={busy} onClick={() => void save()}>
                        Salvar produto
                      </Button>
                    </div>

                    {onLoadVariants &&
                    onCreateVariant &&
                    onUpdateVariant &&
                    onSetVariantPrice &&
                    onActivateVariant &&
                    onDeactivateVariant &&
                    onMarkVariantDefault ? (
                      <VariantPriceEditor
                        variants={variants}
                        loading={variantsLoading}
                        onCreate={async (input) => {
                          const created = await onCreateVariant(selected.id, input);
                          await refreshVariants();
                          return created;
                        }}
                        onUpdate={async (id, input) => {
                          const updated = await onUpdateVariant(id, input);
                          await refreshVariants();
                          return updated;
                        }}
                        onSetPrice={async (id, input) => {
                          const price = await onSetVariantPrice(id, input);
                          await refreshVariants();
                          return price;
                        }}
                        onActivate={async (id) => {
                          const updated = await onActivateVariant(id);
                          await refreshVariants();
                          return updated;
                        }}
                        onDeactivate={async (id) => {
                          const updated = await onDeactivateVariant(id);
                          await refreshVariants();
                          return updated;
                        }}
                        onMarkDefault={async (id) => {
                          const updated = await onMarkVariantDefault(id);
                          await refreshVariants();
                          return updated;
                        }}
                      />
                    ) : null}
                  </Card>

                  <Card
                    className="catalog-preview"
                    title="Pré-visualização"
                    subtitle="Como aparece no cardápio da mesa"
                  >
                    <MenuItemCard
                      name={name || selected.name}
                      description={ingredientsText || description || undefined}
                      price={
                        fromPriceCents === undefined
                          ? 'Preço a definir'
                          : variants.filter((variant) => variant.isActive).length > 1
                            ? `A partir de R$ ${centsToDisplay(fromPriceCents)}`
                            : `R$ ${centsToDisplay(fromPriceCents)}`
                      }
                      {...(selected.imageUrl ? { imageSrc: selected.imageUrl } : {})}
                      unavailable={!selected.isAvailable}
                    />
                  </Card>
                </div>
              ) : null}
            </div>
          )}
        </>
      )}

      {creating ? (
        <div className="catalog-dialog-backdrop">
          <section
            className="catalog-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-product-title"
          >
            <p className="catalog-eyebrow">NOVO PRODUTO</p>
            <h2 id="create-product-title">Criar produto</h2>
            <div className="catalog-dialog__fields">
              <Field label="Nome" htmlFor={createNameFieldId}>
                <Input
                  id={createNameFieldId}
                  value={createName}
                  onChange={(event) => setCreateName(event.target.value)}
                />
              </Field>
              <Field label="Categoria" htmlFor={createCategoryFieldId}>
                <Select
                  id={createCategoryFieldId}
                  value={createCategoryId}
                  onChange={(event) => setCreateCategoryId(event.target.value)}
                  options={categoryOptions}
                />
              </Field>
              <Field label="Praça de produção" htmlFor={createStationFieldId}>
                <Select
                  id={createStationFieldId}
                  value={createStationId}
                  onChange={(event) => setCreateStationId(event.target.value)}
                  options={stationOptions}
                />
              </Field>
              <Field label="Descrição" htmlFor={createDescriptionFieldId}>
                <Input
                  id={createDescriptionFieldId}
                  value={createDescription}
                  onChange={(event) => setCreateDescription(event.target.value)}
                />
              </Field>
              <Field label="Ingredientes" htmlFor={createIngredientsFieldId}>
                <Input
                  id={createIngredientsFieldId}
                  value={createIngredientsText}
                  onChange={(event) => setCreateIngredientsText(event.target.value)}
                />
              </Field>
              <Field
                label="Alérgenos"
                htmlFor={createAllergensFieldId}
                hint="Separados por vírgula"
              >
                <Input
                  id={createAllergensFieldId}
                  value={createAllergensText}
                  onChange={(event) => setCreateAllergensText(event.target.value)}
                />
              </Field>
              <Switch
                label="Permite frações"
                checked={createAllowsFractions}
                onChange={(event) => setCreateAllowsFractions(event.target.checked)}
              />
              {createAllowsFractions ? (
                <Field label="Máximo de frações" htmlFor={createMaxFractionsFieldId}>
                  <Input
                    id={createMaxFractionsFieldId}
                    type="number"
                    min={1}
                    numeric
                    value={createMaxFractions}
                    onChange={(event) => setCreateMaxFractions(event.target.value)}
                  />
                </Field>
              ) : null}
              <Field
                label="Foto do produto"
                htmlFor={createImageFieldId}
                hint={
                  createImage ? 'Recorte pronto para envio.' : 'PNG, JPEG, WebP ou HEIC — até 10 MB'
                }
              >
                <input
                  id={createImageFieldId}
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/heic,image/heif,.heic,.heif"
                  onChange={(event) => {
                    const file = event.target.files?.[0];
                    if (file) {
                      setCropTarget('create');
                      setPendingFile(file);
                    }
                    event.target.value = '';
                  }}
                />
              </Field>
            </div>
            <div className="catalog-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createProduct()}>
                Criar produto
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {pendingFile ? (
        <ImageCropDialog
          file={pendingFile}
          onCancel={() => setPendingFile(undefined)}
          onConfirm={(result) => void confirmCrop(result)}
        />
      ) : null}
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

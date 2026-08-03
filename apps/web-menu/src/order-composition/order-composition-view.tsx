import { useEffect, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  Checkbox,
  EmptyState,
  Icon,
  MenuItemCard,
  OrderLine,
  QuantityStepper,
  averageFractionPriceCents,
  buildCreateOrderItems,
  cartTotalCents,
  describeOrderValidationError,
  findModifierGroupValidationError,
  formatCentsBrl,
  lineTotalCents,
  moneyToCents,
  splitEqualWeights,
  toCartLineModifiers,
  type CartLine,
  type CartLineFractionSelection,
  type ComposableProduct,
} from '@nexora/ui';
import { OrderCompositionApiError, PublicOrderCompositionApi } from './order-composition-api.js';
import './order-composition-view.css';

// Ligado UMA VEZ no carregamento do módulo — nunca recriado inline no valor default de uma prop
// (isso geraria uma função NOVA a cada render, e como `fetcher` entra no array de deps do
// `useMemo` abaixo, cada render recriaria `api` e disparava `useEffect` de novo: loop infinito de
// requisição observado em teste real). Ver docstring de
// `packages/ui/src/auth/operational-authenticated-fetch.ts` sobre o motivo do `.bind`.
const boundFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

export interface OrderCompositionViewProps {
  readonly sessionToken: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /**
   * US-034 §8 — chamado assim que um pedido entra na fila local (queda de LAN durante a
   * confirmação). O shell da tela de acesso (`BrandedTableMenu`, `table-access-page.tsx`) usa
   * isso para atualizar o contador do indicador permanente sem esperar o próximo `flush()`.
   */
  readonly onOrderQueued?: () => void;
}

interface ConfiguringState {
  readonly product: ComposableProduct;
  readonly quantity: number;
  readonly notes: string;
  readonly selectedModifiers: ReadonlyMap<string, number>;
  readonly fractionMode: boolean;
  readonly selectedFlavorIds: readonly string[];
}

/**
 * Composição de pedido do cliente pela mesa (US-030 §7/§10, cenário "Pedido do cliente na mesa
 * via QR") — escolher produto → configurar (quantidade/modificadores/frações/observação) → ver o
 * carrinho com total sempre visível → confirmar (`POST /v1/public/orders`, autenticado pelo
 * `sessionToken` anônimo da mesa). Mesma lógica de composição de `apps/web-pos/src/order-composition/order-composition-page.tsx`
 * — DUPLICADA de propósito (decisão documentada no relatório da tarefa): os dois apps não
 * compartilham nenhum código-fonte hoje além de `packages/ui`/`packages/contracts`, e extrair um
 * pacote novo só para ~250 linhas de UI seria desproporcional; a MATEMÁTICA/estado do carrinho
 * (`order-cart.ts`) e o modelo de cardápio (`composable-menu.ts`) já são compartilhados via
 * `@nexora/ui`, que é onde a lógica de negócio realmente reutilizável vive.
 */
export function OrderCompositionView({
  sessionToken,
  baseUrl = '',
  fetcher = boundFetch,
  onOrderQueued,
}: Readonly<OrderCompositionViewProps>) {
  const api = useMemo(() => new PublicOrderCompositionApi(sessionToken, baseUrl, fetcher), [sessionToken, baseUrl, fetcher]);
  const [products, setProducts] = useState<ComposableProduct[]>();
  const [loadError, setLoadError] = useState<string>();
  const [configuring, setConfiguring] = useState<ConfiguringState>();
  const [configError, setConfigError] = useState<string>();
  const [lines, setLines] = useState<CartLine[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string>();
  const [confirmed, setConfirmed] = useState<{ shortCode: string; total: string }>();
  // US-034 §10 — aviso discreto e temporário (nunca modal) de que o pedido ficou na fila local por
  // falta de rede; some sozinho, sem exigir nenhuma ação do cliente.
  const [queuedNotice, setQueuedNotice] = useState<string>();

  useEffect(() => {
    if (!queuedNotice) return;
    const timeout = setTimeout(() => setQueuedNotice(undefined), 6000);
    return () => clearTimeout(timeout);
  }, [queuedNotice]);

  useEffect(() => {
    let active = true;
    api
      .getMenu()
      .then((items) => {
        if (active) setProducts(items);
      })
      .catch(() => {
        if (active) setLoadError('Não foi possível carregar o cardápio agora.');
      });
    return () => {
      active = false;
    };
  }, [api]);

  const categoryGroups = useMemo(() => {
    if (!products) return [];
    const byCategory = new Map<string, { name: string; products: ComposableProduct[] }>();
    for (const product of products) {
      const bucket = byCategory.get(product.categoryId);
      if (bucket) bucket.products.push(product);
      else byCategory.set(product.categoryId, { name: product.categoryName, products: [product] });
    }
    return [...byCategory.values()];
  }, [products]);

  function startConfiguring(product: ComposableProduct) {
    setConfigError(undefined);
    setConfiguring({
      product,
      quantity: 1,
      notes: '',
      selectedModifiers: new Map(),
      fractionMode: false,
      selectedFlavorIds: [],
    });
  }

  function toggleModifier(modifierId: string) {
    setConfiguring((current) => {
      if (!current) return current;
      const next = new Map(current.selectedModifiers);
      if (next.has(modifierId)) next.delete(modifierId);
      else next.set(modifierId, 1);
      return { ...current, selectedModifiers: next };
    });
  }

  function toggleFlavor(variantId: string) {
    setConfiguring((current) => {
      if (!current) return current;
      if (current.selectedFlavorIds.includes(variantId)) {
        return { ...current, selectedFlavorIds: current.selectedFlavorIds.filter((id) => id !== variantId) };
      }
      if (current.selectedFlavorIds.length >= current.product.maxFractions) return current;
      return { ...current, selectedFlavorIds: [...current.selectedFlavorIds, variantId] };
    });
  }

  function confirmAddLine() {
    if (!configuring) return;
    const { product } = configuring;

    if (configuring.fractionMode) {
      if (configuring.selectedFlavorIds.length < 2) {
        setConfigError('Escolha ao menos dois sabores para o meio a meio.');
        return;
      }
    } else {
      const validationError = findModifierGroupValidationError(product, configuring.selectedModifiers);
      if (validationError) {
        setConfigError(`Selecione uma opção do grupo "${validationError.groupName}" antes de adicionar.`);
        return;
      }
    }

    const allModifiers = product.modifierGroups.flatMap((group) => group.modifiers);
    const modifiers = configuring.fractionMode ? [] : toCartLineModifiers(allModifiers, configuring.selectedModifiers);
    const fractions = configuring.fractionMode ? buildFractionSelections(product, configuring.selectedFlavorIds) : [];
    const unitPriceCents = configuring.fractionMode
      ? averageFractionPriceCents(product.fractionFlavors.filter((flavor) => configuring.selectedFlavorIds.includes(flavor.variantId)))
      : moneyToCents(product.variants[0]?.price ?? '0.00');

    const newLine: CartLine = {
      localId: crypto.randomUUID(),
      productId: product.id,
      productName: product.name,
      variantId: product.variants[0]?.id ?? product.id,
      quantity: configuring.quantity,
      notes: configuring.notes,
      unitPriceCents,
      modifiers,
      fractions,
    };

    // Envio otimista (US-030 §10): o item entra no carrinho local IMEDIATAMENTE — nenhuma
    // chamada de rede por item; a confirmação com o servidor só ocorre no lote final ("Confirmar
    // pedido"). Se o servidor recusar, o carrinho continua exatamente como estava (rollback
    // claro), nunca é descartado.
    setLines((current) => [...current, newLine]);
    setConfiguring(undefined);
  }

  function removeLine(localId: string) {
    setLines((current) => current.filter((line) => line.localId !== localId));
  }

  async function handleConfirmOrder() {
    if (lines.length === 0) return;
    setSubmitting(true);
    setSubmitError(undefined);
    try {
      const outcome = await api.createOrder(buildCreateOrderItems(lines));
      if (outcome.status === 'queued') {
        // US-034 §4, cenário "queda no meio de um pedido": "o cliente não deve perceber diferença
        // no fluxo" — nunca um erro. O pedido já está garantido (a Idempotency-Key foi fixada e
        // vai ser reenviada sozinha), só ainda não tem o código real do servidor.
        setQueuedNotice('Pedido recebido — sincronizando quando a conexão voltar.');
        onOrderQueued?.();
      } else {
        setConfirmed({ shortCode: outcome.order.shortCode, total: outcome.order.total });
      }
      setLines([]);
    } catch (cause) {
      setSubmitError(
        cause instanceof OrderCompositionApiError
          ? describeOrderValidationError(cause.code, cause.meta, lines)
          : 'Não foi possível confirmar o pedido agora. Tente novamente.',
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (confirmed) {
    return (
      <Card as="section" className="order-composition-confirmed nx-anim-scale-in">
        <p className="menu-eyebrow">Pedido confirmado</p>
        <h2>Código do pedido</h2>
        {/* US-030 §10: "código curto do pedido exibido após a confirmação — é o que a cozinha chama em voz alta". */}
        <p className="order-composition-confirmed__code">{confirmed.shortCode}</p>
        <p className="order-composition-confirmed__total">Total: {formatCentsBrl(moneyToCents(confirmed.total))}</p>
        <Button type="button" onClick={() => setConfirmed(undefined)}>
          Continuar pedindo
        </Button>
      </Card>
    );
  }

  if (configuring) {
    const { product } = configuring;
    const unitPriceCents = computeConfiguringUnitPriceCents(configuring);
    return (
      <section className="order-composition-configure nx-anim-in">
        <header className="order-composition-configure__header">
          <Button type="button" variant="ghost" size="sm" onClick={() => setConfiguring(undefined)}>
            <Icon name="arrow_back" size={20} />
            Voltar
          </Button>
          <h2>{product.name}</h2>
        </header>

        <div className="order-composition-configure__qty">
          <span>Quantidade</span>
          <QuantityStepper
            value={configuring.quantity}
            min={1}
            max={20}
            onChange={(value) => setConfiguring((current) => (current ? { ...current, quantity: value } : current))}
          />
        </div>

        {product.allowsFractions && product.fractionFlavors.length >= 2 ? (
          <Checkbox
            label="Meio a meio"
            checked={configuring.fractionMode}
            onChange={(event) =>
              setConfiguring((current) =>
                current ? { ...current, fractionMode: event.target.checked, selectedFlavorIds: [] } : current,
              )
            }
          />
        ) : null}

        {configuring.fractionMode ? (
          <div className="order-composition-configure__group" role="group" aria-label="Escolha os sabores">
            <h3>
              Sabores ({configuring.selectedFlavorIds.length}/{product.maxFractions})
            </h3>
            {product.fractionFlavors.map((flavor) => (
              <Checkbox
                key={flavor.variantId}
                label={flavor.name}
                price={formatCentsBrl(moneyToCents(flavor.price))}
                disabled={!flavor.available}
                checked={configuring.selectedFlavorIds.includes(flavor.variantId)}
                onChange={() => toggleFlavor(flavor.variantId)}
              />
            ))}
          </div>
        ) : (
          product.modifierGroups.map((group) => (
            <div key={group.id} className="order-composition-configure__group" role="group" aria-label={group.name}>
              <h3>
                {group.name}
                {group.isRequired ? (
                  <Badge tone="warning" size="sm">
                    Obrigatório
                  </Badge>
                ) : null}
              </h3>
              {group.modifiers.map((modifier) => (
                <Checkbox
                  key={modifier.id}
                  label={modifier.name}
                  price={formatCentsBrl(moneyToCents(modifier.priceDelta))}
                  checked={configuring.selectedModifiers.has(modifier.id)}
                  onChange={() => toggleModifier(modifier.id)}
                />
              ))}
            </div>
          ))
        )}

        <label className="order-composition-configure__notes">
          Observação
          <textarea
            value={configuring.notes}
            onChange={(event) =>
              setConfiguring((current) => (current ? { ...current, notes: event.target.value } : current))
            }
            placeholder="Ex.: bem assada, sem cebola"
            maxLength={500}
          />
        </label>

        {configError ? (
          <p role="alert" className="order-composition-configure__error">
            {configError}
          </p>
        ) : null}

        <footer className="order-composition-configure__footer">
          <strong>{formatCentsBrl(unitPriceCents * configuring.quantity)}</strong>
          <Button type="button" onClick={confirmAddLine}>
            Adicionar ao pedido
          </Button>
        </footer>
      </section>
    );
  }

  return (
    <div className="order-composition">
      {loadError ? <AlertBanner tone="danger">{loadError}</AlertBanner> : null}

      {queuedNotice ? (
        // US-034 §10: nunca modal/pop-up — toast discreto que some sozinho. Linguagem sem jargão
        // técnico (RF-OFF).
        <p className="order-composition__queued-toast nx-anim-toast-in" role="status">
          {queuedNotice}
        </p>
      ) : null}

      {products === undefined ? (
        <p role="status" className="order-composition__loading">
          Carregando cardápio…
        </p>
      ) : products.length === 0 ? (
        <EmptyState icon="restaurant_menu">Nenhum produto disponível no cardápio agora.</EmptyState>
      ) : (
        categoryGroups.map((group) => (
          <section key={group.name} className="order-composition__category" aria-label={group.name}>
            <h2>{group.name}</h2>
            <div className="order-composition__grid nx-stagger">
              {group.products.map((product) => (
                <MenuItemCard
                  key={product.id}
                  name={product.name}
                  description={product.description ?? undefined}
                  price={product.fromPrice ? `A partir de ${formatCentsBrl(moneyToCents(product.fromPrice))}` : 'Consulte'}
                  onClick={() => startConfiguring(product)}
                />
              ))}
            </div>
          </section>
        ))
      )}

      <section className="order-composition__cart" aria-label="Meu pedido">
        <h2>Meu pedido</h2>
        {lines.length === 0 ? (
          <p className="order-composition__cart-empty">Nenhum item adicionado ainda.</p>
        ) : (
          <div className="order-composition__cart-lines nx-stagger">
            {lines.map((line) => (
              <OrderLine
                key={line.localId}
                qty={line.quantity}
                name={line.productName}
                modifiers={describeLineExtras(line)}
                note={line.notes || undefined}
                price={formatCentsBrl(lineTotalCents(line))}
                actions={
                  <Button type="button" size="sm" variant="ghost" onClick={() => removeLine(line.localId)}>
                    Remover
                  </Button>
                }
              />
            ))}
          </div>
        )}

        {submitError ? (
          <p role="alert" className="order-composition__cart-error">
            {submitError}
          </p>
        ) : null}

        <footer className="order-composition__cart-footer">
          {/* US-030 §10: "preço total sempre visível durante a montagem, atualizado a cada escolha". */}
          <span className="order-composition__cart-total">Total: {formatCentsBrl(cartTotalCents(lines))}</span>
          <Button type="button" onClick={() => void handleConfirmOrder()} disabled={lines.length === 0 || submitting} busy={submitting}>
            {submitting ? 'Enviando…' : 'Confirmar pedido'}
          </Button>
        </footer>
      </section>
    </div>
  );
}

function describeLineExtras(line: CartLine): string | undefined {
  if (line.fractions.length > 0) {
    return `Meio a meio: ${line.fractions.map((fraction) => fraction.name).join(' / ')}`;
  }
  if (line.modifiers.length > 0) {
    return line.modifiers.map((modifier) => modifier.name).join(', ');
  }
  return undefined;
}

function computeConfiguringUnitPriceCents(configuring: ConfiguringState): number {
  const { product } = configuring;
  if (configuring.fractionMode) {
    const flavors = product.fractionFlavors.filter((flavor) => configuring.selectedFlavorIds.includes(flavor.variantId));
    return averageFractionPriceCents(flavors);
  }
  const basePriceCents = moneyToCents(product.variants[0]?.price ?? '0.00');
  const allModifiers = product.modifierGroups.flatMap((group) => group.modifiers);
  const modifiersCents = [...configuring.selectedModifiers.keys()].reduce((sum, modifierId) => {
    const modifier = allModifiers.find((candidate) => candidate.id === modifierId);
    return sum + (modifier ? moneyToCents(modifier.priceDelta) : 0);
  }, 0);
  return basePriceCents + modifiersCents;
}

function buildFractionSelections(
  product: ComposableProduct,
  selectedFlavorIds: readonly string[],
): CartLineFractionSelection[] {
  const weights = splitEqualWeights(selectedFlavorIds.length);
  return selectedFlavorIds.map((variantId, index) => {
    const flavor = product.fractionFlavors.find((candidate) => candidate.variantId === variantId);
    return {
      variantId,
      name: flavor?.name ?? variantId,
      weight: weights[index]!,
      priceCents: flavor ? moneyToCents(flavor.price) : 0,
    };
  });
}

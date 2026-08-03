import { z } from 'zod';
import {
  publicMenuCategorySchema,
  publicMenuProductSchema,
  publicMenuResponseSchema,
} from '@nexora/contracts';

/**
 * US-030 (Criar pedido com itens, modificadores e frações) §10 — modelo de composição de pedido
 * compartilhado por `apps/web-pos/src/order-composition` e `apps/web-menu/src/order-composition`
 * (o cliente pela mesa via QR e o garçom pelo celular reusam a MESMA leitura de cardápio e a
 * MESMA forma de produto "pronto para compor", só o transporte HTTP final — `POST /v1/orders` vs.
 * `POST /v1/public/orders` — difere entre os dois apps).
 *
 * GAP DE CATÁLOGO CONHECIDO (documentar para quem tocar `web-kds`/estoque/offline a seguir):
 * `GET /v1/public/menu` — único endpoint de leitura de cardápio hoje, idêntico no edge
 * (`Nexora.Api.Edge.Controllers.PublicMenuController`) e na nuvem, ambos delegando a
 * `PublicMenuBuilder.BuildAsync` — devolve só `id`/`name`/`description`/`imageUrl`/`fromPrice` por
 * produto. NÃO existe hoje nenhum endpoint (edge ou nuvem, público ou autenticado) que devolva a
 * lista de variantes, grupos de modificador ou elegibilidade de fração de um produto para quem
 * está montando um pedido — `ProductVariantsController`/`ModifierGroupsController`/
 * `ProductModifierGroupsController` vivem só em `Nexora.Api.Cloud`, atrás de autenticação de
 * administrador (cookie), inacessíveis ao POS/mesa (autenticação operacional/anônima, e a operação
 * precisa funcionar OFFLINE contra o edge — doc. 02 §2.1). `FractionBuilder`
 * (apps/web-menu/src/fraction-builder) já documentava essa lacuna antes desta tarefa ("web-menu
 * ainda não tem app de cardápio do cliente construído... os sabores já vêm resolvidos de fora").
 *
 * Este módulo NÃO resolve essa lacuna (fora do mandato desta tarefa, que é consumir contrato já
 * existente — ver instruções da US-030 FE). Em vez disso, define uma extensão ADITIVA e
 * OPCIONAL do produto do cardápio público: contra o backend real de hoje nenhum desses campos
 * chega, e a composição degrada de forma honesta para "1 variante = o próprio produto, sem
 * modificador, sem fração" (`toComposableProduct` abaixo). O dia em que uma próxima história de
 * catálogo expuser esse detalhe (endpoint de detalhe do produto, ou estes mesmos campos
 * adicionados a `PublicMenuProductResponse`), a composição já lê os dados certos sem precisar
 * mudar UI nenhuma — e a lógica de grupo obrigatório/fração já está testada com fixtures que
 * incluem esses campos (`order-cart.test.ts`).
 */
const composableMoneySchema = z.string().regex(/^\d+\.\d{2}$/, 'Valor monetário inválido');

export const composableModifierSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  priceDelta: composableMoneySchema,
});

export const composableModifierGroupSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  minSelect: z.number().int().nonnegative(),
  maxSelect: z.number().int().nonnegative(),
  isRequired: z.boolean(),
  modifiers: z.array(composableModifierSchema),
});

export const composableFractionFlavorSchema = z.object({
  variantId: z.string().uuid(),
  name: z.string().min(1),
  fractionGroup: z.string().min(1),
  price: composableMoneySchema,
  available: z.boolean(),
});

export const composableVariantSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  price: composableMoneySchema,
});

const publicMenuProductExtensionSchema = z.object({
  variants: z.array(composableVariantSchema).optional(),
  modifierGroups: z.array(composableModifierGroupSchema).optional(),
  allowsFractions: z.boolean().optional(),
  maxFractions: z.number().int().positive().optional(),
  fractionFlavors: z.array(composableFractionFlavorSchema).optional(),
});

export const composableMenuProductSchema = publicMenuProductSchema.merge(
  publicMenuProductExtensionSchema,
);

export const composableMenuCategorySchema = publicMenuCategorySchema.extend({
  products: z.array(composableMenuProductSchema),
});

export const composableMenuResponseSchema = publicMenuResponseSchema.extend({
  categories: z.array(composableMenuCategorySchema),
});

export type ComposableMenuProduct = z.infer<typeof composableMenuProductSchema>;
export type ComposableMenuResponse = z.infer<typeof composableMenuResponseSchema>;

export interface ComposableModifier {
  readonly id: string;
  readonly name: string;
  readonly priceDelta: string;
}

export interface ComposableModifierGroup {
  readonly id: string;
  readonly name: string;
  readonly minSelect: number;
  readonly maxSelect: number;
  readonly isRequired: boolean;
  readonly modifiers: readonly ComposableModifier[];
}

export interface ComposableFractionFlavor {
  readonly variantId: string;
  readonly name: string;
  readonly fractionGroup: string;
  readonly price: string;
  readonly available: boolean;
}

export interface ComposableVariant {
  readonly id: string;
  readonly name: string;
  readonly price: string;
}

/** Produto pronto para composição de pedido — ver docstring do módulo para a origem/limite de cada campo. */
export interface ComposableProduct {
  readonly id: string;
  readonly categoryId: string;
  readonly categoryName: string;
  readonly name: string;
  readonly description: string | null;
  readonly imageUrl: string | null;
  /** "A partir de R$ X" — só rótulo, nunca usado para montar o preço do carrinho (ver `variants`). */
  readonly fromPrice: string | null;
  readonly variants: readonly ComposableVariant[];
  readonly modifierGroups: readonly ComposableModifierGroup[];
  readonly allowsFractions: boolean;
  readonly maxFractions: number;
  readonly fractionFlavors: readonly ComposableFractionFlavor[];
}

export function mapComposableMenuResponseToProducts(
  menu: ComposableMenuResponse,
): ComposableProduct[] {
  return menu.categories.flatMap((category) =>
    category.products.map((product) => toComposableProduct(category, product)),
  );
}

function toComposableProduct(
  category: Pick<z.infer<typeof composableMenuCategorySchema>, 'id' | 'name'>,
  product: ComposableMenuProduct,
): ComposableProduct {
  const variants =
    product.variants && product.variants.length > 0
      ? product.variants
      : [{ id: product.id, name: product.name, price: product.fromPrice ?? '0.00' }];
  return {
    id: product.id,
    categoryId: category.id,
    categoryName: category.name,
    name: product.name,
    description: product.description,
    imageUrl: product.imageUrl,
    fromPrice: product.fromPrice,
    variants,
    modifierGroups: product.modifierGroups ?? [],
    allowsFractions: product.allowsFractions ?? false,
    maxFractions: product.maxFractions ?? 1,
    fractionFlavors: product.fractionFlavors ?? [],
  };
}

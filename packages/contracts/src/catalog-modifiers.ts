import { z } from 'zod';

const uuid = z.string().uuid();

/**
 * Dinheiro/quantidade como string (ADR-017: double/float proibidos para dinheiro — string evita
 * perda de precisão no cliente). Mesmo formato de `money` em `sync.ts` (initialSyncModifierSchema),
 * duplicado aqui porque aquele const não é exportado — os dois precisam ficar em sincronia se o
 * formato mudar.
 */
const money = z
  .string()
  .regex(/^-?\d+\.\d{2}$/, 'Valor monetário inválido — use duas casas decimais');
const quantity = z
  .string()
  .regex(/^\d+(?:\.\d{1,4})?$/, 'Quantidade inválida — use até quatro casas decimais');

export const modifierSchema = z.object({
  id: uuid,
  groupId: uuid,
  name: z.string().min(1),
  priceDelta: money,
  ingredientId: uuid.nullable(),
  quantity: quantity.nullable(),
  isAvailable: z.boolean(),
  sortOrder: z.number().int(),
});

export const modifierGroupSchema = z.object({
  id: uuid,
  name: z.string().min(1),
  minSelect: z.number().int().nonnegative(),
  maxSelect: z.number().int().nonnegative(),
  isRequired: z.boolean(),
  sortOrder: z.number().int(),
  modifiers: z.array(modifierSchema),
  productIds: z.array(uuid),
});

export const modifierGroupListResponseSchema = z.object({
  items: z.array(modifierGroupSchema),
});

export const productModifierGroupSchema = z.object({
  productId: uuid,
  groupId: uuid,
  sortOrder: z.number().int(),
});

export const createModifierGroupRequestSchema = z
  .object({
    name: z.string().trim().min(1, 'Informe um nome para o grupo').max(100),
    minSelect: z.number().int().nonnegative('A quantidade mínima não pode ser negativa'),
    maxSelect: z.number().int().nonnegative().max(100, 'Máximo de 100 opções por grupo'),
    isRequired: z.boolean().default(false),
    sortOrder: z.number().int().default(0),
  })
  .refine((value) => value.maxSelect >= value.minSelect, {
    message: 'A quantidade máxima não pode ser menor que a mínima',
    path: ['maxSelect'],
  })
  .refine((value) => !value.isRequired || value.minSelect >= 1, {
    message: 'Grupo obrigatório precisa exigir ao menos uma seleção',
    path: ['minSelect'],
  });

/**
 * `Nexora.Domain.Catalog.ModifierGroup` só expõe `UpdateSelectionRange` (mínimo/máximo) depois de
 * criado — não há como renomear nem alternar `isRequired` sem recriar o grupo (ver
 * `backend/src/Nexora.Contracts/Catalog/ModifierGroupRequests.cs`, mesma limitação documentada lá).
 * Este schema reflete só o que a API aceita hoje.
 */
export const updateModifierGroupRequestSchema = z
  .object({
    minSelect: z.number().int().nonnegative('A quantidade mínima não pode ser negativa'),
    maxSelect: z.number().int().nonnegative().max(100, 'Máximo de 100 opções por grupo'),
  })
  .refine((value) => value.maxSelect >= value.minSelect, {
    message: 'A quantidade máxima não pode ser menor que a mínima',
    path: ['maxSelect'],
  });

export const createModifierRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe um nome para o modificador').max(100),
  priceDelta: money,
  ingredientId: uuid.nullable().default(null),
  quantity: quantity.nullable().default(null),
  sortOrder: z.number().int().default(0),
});

export const updateModifierRequestSchema = z.object({
  priceDelta: money,
});

export const updateModifierAvailabilityRequestSchema = z.object({
  isAvailable: z.boolean(),
});

export const linkModifierGroupToProductRequestSchema = z.object({
  groupId: uuid,
  sortOrder: z.number().int().default(0),
});

export type Modifier = z.infer<typeof modifierSchema>;
export type ModifierGroup = z.infer<typeof modifierGroupSchema>;
export type ModifierGroupListResponse = z.infer<typeof modifierGroupListResponseSchema>;
export type ProductModifierGroup = z.infer<typeof productModifierGroupSchema>;
export type CreateModifierGroupRequest = z.infer<typeof createModifierGroupRequestSchema>;
export type UpdateModifierGroupRequest = z.infer<typeof updateModifierGroupRequestSchema>;
export type CreateModifierRequest = z.infer<typeof createModifierRequestSchema>;
export type UpdateModifierRequest = z.infer<typeof updateModifierRequestSchema>;
export type UpdateModifierAvailabilityRequest = z.infer<
  typeof updateModifierAvailabilityRequestSchema
>;
export type LinkModifierGroupToProductRequest = z.infer<
  typeof linkModifierGroupToProductRequestSchema
>;

/**
 * Validação pura de limite de seleção (US-012 §10 "contador de seleção restante visível", cenário
 * Gherkin "Limite máximo de seleção") — replicada aqui porque o carrinho de pedido real ainda não
 * existe como módulo (US-030/E-03). Usada pela tela de preview em
 * `apps/web-admin/src/modifiers/modifier-group-management-page.tsx` para simular a regra que o
 * futuro carrinho vai aplicar; a validação de servidor equivalente já existe hoje via
 * `ModifierGroup.MinSelect`/`MaxSelect` (persistidos e devolvidos aqui).
 */
export interface SelectionValidationResult {
  readonly canSelectMore: boolean;
  readonly meetsMinimum: boolean;
  readonly remaining: number;
}

export function validateModifierSelection(
  group: Pick<ModifierGroup, 'minSelect' | 'maxSelect'>,
  selectedCount: number,
): SelectionValidationResult {
  const remaining = Math.max(0, group.maxSelect - selectedCount);
  return {
    canSelectMore: selectedCount < group.maxSelect,
    meetsMinimum: selectedCount >= group.minSelect,
    remaining,
  };
}

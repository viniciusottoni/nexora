import { z } from 'zod';

/**
 * US-010 (Cadastrar categorias e produtos) — porta de `Nexora.Contracts.Catalog.Category*`
 * (backend/src/Nexora.Contracts/Catalog/CategoryRequests.cs, CategoryResponses.cs).
 */
export const createCategoryRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe um nome').max(100),
  description: z.string().trim().max(500).optional(),
  position: z.number().int().nonnegative().default(0),
});

export const updateCategoryRequestSchema = z
  .object({
    name: z.string().trim().min(1, 'Informe um nome').max(100).optional(),
    description: z.string().trim().max(500).optional(),
    position: z.number().int().nonnegative().optional(),
    isActive: z.boolean().optional(),
  })
  .refine(
    (value) =>
      value.name !== undefined ||
      value.description !== undefined ||
      value.position !== undefined ||
      value.isActive !== undefined,
    { message: 'Informe ao menos uma alteração' },
  );

export const reorderCategoriesRequestSchema = z.object({
  order: z.array(z.string().uuid()).min(1, 'Informe a nova ordem das categorias'),
});

export const categorySchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  description: z.string().nullable(),
  position: z.number().int(),
  isActive: z.boolean(),
  /** Contagem de produtos vinculados — usada para avisar o gestor antes de desativar uma categoria com itens ativos. */
  productCount: z.number().int().nonnegative(),
});

export const categoryListResponseSchema = z.object({
  items: z.array(categorySchema),
});

export type CreateCategoryRequest = z.infer<typeof createCategoryRequestSchema>;
export type UpdateCategoryRequest = z.infer<typeof updateCategoryRequestSchema>;
export type ReorderCategoriesRequest = z.infer<typeof reorderCategoriesRequestSchema>;
export type CategoryDto = z.infer<typeof categorySchema>;
export type CategoryListResponse = z.infer<typeof categoryListResponseSchema>;

import { z } from 'zod';

export const permissionCodes = [
  '*',
  'table:*',
  'table:open',
  'table:read',
  'table:transfer',
  'table:close_request',
  'table:close',
  'table:manage',
  'order:*',
  'order:create',
  'order:read',
  'order:add_item',
  'order:cancel_queued',
  'order:cancel_started',
  'order:override_price',
  'order:close_with_pending',
  'kds:*',
  'kds:read',
  'kds:advance',
  'kds:refire',
  'cash:*',
  'cash:open',
  'cash:close',
  'cash:movement',
  'cash:discount_limited',
  'cash:discount_any',
  'cash:close_divergent',
  'payment:*',
  'payment:register',
  'payment:refund',
  'stock:*',
  'stock:read',
  'stock:purchase',
  'stock:waste',
  'stock:count',
  'stock:adjust',
  'supplier:*',
  'recipe:*',
  'recipe:read',
  'recipe:write',
  'catalog:*',
  'catalog:read',
  'catalog:write',
  'catalog:set_unavailable',
  'report:*',
  'report:read',
  'report:read_own',
  'finance:*',
  'finance:read',
  'finance:write',
  'delivery:read_own',
  'delivery:advance',
  'user:*',
  'user:read',
  'user:write',
  'config:*',
  'config:read',
  'config:write',
  'device:*',
  'device:manage',
  'tenant:*',
  'tenant:manage',
  'audit:*',
  'audit:read',
] as const;

export const permissionCodeSchema = z.enum(permissionCodes, {
  errorMap: () => ({ message: 'Permissão desconhecida' }),
});

const permissionsSchema = z
  .array(permissionCodeSchema)
  .max(permissionCodes.length)
  .transform((values) => [...new Set(values)]);

export const createRoleRequestSchema = z.object({
  code: z
    .string()
    .trim()
    .min(2, 'Informe um código')
    .max(32)
    .regex(/^[A-Z][A-Z0-9_]*$/, 'Use letras maiúsculas, números e sublinhado'),
  name: z.string().trim().min(2, 'Informe um nome').max(100),
  permissions: permissionsSchema.default([]),
});

export const updateRoleRequestSchema = z
  .object({
    name: z.string().trim().min(2, 'Informe um nome').max(100).optional(),
    permissions: permissionsSchema.optional(),
  })
  .refine((value) => value.name !== undefined || value.permissions !== undefined, {
    message: 'Informe ao menos uma alteração',
  });

export const roleSchema = z.object({
  id: z.string().uuid(),
  code: z.string().min(1),
  name: z.string().min(1),
  permissions: z.array(permissionCodeSchema),
  system: z.boolean(),
  userCount: z.number().int().nonnegative(),
});

export const permissionCatalogItemSchema = z.object({
  code: permissionCodeSchema,
  resource: z.string().min(1),
  description: z.string().min(1),
  sensitive: z.boolean(),
});

export const roleListResponseSchema = z.object({
  items: z.array(roleSchema),
  permissionCatalog: z.array(permissionCatalogItemSchema),
});

export type PermissionCode = z.infer<typeof permissionCodeSchema>;
export type CreateRoleRequest = z.infer<typeof createRoleRequestSchema>;
export type UpdateRoleRequest = z.infer<typeof updateRoleRequestSchema>;
export type RoleDto = z.infer<typeof roleSchema>;
export type RoleListResponse = z.infer<typeof roleListResponseSchema>;
export type PermissionCatalogItem = z.infer<typeof permissionCatalogItemSchema>;

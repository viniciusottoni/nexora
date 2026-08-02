import { z } from 'zod';

const idSchema = z.string().uuid();
export const tenantSlugSchema = z
  .string()
  .min(1, 'Informe o endereço do estabelecimento.')
  .max(64)
  .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Use apenas letras minúsculas, números e hífens.');

export const createTenantRequestSchema = z.object({
  name: z.string().trim().min(2).max(160),
  slug: tenantSlugSchema,
  plan: z
    .string()
    .trim()
    .min(1)
    .max(32)
    .regex(/^[A-Z][A-Z0-9_]*$/),
  template: z.literal('PIZZERIA'),
  owner: z.object({
    name: z.string().trim().min(2).max(160),
    email: z.string().trim().email().max(254),
  }),
  store: z.object({
    name: z.string().trim().min(2).max(160),
    timezone: z
      .string()
      .trim()
      .min(1)
      .max(100)
      .refine((value) => {
        try {
          new Intl.DateTimeFormat('pt-BR', { timeZone: value }).format();
          return true;
        } catch {
          return false;
        }
      }, 'Informe um fuso horário IANA válido.'),
  }),
});

export const provisioningChecklistItemSchema = z.object({
  code: z.string().min(1),
  label: z.string().min(1),
  status: z.enum(['COMPLETED', 'PENDING']),
});

export const tenantSummarySchema = z.object({
  id: idSchema,
  slug: tenantSlugSchema,
  name: z.string().min(1),
  plan: z.string().min(1),
  status: z.enum(['PROVISIONED', 'INSTALLING', 'ACTIVE', 'SUSPENDED']),
  createdAt: z.string().datetime(),
});

export const createTenantResponseSchema = z.object({
  tenant: tenantSummarySchema.pick({ id: true, slug: true, status: true }),
  store: z.object({ id: idSchema, name: z.string().min(1) }),
  installToken: z.string().min(16),
  installCommand: z.string().startsWith('./install.sh --tenant='),
  ownerInviteSentTo: z.string().email(),
  checklist: z.array(provisioningChecklistItemSchema).length(9),
});

export const tenantListResponseSchema = z.object({ data: z.array(tenantSummarySchema) });
export const slugAvailabilityResponseSchema = z.object({
  slug: tenantSlugSchema,
  available: z.boolean(),
});

export const acceptOwnerInviteRequestSchema = z.object({
  token: z.string().min(32).max(256),
  password: z.string().min(12).max(128),
});

export type CreateTenantRequest = z.infer<typeof createTenantRequestSchema>;
export type CreateTenantResponse = z.infer<typeof createTenantResponseSchema>;
export type TenantSummaryContract = z.infer<typeof tenantSummarySchema>;
export type TenantListResponse = z.infer<typeof tenantListResponseSchema>;
export type SlugAvailabilityResponse = z.infer<typeof slugAvailabilityResponseSchema>;

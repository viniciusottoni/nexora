import { z } from 'zod';

const uuid = z.string().uuid();
const timestamp = z.coerce.date();
const nullableTimestamp = timestamp.nullable();
const money = z.coerce.string().regex(/^-?\d+(?:\.\d{1,4})?$/);

export const initialSyncStationSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    storeId: uuid,
    code: z.string(),
    name: z.string(),
    type: z.enum(['ASSEMBLY', 'OVEN', 'GRILL', 'FRY', 'BAR', 'DESSERT', 'OTHER']),
    capacitySlots: z.number().int().nullable(),
    avgCookSeconds: z.number().int().nullable(),
    sortOrder: z.number().int(),
    isActive: z.boolean(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncCategorySchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    name: z.string(),
    description: z.string().nullable(),
    sortOrder: z.number().int(),
    availableSchedule: z.unknown().nullable(),
    isActive: z.boolean(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncProductSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    categoryId: uuid,
    stationId: uuid.nullable(),
    name: z.string(),
    description: z.string().nullable(),
    ingredientsText: z.string().nullable(),
    allergens: z.array(z.string()),
    sortOrder: z.number().int(),
    isActive: z.boolean(),
    isAvailable: z.boolean(),
    unavailableReason: z.string().nullable(),
    unavailableSince: nullableTimestamp,
    allowsFractions: z.boolean(),
    maxFractions: z.number().int(),
    fractionGroup: z.string().nullable(),
    ncm: z.string().nullable(),
    cest: z.string().nullable(),
    cfop: z.string().nullable(),
    originCode: z.number().int().nullable(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncVariantSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    productId: uuid,
    name: z.string(),
    sku: z.string().nullable(),
    sizeCode: z.string().nullable(),
    prepMinutes: z.number().int(),
    isDefault: z.boolean(),
    isActive: z.boolean(),
    fiscalRates: z.unknown().nullable(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncPriceSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    variantId: uuid,
    channel: z.enum(['DINE_IN', 'DELIVERY', 'TAKEOUT', 'MARKETPLACE']),
    amount: money,
    validFrom: timestamp,
    validTo: nullableTimestamp,
    createdAt: timestamp,
    createdBy: uuid.nullable(),
  })
  .strict();

export const initialSyncModifierGroupSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    name: z.string(),
    minSelect: z.number().int(),
    maxSelect: z.number().int(),
    isRequired: z.boolean(),
    sortOrder: z.number().int(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncModifierSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    groupId: uuid,
    name: z.string(),
    priceDelta: money,
    ingredientId: uuid.nullable(),
    quantity: money.nullable(),
    isAvailable: z.boolean(),
    sortOrder: z.number().int(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncProductModifierGroupSchema = z
  .object({
    tenantId: uuid,
    productId: uuid,
    groupId: uuid,
    sortOrder: z.number().int(),
  })
  .strict();

export const initialSyncRoleSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    code: z.string(),
    name: z.string(),
    permissions: z.array(z.string()),
    isSystem: z.boolean(),
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncOperationalUserSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    name: z.string(),
    pinHash: z.string().min(1),
    pinLookup: z.string().min(1),
    status: z.enum(['ACTIVE', 'INACTIVE', 'BLOCKED']),
    pinRotatedAt: nullableTimestamp,
    createdAt: timestamp,
    updatedAt: timestamp,
    deletedAt: nullableTimestamp,
  })
  .strict();

export const initialSyncUserRoleSchema = z
  .object({
    id: uuid,
    tenantId: uuid,
    userId: uuid,
    roleId: uuid,
    storeId: uuid.nullable(),
  })
  .strict();

export const initialSyncCatalogSchema = z
  .object({
    stations: z.array(initialSyncStationSchema).default([]),
    categories: z.array(initialSyncCategorySchema).default([]),
    products: z.array(initialSyncProductSchema).default([]),
    variants: z.array(initialSyncVariantSchema).default([]),
    prices: z.array(initialSyncPriceSchema).default([]),
    modifierGroups: z.array(initialSyncModifierGroupSchema).default([]),
    modifiers: z.array(initialSyncModifierSchema).default([]),
    productModifierGroups: z.array(initialSyncProductModifierGroupSchema).default([]),
  })
  .strict();

export const initialSyncAuthorizationSchema = z
  .object({
    roles: z.array(initialSyncRoleSchema).default([]),
    users: z.array(initialSyncOperationalUserSchema).default([]),
    userRoles: z.array(initialSyncUserRoleSchema).default([]),
  })
  .strict();

export const initialSyncBootstrapPayloadSchema = z
  .object({
    configVersion: z.number().int().positive(),
    catalogVersion: z.number().int().positive().default(1),
    branding: z.record(z.unknown()),
    operation: z.record(z.unknown()),
    thresholds: z.record(z.unknown()),
    modules: z.record(z.unknown()),
    fiscal: z.record(z.unknown()),
    printers: z.array(z.unknown()),
    payments: z.record(z.unknown()),
    maintenance: z.record(z.unknown()),
    catalog: initialSyncCatalogSchema,
    authorization: initialSyncAuthorizationSchema.default({ roles: [], users: [], userRoles: [] }),
  })
  .strict();

export type InitialSyncBootstrapPayload = z.infer<typeof initialSyncBootstrapPayloadSchema>;

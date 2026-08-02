import { z } from 'zod';
export { validateBrandingContrast } from './branding-contrast.js';

const uuidSchema = z.string().uuid();
export const hexColorSchema = z
  .string()
  .regex(/^#[0-9A-F]{6}$/i, 'Use uma cor no formato #RRGGBB.');
const assetUrlSchema = z
  .string()
  .url()
  .refine((value) => value.startsWith('https://'), 'A mídia deve usar HTTPS.');

export const brandingColorsSchema = z.object({
  primary: hexColorSchema,
  secondary: hexColorSchema,
  surface: hexColorSchema,
  onPrimary: hexColorSchema,
});

export const brandingLogoSchema = z.object({
  light: assetUrlSchema.optional(),
  dark: assetUrlSchema.optional(),
});

export const brandingFontsSchema = z.object({
  body: z
    .string()
    .trim()
    .min(1)
    .max(80)
    .regex(/^[\p{L}\p{N} ._-]+$/u),
  display: z
    .string()
    .trim()
    .min(1)
    .max(80)
    .regex(/^[\p{L}\p{N} ._-]+$/u),
});

export const brandingTextsSchema = z.object({
  welcome: z.string().max(240),
  orderConfirmed: z.string().max(240),
  thanks: z.string().max(240),
  terms: z.string().max(20_000),
});

export const pwaIconSchema = z.object({
  src: assetUrlSchema,
  sizes: z.string().regex(/^\d+x\d+$/),
  type: z.enum(['image/png', 'image/webp']),
  purpose: z.enum(['any', 'maskable', 'any maskable']).optional(),
});

export const brandingPwaSchema = z.object({
  name: z.string().trim().min(1).max(45),
  shortName: z.string().trim().min(1).max(12),
  themeColor: hexColorSchema,
  icons: z.array(pwaIconSchema),
});

export const brandingSchema = z.object({
  colors: brandingColorsSchema,
  logo: brandingLogoSchema,
  favicon: assetUrlSchema.optional(),
  fonts: brandingFontsSchema,
  radius: z.number().int().min(0).max(32),
  texts: brandingTextsSchema,
  pwa: brandingPwaSchema,
});

export const brandingResponseSchema = z.object({
  tenant: z.object({ id: uuidSchema, name: z.string().min(1) }),
  branding: brandingSchema,
  configVersion: z.number().int().positive(),
});

const nonEmptyPartial = <T extends z.ZodRawShape>(schema: z.ZodObject<T>) =>
  schema.partial().refine((value) => Object.keys(value).length > 0, 'Informe ao menos um campo.');

export const updateBrandingRequestSchema = z
  .object({
    colors: nonEmptyPartial(brandingColorsSchema).optional(),
    logo: brandingLogoSchema.partial().optional(),
    favicon: assetUrlSchema.optional(),
    fonts: nonEmptyPartial(brandingFontsSchema).optional(),
    radius: z.number().int().min(0).max(32).optional(),
    texts: nonEmptyPartial(brandingTextsSchema).optional(),
    pwa: brandingPwaSchema.partial().optional(),
  })
  .refine((value) => Object.keys(value).length > 0, 'Informe ao menos uma alteração.');

export const uploadBrandingAssetRequestSchema = z
  .object({
    kind: z.enum(['LOGO_LIGHT', 'LOGO_DARK', 'FAVICON', 'PWA_ICON']),
    contentType: z.enum(['image/svg+xml', 'image/png', 'image/jpeg', 'image/webp']),
    bytes: z.number().int().positive().max(10_000_000),
    sha256: z.string().regex(/^[0-9a-f]{64}$/i),
  })
  .superRefine((value, context) => {
    if (
      (value.kind === 'FAVICON' || value.kind === 'PWA_ICON') &&
      value.contentType === 'image/svg+xml'
    ) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['contentType'],
        message: 'Ícones PWA devem ser rasterizados.',
      });
    }
  });

export const uploadBrandingAssetResponseSchema = z.object({
  assetId: uuidSchema,
  uploadUrl: z.string().url(),
  publicUrl: assetUrlSchema,
  expiresAt: z.string().datetime(),
});

export const brandingContrastSchema = z.object({
  valid: z.boolean(),
  minimumRatio: z.literal(4.5),
  issues: z.array(
    z.object({
      pair: z.enum(['primary/surface', 'onPrimary/primary']),
      ratio: z.number().positive(),
      suggested: hexColorSchema,
    }),
  ),
});

export const updateBrandingResponseSchema = brandingResponseSchema.extend({
  contrast: brandingContrastSchema,
});

export type Branding = z.infer<typeof brandingSchema>;
export type BrandingResponse = z.infer<typeof brandingResponseSchema>;
export type UpdateBrandingRequest = z.infer<typeof updateBrandingRequestSchema>;
export type UploadBrandingAssetRequest = z.infer<typeof uploadBrandingAssetRequestSchema>;

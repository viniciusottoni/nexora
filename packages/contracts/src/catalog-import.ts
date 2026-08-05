import { z } from 'zod';

/**
 * US-144 (Importação de cardápio por planilha) — porta de
 * `Nexora.Contracts.Catalog.CatalogImportContracts` (backend/src/Nexora.Contracts/Catalog/CatalogImportContracts.cs).
 *
 * Modelo de planilha (5 colunas, mínimo necessário — US-144 §15 "modelo de planilha complexo
 * demais faz o cliente errar e desistir"): `categoria`, `produto`, `descricao` (opcional),
 * `variacao` (opcional — vazio cria uma variação única implícita, mesmo nome do produto) e `preco`.
 */
export const catalogImportRowErrorSchema = z.object({
  row: z.number().int(),
  column: z.string(),
  message: z.string(),
});

export const catalogImportCountsSchema = z.object({
  categories: z.number().int().nonnegative(),
  products: z.number().int().nonnegative(),
  variants: z.number().int().nonnegative(),
});

export const catalogImportPreviewSchema = z.object({
  toCreate: catalogImportCountsSchema,
  toUpdate: catalogImportCountsSchema,
});

/** Corpo de `POST /v1/catalog/import/validate` — sempre 200; `valid: false` é um resultado esperado, não uma falha HTTP. */
export const catalogImportValidateResponseSchema = z.object({
  valid: z.boolean(),
  errors: z.array(catalogImportRowErrorSchema),
  preview: catalogImportPreviewSchema,
});

/**
 * Corpo de `POST /v1/catalog/import`. `valid: false` (HTTP 422) significa que nada foi gravado —
 * `created`/`updated` vêm zerados nesse caso; `valid: true` (HTTP 201) traz as contagens reais.
 */
export const catalogImportCommitResponseSchema = z.object({
  valid: z.boolean(),
  errors: z.array(catalogImportRowErrorSchema),
  created: catalogImportCountsSchema,
  updated: catalogImportCountsSchema,
  skipped: z.number().int().nonnegative(),
});

export type CatalogImportRowError = z.infer<typeof catalogImportRowErrorSchema>;
export type CatalogImportCounts = z.infer<typeof catalogImportCountsSchema>;
export type CatalogImportPreview = z.infer<typeof catalogImportPreviewSchema>;
export type CatalogImportValidateResponse = z.infer<typeof catalogImportValidateResponseSchema>;
export type CatalogImportCommitResponse = z.infer<typeof catalogImportCommitResponseSchema>;

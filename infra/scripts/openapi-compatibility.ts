import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

type Json = null | boolean | number | string | Json[] | { [key: string]: Json };
type ObjectJson = { [key: string]: Json };

function argument(name: string): string {
  const index = process.argv.indexOf(name);
  const value = index >= 0 ? process.argv[index + 1] : undefined;
  if (!value) throw new Error(`Argumento obrigatório ausente: ${name}`);
  return value;
}

function parseDocument(file: string): ObjectJson {
  try {
    const value: unknown = JSON.parse(readFileSync(resolve(file), 'utf8'));
    if (!value || typeof value !== 'object' || Array.isArray(value))
      throw new Error('raiz não é objeto');
    return value as ObjectJson;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`OpenAPI inválido em ${file}: ${message}`);
  }
}

function object(value: Json | undefined): ObjectJson {
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {};
}

function strings(value: Json | undefined): string[] {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === 'string')
    : [];
}

function breakingChanges(baseline: ObjectJson, current: ObjectJson): string[] {
  const changes: string[] = [];
  const baselinePaths = object(baseline.paths);
  const currentPaths = object(current.paths);
  const methods = ['get', 'post', 'put', 'patch', 'delete', 'options', 'head', 'trace'];

  for (const [path, baselinePathValue] of Object.entries(baselinePaths)) {
    const currentPath = object(currentPaths[path]);
    if (!(path in currentPaths)) {
      changes.push(`path publicado removido: ${path}`);
      continue;
    }
    const baselinePath = object(baselinePathValue);
    for (const method of methods) {
      if (method in baselinePath && !(method in currentPath)) {
        changes.push(`operação publicada removida: ${method.toUpperCase()} ${path}`);
        continue;
      }
      if (!(method in baselinePath)) continue;

      const baselineResponses = object(object(baselinePath[method]).responses);
      const currentResponses = object(object(currentPath[method]).responses);
      for (const status of Object.keys(baselineResponses)) {
        if (!(status in currentResponses)) {
          changes.push(`resposta publicada removida: ${method.toUpperCase()} ${path} ${status}`);
          continue;
        }
        const baselineContent = object(object(baselineResponses[status]).content);
        const currentContent = object(object(currentResponses[status]).content);
        for (const mediaType of Object.keys(baselineContent)) {
          if (!(mediaType in currentContent)) {
            changes.push(
              `media type publicado removido: ${method.toUpperCase()} ${path} ${status} ${mediaType}`,
            );
          }
        }
      }
    }
  }

  const baselineSchemas = object(object(baseline.components).schemas);
  const currentSchemas = object(object(current.components).schemas);
  for (const [schemaName, baselineSchemaValue] of Object.entries(baselineSchemas)) {
    if (!(schemaName in currentSchemas)) {
      changes.push(`schema publicado removido: ${schemaName}`);
      continue;
    }

    const baselineSchema = object(baselineSchemaValue);
    const currentSchema = object(currentSchemas[schemaName]);
    const baselineProperties = object(baselineSchema.properties);
    const currentProperties = object(currentSchema.properties);
    const baselineRequired = new Set(strings(baselineSchema.required));
    const currentRequired = new Set(strings(currentSchema.required));

    for (const property of Object.keys(baselineProperties)) {
      if (!(property in currentProperties)) {
        changes.push(`campo publicado removido: ${schemaName}.${property}`);
      } else if (baselineRequired.has(property) && !currentRequired.has(property)) {
        changes.push(
          `campo obrigatório publicado deixou de ser obrigatório: ${schemaName}.${property}`,
        );
      }
    }

    for (const property of currentRequired) {
      if (!baselineRequired.has(property)) {
        changes.push(`novo campo obrigatório em schema publicado: ${schemaName}.${property}`);
      }
    }
  }

  return changes;
}

function main(): void {
  const baselineFile = argument('--baseline');
  const currentFile = argument('--current');
  const changes = breakingChanges(parseDocument(baselineFile), parseDocument(currentFile));

  if (changes.length === 0) {
    console.log('Contrato OpenAPI compatível com o snapshot versionado.');
    return;
  }

  for (const change of changes) console.error(`::error title=Contrato OpenAPI::${change}`);
  console.error(
    'Quebra incompatível de contrato. Preserve /v1 de forma compatível ou publique a mudança em uma nova versão de path (/v2).',
  );
  process.exitCode = 1;
}

try {
  main();
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
}

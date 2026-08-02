import eslint from '@eslint/js';
import importPlugin from 'eslint-plugin-import';
import tseslint from 'typescript-eslint';

// O backend TypeScript (apps/api-edge, apps/api-cloud, packages/db|domain|events|metrics) foi
// removido — a implementação de backend agora é o backend/ .NET (ADR-036 a ADR-039), fora do
// escopo deste eslint. As zonas de fronteira abaixo cobrem só o que resta neste monorepo TS: os
// apps de frontend (apps/web-*) e os pacotes compartilhados restantes (packages/contracts,
// packages/ui).

export default tseslint.config(
  { ignores: ['**/dist/**', '**/coverage/**', '**/node_modules/**', '**/.turbo/**'] },
  eslint.configs.recommended,
  ...tseslint.configs.recommendedTypeChecked,
  {
    files: ['**/*.{ts,tsx,mts,cts}'],
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: { import: importPlugin },
    rules: {
      'import/no-restricted-paths': [
        'error',
        {
          basePath: import.meta.dirname,
          zones: [
            {
              target: './packages/ui',
              from: ['./apps'],
            },
          ],
        },
      ],
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['../../packages/*', '../../../packages/*', '../../../../packages/*'],
              message: 'Imports entre pacotes devem usar aliases @nexora/* (ADR-015).',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['packages/contracts/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@nestjs/*', '@prisma/*', 'react', 'react/*', '@nexora/ui'],
              message:
                'packages/contracts não pode depender de infraestrutura ou interface (ADR-015).',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['packages/ui/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@nestjs/*', '@prisma/*'],
              message:
                'Camada web não pode depender de bibliotecas exclusivas de backend (ADR-015).',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['apps/web-*/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@nestjs/*', '@prisma/*'],
              message:
                'Camada web não pode depender de bibliotecas exclusivas de backend (ADR-015).',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.{test,spec}.{ts,tsx}', '**/tests/**/*.{ts,tsx}'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/require-await': 'off',
      '@typescript-eslint/unbound-method': 'off',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_', caughtErrorsIgnorePattern: '^_' },
      ],
    },
  },
);

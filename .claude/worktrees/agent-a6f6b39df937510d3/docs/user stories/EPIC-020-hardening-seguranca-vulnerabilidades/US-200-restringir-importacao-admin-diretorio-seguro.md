---
title: US-200 — Restringir importação admin a diretório seguro
sidebar_position: 200
---

# US-200 — Restringir importação admin a diretório seguro

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-200 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P1 |
| Fase | Pré-teste aberto |
| Perfil principal | Admin interno, backend e segurança |
| Plano | Admin interno |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | AdminExercisesController, ImportExercisesCommandHandler |
| Status | Planejada |

## 2. História do usuário

Como **administrador interno**,

quero **importar exercícios apenas de uma área controlada do servidor**,

para **evitar leitura ou processamento acidental de arquivos fora do escopo permitido**.

## 3. Contexto

A importação admin recebe um diretório via request e o backend lê arquivos locais desse caminho. Mesmo protegido por RBAC, esse padrão aumenta impacto caso uma conta admin seja comprometida ou usada de forma incorreta.

## 4. Objetivo

Restringir importação de exercícios a um diretório raiz seguro configurado no backend, sem aceitar caminhos arbitrários do cliente.

## 5. Escopo

### Entra nesta US

- Criar configuração `ExerciseImport:RootDirectory`.
- Remover aceitação de path absoluto livre no endpoint admin.
- Aceitar apenas identificador de lote/subdiretório relativo permitido.
- Validar path normalizado dentro da raiz configurada.
- Rejeitar tentativas de traversal ou path fora da raiz.
- Evitar persistir caminho físico completo quando não necessário.

### Fora desta US

- Upload de arquivo pelo admin.
- Interface visual do site admin.
- Importação assíncrona por fila.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cliente não pode enviar caminho absoluto para importação. |
| RN-002 | Todo caminho resolvido deve permanecer dentro da raiz permitida. |
| RN-003 | Path inválido deve retornar erro 400/422 sem revelar estrutura interna do servidor. |
| RN-004 | Apenas Admin pode executar importação. |
| RN-005 | Auditoria deve registrar lote/importação, não caminho sensível completo. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário comum | Não pode importar. |
| Premium/Trial | Não pode importar. |
| Admin interno | Pode importar de lote permitido. |
| Sistema/Worker | Pode processar lote previamente validado. |

## 8. Fluxo principal

1. Admin solicita importação informando um `batchKey` ou subpasta permitida.
2. Backend resolve o caminho dentro da raiz configurada.
3. Backend valida que o caminho final não saiu da raiz.
4. Backend enumera arquivos permitidos.
5. Backend processa importação e registra auditoria.

## 9. Fluxos alternativos

- Subpasta inexistente: retorna 404/422.
- Path com traversal: retorna 400/422.
- Raiz não configurada em produção: startup falha pela US-197.

## 10. Estados esperados

- lote válido;
- lote inexistente;
- path inválido;
- acesso negado;
- importação concluída;
- importação parcial;
- erro inesperado com correlationId.

## 11. Impacto no Frontend Flutter

Sem impacto no app mobile.

## 12. Impacto no Backend

- Alterar contrato de `ImportExercisesRequest`.
- Ajustar `AdminExercisesController`.
- Ajustar `ImportExercisesCommandHandler`.
- Criar serviço de resolução segura de diretório.
- Adicionar testes de path permitido/proibido.

## 13. Impacto no Banco de Dados

- Avaliar remoção/mascaramento de `sourceFilePath` físico em importações.
- Manter lote, provider e status.

## 14. Impacto em Gamificação

Indireto: catálogo de exercícios permanece íntegro e confiável.

## 15. Impacto em Monetização

Sem impacto direto.

## 16. Impacto em Internacionalização

Mensagens admin podem ficar inicialmente em PT-BR, com preparo para EN/ES/FR no futuro.

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/import
```

Request sugerido:

```json
{
  "batchKey": "exercisedb-2026-06",
  "provider": "local_files",
  "maxFiles": 100,
  "approveOnImport": false
}
```

## 18. Eventos de Analytics

Não aplicável ao app. Auditoria operacional obrigatória.

## 19. Critérios de aceite

- Admin consegue importar lote permitido.
- Path absoluto enviado pelo cliente é rejeitado.
- Path com traversal é rejeitado.
- Usuário não-admin recebe 403.
- Erro não revela estrutura interna do servidor.

## 20. Critérios de teste para QA

- lote válido;
- lote inexistente;
- path absoluto;
- traversal;
- não-admin;
- raiz ausente em produção;
- auditoria sem path sensível completo.

## ✅ Decisão registrada

Importação admin deve operar apenas sobre diretório raiz controlado pelo backend, nunca sobre caminho arbitrário informado pelo cliente.
---
title: US-209 — Aplicar cursor pagination e limites de page size
sidebar_position: 209
---

# US-209 — Aplicar cursor pagination e limites de page size

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-209 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P1 |
| Fase | MVP em produção / antes de histórico crescer |
| Perfil principal | Usuário premium, backend e Flutter |
| Plano | Mensal e Anual, com impacto indireto no Trial |
| Dependência principal | BattleLog, listagens, contratos de API |
| Status | Planejada |

## 2. História do usuário

Como **usuário que consulta histórico e listagens**,

quero **navegar por registros antigos sem lentidão progressiva**,

para **manter a experiência rápida mesmo depois de meses de uso**.

## 3. Contexto

Paginação por página e offset funciona no início, mas degrada em páginas profundas. Para o MVP, as listagens devem ter limite máximo e, nas áreas com crescimento contínuo, devem evoluir para paginação por cursor.

## 4. Objetivo

Padronizar paginação segura nas listagens críticas e preparar o histórico para crescimento.

## 5. Escopo

### Entra nesta US

- Definir page size máximo global por endpoint.
- Proteger page/pageSize contra valores abusivos.
- Implementar cursor pagination no Battle Log.
- Preparar padrão para histórico, notificações, pedidos e suporte.
- Atualizar contrato do Flutter quando necessário.
- Garantir ordenação estável por data e id.

### Fora desta US

- Busca textual avançada.
- Filtros complexos do admin.
- Data lake/analytics.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nenhuma listagem pode aceitar page size ilimitado. |
| RN-002 | Cursor deve ser opaco ou validado pelo backend. |
| RN-003 | Ordenação deve ser estável para evitar duplicidade ou salto de item. |
| RN-004 | Histórico antigo não pode ficar progressivamente mais lento por offset profundo. |
| RN-005 | Mudança de contrato deve manter compatibilidade quando possível. |

## 7. Contrato sugerido

```txt
GET /api/hunter/battle-log?cursor={cursor}&limit=20
```

Resposta sugerida:

```json
{
  "items": [],
  "nextCursor": "string|null",
  "hasMore": true
}
```

## 8. Fluxo principal

1. App solicita primeira página sem cursor.
2. Backend retorna itens e próximo cursor.
3. App solicita próxima página com cursor.
4. Backend busca itens após o marcador estável.
5. Processo continua até `hasMore=false`.

## 9. Impacto no Backend

- Ajustar Battle Log para cursor.
- Criar helper/contrato padrão de paginação.
- Validar limites em controllers/validators.
- Ajustar índices conforme US-208.

## 10. Impacto no Flutter

- Ajustar datasource/repository do histórico.
- Guardar `nextCursor` no estado da tela.
- Implementar carregamento incremental.

## 11. Critérios de aceite

- Page size máximo é aplicado.
- Battle Log usa cursor ou possui caminho de migração definido.
- Ordenação é estável.
- Cursor inválido retorna erro controlado.
- Páginas profundas não usam offset pesado.

## 12. Critérios de teste para QA

- primeira página;
- próxima página;
- cursor inválido;
- limite acima do permitido;
- histórico com muitos registros;
- regressão visual no app.

## ✅ Decisão registrada

Listagens do MVP devem ter limites explícitos, e histórico crescente deve usar cursor pagination para evitar degradação conforme a base aumenta.
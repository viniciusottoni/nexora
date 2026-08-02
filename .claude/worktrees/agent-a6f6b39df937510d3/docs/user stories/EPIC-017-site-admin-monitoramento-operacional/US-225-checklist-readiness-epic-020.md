---
title: US-225 — Visualizar checklist de readiness da EPIC-020
sidebar_position: 225
---

# US-225 — Visualizar checklist de readiness da EPIC-020

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-225 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-194 a US-215 |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | Produto, Engenharia, QA e DevOps |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **responsável pelo MVP**,

quero **visualizar um checklist das US-194 a US-215**,

para **saber quais itens obrigatórios já estão prontos, pendentes ou bloqueados antes do lançamento**.

## 3. Contexto

A EPIC-020 virou bloqueadora do MVP e possui várias USes técnicas. Sem um checklist visível, o time pode perder clareza do que ainda falta para abrir teste público. O Admin deve traduzir esse backlog técnico em status operacional claro.

## 4. Objetivo

Criar uma tela de checklist da EPIC-020, com status por US, evidência, responsável e bloqueadores.

## 5. Escopo

### Entra nesta US

- Lista das US-194 a US-215 com status.
- Agrupamento por domínio: monetização, configuração, autenticação, performance, rotinas, observabilidade e capacidade.
- Evidência por item: teste, endpoint, dashboard, relatório, CI ou execução validada.
- Status: não iniciado, em andamento, pronto, bloqueado, dispensado com justificativa.
- Indicação de itens bloqueadores do teste aberto.
- Exportação simples do checklist.

### Fora desta US

- Substituir ferramenta de gestão de projeto.
- Alterar status automaticamente sem integração definida.
- Permitir editar código ou configuração pelo Admin.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | US P0 pendente deve bloquear readiness do MVP. |
| RN-002 | Item marcado como pronto deve ter evidência associada. |
| RN-003 | Item dispensado deve ter justificativa e responsável. |
| RN-004 | Histórico de alteração de status deve ser auditado. |
| RN-005 | Exportação não pode incluir dados sensíveis. |

## 7. Domínios mínimos

- Assinatura e IAP.
- Configuração e build.
- Autenticação e autorização.
- Auditoria.
- Cache e banco.
- Rotinas e workers.
- Observabilidade.
- Mídia/CDN.
- Teste de carga.

## 8. Fluxo principal

1. Admin acessa Checklist EPIC-020.
2. Sistema lista USes e status.
3. Admin filtra por domínio ou prioridade.
4. Admin abre detalhe do item.
5. Admin vê evidências, bloqueadores e responsável.
6. Admin exporta checklist para decisão go/no-go.

## 9. Impacto no Frontend

- Nova página `Checklist MVP` ou aba em Saúde do MVP.
- Tabela com filtros, chips de status e resumo por domínio.
- Modal/drawer de evidência por US.

## 10. Impacto no Backend

- Endpoint admin para checklist de readiness.
- Modelo de status/evidência por item.
- Auditoria de alteração de status, se houver edição pelo Admin.

## 11. Critérios de aceite

- Admin visualiza US-194 a US-215 com status.
- P0 pendente aparece como bloqueador.
- Item pronto possui evidência.
- Checklist pode ser filtrado por domínio.
- Exportação simples está disponível.
- Alterações ficam auditadas quando houver edição.

## 12. Critérios de teste para QA

- checklist completo;
- item P0 pendente;
- item pronto com evidência;
- item dispensado com justificativa;
- filtro por domínio;
- exportação sem dados sensíveis.

## ✅ Decisão registrada

A EPIC-020 deve ficar visível no Admin como checklist de readiness do MVP, conectando backlog técnico, evidências e decisão de abertura pública.
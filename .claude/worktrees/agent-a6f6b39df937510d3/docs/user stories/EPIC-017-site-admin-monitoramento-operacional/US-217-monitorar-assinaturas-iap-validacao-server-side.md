---
title: US-217 — Monitorar assinaturas e IAP com validação server-side
sidebar_position: 217
---

# US-217 — Monitorar assinaturas e IAP com validação server-side

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-217 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-194, US-195 |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Admin, Produto, Segurança, Suporte e Financeiro operacional |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**,

quero **acompanhar assinaturas, IAPs e validações server-side em uma tela operacional**,

para **detectar falhas, transações pendentes, divergências e tentativas suspeitas antes que afetem usuários ou receita**.

## 3. Contexto

As US-194 e US-195 tornam o backend a autoridade final para assinatura e IAP. O Admin precisa visualizar o estado desse fluxo: eventos recebidos, validações aprovadas, validações negadas, concessões pendentes, falhas de provider e divergências entre app, backend e RevenueCat.

## 4. Objetivo

Criar uma tela ou aba de diagnóstico financeiro operacional para assinatura premium e compras IAP.

## 5. Escopo

### Entra nesta US

- Cards com validações aprovadas, negadas, pendentes e com falha.
- Lista de eventos de assinatura e IAP por período.
- Filtros por tipo, loja, status, plano, produto, ambiente e usuário.
- Detalhe seguro de uma validação sem expor dados sensíveis.
- Destaque para transações repetidas, transações sem validação e concessões pendentes.
- Link para audit log e usuário afetado.

### Fora desta US

- Alteração manual de assinatura.
- Concessão manual de item no MVP.
- Gestão fiscal/contábil.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A tela deve deixar claro que o backend é a fonte de verdade. |
| RN-002 | Transação pendente deve ficar visível até validação ou expiração. |
| RN-003 | Transação negada não pode aparecer como compra concluída. |
| RN-004 | Dados sensíveis de provider devem ser mascarados ou omitidos. |
| RN-005 | Divergência entre app, backend e provider deve gerar alerta operacional. |

## 7. Indicadores mínimos

- Validações de assinatura aprovadas.
- Validações de IAP aprovadas.
- Validações negadas.
- Pendências por mais de X minutos.
- Falhas por provider.
- Tentativas repetidas por usuário/transação.
- Concessões de item aguardando confirmação.

## 8. Fluxo principal

1. Admin acessa a tela financeira operacional.
2. Sistema exibe cards e lista de eventos.
3. Admin filtra por período/status/produto.
4. Admin abre detalhe de uma validação.
5. Admin navega para usuário, audit log ou alerta relacionado.

## 9. Impacto no Frontend

- Criar página ou aba `Assinaturas e IAP` dentro do Admin.
- Adicionar item de navegação ou link em Saúde do MVP.
- Criar tabela com filtros e detalhe em modal/drawer.

## 10. Impacto no Backend

- Criar endpoints admin de leitura para eventos de assinatura/IAP.
- Retornar dados agregados e detalhes seguros.
- Integrar com logs/auditoria da US-194 e US-195.

## 11. Critérios de aceite

- Admin vê volume de validações por status.
- Admin consegue filtrar eventos por produto/plano/status/período.
- Validações negadas e pendentes ficam destacadas.
- Detalhe não expõe payload sensível.
- Evento permite navegar para audit log e usuário afetado.

## 12. Critérios de teste para QA

- assinatura aprovada;
- assinatura negada;
- IAP aprovado;
- IAP pendente;
- transação repetida;
- falha do provider;
- usuário com múltiplos eventos.

## ✅ Decisão registrada

O Admin precisa mostrar a saúde da monetização server-side para prevenir fraude, falhas de concessão e perda de receita no MVP.
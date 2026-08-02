---
title: US-216 — Visualizar dashboard de saúde do MVP e hardening
sidebar_position: 216
---

# US-216 — Visualizar dashboard de saúde do MVP e hardening

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-216 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | EPIC-020 — US-194 a US-215 |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | Admin, Engenharia, DevOps, Segurança e Produto |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **admin responsável pelo MVP**,

quero **visualizar um painel consolidado de saúde, segurança e prontidão operacional**,

para **saber rapidamente se o AWAKEN pode continuar operando ou abrir para mais usuários**.

## 3. Contexto

O Admin atual possui dashboard operacional com usuários, DAU, tickets, MRR, atividade recente e top eventos. Após as US-194 a US-215, o MVP precisa também mostrar sinais preventivos de segurança, performance e escala: validações financeiras, configurações críticas, cache, jobs, worker, migrations, CDN, observabilidade e teste de carga.

## 4. Objetivo

Criar uma visão executiva de readiness do MVP, com status por domínio e indicação clara de bloqueadores.

## 5. Escopo

### Entra nesta US

- Novo painel ou seção no Dashboard chamada **Saúde do MVP**.
- Cards de status por domínio: Segurança, Assinaturas/IAP, Configuração, Cache, Jobs, Banco, Redis, Worker, Mídia/CDN, Observabilidade e Teste de Carga.
- Indicador visual: saudável, atenção, crítico e sem dados.
- Lista de bloqueadores P0 ativos.
- Última atualização dos sinais.
- Link rápido para telas de detalhe.

### Fora desta US

- Correção automática de falhas.
- BI avançado.
- Substituição de ferramentas externas de observabilidade.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O painel deve priorizar sinais P0 do MVP. |
| RN-002 | Qualquer domínio crítico deve deixar o status geral como crítico. |
| RN-003 | Sinal sem dados deve aparecer explicitamente como sem dados, nunca como saudável. |
| RN-004 | O painel não pode expor credenciais, tokens ou payloads sensíveis. |
| RN-005 | Cada card deve indicar quando foi atualizado pela última vez. |

## 7. Domínios mínimos

- Assinatura e IAP server-side.
- Configurações críticas.
- Google Sign-In e autenticação.
- Rate limit e bloqueios.
- Status de acesso e cache.
- Catálogo/produtos em cache.
- Jobs recorrentes.
- API/Worker.
- Banco e Redis.
- Observabilidade.
- Mídia/CDN.
- Teste de carga.

## 8. Fluxo principal

1. Admin acessa `/admin/dashboard` ou nova rota de saúde.
2. Sistema carrega sinais agregados do backend.
3. Cards mostram status por domínio.
4. Admin identifica bloqueadores.
5. Admin clica no domínio para abrir detalhe.

## 9. Impacto no Frontend

- Adicionar seção ao Dashboard atual ou nova página no menu.
- Criar componente reutilizável de status de domínio.
- Usar padrão visual atual dark/RPG do Admin.
- Criar hook `useMvpHealth`.

## 10. Impacto no Backend

- Criar endpoint agregado de saúde do MVP.
- Consolidar sinais de segurança, performance e operação.
- Retornar apenas dados resumidos e seguros.

## 11. Critérios de aceite

- Admin visualiza status geral do MVP.
- Admin visualiza status por domínio.
- Domínios críticos ficam destacados.
- Itens sem telemetria aparecem como sem dados.
- Cada item possui link para detalhe.
- Não há exposição de segredo ou payload sensível.

## 12. Critérios de teste para QA

- cenário tudo saudável;
- cenário com domínio crítico;
- cenário sem dados;
- clique para detalhe;
- erro de carregamento;
- responsividade básica.

## ✅ Decisão registrada

O Admin deve ter uma visão consolidada de saúde do MVP para diagnóstico e prevenção, conectando as entregas das US-194 a US-215 em uma leitura operacional única.
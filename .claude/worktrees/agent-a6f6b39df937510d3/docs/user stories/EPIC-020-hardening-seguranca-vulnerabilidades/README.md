---
title: EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP
sidebar_position: 20
---

# EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-020 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Prioridade | P0 / P1 |
| Perfil principal | Engenharia, Segurança, Backend, Flutter, DevOps, QA e Produto |
| Planos impactados | Trial, Mensal, Anual, Assinatura expirada e Admin |
| Plataforma | Flutter Android + Backend .NET 10 + PostgreSQL + Redis + Hangfire + RevenueCat |
| Status | Planejado |

## 2. Objetivo

Fechar as brechas identificadas na revisão da branch `master` e preparar o AWAKEN para o MVP em produção, protegendo o sistema contra fraude, configuração insegura, gargalos de banco, excesso de processamento síncrono, jobs não escaláveis, builds mobile incorretos, inconsistência na economia Gold e ausência de observabilidade mínima.

## 3. Contexto

A revisão da `master` identificou que o produto já possui boas bases de segurança e arquitetura: JWT, BCrypt, CORS com allowlist em produção, headers básicos, RBAC admin, PostgreSQL, Redis, Hangfire e armazenamento seguro de tokens no app. Porém, os fluxos recém-adicionados de economia, loja, IAP, assinatura, admin, geração de quest, notificações e configurações críticas introduziram superfícies sensíveis e gargalos que precisam ser tratados antes de qualquer abertura pública.

O maior risco de segurança é o backend aceitar informações financeiras vindas do cliente como fonte de verdade. Assinaturas, IAP e compra de Gold devem ser validados no servidor com RevenueCat, Google Play/App Store ou webhooks assinados. O maior risco de performance é o sistema executar consultas repetidas por request, carregar listas inteiras em memória, rodar jobs sem paginação/batching e permitir configurações de produção sem controles mínimos.

A EPIC-020 concentra o hardening obrigatório para o MVP: não é uma otimização futura. É a base para que o app possa ser publicado com segurança, previsibilidade operacional e caminho real de crescimento.

## 4. Escopo

### Entra neste épico

- Validação server-side de assinatura premium via RevenueCat.
- Validação server-side de IAP consumível/slot antes da concessão de item.
- Validação server-side de compra de Gold antes de creditar carteira.
- Atomicidade entre Gold, pedido, ledger e inventário.
- Reconciliação da economia Gold com alertas operacionais.
- Remoção, rotação e prevenção de credenciais versionadas.
- Fail-fast de configurações críticas do backend.
- Hardening de Google Sign-In com audience obrigatória e e-mail verificado.
- Rate limit particionado por IP, e-mail/usuário e proteção de sessão.
- Restrição da importação admin de exercícios a diretório seguro.
- Bloqueio de usuário autenticado sem claim `sub`/UserId válida.
- Guard de build release no Flutter para impedir configuração local, insegura ou de teste.
- Serialização segura de metadados de auditoria.
- Pipeline de segurança com Dependabot, verificação de credenciais, SAST e auditoria de dependências.
- Cache de status de acesso para reduzir queries em toda request autenticada.
- Cache de catálogo aprovado de exercícios e produtos ativos de loja.
- Refatoração de jobs recorrentes com paginação, batching e execução idempotente.
- Índices críticos, `AsNoTracking`, projeções e remoção de `GetAll` perigosos.
- Paginação por cursor e limites máximos de page size em listagens.
- Separação entre API e Worker Hangfire, com filas por tipo de carga.
- Remoção de migration automática no startup em produção.
- Controle single-flight de refresh no Flutter para evitar rajadas de renovação.
- Observabilidade mínima de MVP: logs, métricas, tracing, p95/p99, jobs e health/readiness.
- CDN/storage para mídia de exercícios e assets estáticos.
- Teste de carga e plano de capacidade do MVP.

### Fora deste épico

- Implementação completa do site admin.
- Alteração visual da loja.
- Criação de novos produtos de loja.
- Mudança de preço dos planos.
- Política comercial de reembolso.
- Antifraude avançado com machine learning.
- Pentest externo formal.
- Sharding de banco de dados.
- Read replicas obrigatórias no MVP.
- Kubernetes obrigatório no MVP.
- Marketplace entre usuários.
- Transferência de Gold entre usuários.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-194 | Validar assinatura premium server-side via RevenueCat | P0 | [Abrir](./US-194-validar-assinatura-server-side-revenuecat.md) |
| US-195 | Validar IAP consumível e slot server-side antes de conceder item | P0 | [Abrir](./US-195-validar-iap-server-side-concessao-item.md) |
| US-196 | Remover credenciais versionadas e ativar prevenção de vazamento | P0 | [Abrir](./US-196-remover-segredos-versionados-secret-scanning.md) |
| US-197 | Validar configurações críticas no startup do backend | P0 | [Abrir](./US-197-validar-configuracoes-criticas-startup.md) |
| US-198 | Fortalecer Google Sign-In com audience obrigatória e e-mail verificado | P0 | [Abrir](./US-198-hardening-google-signin-audience-email-verificado.md) |
| US-199 | Implementar rate limit particionado, lockout e proteção de refresh-token | P0 | [Abrir](./US-199-rate-limit-particionado-lockout-refresh-token.md) |
| US-200 | Restringir importação admin a diretório seguro | P1 | [Abrir](./US-200-restringir-importacao-admin-diretorio-seguro.md) |
| US-201 | Exigir claim válida de usuário autenticado | P0 | [Abrir](./US-201-exigir-claim-usuario-autenticado-valida.md) |
| US-202 | Bloquear build release mobile com configuração insegura | P0 | [Abrir](./US-202-bloquear-release-mobile-configuracao-insegura.md) |
| US-203 | Serializar metadados de auditoria com JSON seguro | P1 | [Abrir](./US-203-serializar-metadados-auditoria-json-seguro.md) |
| US-204 | Automatizar segurança de dependências, SAST e análise no CI | P1 | [Abrir](./US-204-automatizar-seguranca-dependencias-sast-ci.md) |
| US-205 | Cachear status de acesso e reduzir consultas por request | P0 | [Abrir](./US-205-cachear-status-acesso-reduzir-queries.md) |
| US-206 | Cachear catálogo aprovado e produtos ativos | P0 | [Abrir](./US-206-cachear-catalogo-produtos-ativos.md) |
| US-207 | Refatorar jobs recorrentes com paginação e batching | P0 | [Abrir](./US-207-jobs-recorrentes-paginacao-batching.md) |
| US-208 | Criar índices críticos e otimizar consultas de leitura | P0 | [Abrir](./US-208-indices-criticos-consultas-leitura.md) |
| US-209 | Aplicar cursor pagination e limites de page size | P1 | [Abrir](./US-209-cursor-pagination-limites-page-size.md) |
| US-210 | Separar API e Worker Hangfire com filas por carga | P0 | [Abrir](./US-210-separar-api-worker-hangfire-filas.md) |
| US-211 | Remover migrations automáticas do startup em produção | P0 | [Abrir](./US-211-remover-migrations-startup-producao.md) |
| US-212 | Implementar single-flight refresh no Flutter | P0 | [Abrir](./US-212-single-flight-refresh-flutter.md) |
| US-213 | Implantar observabilidade mínima de performance e escala | P0 | [Abrir](./US-213-observabilidade-minima-performance-escala.md) |
| US-214 | Servir mídia e assets por CDN/storage com cache | P1 | [Abrir](./US-214-cdn-storage-midia-assets-cache.md) |
| US-215 | Executar teste de carga e definir plano de capacidade do MVP | P0 | [Abrir](./US-215-teste-carga-plano-capacidade-mvp.md) |
| US-226 | Validar compra de Gold server-side antes de creditar carteira | P0 | [Abrir](./US-226-validar-compra-gold-server-side.md) |
| US-227 | Garantir atomicidade entre Gold, pedido e inventário | P0 | [Abrir](./US-227-atomicidade-economia-gold.md) |
| US-228 | Reconciliar Gold, ledger, pedidos e inventário com alertas antifraude | P0 | [Abrir](./US-228-reconciliacao-antifraude-gold.md) |

## 6. Regras de negócio do épico

| ID | Regra |
|---|---|
| RN-EPIC-020-001 | Nenhum acesso premium pode ser concedido apenas com dados enviados pelo app. |
| RN-EPIC-020-002 | Nenhum item IAP pode ser concedido sem validação server-side da transação. |
| RN-EPIC-020-003 | Produção não pode iniciar com credenciais vazias, placeholders ou valores default de teste. |
| RN-EPIC-020-004 | Token Google só é aceito se audience/client id for esperado e e-mail estiver verificado. |
| RN-EPIC-020-005 | Endpoints de autenticação e sessão devem ter rate limit particionado e proteção contra excesso de chamadas. |
| RN-EPIC-020-006 | Endpoints admin não podem aceitar caminhos arbitrários fora de diretórios explicitamente permitidos. |
| RN-EPIC-020-007 | Requisição autenticada sem usuário válido deve ser rejeitada como 401/403, nunca operar como `Guid.Empty`. |
| RN-EPIC-020-008 | Build mobile release deve falhar quando usar configuração local, insegura ou de teste. |
| RN-EPIC-020-009 | Logs e auditoria devem serializar metadados de forma segura, sem interpolação manual de JSON. |
| RN-EPIC-020-010 | O CI deve detectar credenciais, dependências vulneráveis e padrões inseguros antes do merge. |
| RN-EPIC-020-011 | Rotas autenticadas frequentes não devem consultar banco em toda request para status de acesso quando houver cache válido. |
| RN-EPIC-020-012 | Catálogo de exercícios e produtos ativos devem usar cache com invalidação controlada. |
| RN-EPIC-020-013 | Jobs recorrentes devem processar dados em lotes, de forma idempotente e sem carregar toda a base em memória. |
| RN-EPIC-020-014 | Listagens devem ter limite máximo de tamanho e paginação segura. |
| RN-EPIC-020-015 | API web não deve competir recursos com workers pesados de background em produção. |
| RN-EPIC-020-016 | Migrações de banco em produção devem ser executadas por etapa controlada, não por todas as réplicas no startup. |
| RN-EPIC-020-017 | O app mobile não pode disparar múltiplas renovações de sessão simultâneas para a mesma expiração. |
| RN-EPIC-020-018 | Produção deve ter métricas mínimas de latência, erro, banco, cache, filas e jobs antes do teste aberto. |
| RN-EPIC-020-019 | Mídia de exercício deve ser servida por storage/CDN, não pela API principal. |
| RN-EPIC-020-020 | O MVP só pode abrir para público após teste de carga mínimo com metas de p95/p99 e erro definidas. |
| RN-EPIC-020-021 | Compra de Gold com dinheiro real deve ser validada server-side antes de creditar carteira. |
| RN-EPIC-020-022 | O app nunca define quantidade de Gold, preço, saldo ou concessão de item. |
| RN-EPIC-020-023 | Débito de Gold, ledger, pedido e inventário devem confirmar juntos ou falhar juntos. |
| RN-EPIC-020-024 | Economia Gold deve ter reconciliação periódica e alertas de divergência. |

## 7. Impactos técnicos

### Flutter

- Guard de configuração em release.
- Integração com RevenueCat mantendo o app como iniciador de compra, não fonte de verdade.
- Tratamento de estados de compra pendente, validação falha, concessão pendente e sync posterior.
- Controle single-flight para renovação de sessão.
- Testes de build/configuração e fluxos negativos.
- Cache local de mídia quando aplicável.
- Fluxo de compra de Gold sem envio de quantidade/preço pelo app.
- Sincronização de carteira e inventário após compra.

### Backend

- Webhook/validação RevenueCat para assinatura, IAP e compra de Gold.
- Fail-fast de configurações críticas.
- Rate limit particionado e lockout.
- Validação rígida de claim de usuário.
- Import admin com diretório base seguro.
- Auditoria com JSON serializado.
- Cache Redis para status de acesso, catálogo e produtos ativos.
- Queries de leitura com `AsNoTracking`, projeções e paginação.
- Jobs com paginação, batching e idempotência.
- Separação de processo API/Worker e filas Hangfire.
- Transação atômica para compra com Gold.
- Reconciliador de economia Gold.

### Banco de dados

- Eventos/webhooks de RevenueCat, status de validação, payload seguro e idempotência.
- Índices únicos para transações externas por loja/provider.
- Índices compostos para quests pendentes, battle log, notificações, catálogo e produtos ativos.
- Índices para ledger, pedidos Gold, referência externa e reconciliação.
- Tabelas/colunas de lockout e tentativas de login, se necessário.
- Migrações executadas por pipeline/job controlado.

### DevOps / CI

- GitHub Dependabot.
- Verificação de credenciais versionadas.
- Auditoria de dependências .NET e Flutter.
- SAST CodeQL ou equivalente.
- Bloqueio de merge para P0.
- Pipeline de migration controlado.
- Deploy separado de API e Worker.
- Teste de carga automatizado ou semi-automatizado antes do teste aberto.
- Teste de concorrência e reconciliação da economia Gold.

### QA

- Testes negativos de fraude de assinatura, IAP e compra de Gold.
- Testes de startup sem configuração obrigatória.
- Testes de Google Sign-In inválido.
- Testes de rate limit por IP/e-mail/usuário.
- Testes de import admin com path permitido e path proibido.
- Testes de build release com config insegura.
- Testes de cache/invalidação.
- Testes de jobs em lotes.
- Testes de paginação/cursor.
- Testes de carga com metas de latência e erro.
- Testes de compra com Gold, concorrência, rollback e reconciliação.

## 8. Dependências

- EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso.
- EPIC-015 — Segurança, Privacidade e LGPD.
- EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP.
- EPIC-019 — Fundação de Economia, Loja e Inventário.
- Configuração real do RevenueCat.
- Credenciais do ambiente de deploy.
- Definição dos client IDs Google Android/iOS.
- Redis configurado e monitorado.
- PostgreSQL com plano gerenciado, backup e pool compatível.
- Storage/CDN para mídia do catálogo.
- Ambiente de staging para carga mínima.
- Catálogo server-side de pacotes de Gold e itens compráveis.

## 9. Critérios de aceite do épico

- Usuário não consegue ativar premium enviando expiração futura pelo app.
- Usuário não consegue conceder item enviando transação inventada.
- Usuário não consegue creditar Gold informando quantidade pelo app.
- Produção falha ao iniciar com configuração crítica ausente ou placeholder.
- Nenhuma credencial real permanece versionada no repositório.
- Google Sign-In rejeita token com audience inválida ou e-mail não verificado.
- Login, registro, recuperação, Google auth e renovação de sessão têm rate limit adequado.
- Import admin rejeita path fora do diretório seguro.
- Requisição autenticada sem claim válida não opera com `Guid.Empty`.
- Build release mobile falha com configuração local, insegura ou de teste.
- Auditoria usa serialização segura.
- CI executa checks mínimos de segurança antes de merge.
- Status de acesso usa cache de curta duração com invalidação controlada.
- Catálogo aprovado e produtos ativos usam cache.
- Jobs de penalidade e notificação processam em lotes e não carregam toda a base em memória.
- Listagens críticas têm paginação segura e limites máximos.
- API e Worker estão separados em produção ou possuem plano explícito de separação antes do teste aberto.
- Migração de banco não roda automaticamente em todas as réplicas de produção.
- App evita rajada de refresh simultâneo.
- Métricas mínimas de p95/p99, erros, banco, Redis e filas estão disponíveis.
- Mídias de exercícios são servidas por storage/CDN.
- Teste de carga mínimo do MVP foi executado e registrado.
- Compra de Gold com dinheiro real só credita após validação server-side.
- Compra com Gold confirma saldo, ledger, pedido e inventário de forma atômica.
- Reconciliação de Gold detecta divergência entre saldo, ledger, pedido e inventário.

## 10. Decisão registrada

A EPIC-020 passa a ser bloqueadora para teste aberto e produção. Qualquer fluxo que conceda acesso premium, item, Gold, slot, permissão administrativa ou mudança financeira deve tratar o backend como autoridade final. Além disso, qualquer rota ou job crítico do MVP deve ser seguro, mensurável, paginado, cacheável quando possível e preparado para crescimento gradual sem reescrita emergencial.

Gold passa a ser tratado como ativo sensível da economia interna: compra, crédito, débito, pedido, ledger e inventário devem ser validados, idempotentes, atômicos, auditáveis e reconciliáveis.
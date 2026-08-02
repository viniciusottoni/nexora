---
title: EPIC-017 — Site Admin e Monitoramento Operacional
sidebar_position: 17
---

# EPIC-017 — Site Admin e Monitoramento Operacional

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-017 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Admin, Suporte, Engenharia, Segurança, DevOps e Produto |
| Planos impactados | Trial, Mensal e Anual |
| Plataforma | Web Admin (React) |
| Ordem de implementação | Antes do EPIC-016 — Release Android e Qualidade MVP |
| Status | Planejado |

## 2. Objetivo

Criar um site administrativo em React para centralizar a operação do AWAKEN antes do release Android, reunindo dashboard, usuários, tickets, bugs, alertas de segurança, eventos, engajamento, relatórios, saúde do MVP, performance, jobs, capacidade, mídia/CDN e readiness operacional em uma interface segura para administradores.

## 3. Contexto de produto

Antes de abrir o app para teste amplo, a equipe precisa enxergar o que está acontecendo no produto: usuários, tickets abertos exclusivamente pelo app, falhas de backend, crashes, eventos suspeitos, retenção, uso das funcionalidades e possíveis ataques. O site admin não é uma experiência pública do jogador; é uma ferramenta interna para reduzir tempo de resposta, dar confiança ao lançamento e impedir que problemas críticos fiquem invisíveis.

Como o painel concentra dados sensíveis e poder operacional, autenticação forte é parte do escopo mínimo: senha complexa, MFA via aplicativo autenticador compatível com Google Authenticator e sessão segura.

Com a criação da EPIC-020, o Admin também passa a ser a camada visual de diagnóstico e prevenção do MVP. As US-194 a US-215 adicionam controles de assinatura/IAP, configuração, segurança, cache, performance, jobs, worker, CDN e teste de carga; portanto, o Web Admin precisa expor esses sinais de forma clara para decisão go/no-go.

## 4. Escopo

### Entra neste épico

- Frontend web em React para administração.
- Shell web com navegação lateral, barra superior, busca global, badges de notificação e layout responsivo para desktop.
- Login de admin com senha complexa.
- MFA obrigatório com aplicativo autenticador compatível com Google Authenticator.
- Painel/dashboard operacional com indicadores do app e backend.
- Página de dashboard com cards de total de usuários, DAU, tickets abertos e MRR.
- Gráfico de usuários ativos diários, feed de atividade recente e lista de top eventos.
- Página de usuários com busca, filtros por plano e status, exportação CSV e detalhamento.
- Acompanhamento, triagem e atualização de tickets abertos exclusivamente pelo app.
- Página de tickets com busca, filtros por status, prioridade e categoria, além de leitura do detalhe.
- Visualização, triagem e registro interno de bugs do backend.
- Página de bugs com filtro por severidade, status, componente, origem e data.
- Visualização de alertas de segurança e sinais de ataque.
- Página de segurança com ações de bloqueio, monitoramento de falhas de login e alertas suspeitos.
- Página de audit log com histórico de ações de admin, sistema e usuário.
- Página de eventos com volume e distribuição dos eventos do produto.
- Página de engajamento com DAU/MAU, retenção, sessões e uso por funcionalidade.
- Página de relatórios com resumos operacionais e recortes exportáveis.
- Filtros por status, prioridade, origem, ambiente, período e perfil administrativo.
- Registro de ações administrativas para auditoria.
- Controle de acesso por perfil administrativo.
- Integração com logs, métricas, crash/erro e eventos relevantes já existentes.
- Saúde do MVP e readiness das entregas da EPIC-020.
- Monitoramento de assinatura/IAP server-side.
- Readiness de configuração e builds.
- Diagnóstico preventivo de segurança.
- Visualização de performance, cache, banco e Redis.
- Monitoramento de rotinas, workers e atualizações operacionais.
- Diagnóstico de mídia/CDN e assets do catálogo.
- Visualização de testes de carga e capacidade do MVP.
- Linha do tempo operacional com sinais relacionados.
- Checklist de readiness da EPIC-020.

### Fora deste épico

- Site público de marketing.
- Portal público de suporte completo.
- Abertura de tickets pelo site admin.
- Chat em tempo real com usuário final.
- Sistema avançado de SOC/SIEM corporativo.
- Resolução automática de ataques.
- Ferramentas complexas de BI.
- Administração financeira avançada de assinaturas.
- Correção automática de configurações, jobs, cache ou infraestrutura.
- Execução de ações sensíveis de produção diretamente pelo Admin no MVP.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-158 | Criar base do site admin em React | P0 | [Abrir](./US-158-base-site-admin-react.md) |
| US-159 | Login de admin com senha complexa | P0 | [Abrir](./US-159-login-admin-senha-complexa.md) |
| US-160 | Configurar MFA obrigatório com Google Authenticator | P0 | [Abrir](./US-160-mfa-obrigatorio-google-authenticator.md) |
| US-161 | Visualizar dashboard operacional do app | P0 | [Abrir](./US-161-dashboard-operacional-app.md) |
| US-162 | Acompanhar tickets abertos pelo app | P0 | [Abrir](./US-162-acompanhar-tickets-app.md) |
| US-163 | Triar e atualizar tickets de suporte no site admin | P0 | [Abrir](./US-163-triar-atualizar-tickets-suporte.md) |
| US-164 | Monitorar bugs e erros do backend | P0 | [Abrir](./US-164-monitorar-bugs-erros-backend.md) |
| US-165 | Monitorar alertas de segurança e ataques | P0 | [Abrir](./US-165-monitorar-alertas-seguranca-ataques.md) |
| US-166 | Registrar auditoria das ações administrativas | P0 | [Abrir](./US-166-auditoria-acoes-administrativas.md) |
| US-167 | Gerenciar usuários do admin com busca, filtros e exportação CSV | P0 | [Abrir](./US-167-gerenciar-usuarios-admin.md) |
| US-168 | Visualizar eventos do produto por volume e distribuição | P0 | [Abrir](./US-168-eventos-produto-volume-distribuicao.md) |
| US-169 | Visualizar engajamento e retenção por coorte | P0 | [Abrir](./US-169-engajamento-retencao-coorte.md) |
| US-170 | Gerar relatórios operacionais do admin | P0 | [Abrir](./US-170-relatorios-operacionais-admin.md) |
| US-171 | Registrar bug interno ou incidente de backend | P0 | [Abrir](./US-171-registrar-bug-incidente-backend.md) |
| US-216 | Visualizar dashboard de saúde do MVP e hardening | P0 | [Abrir](./US-216-dashboard-saude-mvp-hardening.md) |
| US-217 | Monitorar assinaturas e IAP com validação server-side | P0 | [Abrir](./US-217-monitorar-assinaturas-iap-validacao-server-side.md) |
| US-218 | Visualizar readiness de configuração e builds | P0 | [Abrir](./US-218-readiness-configuracoes-criticas-builds.md) |
| US-219 | Aprimorar Segurança com diagnóstico preventivo | P0 | [Abrir](./US-219-aprimorar-seguranca-diagnostico-preventivo.md) |
| US-220 | Visualizar performance, cache, banco e Redis | P0 | [Abrir](./US-220-painel-performance-cache-banco-redis.md) |
| US-221 | Monitorar rotinas, workers e atualizações operacionais | P0 | [Abrir](./US-221-monitorar-rotinas-workers-atualizacoes.md) |
| US-222 | Monitorar mídia, CDN e assets do catálogo | P1 | [Abrir](./US-222-monitorar-midia-cdn-assets.md) |
| US-223 | Visualizar teste de carga e capacidade do MVP | P0 | [Abrir](./US-223-visualizar-teste-carga-capacidade.md) |
| US-224 | Visualizar linha do tempo operacional com sinais relacionados | P1 | [Abrir](./US-224-linha-tempo-operacional-sinais-relacionados.md) |
| US-225 | Visualizar checklist de readiness da EPIC-020 | P0 | [Abrir](./US-225-checklist-readiness-epic-020.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-017-001 | Apenas usuários admin autenticados podem acessar o site admin. |
| RN-EPIC-017-002 | Todo admin deve usar senha complexa, com tamanho mínimo, variedade de caracteres e bloqueio de senhas fracas/reutilizadas. |
| RN-EPIC-017-003 | MFA é obrigatório para login admin, usando TOTP compatível com Google Authenticator. |
| RN-EPIC-017-004 | Admin sem MFA configurado deve ser direcionado ao setup antes de acessar o painel. |
| RN-EPIC-017-005 | Na fase inicial, tickets de suporte devem ser abertos exclusivamente pelo app. |
| RN-EPIC-017-006 | O site admin pode listar, filtrar, realizar triagem, atualizar status e registrar ações em tickets, mas não criar ticket de suporte para usuário final no MVP inicial. |
| RN-EPIC-017-007 | Bugs de backend podem ser registrados internamente e devem exibir severidade, status, componente, ambiente, origem e data de ocorrência. |
| RN-EPIC-017-008 | Alertas de ataque devem priorizar sinais de brute force, abuso de API, excesso de tentativas, tráfego anômalo, tokens inválidos, bloqueios de segurança e scraping. |
| RN-EPIC-017-009 | A listagem de usuários deve permitir busca, filtros por plano e status e exportação CSV sem expor dados sensíveis. |
| RN-EPIC-017-010 | O dashboard deve usar apenas dados agregados dos serviços existentes, como total de usuários, DAU, tickets abertos, MRR, atividade recente e top eventos. |
| RN-EPIC-017-011 | A tela de eventos deve refletir a taxonomia atual dos analytics e mostrar volume e distribuição por período. |
| RN-EPIC-017-012 | A tela de engajamento deve cobrir DAU/MAU, retenção por cohort, sessões e uso por funcionalidade. |
| RN-EPIC-017-013 | Toda ação administrativa relevante, incluindo login, falha de login, bloqueio, exportação, triagem e atualização, deve gerar trilha de auditoria. |
| RN-EPIC-017-014 | Logs exibidos no painel não podem conter senha, token, payload sensível ou dados desnecessários. |
| RN-EPIC-017-015 | Dados pessoais devem ser minimizados e protegidos conforme LGPD. |
| RN-EPIC-017-016 | Relatórios operacionais devem consolidar somente dados já disponíveis e respeitar o perfil administrativo. |
| RN-EPIC-017-017 | Itens P0 da EPIC-020 devem aparecer no Admin como readiness, diagnóstico ou evidência. |
| RN-EPIC-017-018 | Painéis preventivos devem diferenciar saudável, atenção, crítico e sem dados. |
| RN-EPIC-017-019 | O Admin deve permitir drilldown seguro entre saúde do MVP, segurança, performance, rotinas, mídia, capacidade e audit log. |
| RN-EPIC-017-020 | Nenhuma tela de diagnóstico pode expor credenciais, tokens, payloads sensíveis ou dados pessoais desnecessários. |
| RN-EPIC-017-021 | Teste aberto deve considerar o checklist de readiness da EPIC-020. |

## 7. Impactos técnicos

### Frontend React

- Novo app web admin em React.
- Roteamento autenticado com proteção por perfil.
- Tela de login.
- Fluxo de setup e validação de MFA.
- Dashboard com cards, gráficos, tabelas, filtros, badges e feeds.
- Telas de usuários, tickets, bugs, alertas, audit log, eventos, engajamento e relatórios.
- Telas adicionais de saúde do MVP, monetização server-side, readiness, performance, rotinas/workers, mídia/CDN, capacidade e checklist EPIC-020.
- Componentes reutilizáveis para cards, tabelas, chips, estados de loading/erro/vazio, status de domínio, timeline operacional e exportação.
- Design responsivo para desktop e uso emergencial em mobile.

### Backend

- Autenticação admin separada do usuário comum.
- Endpoints para login admin, setup MFA, validação TOTP e sessão.
- APIs para dashboard, usuários, tickets, bugs, alertas de segurança, audit log, eventos, engajamento e relatórios.
- Endpoints agregados para cards, gráficos e listas do painel.
- Endpoints novos para saúde do MVP, readiness, monetização server-side, performance/cache, rotinas/workers, mídia/CDN, capacidade e checklist EPIC-020.
- Rate limit forte nos endpoints de autenticação admin.
- Logs sanitizados para eventos de admin e segurança.
- Integração com serviços de observabilidade existentes ou planejados.

### Banco de dados

- Entidade de admin.
- Segredo MFA criptografado ou protegido por mecanismo seguro.
- Tickets operacionais.
- Perfis e visões agregadas para usuários, eventos e engajamento.
- Eventos de bug/erro normalizados.
- Alertas de segurança.
- Auditoria de ações administrativas.
- Base para relatórios e exportações do painel.
- Possível base de evidências/status para checklist de readiness.
- Possível armazenamento seguro de resultados de teste de carga e diagnósticos operacionais.

### Segurança

- Senha complexa obrigatória.
- MFA obrigatório por TOTP.
- Bloqueio temporário após tentativas inválidas.
- Sessão curta e renovação segura.
- Proteção contra brute force.
- Permissões por perfil administrativo.
- Auditoria de login, falha de login, bloqueio de IP, alteração de ticket, triagem de bug, exportação e leitura de alerta sensível.
- Diagnósticos devem usar dados agregados, mascarados e minimizados.

### Analytics e observabilidade

- Métricas de tickets abertos, resolvidos e tempo de resposta.
- Métricas de erros por ambiente e severidade.
- Métricas de ataques ou tentativas suspeitas.
- Visão de saúde do backend e integrações críticas.
- Métricas de usuários, DAU, MRR, engajamento, retenção e top eventos.
- Métricas de cache, banco, Redis, jobs, workers, CDN e teste de carga quando disponíveis.

### QA

- Testar login admin com senha válida e inválida.
- Testar bloqueio de senha fraca.
- Testar setup, validação e falha de MFA.
- Testar bloqueio por tentativas repetidas.
- Testar dashboard com dados, vazio, loading e erro.
- Testar listagem, filtros, triagem e atualização de tickets abertos pelo app.
- Testar listagem de usuários, filtros por plano/status e exportação CSV.
- Testar bugs de backend, registro interno de incidente e alertas de ataque.
- Testar audit log com filtros e trilha das ações administrativas.
- Testar eventos, engajamento e relatórios.
- Testar Saúde do MVP, Readiness, Performance, Rotinas/Workers, Mídia/CDN, Capacidade e Checklist EPIC-020 com dados saudáveis, críticos e sem dados.

## 8. Dependências

- EPIC-002 para base de autenticação e sessão.
- EPIC-014 para analytics, crash, logs e observabilidade.
- EPIC-015 para segurança, privacidade e LGPD.
- EPIC-020 para hardening de segurança, performance e escalabilidade do MVP.
- Backend com ambientes minimamente separados.
- Estratégia de logs sem dados sensíveis.
- Métricas e eventos agregados para os novos diagnósticos.

## 9. Critérios de aceite do épico

- Site admin em React existe e pode ser acessado em ambiente interno.
- Admin consegue logar apenas com senha complexa e MFA válido.
- Admin sem MFA configurado não acessa o dashboard.
- Dashboard exibe indicadores operacionais essenciais do produto, incluindo usuários, DAU, tickets abertos, MRR, atividade recente e top eventos.
- Usuários podem ser listados, filtrados e exportados em CSV.
- Tickets abertos pelo app são listados, filtráveis e atualizáveis no site admin.
- Site admin não permite abertura inicial de ticket de suporte para usuário final.
- Bugs de backend aparecem com severidade, status, ambiente e componente, e podem ser registrados internamente.
- Alertas de segurança e sinais de ataque são visíveis para admin autorizado.
- Eventos e engajamento aparecem com volume, distribuição, retenção e uso por funcionalidade.
- Relatórios operacionais consolidam os dados já existentes.
- Ações administrativas relevantes ficam auditadas.
- Nenhum log ou tela expõe senha, token ou payload sensível.
- Saúde do MVP exibe status geral e por domínio.
- Assinaturas/IAP server-side possuem visão operacional de validações, falhas e pendências.
- Readiness de configuração e builds fica visível por ambiente.
- Segurança possui visão preventiva com tendências e agrupamentos.
- Performance exibe p95/p99, erro, cache, banco e Redis.
- Rotinas/workers exibem atrasos, falhas e filas acumuladas.
- Mídia/CDN exibe cobertura do catálogo e links problemáticos.
- Capacidade MVP exibe resultado de teste de carga e go/no-go.
- Checklist EPIC-020 mostra status das US-194 a US-215.
- EPIC-016 só deve avançar para release Android após este painel mínimo estar validado.

## 10. Decisão registrada

O AWAKEN terá um site admin em React antes do release Android. O painel será ferramenta interna obrigatória para acompanhar operação, suporte, bugs, backend e segurança. Acesso admin exige senha complexa e MFA via autenticador compatível com Google Authenticator, com auditoria e proteção contra exposição de dados sensíveis.

Com a EPIC-020, o Admin também passa a ser a superfície oficial de diagnóstico e prevenção do MVP. As telas devem transformar hardening, performance e capacidade em indicadores visíveis de readiness para abertura pública.
# E-15 · Gestão Geral da Plataforma

|  |  |
|---|---|
| **Fase** | 5 — Produto replicável em escala |
| **Histórias** | 8 |
| **Pontos** | 58 |
| **Sprints previstas** | Fase 5, após a base da E-14 |
| **Aplicações afetadas** | web-platform, api-cloud |
| **Pacotes do monorepo** | packages/contracts, packages/ui |

---

## 1. Objetivo do épico

Entregar ao administrador da plataforma (P9) uma área central para localizar, compreender e administrar todos os estabelecimentos sem depender de console, banco de dados ou chamadas manuais de API.

A E-00 criou a fundação multi-tenant e o fluxo de provisionamento. A E-14 adiciona ferramentas de escala, como saúde das instalações e suporte auditado. Esta épica fecha a lacuna entre essas capacidades: transforma o `web-platform` em uma aplicação administrativa completa, com página inicial, diretório de clientes, detalhe, ciclo de vida, plano, responsáveis, recuperação do provisionamento e visão operacional consolidada.

## 2. Valor entregue

- Uma raiz clara para a plataforma, com navegação e indicadores acionáveis
- Todos os estabelecimentos localizáveis em uma lista segura, pesquisável e filtrável
- Visão 360° de cada estabelecimento sem exposição indevida de dados de negócio
- Ciclo de vida administrativo explícito, auditável e sem exclusão física acidental
- Plano e configuração comercial coerentes com o que foi contratado
- Proprietário, convites e acessos iniciais gerenciáveis pela equipe autorizada
- Recuperação segura quando o token ou o comando de instalação não estiver mais disponível
- Atalhos contextuais para saúde, auditoria e suporte, reaproveitando E-09 e E-14

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-150](./US-150-Estrutura-e-navegacao-do-painel-de-plataforma.md) | Estrutura e navegação do painel de plataforma | M | 5 | RF-PLT-09 |
| [US-151](./US-151-Diretorio-de-estabelecimentos-com-busca-e-filtros.md) | Diretório de estabelecimentos com busca e filtros | M | 8 | RF-PLT-10 |
| [US-152](./US-152-Visao-360-e-acesso-aos-modulos-do-estabelecimento.md) | Visão 360 e acesso aos módulos do estabelecimento | M | 8 | RF-PLT-11 |
| [US-153](./US-153-Ciclo-de-vida-do-estabelecimento.md) | Ciclo de vida do estabelecimento | M | 8 | RF-PLT-12 |
| [US-154](./US-154-Gestao-de-planos-e-configuracao-comercial.md) | Gestão de planos e configuração comercial | S | 8 | RF-PLT-13 |
| [US-155](./US-155-Proprietarios-usuarios-iniciais-e-convites.md) | Proprietários, usuários iniciais e convites | M | 8 | RF-PLT-14 |
| [US-156](./US-156-Recuperacao-do-provisionamento-e-token-de-instalacao.md) | Recuperação do provisionamento e token de instalação | M | 8 | RF-PLT-15 |
| [US-157](./US-157-Central-operacional-auditoria-e-atalhos-de-suporte.md) | Central operacional, auditoria e atalhos de suporte | M | 5 | RF-PLT-16 |

## 4. Ordem de execução recomendada

1. US-150 — estabelece o shell, as rotas e a navegação
2. US-151 — torna os estabelecimentos localizáveis e substitui o formulário como raiz
3. US-152 — cria a página de detalhe que recebe as demais capacidades
4. US-153 — formaliza estados e ações administrativas seguras
5. US-155 — resolve responsáveis, convites e acesso inicial
6. US-156 — fecha a perda do token/comando após o provisionamento
7. US-154 — conecta o cadastro ao plano comercial
8. US-157 — consolida visão operacional, auditoria e atalhos da E-14

## 5. Dependências do épico

**Depende de:** E-00, E-09, US-140, US-145  
**Complementa:** E-14  
**Habilita:** operação cotidiana da Replay com múltiplos clientes sem acesso técnico direto

## 6. Definition of Done do épico

- [ ] A raiz do `web-platform` exibe visão geral e navegação, não o formulário de criação
- [ ] O administrador consegue listar, buscar, filtrar e abrir qualquer estabelecimento autorizado
- [ ] O detalhe reúne identidade, plano, responsáveis, lojas, instalações e estado operacional
- [ ] Toda mudança de status, plano, proprietário ou credencial exige confirmação, motivo e auditoria
- [ ] Nenhuma ação administrativa exclui silenciosamente dados históricos
- [ ] Token de instalação perdido pode ser substituído com revogação atômica do anterior
- [ ] Estados vazios, carregamento, falha parcial e ausência de permissão estão cobertos
- [ ] Contratos OpenAPI e TypeScript são compatíveis e validados em testes de contrato
- [ ] Testes E2E cobrem o percurso raiz → lista → detalhe → ação administrativa
- [ ] A interface não expõe dado operacional do cliente sem fluxo de suporte autorizado

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|:-:|:-:|---|
| Painel global contornar o isolamento multi-tenant | Baixa | Crítico | Policy exclusiva de plataforma, queries globais explícitas e testes negativos de autorização |
| Ação de ciclo de vida interromper operação ativa | Média | Crítico | Pré-validações, impacto visível, confirmação forte, idempotência e auditoria |
| Divergência entre plano exibido e persistido | Média | Alto | Catálogo de planos único, histórico temporal e teste de contrato |
| Token reemitido deixar credencial antiga válida | Baixa | Crítico | Rotação atômica, armazenamento apenas do hash e evento auditável |
| Detalhe virar acesso irrestrito ao negócio do cliente | Média | Alto | Exibir apenas metadados administrativos; dado de negócio somente via US-145 |
| Sobreposição confusa com a E-14 | Média | Médio | E-15 agrega e navega; E-14 continua dona de saúde, suporte e atualização do parque |

---

*Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

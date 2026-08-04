# E-00 · Fundacao da Plataforma

|  |  |
|---|---|
| **Fase** | 0 — Fundação da plataforma |
| **Histórias** | 7 |
| **Pontos** | 55 |
| **Sprints previstas** | Sprint 0 |
| **Aplicações afetadas** | api-cloud, api-edge, web-admin, web-platform |
| **Pacotes do monorepo** | packages/db, packages/domain, packages/contracts, packages/ui, infra/edge, infra/cloud |

---

## 1. Objetivo do épico

Estabelecer a fundação técnica que torna o produto **multi-estabelecimento, personalizável e replicável** desde a primeira linha de código. Nenhuma história deste épico é entregável ao cliente final — todas são pré-condição estrutural. Retrofit de multi-tenancy, de theming em runtime ou de trilha de auditoria significa reescrever o núcleo, e é exatamente isso que o ADR-001 e o ADR-004 existem para evitar.

## 2. Valor entregue

- Isolamento de dados entre estabelecimentos imposto pelo banco, não pela disciplina do desenvolvedor (ADR-004)
- Capacidade de provisionar um novo cliente sem tocar em código (ADR-013)
- Identidade visual por tenant aplicada em runtime, com artefato único de build (ADR-010)
- Autenticação adequada a cada perfil: senha para gestão, PIN para operação (ADR-014)
- Servidor da loja instalável por script, em menos de 30 minutos
- Pipeline que falha automaticamente quando alguém tenta escrever código específico de cliente

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-001](./US-001-Estrutura-multi-tenant-com-isolamento.md) | Estrutura multi-tenant com isolamento | M | 8 | RF-PLT-01 |
| [US-002](./US-002-Provisionar-novo-estabelecimento.md) | Provisionar novo estabelecimento | M | 5 | RF-PLT-05 |
| [US-003](./US-003-Identidade-visual-por-estabelecimento.md) | Identidade visual por estabelecimento | M | 8 | RF-PLT-02, RF-PLT-04 |
| [US-004](./US-004-Autenticacao-e-perfis-de-acesso.md) | Autenticacao e perfis de acesso | M | 13 | RF-IAM-01, RF-IAM-02, RF-IAM-03, RF-IAM-04, RF-IAM-06, RF-IAM-07 |
| [US-005](./US-005-Registro-de-dispositivos-autorizados.md) | Registro de dispositivos autorizados | M | 5 | RF-IAM-05 |
| [US-006](./US-006-Servidor-local-instalavel-por-script.md) | Servidor local instalavel por script | M | 8 | RF-PLT-05, RF-OFF-01 |
| [US-007](./US-007-Pipeline-de-CI-CD-com-travas-de-governanca.md) | Pipeline de CI-CD com travas de governanca | M | 8 | — |

## 4. Ordem de execução recomendada

1. US-001 — o isolamento precisa existir antes de qualquer tabela de negócio
2. US-004 — autenticação e perfis, base de toda rota autenticada
3. US-005 — registro de dispositivos, pré-requisito do login por PIN operacional
4. US-002 — provisionamento de tenant, que passa a exercitar o isolamento
5. US-003 — identidade visual em runtime
6. US-006 — servidor local instalável
7. US-007 — pipeline de CI/CD com as travas de governança

## 5. Dependências do épico

**Depende de:** nenhuma — é o ponto de partida do projeto  
**Habilita:** E-01, E-02, E-03, E-04, E-05, E-06, E-07, E-08, E-09

## 6. Definition of Done do épico

- [ ] Migration inicial aplicada com RLS habilitado em todas as tabelas com `tenant_id`
- [ ] Teste automatizado de isolamento entre dois tenants rodando no CI de todo PR
- [ ] Dois tenants de demonstração provisionados com marcas distintas a partir do mesmo build
- [ ] Login por senha e login por PIN funcionando em dispositivo registrado
- [ ] Servidor local subindo por `./install.sh` em máquina limpa em menos de 30 minutos
- [ ] Pipeline bloqueando PR que viole o ADR-013
- [ ] ADR-001, 004, 010, 013, 014, 015 e 023 refletidos no código

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| RLS mal configurado passar despercebido e vazar dados entre tenants | Baixa | Crítico | Teste de isolamento obrigatório em cada PR; política negada por padrão; revisão de segurança na Sprint 0 |
| Time subestimar a Sprint 0 e começar o MVP sem fundação pronta | Média | Alto | Sprint 0 é bloqueante; nenhuma história de E-01 a E-09 entra antes do DoD deste épico |
| Hardware do edge ainda indefinido atrasar US-006 | Média | Médio | Validar mini-PC, monitor de KDS e teclado numérico ainda na Sprint 0 (risco T1 do doc. 02) |

---

*Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*
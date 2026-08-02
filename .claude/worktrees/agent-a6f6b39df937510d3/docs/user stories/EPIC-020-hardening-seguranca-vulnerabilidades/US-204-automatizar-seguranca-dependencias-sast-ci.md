---
title: US-204 — Automatizar segurança de dependências, SAST e análise no CI
sidebar_position: 204
---

# US-204 — Automatizar segurança de dependências, SAST e análise no CI

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-204 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P1 |
| Fase | Pré-teste aberto |
| Perfil principal | Engenharia, DevOps, Segurança e QA |
| Plano | Todos |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | GitHub Actions, .NET, Flutter, Dependabot, SAST |
| Status | Planejada |

## 2. História do usuário

Como **engenheiro do AWAKEN**,

quero **que o CI detecte dependências vulneráveis, padrões inseguros e regressões de segurança**,

para **reduzir risco antes do merge e antes do release**.

## 3. Contexto

A revisão indicou ausência de configuração visível de Dependabot/CI de segurança. O projeto usa Flutter, .NET, Firebase, RevenueCat, Hangfire, PostgreSQL e bibliotecas externas. Atualizações e alertas precisam ser automatizados.

## 4. Objetivo

Criar pipeline mínimo de segurança para dependências, análise estática e checks de configuração.

## 5. Escopo

### Entra nesta US

- Configurar Dependabot para NuGet, pub e GitHub Actions.
- Adicionar workflow de segurança.
- Rodar auditoria de dependências .NET.
- Rodar verificação de dependências Flutter/Dart.
- Rodar SAST com CodeQL ou ferramenta equivalente.
- Rodar verificação de credenciais versionadas.
- Bloquear merge em falha P0.

### Fora desta US

- Pentest externo.
- WAF/CDN.
- Monitoramento runtime avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | PR não pode ser mergeado se check P0 de segurança falhar. |
| RN-002 | Dependências críticas vulneráveis devem abrir PR automático ou alerta. |
| RN-003 | CI deve cobrir backend, app mobile e workflows. |
| RN-004 | Checks não devem imprimir valores sensíveis. |
| RN-005 | Falso positivo deve ser documentado com justificativa. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Dev | Recebe feedback no PR. |
| Maintainer | Pode aprovar exceção documentada. |
| Admin repo | Configura branch protection. |
| Usuário final | Não impactado diretamente. |

## 8. Fluxo principal

1. PR é aberto.
2. CI executa build/testes.
3. CI executa checks de segurança.
4. Dependências vulneráveis ou padrões inseguros falham o check.
5. Dev corrige ou documenta exceção aprovada.
6. Merge só ocorre com checks verdes.

## 9. Fluxos alternativos

- Alerta sem correção imediata: issue técnica é criada com prioridade.
- Falso positivo: exceção registrada e revisada periodicamente.
- Dependência sem patch: risco aceito temporariamente com mitigação.

## 10. Estados esperados

- CI verde;
- CI falhou por dependência;
- CI falhou por padrão inseguro;
- exceção documentada;
- PR automático de atualização.

## 11. Impacto no Frontend Flutter

- Auditoria de dependências pub.
- Testes de configuração release.
- Análise estática Dart/Flutter.

## 12. Impacto no Backend

- Auditoria de pacotes NuGet.
- Testes de segurança e configuração.
- Análise estática C#.

## 13. Impacto no Banco de Dados

Sem migration.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Reduz risco de vulnerabilidades em RevenueCat/IAP/backend financeiro.

## 16. Impacto em Internacionalização

Não aplicável.

## 17. Contrato técnico sugerido

Arquivos sugeridos:

```txt
.github/dependabot.yml
.github/workflows/security.yml
.github/workflows/codeql.yml
```

Checks mínimos:

```txt
backend-security
mobile-security
secret-scan
sast
```

## 18. Eventos de Analytics

Não aplicável.

## 19. Critérios de aceite

- Dependabot configurado para NuGet, pub e Actions.
- Workflow de segurança roda em PR e push na master.
- Falha crítica bloqueia merge.
- SAST executa para C# e, quando viável, Dart.
- Scanner de credenciais roda sem expor valores.
- Documentação de exceção existe.

## 20. Critérios de teste para QA

- abrir PR de teste com dependência desatualizada controlada;
- validar execução do workflow;
- validar branch protection;
- validar PR automático do Dependabot;
- validar logs sem valores sensíveis.

## ✅ Decisão registrada

Segurança de dependências e análise estática devem fazer parte do fluxo normal de PR, não de uma revisão manual eventual.
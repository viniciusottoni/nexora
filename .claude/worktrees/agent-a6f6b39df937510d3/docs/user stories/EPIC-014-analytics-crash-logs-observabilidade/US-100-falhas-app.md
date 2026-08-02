---
title: US-100 — Registrar falhas do app
sidebar_position: 100
---

# US-100 — Registrar falhas do app

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-100 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Engenharia e QA |
| Integrações | Firebase |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia**, quero **registrar falhas do app**, para **identificar problemas críticos antes e depois do lançamento Android**.

## 3. Contexto

O MVP precisa ser estável. Falhas em splash, login, onboarding, quest ou assinatura podem impedir ativação e conversão.

## 4. Objetivo

Configurar captura de falhas fatais e erros não fatais relevantes no app Flutter.

## 5. Escopo

### Entra nesta US

- Configurar ferramenta Firebase de falhas.
- Capturar falhas fatais.
- Registrar erros não fatais críticos.
- Enviar contexto mínimo de tela e fluxo.
- Validar relatório em ambiente de teste.

### Fora desta US

- Observabilidade mobile avançada.
- Gravação de tela.
- Dados pessoais sensíveis em logs.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Falhas devem ser capturadas desde testes internos. |
| RN-002 | Logs não devem expor dados sensíveis de saúde, pagamento ou limitações. |
| RN-003 | Erros não fatais críticos devem ter contexto de fluxo. |
| RN-004 | Ambiente deve ser identificável: dev, staging ou production. |

## 7. Impacto Flutter

- Configurar captura no bootstrap.
- Capturar FlutterError.
- Capturar erros assíncronos da zona principal.
- Adicionar chaves não sensíveis como `screen`, `flow` e `environment`.

## 8. Impacto QA

- Gerar falha controlada em build de teste.
- Validar recebimento do relatório.
- Validar ausência de dados sensíveis.

## 9. Critérios de aceite

### CA-001 — Falha capturada

Dado que ocorre falha controlada em teste,
Quando o app reiniciar,
Então a ferramenta deve receber o relatório.

### CA-002 — Sem dados sensíveis

Dado que ocorre falha em onboarding,
Quando o relatório for consultado,
Então não deve conter peso, idade, dores ou limitações.

## 10. Decisão registrada

Registro de falhas é obrigatório no MVP Android para proteger estabilidade, ativação e conversão.

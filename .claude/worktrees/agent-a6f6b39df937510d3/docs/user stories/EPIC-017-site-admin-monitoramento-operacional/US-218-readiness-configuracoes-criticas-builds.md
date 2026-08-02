---
title: US-218 — Visualizar readiness de configuração e builds
sidebar_position: 218
---

# US-218 — Visualizar readiness de configuração e builds

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-218 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-196, US-197, US-198, US-202, US-204 |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | Admin, DevOps, Segurança e Engenharia |
| Plataforma | Web Admin React + Backend Admin API + CI |
| Status | Planejada |

## 2. História do usuário

Como **admin técnico do AWAKEN**,

quero **visualizar se a configuração obrigatória e os builds do sistema estão prontos para produção**,

para **prevenir deploy inseguro, app apontando para ambiente errado e falhas por configuração incompleta**.

## 3. Contexto

As US-196, US-197, US-198, US-202 e US-204 criam barreiras para impedir produção com valores indevidos, configuração incompleta, login social mal preparado, build mobile incorreto e checks de CI ausentes. O Admin deve transformar esses checks em uma tela de readiness simples e objetiva.

## 4. Objetivo

Exibir no Admin o status das configurações obrigatórias por ambiente e por versão de build, sem revelar valores sensíveis.

## 5. Escopo

### Entra nesta US

- Página ou seção `Readiness` no Admin.
- Status de configuração por ambiente: dev, staging e produção.
- Sinais de configuração obrigatória presente, ausente, inválida ou sem dados.
- Status de build mobile: ambiente, modo de distribuição, API de destino e flags críticas.
- Status do último CI: dependências, análise estática e verificação de segurança.
- Histórico dos últimos checks de readiness.

### Fora desta US

- Exibir valores reais de configuração.
- Editar configuração pelo Admin.
- Substituir pipeline de CI/CD.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A tela deve mostrar apenas presença/status, nunca valores sensíveis. |
| RN-002 | Produção com configuração obrigatória ausente deve aparecer como bloqueada. |
| RN-003 | Build mobile release com configuração de teste deve aparecer como crítico. |
| RN-004 | Último check de CI deve indicar commit, data, status e motivo seguro da falha. |
| RN-005 | Ambiente sem telemetria deve aparecer como sem dados. |

## 7. Indicadores mínimos

- Configuração obrigatória presente.
- Configuração com valor de teste detectado.
- Build mobile release válido ou bloqueado.
- Login social pronto para o ambiente.
- CI de segurança aprovado ou falho.
- Última execução de readiness.

## 8. Fluxo principal

1. Admin acessa Readiness.
2. Sistema mostra status por ambiente.
3. Admin vê bloqueadores em destaque.
4. Admin abre detalhe sem valores sensíveis.
5. Admin usa o resultado como go/no-go de produção.

## 9. Impacto no Frontend

- Nova página/aba no Admin.
- Cards de status por ambiente.
- Tabela de checks com status, severidade, descrição e última verificação.

## 10. Impacto no Backend

- Endpoint admin de readiness.
- Integração com validações de configuração.
- Integração opcional com resultado de CI.

## 11. Critérios de aceite

- Admin vê status de configuração por ambiente.
- Valores sensíveis não aparecem.
- Configuração obrigatória ausente aparece como bloqueador.
- Build release inseguro aparece como crítico.
- Último CI/check é exibido.
- Tela pode ser usada como checklist go/no-go.

## 12. Critérios de teste para QA

- ambiente saudável;
- ambiente com configuração ausente;
- ambiente sem dados;
- build release válido;
- build release com configuração de teste;
- CI aprovado e CI falho.

## ✅ Decisão registrada

O Admin deve tornar visível o readiness de configuração e build para impedir publicação insegura do MVP.
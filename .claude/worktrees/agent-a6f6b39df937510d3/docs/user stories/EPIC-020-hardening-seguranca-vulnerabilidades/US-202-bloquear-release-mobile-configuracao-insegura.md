---
title: US-202 — Bloquear build release mobile com configuração insegura
sidebar_position: 202
---

# US-202 — Bloquear build release mobile com configuração insegura

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-202 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Flutter, DevOps, QA e Segurança |
| Plano | Todos |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | AppConfig, build release, assinatura/IAP, backend público |
| Status | Planejada |

## 2. História do usuário

Como **responsável pelo release mobile**,

quero **que o app não gere build release com configuração de desenvolvimento ou teste**,

para **evitar publicar uma versão apontando para ambiente incorreto**.

## 3. Contexto

O app possui defaults úteis para desenvolvimento. Em release, esses defaults não podem ser aceitos silenciosamente, pois podem quebrar acesso ao backend real, assinatura, loja e analytics.

## 4. Objetivo

Criar validação de configuração para build release e checks no CI para bloquear builds móveis inseguros.

## 5. Escopo

### Entra nesta US

- Validar endpoint base em release.
- Bloquear host local ou protocolo inseguro em release.
- Bloquear modo de loja/assinatura de teste em release.
- Bloquear identificadores default de desenvolvimento.
- Criar teste automatizado de configuração.
- Documentar comandos corretos de build por ambiente.

### Fora desta US

- Configuração final das lojas.
- Publicação na Play Store.
- Troca de SDK de assinatura.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Release não pode usar backend local. |
| RN-002 | Release deve usar comunicação segura. |
| RN-003 | Release não pode usar modo de compra/assinatura de teste. |
| RN-004 | Release não pode usar identificadores default de desenvolvimento. |
| RN-005 | Falha deve ocorrer antes da publicação. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Dev local | Pode usar defaults em debug. |
| QA interno | Pode usar staging explicitamente configurado. |
| Produção | Deve usar backend e loja de produção. |

## 8. Fluxo principal

1. Build release é iniciado.
2. Configuração é validada.
3. Se houver valor inseguro, build falha.
4. Se valores estiverem corretos, build continua.

## 9. Fluxos alternativos

- Debug local: defaults permitidos.
- Staging: permitido apenas com ambiente explicitamente configurado.
- Release sem configuração obrigatória: falha.

## 10. Estados esperados

- debug válido;
- staging válido;
- release válido;
- release com backend local bloqueado;
- release com comunicação insegura bloqueada;
- release com modo teste bloqueado.

## 11. Impacto no Frontend Flutter

- Ajustar `AppConfig` com validação para release.
- Adicionar testes unitários de configuração.
- Ajustar documentação de build.

## 12. Impacto no Backend

Sem impacto direto.

## 13. Impacto no Banco de Dados

Sem impacto.

## 14. Impacto em Gamificação

Evita release apontando para ambiente errado e perda de progresso real.

## 15. Impacto em Monetização

Evita release com loja em modo teste ou sem integração correta de assinatura/IAP.

## 16. Impacto em Internacionalização

Não aplicável.

## 17. Contrato técnico sugerido

Criar função interna de validação de configuração de release e executá-la nos testes/CI.

## 18. Eventos de Analytics

Não aplicável.

## 19. Critérios de aceite

- Build release com backend local falha.
- Build release com comunicação insegura falha.
- Build release com modo teste falha.
- Build debug continua permitindo configuração local.
- CI executa teste de configuração.

## 20. Critérios de teste para QA

- debug local;
- release sem configuração obrigatória;
- release com configuração válida;
- release com modo teste;
- staging explicitamente configurado;
- documentação revisada.

## ✅ Decisão registrada

Nenhum build release do AWAKEN pode ser gerado com configuração local, insegura ou de teste por acidente.
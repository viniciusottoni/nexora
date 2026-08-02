---
title: US-197 — Validar configurações críticas no startup do backend
sidebar_position: 197
---

# US-197 — Validar configurações críticas no startup do backend

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-197 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Backend, DevOps e Segurança |
| Plano | Todos |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | Configuração ASP.NET Core, autenticação, CORS, integrações externas |
| Status | Planejada |

## 2. História do usuário

Como **engenheiro responsável pelo deploy**,

quero **que a API falhe ao iniciar quando configurações críticas estiverem ausentes ou inseguras**,

para **evitar que produção rode com placeholders, defaults de teste ou valores fracos**.

## 3. Contexto

O backend depende de configurações críticas para autenticação, login social, CORS, banco de dados, cache, assinatura, IAP, IA, storage e provedores externos. Em produção, qualquer valor ausente, vazio, placeholder ou default de teste deve impedir o startup.

## 4. Objetivo

Criar validação centralizada de configurações críticas com fail-fast em ambientes não Development.

## 5. Escopo

### Entra nesta US

- Criar serviço/extensão de validação de configuração no startup.
- Validar configuração de assinatura de tokens.
- Validar configuração de login social quando habilitado.
- Validar lista explícita de origens permitidas fora de Development.
- Validar conexões obrigatórias.
- Validar configurações de integrações habilitadas por feature flag.
- Criar testes de startup inválido.

### Fora desta US

- Implementar cofre externo.
- Trocar algoritmo de autenticação.
- Refatorar todo o sistema de options.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Produção não pode iniciar com configuração de token vazia, curta ou placeholder. |
| RN-002 | Produção não pode iniciar sem CORS explicitamente configurado. |
| RN-003 | Produção não pode iniciar com login social habilitado e client id ausente. |
| RN-004 | Produção não pode iniciar sem conexões obrigatórias configuradas. |
| RN-005 | Feature habilitada deve exigir suas configurações obrigatórias. |
| RN-006 | Erro de startup deve explicar a chave lógica ausente sem imprimir valor sensível. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário final | Não impactado diretamente. |
| Dev local | Pode usar configuração local controlada em Development. |
| Ambiente de teste | Deve configurar valores mínimos válidos. |
| Produção | Deve falhar quando configuração crítica estiver inválida. |

## 8. Fluxo principal

1. API inicia.
2. Validador lê ambiente atual.
3. Configurações críticas são avaliadas.
4. Se inválidas, startup falha com erro claro e seguro.
5. Se válidas, aplicação continua normalmente.

## 9. Fluxos alternativos

### Development

Pode aceitar valores locais controlados, mas deve alertar quando estiver usando placeholders.

### Feature desabilitada

Configuração específica pode ser opcional apenas se a feature estiver realmente desabilitada.

## 10. Estados esperados

- startup válido;
- configuração de token inválida;
- login social inválido;
- CORS ausente;
- conexão ausente;
- provider habilitado sem configuração;
- erro seguro sem valor sensível.

## 11. Impacto no Frontend Flutter

Sem impacto direto.

## 12. Impacto no Backend

- Nova classe de validação de configuração crítica.
- Registro no startup antes do app aceitar requisições.
- Testes de integração para produção com configuração ausente.

## 13. Impacto no Banco de Dados

Não há migration obrigatória.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Impede produção com assinatura/IAP mal configurados.

## 16. Impacto em Internacionalização

Não aplicável.

## 17. Contrato técnico sugerido

```txt
ValidateCriticalConfiguration(configuration, environment)
```

O erro deve indicar apenas a chave lógica, nunca o valor bruto.

## 18. Eventos de Analytics

Não aplicável.

## 19. Critérios de aceite

### CA-001 — Configuração de token insegura

Dado ambiente Production com configuração de token placeholder,
Quando a API inicia,
Então o startup falha.

### CA-002 — CORS ausente

Dado ambiente Production sem origens permitidas,
Quando a API inicia,
Então o startup falha.

### CA-003 — Login social incompleto

Dado login social habilitado sem client id,
Quando a API inicia,
Então o startup falha.

### CA-004 — Valor sensível não aparece no erro

Dado uma configuração inválida,
Quando o erro é logado,
Então o valor bruto não é impresso.

## 20. Critérios de teste para QA

- startup Production com configuração válida;
- startup Production com token inválido;
- startup Production sem CORS;
- startup Development permitido;
- logs sem valores sensíveis;
- regressão de health check.

## ✅ Decisão registrada

Produção deve falhar rápido e de forma segura quando configuração crítica estiver ausente, fraca ou usando valor de teste.
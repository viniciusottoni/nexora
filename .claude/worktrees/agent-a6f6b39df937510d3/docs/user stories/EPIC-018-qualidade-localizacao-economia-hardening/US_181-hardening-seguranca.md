---
title: US-181 — Hardening de segurança
sidebar_position: 181
---

# US-181 — Hardening de segurança

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-181 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia e Segurança |
| Plataforma | Backend .NET 10 |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia**,
quero **aplicar hardening básico de segurança no backend**,
para **reduzir riscos antes do teste aberto e do site admin**.

## 3. Objetivo

Configurar controles mínimos de proteção: rate limiting, CORS, headers de segurança e proteção de rotas operacionais.

## 4. Escopo

### Entra nesta US

- Rate limiting para rotas públicas e autenticação.
- CORS restrito ao admin/front permitido.
- Security headers básicos.
- HSTS em ambiente produtivo.
- Proteção do dashboard operacional/Hangfire.
- Logs de bloqueio sem dados sensíveis.

### Fora desta US

- Pentest formal.
- SIEM avançado.
- WAF dedicado.
- Gestão completa de segredos.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Endpoints públicos devem ter limite de requisições. |
| RN-002 | Autenticação deve ter limite mais estrito. |
| RN-003 | CORS não deve aceitar origem irrestrita em produção. |
| RN-004 | Headers de segurança devem ser aplicados. |
| RN-005 | Área operacional deve exigir autorização adequada. |

## 6. Impacto Backend

- Configurar `AddRateLimiter`.
- Configurar CORS por ambiente.
- Aplicar headers como HSTS, X-Frame e Referrer-Policy quando aplicável.
- Proteger dashboard Hangfire/operacional.
- Testes automatizados para rotas críticas.

## 7. Impacto QA

- Testar rate limit em autenticação.
- Testar CORS em origem permitida e negada.
- Verificar headers em produção/staging.
- Verificar dashboard operacional protegido.

## 8. Critérios de aceite

### CA-001 — Rate limit ativo

Dado que uma rota pública recebe requisições acima do limite,
quando o limite for excedido,
então o backend deve responder com bloqueio apropriado.

### CA-002 — CORS restrito

Dado que uma origem não permitida chama a API,
quando estiver em produção,
então a requisição deve ser negada pela política de CORS.

## 9. Decisão registrada

> Hardening P0 é obrigatório antes do teste aberto, especialmente para autenticação, admin e rotas públicas.

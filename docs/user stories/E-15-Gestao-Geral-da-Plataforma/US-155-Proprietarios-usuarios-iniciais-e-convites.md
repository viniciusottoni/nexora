# US-155 · Proprietários, usuários iniciais e convites

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Terceiro incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-14, RF-IAM-01, RF-IAM-06 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-021, ADR-023, ADR-031 |
| **Eventos** | EVT-058 `tenant.owner_access_changed`, EVT-072 `permission.changed` |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** acompanhar e recuperar o acesso inicial do proprietário,
> **para** que um convite expirado, e-mail incorreto ou troca de responsável não deixe o cliente sem gestão.

## 2. Contexto e motivação

A US-002 cria o proprietário e envia um convite válido por 72 horas, mas não define uma superfície para consultar, reenviar, revogar ou corrigir esse acesso. Como o convite contém segredo, recuperação deve sempre criar uma nova credencial e invalidar a anterior.

Esta história cobre o acesso administrativo inicial e a titularidade. A gestão cotidiana de funcionários e papéis permanece no `web-admin` do próprio tenant.

## 3. Escopo

### 3.1 Dentro desta história

- Exibição do proprietário atual e do estado do acesso: convidado, ativo, bloqueado ou sem proprietário
- Histórico de convites com expiração, entrega, aceitação, revogação e motivo de falha
- Reenvio com novo token e invalidação do anterior
- Correção de nome/e-mail antes da aceitação, com verificação de unicidade
- Transferência de titularidade com confirmação do novo e registro do antigo
- Revogação de convite pendente
- Desbloqueio administrativo seguro sem definir senha pelo suporte
- Resumo dos administradores iniciais, sem substituir o gerenciamento interno do tenant

### 3.2 Fora desta história

- Visualização ou definição de senha do usuário
- Cadastro cotidiano de garçons, cozinha e caixa
- Elevação silenciosa de permissões
- Acesso aos dados do cliente sem US-145

## 4. Critérios de aceite

```gherkin
Funcionalidade: Acesso inicial do proprietário

  Cenário: Convite expirado
    Dado um convite de proprietário expirado
    Quando o administrador solicitar reenvio
    Então um novo token deve ser criado com 72 horas de validade
    E qualquer token anterior deve ser invalidado
    E apenas o novo convite deve poder ser aceito

  Cenário: E-mail corrigido antes da aceitação
    Dado um convite pendente enviado ao endereço incorreto
    Quando o administrador corrigir o e-mail com motivo
    Então o convite anterior deve ser revogado
    E um novo convite deve ser enviado ao endereço corrigido

  Cenário: Transferência de titularidade
    Dado um proprietário ativo
    Quando a transferência para outro usuário for confirmada
    Então deve existir exatamente um proprietário principal
    E o anterior não deve manter privilégios por acidente
    E a transição deve ser auditada

  Cenário: Segredo não recuperável
    Dado um convite já emitido
    Quando o administrador consultar o histórico
    Então nenhum token bruto ou hash deve ser retornado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Ação sensível é auditada | Reenvio, correção, transferência, revogação e desbloqueio registram motivo |
| RN-015 | Isolamento total | Convites e usuários sempre validados contra o tenant da rota |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-058 | `tenant.owner_access_changed` | Convite/titularidade muda | tenantId, action, userId, inviteId, previousOwnerId, actorId | ↓ |
| EVT-072 | `permission.changed` | Papel OWNER é atribuído/removido | roleId, userId, permissions | ↑ |

## 7. Contrato de API

```http
GET /v1/platform/tenants/{id}/ownership
→ 200 { "owner": { "id": "...", "name": "...", "email": "...", "status": "INVITED" }, "invites": [...] }

POST /v1/platform/tenants/{id}/owner-invites
Idempotency-Key: <uuid>
{ "name": "...", "email": "...", "reason": "Correção solicitada no chamado #91" }
→ 201 { "inviteId": "...", "sentTo": "...", "expiresAt": "..." }

POST /v1/platform/tenants/{id}/ownership-transfers
{ "newOwnerUserId": "...", "reason": "Alteração societária", "keepPreviousAsAdmin": false }
```

## 8. Modelo de dados

Usa `app_user`, `role`, `user_role`, `owner_invite`, `email_outbox` e `audit_log`. A transferência exige constraint/garantia transacional de um único OWNER principal por tenant.

## 9. Comportamento offline

Convites e titularidade são operações de nuvem. Falha de entrega de e-mail não desfaz o usuário, mas marca o convite como falho e oferece reenvio seguro.

## 10. Interface e experiência

- Estado do convite com data de expiração e entrega
- E-mail parcialmente mascarado em listas; completo apenas quando necessário
- Reenvio explica que o link anterior deixará de funcionar
- Transferência mostra claramente quais permissões o antigo proprietário manterá
- Nenhuma interface exibe senha, token ou hash

## 11. Métricas, alertas e observabilidade

- Convites pendentes, expirados e falhos
- Tempo entre provisionamento e primeira autenticação do proprietário
- Reenvios por tenant e falhas do provedor de e-mail
- Transferências de titularidade e tentativas recusadas

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Expiração, revogação e transição de estados do convite |
| Integração | Rotação do token, outbox e unicidade do proprietário |
| Segurança | Token bruto nunca persistido/retornado; cross-tenant negado |
| E2E | Convite expirado → reenvio → aceitação do novo → rejeição do antigo |
| Concorrência | Duas transferências simultâneas não criam dois proprietários |

## 13. Dependências

**Depende de:** US-002, US-004, US-152  
**Habilita:** ativação confiável e autoatendimento da US-141

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Política de proprietário principal aprovada
- [ ] Estados e validade do convite definidos
- [ ] Conteúdo e remetente dos e-mails aprovados

**DoD**

- [ ] Histórico, reenvio, correção, revogação e transferência implementados
- [ ] Tokens antigos invalidados atomicamente
- [ ] Nenhum segredo aparece em log, API ou UI
- [ ] Auditoria e notificações registradas
- [ ] Testes de concorrência e isolamento passando

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA]** Definir se o proprietário anterior permanece como administrador por padrão após transferência.
- Mudança de e-mail após a ativação pode exigir confirmação nos dois endereços conforme política de segurança/LGPD.

---

*US-155 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

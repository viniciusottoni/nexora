# US-156 · Recuperação do provisionamento e token de instalação

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Terceiro incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-15 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-020, ADR-021, ADR-031 |
| **Eventos** | EVT-059 `installation.token_reissued` |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** retomar um provisionamento incompleto e reemitir a credencial de instalação,
> **para** recuperar com segurança um comando perdido sem recriar o tenant ou acessar o banco.

## 2. Contexto e motivação

O token de instalação da US-002 é exibido uma única vez. Se a interface falhar depois do `201`, a aba for fechada ou o comando for perdido, o tenant já existe, mas não há caminho administrativo de recuperação. Como apenas o hash deve ser persistido, o token original não pode ser revelado novamente.

A recuperação correta é rotação: revogar a credencial pendente anterior, emitir outra, mostrá-la uma vez e manter o mesmo tenant, loja e instalação.

## 3. Escopo

### 3.1 Dentro desta história

- Checklist de provisionamento reconstruído a partir de fatos persistidos
- Identificação de etapa incompleta e próxima ação segura
- Reemissão de token para instalação ainda não consumida
- Rotação/revogação atômica de todos os tokens pendentes anteriores da instalação
- Validade configurável com limite seguro
- Exibição única do novo token e comando, com copiar/baixar de forma consciente
- Revogação manual de token comprometido
- Histórico de emissões sem armazenamento do segredo bruto
- Tratamento específico do caso “tenant criado, resposta não exibida”

### 3.2 Fora desta história

- Recuperar ou descriptografar token anterior
- Reutilizar token consumido
- Criar um segundo tenant para contornar falha do primeiro
- Atualizar o parque instalado (US-146)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Recuperação do provisionamento

  Cenário: Resposta de criação foi perdida
    Dado que o tenant, a loja e a instalação foram criados
    E o token original não foi consumido
    Quando o administrador abrir o detalhe
    Então deve ver o provisionamento incompleto
    E deve poder reemitir um token sem duplicar tenant, loja ou instalação

  Cenário: Reemissão segura
    Dado um token pendente ainda válido
    Quando um novo token for emitido
    Então o anterior deve deixar de funcionar imediatamente
    E somente o hash do novo token deve ser persistido
    E o token bruto deve ser mostrado uma única vez

  Cenário: Token já consumido
    Dado uma instalação registrada com sucesso
    Quando alguém tentar reemitir o token inicial
    Então deve receber 409 INSTALLATION_ALREADY_REGISTERED
    E deve ser direcionado ao fluxo de manutenção apropriado

  Cenário: Repetição idempotente
    Dado que uma reemissão foi concluída
    Quando a mesma Idempotency-Key for repetida
    Então a resposta segura da intenção deve ser consistente
    E nenhum terceiro token deve ser criado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Ação sensível registra autor/contexto | Emissão e revogação entram em auditoria sem segredo |
| RN-015 | Isolamento total | Token fica vinculado a exatamente um tenant, store e installation |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-059 | `installation.token_reissued` | Rotação concluída | tenantId, installationId, previousCredentialId, credentialId, expiresAt, actorId | — |

O evento nunca carrega token bruto nem hash.

## 7. Contrato de API

```http
GET /v1/platform/tenants/{tenantId}/deployment
→ 200 { "completed": 7, "total": 9, "installation": { "id": "...", "status": "PENDING", "canReissueToken": true }, "nextAction": "REISSUE_INSTALL_TOKEN" }

POST /v1/platform/installations/{installationId}/tokens
Idempotency-Key: <uuid>
{ "reason": "Comando original não foi exibido", "expiresInHours": 24 }
→ 201 {
  "credentialId": "...", "expiresAt": "...",
  "installToken": "<mostrado uma vez>",
  "installCommand": "./install.sh --tenant=<id> --token=<token>"
}

DELETE /v1/platform/installations/{installationId}/tokens/{credentialId}
{ "reason": "Credencial possivelmente exposta" }
→ 204
```

## 8. Modelo de dados

| Tabela | Papel | Campos relevantes |
|---|---|---|
| `edge_installation` | Instalação alvo | tenant, store, status, registered_at |
| `installation_credential` | Credenciais rotacionáveis | id, installation_id, token_hash, expires_at, consumed_at, revoked_at |
| `audit_log` | Histórico seguro | ação, ator, motivo, credentialId; nunca segredo |

Se o modelo atual guarda hash diretamente em `edge_installation`, migrar para entidade de credencial preservando compatibilidade de consumo.

## 9. Comportamento offline

Emissão é exclusiva de nuvem. O comando pode ser executado no servidor alvo quando este tiver conectividade com a API Cloud. Nenhum token é armazenado em cache persistente do navegador.

## 10. Interface e experiência

- A ação explica que o token antigo será invalidado
- Motivo obrigatório; validade pré-selecionada com limite máximo
- Após sair da tela, o segredo desaparece e não pode ser reaberto
- Botões copiar/baixar mostram confirmação e aviso de custódia
- Checklist diferencia “token emitido” de “instalação registrada”

## 11. Métricas, alertas e observabilidade

- Reemissões por tenant/instalação e motivo
- Tempo entre emissão e consumo
- Tokens expirados sem consumo
- Tentativas com token revogado/consumido
- Reemissão excessiva gera alerta de segurança

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validade, rotação e reconstrução do checklist |
| Integração | Revogação e criação atômicas; consumo único |
| Segurança | Segredo ausente de logs, eventos, banco bruto e respostas posteriores |
| Concorrência | Duas reemissões simultâneas deixam somente uma credencial válida |
| E2E | Tenant existente → reemitir → copiar → sair → segredo não reaparece |

## 13. Dependências

**Depende de:** US-002, US-006, US-152  
**Habilita:** recuperação operacional sem intervenção no banco

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Política de validade e custódia aprovada
- [ ] Modelo de credenciais rotacionáveis revisado por segurança
- [ ] Comportamento idempotente de respostas secretas definido

**DoD**

- [ ] Checklist reconstruído por fatos persistidos
- [ ] Rotação atômica e consumo único testados
- [ ] Segredo exibido uma única vez e nunca logado
- [ ] Concorrência e idempotência cobertas
- [ ] Runbook de recuperação atualizado

## 15. Riscos, premissas e pendências

- Idempotência de resposta com segredo exige armazenamento seguro/efêmero da resposta original ou semântica específica; decidir antes da implementação.
- Reemissão após instalação registrada não pode reutilizar este fluxo, pois a confiança e o impacto são diferentes.

---

*US-156 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

# US-002 · Provisionar novo estabelecimento

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-05 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-013, ADR-032 |
| **Eventos** | EVT-054 |
| **Aplicações** | api-cloud, web-platform |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** criar um novo estabelecimento sem alterar código,
> **para** que a Replay consiga implantar em escala, com custo marginal baixo.

## 2. Contexto e motivação

A Visão Geral (seção 8.5) descreve nove passos de implantação de um novo estabelecimento. Esta história cobre os passos 1 e 2 — criação da instância e aplicação da configuração inicial — de forma que **nenhum deles exija desenvolvimento**.

A métrica de produto associada é dura: tempo de implantação de novo estabelecimento ≤ 5 dias úteis (PRD, seção 7). Se provisionar um tenant exigir um deploy, essa meta é inatingível e o modelo de negócio de receita recorrente não fecha.

## 3. Escopo

### 3.1 Dentro desta história

- Endpoint de criação de tenant com nome, slug, plano e modelo de negócio
- Aplicação de configuração padrão a partir do modelo (`PIZZERIA` como primeiro template)
- Criação do `store` inicial e do registro de `edge_installation`
- Geração de token de instalação de uso único, com validade
- Retorno do comando `./install.sh` pronto para copiar e colar
- Criação do usuário gestor inicial com convite por e-mail
- Tela de criação no `web-platform`

### 3.2 Fora desta história

- Provisionamento autoatendido pelo próprio cliente (US-141, Fase 5)
- Modelos por tipo de negócio além de pizzaria (US-142, Fase 5)
- Domínio próprio por cliente (US-143, Fase 5)
- Cobrança e planos comerciais

## 4. Critérios de aceite

```gherkin
Funcionalidade: Provisionamento de estabelecimento

  Cenário: Criação de tenant a partir de modelo
    Dado que informei nome, slug, plano e modelo de negócio "PIZZERIA"
    Quando confirmar a criação
    Então o tenant deve ser criado com a configuração padrão do modelo
    E devem existir store, praças de produção padrão e papéis padrão
    E deve ser gerado um token de instalação do servidor local
    E deve ser retornado o comando de instalação pronto para uso
    E nenhuma alteração de código deve ter sido necessária

  Cenário: Slug duplicado
    Dado que já existe um tenant com slug "dona-betinha"
    Quando tentar criar outro com o mesmo slug
    Então deve receber 422 com código SLUG_ALREADY_TAKEN
    E nenhum registro parcial deve permanecer no banco

  Cenário: Token de instalação de uso único
    Dado um token de instalação já consumido por uma instalação
    Quando ele for apresentado novamente
    Então deve ser recusado com 403
    E a tentativa deve ser registrada em audit_log

  Cenário: Convite do gestor inicial
    Dado que informei o e-mail do proprietário
    Quando o tenant for criado
    Então deve ser criado um usuário com papel OWNER
    E deve ser enviado convite de definição de senha com validade de 72 horas

  Cenário: Criação transacional
    Dado que a criação falha na etapa de geração de token
    Quando a transação for revertida
    Então nenhum tenant, store ou usuário parcial deve existir
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica de negócio deve existir como configuração, nunca como código | O modelo de negócio é um conjunto de valores em `tenant_config`, não um branch de código |
| RN-015 | Isolamento total entre estabelecimentos | O tenant nasce já com RLS ativo; nenhum dado semeado cruza fronteira |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-054 | `tenant.config_updated` | Configuração inicial aplicada | configVersion, template | ↓ |
| EVT-072 | `permission.changed` | Papéis padrão criados | roleId, permissions[] | ↑ |

## 7. Contrato de API

```http
POST /v1/platform/tenants
Authorization: Bearer <token de admin de plataforma, com 2FA>
Idempotency-Key: <uuid>
{
  "name": "Pizzaria Dona Betinha",
  "slug": "dona-betinha",
  "plan": "COMPLETO",
  "template": "PIZZERIA",
  "owner": { "name": "...", "email": "..." },
  "store": { "name": "Matriz", "timezone": "America/Sao_Paulo" }
}
→ 201 {
    "tenant": { "id": "...", "slug": "dona-betinha", "status": "PROVISIONED" },
    "store": { "id": "...", "name": "Matriz" },
    "installToken": "...",
    "installCommand": "./install.sh --tenant=<id> --token=<token>",
    "ownerInviteSentTo": "..."
  }

GET /v1/platform/tenants
GET /v1/platform/tenants/{id}
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `tenant` | Registro raiz criado | `id`, `slug`, `name`, `plan`, `status` |
| `tenant_config` | Configuração aplicada a partir do modelo | `config_version`, `operation`, `thresholds` (JSONB) |
| `store` | Loja inicial do tenant | `id`, `tenant_id`, `name`, `timezone` |
| `edge_installation` | Instalação prevista, ainda não registrada | `install_token`, `token_expires_at`, `status` |
| `app_user` / `role` / `user_role` | Gestor inicial e papéis padrão | `email`, `status=INVITED` |
| `station` | Praças padrão do modelo (forno, montagem, bebidas) | `code`, `name`, `capacity_slots` |

> Os dados semeados por modelo estão especificados em `Domain/12-Seeds-e-Dados-Iniciais.md`.

## 9. Comportamento offline

Operação exclusiva de nuvem. Não há caminho offline: provisionar um tenant é ato administrativo da plataforma, nunca da loja.

O edge server só existe **depois** desta história — o token gerado aqui é o que a US-006 consome.

## 10. Interface e experiência

- Formulário único no `web-platform`, sem etapas desnecessárias
- Slug sugerido automaticamente a partir do nome, com verificação de disponibilidade em tempo real
- Comando de instalação exibido com botão de copiar e aviso de que o token é de uso único
- Checklist de implantação (os nove passos da Visão Geral 8.5) exibido após a criação, com estado de cada passo

## 11. Métricas, alertas e observabilidade

- Tempo entre criação do tenant e primeira instalação registrada — insumo da meta de 5 dias úteis
- Contagem de tenants por status (PROVISIONED, INSTALLING, ACTIVE, SUSPENDED)
- Alerta de plataforma se um tenant permanecer em PROVISIONED por mais de 7 dias

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração de slug, validação de unicidade e expiração de token |
| Integração | Criação completa em transação única; falha em qualquer etapa reverte tudo |
| Integração | Token de instalação recusado no segundo uso |
| Isolamento | Dados semeados do tenant novo não são visíveis ao tenant existente |
| E2E | Fluxo do painel de plataforma até o comando de instalação copiável |

## 13. Dependências

**Depende de:** US-001  
**Habilita:** US-003, US-006, US-141

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **Pendência comercial aberta** — a definição de propriedade do produto e do modelo comercial (Visão Geral, 8.6) ainda não foi fechada. Isso não bloqueia esta história, mas bloqueia a definição de `plan`.
- O conjunto de seeds por modelo de negócio tende a crescer; manter em `Domain/12` e nunca em código de aplicação.

---

*US-002 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*
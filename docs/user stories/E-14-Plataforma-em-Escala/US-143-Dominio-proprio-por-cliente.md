# US-143 · Dominio proprio por cliente

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-PLT-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-010 |
| **Eventos** | — |
| **Aplicações** | api-cloud, infra/cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** usar o meu próprio domínio no cardápio e no delivery,
> **para** que meus clientes vejam a minha marca, não a de um fornecedor.

## 2. Contexto e motivação

Completa o white-label. Um cardápio em `cardapio.donabetinha.com.br` comunica marca própria; em `donabetinha.plataforma.com.br`, comunica que é um sistema de terceiro.

Tecnicamente exige emissão automática de certificado por domínio e resolução de tenant pelo host — que já é o mecanismo da US-003.

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de domínio ou subdomínio por tenant
- Verificação de propriedade por registro DNS
- Emissão e renovação automática de certificado TLS
- Resolução de tenant pelo host
- Redirecionamento do domínio padrão para o próprio
- Instruções de configuração de DNS para o cliente

### 3.2 Fora desta história

- Registro de domínio pela Replay
- E-mail no domínio do cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Domínio próprio

  Cenário: Verificação de propriedade
    Dado um domínio cadastrado
    Quando o cliente criar o registro DNS indicado
    Então a verificação deve confirmar a propriedade
    E o domínio deve ser ativado

  Cenário: Certificado automático
    Dado um domínio verificado
    Quando for ativado
    Então o certificado TLS deve ser emitido automaticamente
    E deve ser renovado antes do vencimento

  Cenário: Resolução de tenant pelo host
    Dado o domínio cardapio.donabetinha.com.br ativo
    Quando alguém acessar
    Então deve carregar o tenant correto com a marca correta

  Cenário: Domínio não verificado
    Dado um domínio cadastrado sem o registro DNS
    Quando a verificação for tentada
    Então deve falhar com instruções claras do que fazer

  Cenário: Falha de renovação
    Dado um certificado próximo do vencimento sem renovar
    Quando o limiar for atingido
    Então a plataforma deve ser alertada
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Isolamento entre estabelecimentos | Cada domínio resolve exatamente um tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/platform/tenants/{id}/domains
{ "domain": "cardapio.donabetinha.com.br" }
→ 201 { "domain": {...}, "verification": { "type": "TXT",
                                           "name": "_verify.cardapio...",
                                           "value": "..." },
        "status": "PENDING_VERIFICATION" }

POST /v1/platform/domains/{id}/verify
GET  /v1/platform/domains
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `tenant_domain` | Domínio do cliente | `domain`, `status`, `verified_at`, `cert_expires_at` |
| `tenant` | Domínio principal | `custom_domain` |

## 9. Comportamento offline

Operação de nuvem e de infraestrutura.

## 10. Interface e experiência

- Instruções de DNS em linguagem que um cliente não técnico consiga repassar ao provedor
- Verificação com um clique, mostrando o que falta quando falha
- Estado do certificado visível, com aviso antecipado de vencimento

## 11. Métricas, alertas e observabilidade

- Tenants com domínio próprio
- Tempo médio entre cadastro e verificação
- Falhas de renovação de certificado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Verificação por registro DNS |
| Integração | Emissão e renovação automática de certificado |
| Integração | Resolução de tenant pelo host correto |
| Isolamento | Domínio de um tenant não resolve outro |

## 13. Dependências

**Depende de:** US-003  
**Habilita:** —

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

- Configuração de DNS depende do provedor do cliente e é fonte comum de atrito. Instruções claras e verificação diagnóstica reduzem o custo de suporte.

---

*US-143 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*
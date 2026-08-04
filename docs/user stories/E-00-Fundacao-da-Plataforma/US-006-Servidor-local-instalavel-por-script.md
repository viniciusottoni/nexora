# US-006 · Servidor local instalavel por script

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-05, RF-OFF-01 |
| **Regras de negócio** | RN-005 |
| **ADRs** | ADR-001, ADR-033 |
| **Eventos** | — |
| **Aplicações** | infra/edge, api-edge, api-cloud |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** instalar o servidor da loja executando um único comando,
> **para** que a implantação seja replicável, previsível e não exija um especialista em cada cliente.

## 2. Contexto e motivação

O modelo local-first (ADR-001) coloca um servidor físico dentro de cada estabelecimento. Isso resolve o requisito estruturante de operação sem internet, mas cria um problema novo: **N instalações para manter**.

A única forma de isso escalar é a instalação ser um script idempotente, padronizado e auditável. Se a implantação exigir configuração manual, cada loja vira um floco de neve e o suporte se torna inviável — que é exatamente o risco 14 da Visão Geral.

A meta declarada: sistema operacional em menos de 30 minutos, em máquina limpa.

## 3. Escopo

### 3.1 Dentro desta história

- `docker-compose.yml` do edge com postgres, redis, api-edge, web (nginx), sync worker e watchtower
- Script `install.sh --tenant=<id> --token=<token>` idempotente
- Registro automático da instalação na nuvem, consumindo o token da US-002
- Download da configuração e do cardápio inicial
- Geração de certificados TLS locais (mkcert/ACME interno)
- Backup diário automático via cron, com cópia para a nuvem
- Health check dos serviços e comando de diagnóstico
- Documentação de hardware de referência

### 3.2 Fora desta história

- Atualização do parque (US-146, Fase 5)
- Cold standby automatizado (mitigação do risco T1, tratada na proposta de contingência)
- Provisionamento de rede e VLAN — responsabilidade de infraestrutura do cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Instalação do servidor local

  Cenário: Instalação de nova loja
    Dado um mini-PC com Docker instalado e o token de instalação válido
    Quando executar ./install.sh --tenant=X --token=Y
    Então os containers devem subir e passar no health check
    E a instalação deve se registrar na nuvem
    E cardápio e configuração devem ser baixados
    E os certificados TLS locais devem ser gerados
    E o sistema deve estar operacional em menos de 30 minutos

  Cenário: Reexecução do script
    Dado um servidor já instalado e operando
    Quando o script for executado novamente
    Então nenhum dado deve ser perdido
    E a operação deve ser idempotente

  Cenário: Token inválido ou já consumido
    Quando o script for executado com token inválido
    Então deve falhar antes de criar qualquer container
    E deve exibir mensagem clara indicando a causa

  Cenário: Instalação sem internet
    Dado que a internet da loja está indisponível durante a instalação
    Quando o script for executado
    Então deve falhar de forma explícita no passo de registro
    E deve permitir retomar do ponto de falha quando a conexão voltar

  Cenário: Backup diário
    Dado um servidor instalado há mais de 24 horas
    Quando o horário de backup for atingido
    Então deve ser gerado dump local do PostgreSQL
    E deve ser enviada cópia para a nuvem
    E a falha de envio deve alertar a plataforma
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet; a nuvem consolida | Todos os serviços operacionais rodam dentro do compose da loja |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-084 | `edge.reconnected` | Primeira conexão após a instalação | installationId, version | ↑ |

## 7. Contrato de API

```http
# Consumido pelo script, contra a nuvem:
POST /v1/platform/installations/register
X-Install-Token: <token da US-002>
{ "installationId": "<uuid gerado localmente>",
  "hostname": "...", "version": "1.0.0",
  "publicKey": "<chave pública do par gerado na instalação>" }
→ 201 { "tenant": {...}, "store": {...}, "configVersion": 88,
        "syncEndpoint": "https://api.../v1/sync" }

GET /v1/sync/pull?cursor=0&limit=500     # carga inicial de cardápio e config
GET /v1/sync/health                      # verificação de versão esperada

# Diagnóstico local:
GET https://edge.local/v1/health
→ { "postgres": "OK", "redis": "OK", "sync": "OK",
    "pendingEvents": 0, "lastSyncAt": "...", "version": "1.0.0" }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `edge_installation` | Registro da instalação na nuvem | `installation_id`, `public_key`, `version`, `last_seen_at`, `status` |
| `sync_cursor` | Cursor inicial de sincronização | `direction`, `cursor`, `updated_at` |
| Banco local | Cópia completa do schema, single-tenant | Mesmas migrations da nuvem (ADR-019) |

> O par de chaves assimétricas gerado na instalação é o que assina as requisições de sync por HMAC (doc. 02, seção 8).

## 9. Comportamento offline

Esta história **é** o que torna a operação offline possível. Depois de instalada, a loja opera integralmente sem internet: PostgreSQL local, WebSocket local, API local.

A instalação em si exige internet uma vez, no momento do registro e da carga inicial. O script deve falhar de forma explícita e retomável se a conexão cair no meio — nunca deixar a loja com instalação parcial.

## 10. Interface e experiência

- Saída do script legível por técnico não especialista, com passos numerados e estado de cada um
- Mensagem final com a URL de acesso local e as credenciais iniciais
- Comando único de diagnóstico (`./doctor.sh`) para suporte remoto
- Runbook impresso de instalação, para o técnico de campo

## 11. Métricas, alertas e observabilidade

- Tempo total de instalação — meta abaixo de 30 minutos
- Taxa de instalações concluídas na primeira tentativa
- Versão de cada instalação reportada à plataforma (base do risco T7 — deriva de versão)
- Sucesso do backup diário por instalação; falha gera alerta de plataforma

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Instalação completa em máquina limpa (contêiner de teste), do zero ao health check verde |
| Integração | Reexecução idempotente não perde dados |
| Integração | Falha de rede no meio da instalação permite retomada |
| Caos | Queda de energia durante a instalação não deixa estado corrompido |
| Restauração | Backup gerado é restaurável em servidor limpo — testado, não presumido |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-034, US-060, US-140

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
- [ ] Runbook de instalação e de recuperação escrito e testado por alguém que não desenvolveu
- [ ] Restauração de backup validada em ambiente limpo

## 15. Riscos, premissas e pendências

- **Risco T1 (doc. 02)** — falha física do servidor local no pico tem impacto crítico. Esta história entrega o backup; o cold standby pré-configurado é decisão comercial ainda pendente (pendência 5 do índice).
- Hardware do edge ainda não foi definido com o cliente; a especificação de referência do doc. 02, seção 9.1, precisa ser validada antes da compra.
- Responsabilidade pela manutenção física na loja é pendência aberta da Visão Geral (14.3).

---

*US-006 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*
# US-005 · Registro de dispositivos autorizados

> 🔄 **Substituída em 06/08/2026 pela [US-163 (E-16 · iMenu Online)](../E-16-iMenu-Online/US-163-Autorizacao-de-dispositivo-operacional.md).** O mecanismo de código de pareamento de 6 dígitos é reaproveitado; o que muda é o pareamento passar a ocorrer direto contra `iMenu.Api` pela internet (sem edge) e o nome do dispositivo passar a ser obrigatório no momento da autorização. As seções 9 ("comportamento offline") e o cenário de teste "pareamento com o edge desconectado" abaixo não se aplicam mais — ver US-163 para o comportamento atual.

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) — 🔄 **SUBSTITUÍDA POR US-163** |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-IAM-05 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-014, ADR-022 |
| **Eventos** | EVT-073 |
| **Aplicações** | api-edge, api-cloud, web-admin |
| **Autoridade do dado** | Local (registro acontece na loja) → sincronizado para a nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** autorizar explicitamente cada terminal que opera no meu estabelecimento,
> **para** que ninguém acesse o sistema pelo próprio celular sem que eu saiba.

## 2. Contexto e motivação

O login por PIN só é seguro porque é vinculado a um dispositivo confiável. Sem registro de dispositivo, um PIN de quatro dígitos vazado abre a operação inteira a partir de qualquer celular.

O registro também é a base da rastreabilidade exigida pela RN-004: todo evento carrega `deviceId`, e isso é o que permite responder "de qual terminal saiu este cancelamento" na trilha de auditoria.

## 3. Escopo

### 3.1 Dentro desta história

- Registro de dispositivo por código de pareamento gerado pelo gestor
- Identificação persistente do dispositivo no navegador (chave em IndexedDB + fingerprint)
- Tipos de dispositivo: terminal de caixa, KDS, celular de garçom, tablet de apoio
- Listagem, renomeação e revogação de dispositivos
- Revogação imediata encerra todas as sessões daquele dispositivo
- Registro do dispositivo em todo evento emitido

### 3.2 Fora desta história

- Gestão remota de dispositivo (MDM)
- Certificado por dispositivo (avaliado para fase posterior)
- Inventário de hardware

## 4. Critérios de aceite

```gherkin
Funcionalidade: Registro de dispositivos

  Cenário: Pareamento de novo terminal
    Dado que o gestor gerou um código de pareamento de 6 dígitos com validade de 10 minutos
    Quando o dispositivo informar esse código
    Então o dispositivo deve ser registrado com identificador persistente
    E o evento device.registered deve ser emitido
    E o código deve ser invalidado

  Cenário: Código expirado
    Dado um código de pareamento gerado há mais de 10 minutos
    Quando for informado
    Então deve ser recusado com 403
    E o gestor deve poder gerar um novo

  Cenário: Revogação de dispositivo
    Dado um celular de garçom que foi perdido
    Quando o gestor revogar o dispositivo
    Então todas as sessões daquele dispositivo devem ser encerradas imediatamente
    E novas tentativas de PIN nele devem ser recusadas
    E o evento deve ser registrado em audit_log

  Cenário: Identificação em evento
    Dado um pedido lançado no terminal "Caixa 1"
    Quando o evento order.placed for emitido
    Então o payload deve conter o deviceId do terminal
    E a trilha de auditoria deve permitir filtrar por dispositivo
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | O `deviceId` registrado aqui é o que viaja em todo evento do sistema |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-073 | `device.registered` | Terminal autorizado | deviceId, label, kind, registeredBy | ↑ |

> A revogação de dispositivo é registrada em `audit_log`, não como evento de domínio.

## 7. Contrato de API

```http
POST /v1/devices/pairing-codes           # gestor gera o código
→ 201 { "code": "418302", "expiresAt": "...", "expiresInSeconds": 600 }

POST /v1/devices/pair                    # dispositivo se apresenta
{ "code": "418302", "label": "Caixa 1", "kind": "CASHIER",
  "fingerprint": "..." }
→ 201 { "device": { "id": "...", "label": "Caixa 1" }, "deviceSecret": "..." }

GET    /v1/devices
PATCH  /v1/devices/{id}                  { "label": "Caixa 2" }
DELETE /v1/devices/{id}                  # revogação imediata
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `device` | Terminal registrado | `id`, `tenant_id`, `store_id`, `label`, `kind`, `status`, `fingerprint`, `last_seen_at` |
| `audit_log` | Registro de pareamento e revogação | `action`, `actor_id`, `target_id`, `before`, `after` |

## 9. Comportamento offline

O pareamento acontece na rede local, contra o edge server — a loja precisa conseguir registrar um terminal substituto durante uma queda de internet, senão uma falha de equipamento no pico vira parada de operação (risco T1 do documento 02).

A revogação feita na nuvem só chega ao edge no próximo pull de configuração. Para revogação urgente com internet caída, o gestor revoga pelo painel local do edge.

## 10. Interface e experiência

- Código de pareamento de 6 dígitos exibido em fonte grande no painel do gestor
- Tela de pareamento do dispositivo com apenas um campo — sem cadastro, sem senha
- Lista de dispositivos com último acesso, papel típico e estado de conexão
- Revogação com confirmação explícita, avisando que encerra sessões ativas

## 11. Métricas, alertas e observabilidade

- Contagem de dispositivos ativos por tipo e por loja
- Dispositivo sem acesso há mais de 30 dias sinalizado para revisão
- `device.id` como atributo de span OpenTelemetry em toda requisição (ADR-022)
- Alerta ao gestor a cada novo pareamento

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração, expiração e uso único do código de pareamento |
| Integração | Revogação encerra sessões ativas do dispositivo imediatamente |
| Integração | Pareamento funciona com o edge desconectado da nuvem |
| Segurança | Código de pareamento não é adivinhável por força bruta (rate limit + expiração curta) |

## 13. Dependências

**Depende de:** US-001  
**Habilita:** US-004, US-090

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

- Identificação de dispositivo em navegador é frágil por natureza (limpeza de dados do navegador força novo pareamento). Mitigação: pareamento rápido, de baixo atrito, e PWA instalado no terminal fixo.

---

*US-005 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*
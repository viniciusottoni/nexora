# US-163 · Autorização de dispositivo operacional

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-IAM-05 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-041 (substitui ADR-014 nesta parte) |
| **Eventos** | EVT-073 (reaproveitado de US-005) |
| **Aplicações** | `iMenu.Api`, `web-admin`, `web-pos`, `web-kds` |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** autorizar explicitamente cada dispositivo que acessa `/server`, `/kds` ou `/pos`, dando um nome a ele,
> **para** saber, quando alguém fizer login, exatamente qual aparelho está sendo usado e por quem.

## 2. Contexto e motivação

Esta história **substitui** a US-005 (E-00), mantendo o mecanismo já existente — código de pareamento de 6 dígitos — mas redesenhado para um mundo sem edge: o pareamento agora acontece direto contra `iMenu.Api`, pela internet, e o gestor nomeia o dispositivo no momento em que concede acesso.

O nome do dispositivo (ex.: "Celular Garçom 2", "Caixa 1", "Tablet Cozinha") é o que permite ao gestor, ao ver um login, saber de imediato **qual aparelho físico** está sendo usado — complementar à identificação pessoal por PIN (US-164), não um substituto dela.

## 3. Escopo

### 3.1 Dentro desta história

- Geração de código de pareamento de 6 dígitos pelo gestor, válido por 10 minutos (mecanismo já existente, reaproveitado)
- Dispositivo se apresenta com o código e recebe um identificador persistente + secret de dispositivo
- Gestor **nomeia** o dispositivo no momento da autorização (não depois) — nome obrigatório, não opcional
- Tipos de dispositivo: `SERVER` (garçom), `KDS` (cozinha), `POS` (caixa) — a distinção de tipo determina a rota esperada (`/server`, `/kds`, `/pos`)
- Listagem de dispositivos autorizados com nome, tipo, último acesso e estado
- Revogação imediata: encerra todas as sessões daquele dispositivo e invalida o secret de dispositivo
- Exclusão de dispositivo (remoção definitiva do registro, distinta de revogação)
- Todo evento emitido carrega o `deviceId`

### 3.2 Fora desta história

- Login pessoal por PIN no dispositivo já autorizado (US-164)
- Autorização de dispositivo de mesa — mesa não usa este mecanismo, usa o fluxo da US-165 (QR Code + número)
- Gestão remota de dispositivo (MDM), certificado por dispositivo — mantidas fora de escopo, como já estava na US-005

## 4. Critérios de aceite

```gherkin
Funcionalidade: Autorização de dispositivo operacional

  Cenário: Pareamento com nome obrigatório
    Dado que o gestor gerou um código de pareamento de 6 dígitos
    Quando o dispositivo informar esse código
    Então o gestor deve ser solicitado a dar um nome ao dispositivo antes da autorização ser concluída
    E o dispositivo deve ser registrado com esse nome, um identificador persistente e um secret de dispositivo
    E o evento device.registered deve ser emitido

  Cenário: Código expirado
    Dado um código de pareamento gerado há mais de 10 minutos
    Quando for informado
    Então deve ser recusado com 403
    E o gestor deve poder gerar um novo

  Cenário: Revogação de dispositivo
    Dado um celular de garçom perdido
    Quando o gestor revogar o dispositivo
    Então todas as sessões daquele dispositivo devem ser encerradas imediatamente
    E novas tentativas de login nele devem ser recusadas, mesmo com PIN correto
    E o evento deve ser registrado em audit_log

  Cenário: Identificação em evento
    Dado um pedido lançado no dispositivo "Celular Garçom 2"
    Quando o evento order.placed for emitido
    Então o payload deve conter o deviceId
    E a trilha de auditoria deve permitir filtrar por dispositivo e exibir o nome dado pelo gestor

  Cenário: Pareamento pela internet
    Dado que o dispositivo e o painel do gestor estão em redes diferentes
    Quando o pareamento for realizado
    Então deve funcionar normalmente, sem qualquer exigência de rede local compartilhada
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | `deviceId` viaja em todo evento; nome do dispositivo torna a auditoria legível para o gestor |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-073 | `device.registered` | Dispositivo autorizado | deviceId, name, kind, registeredBy | — |

## 7. Contrato de API

```http
POST /v1/devices/pairing-codes
{ "kind": "SERVER" }                       # ou "KDS" | "POS"
→ 201 { "code": "418302", "expiresAt": "...", "expiresInSeconds": 600 }

POST /v1/devices/pair
{ "code": "418302", "name": "Celular Garçom 2", "kind": "SERVER", "fingerprint": "..." }
→ 201 { "device": { "id": "...", "name": "Celular Garçom 2", "kind": "SERVER" },
        "deviceSecret": "<token de dispositivo, guardado no cliente>" }

GET    /v1/devices
PATCH  /v1/devices/{id}         { "name": "Caixa 2" }
DELETE /v1/devices/{id}         # revogação imediata
```

O `deviceSecret` retornado no pareamento é enviado (junto ao `deviceId`) em toda requisição de login por PIN (US-164) — é o que substitui, nesta revisão, a antiga camada de rede local do ADR-014.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `device` | Dispositivo autorizado | `id`, `tenant_id`, `store_id`, `label` (nome dado pelo gestor), `type`, `fingerprint`, `is_active`, `last_seen_at` |
| `audit_log` | Registro de pareamento e revogação | `action`, `actor_id`, `target_id` |

> A coluna `device.label` já existe (domain/01) — passa a ser obrigatória no momento do pareamento, não opcional (renomeada ou reaproveitada, a confirmar na implementação).

## 9. Comportamento offline

_Não se aplica — ver ADR-040. O pareamento depende de conexão com `iMenu.Api`, sempre; não há mais o cenário "pareamento contra o edge desconectado da nuvem" que a US-005 previa._

## 10. Interface e experiência

- Código de pareamento de 6 dígitos exibido em fonte grande no painel do gestor
- Tela de pareamento do dispositivo com campo do código **e** campo do nome, ambos obrigatórios
- Lista de dispositivos com nome, tipo, último acesso e estado — pesquisável e filtrável por tipo
- Revogação com confirmação explícita, avisando que encerra sessões ativas

## 11. Métricas, alertas e observabilidade

- Contagem de dispositivos ativos por tipo
- Dispositivo sem acesso há mais de 30 dias sinalizado para revisão
- Alerta ao gestor a cada novo pareamento
- `device.id` como atributo de span OpenTelemetry em toda requisição

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração, expiração e uso único do código de pareamento; nome obrigatório no pareamento |
| Integração | Revogação encerra sessões ativas e invalida o `deviceSecret` imediatamente |
| Integração | Pareamento funciona normalmente pela internet, sem qualquer rede compartilhada |
| Segurança | Código de pareamento resistente a força bruta (rate limit + expiração curta); `deviceSecret` nunca reexposto após o pareamento inicial |

## 13. Dependências

**Depende de:** US-161, US-162
**Habilita:** US-164

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Campos de `device` confirmados (nome obrigatório, tipo, secret)
- [ ] Fluxo de pareamento desenhado (tela do gestor + tela do dispositivo)

**DoD — a história só é concluída quando:**

- [ ] Pareamento, listagem, renomeação, revogação e exclusão funcionando ponta a ponta
- [ ] `deviceSecret` validado em toda requisição subsequente (ver US-164)
- [ ] Teste de isolamento multi-tenant
- [ ] Eventos emitidos conforme o catálogo
- [ ] Documentação atualizada (OpenAPI, modelo de dados)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- Identificação de dispositivo em navegador continua frágil por natureza (limpeza de dados força novo pareamento) — mesma mitigação da US-005 original: pareamento rápido, de baixo atrito, PWA instalado no terminal fixo quando possível.
- Esta história substitui formalmente a US-005 (E-00) — ver banner na própria US-005.

---

*US-163 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*

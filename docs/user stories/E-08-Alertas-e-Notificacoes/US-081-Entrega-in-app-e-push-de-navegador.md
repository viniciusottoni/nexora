# US-081 · Entrega in-app e push de navegador

|  |  |
|---|---|
| **Épico** | [E-08 · Alertas e Notificacoes](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-ALT-03 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-011, ADR-009 |
| **Eventos** | — |
| **Aplicações** | web-pos, web-kds, web-menu, web-admin, api-edge, api-cloud |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** todos os perfis operacionais e o gestor,
> **quero** receber o alerta onde eu estiver, sem precisar olhar o sistema o tempo todo,
> **para** que a informação chegue no momento em que ela ainda é útil.

## 2. Contexto e motivação

O cliente definiu o canal da primeira versão: *"inicialmente tudo dentro de push-up do sistema, no celular ou navegador"*. E-mail, SMS e WhatsApp ficam para fases posteriores.

São dois mecanismos com propósitos distintos. **In-app** (WebSocket) para quem está com o sistema aberto — cozinha, caixa, garçom. **Push de navegador** (Web Push, VAPID) para quem não está — tipicamente o gestor fora da loja e o cliente de delivery.

Detalhe arquitetural: o push é enviado pela nuvem, não pelo edge (doc. 02, seção 7.2).

## 3. Escopo

### 3.1 Dentro desta história

- Entrega in-app por WebSocket, com fallback de polling
- Push de navegador por VAPID, enviado pela nuvem
- Solicitação de permissão de notificação no momento certo
- Central de notificações in-app com histórico
- Reconhecimento do alerta pelo usuário
- Diferenciação visual e sonora por severidade

### 3.2 Fora desta história

- E-mail, SMS e WhatsApp (Fase 6)
- Direcionamento por perfil (US-082)
- Silenciamento por tipo (RF-ALT-05, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Entrega de alertas

  Cenário: Entrega in-app
    Dado o garçom com o sistema aberto
    Quando um alerta direcionado a ele for criado
    Então deve aparecer na interface em até 2 segundos
    E deve haver sinal visual e sonoro conforme a severidade

  Cenário: Push com o sistema fechado
    Dado o gestor fora da loja, com o navegador fechado
    E permissão de notificação concedida
    Quando um alerta crítico for criado
    Então deve receber push de navegador

  Cenário: Permissão não concedida
    Dado um usuário que não concedeu permissão de notificação
    Quando um alerta for criado
    Então deve ser entregue in-app quando ele abrir o sistema
    E deve haver convite discreto para ativar as notificações

  Cenário: Central de notificações
    Dado alertas recebidos ao longo do turno
    Quando o usuário abrir a central
    Então deve ver o histórico com estado de cada um
    E deve poder reconhecer os pendentes

  Cenário: Reconhecimento
    Dado um alerta entregue
    Quando o usuário reconhecê-lo
    Então o tempo até o reconhecimento deve ser registrado
    E o alerta deve sair da lista de pendentes

  Cenário: Entrega offline na rede local
    Dado que a loja está sem internet
    Quando um alerta operacional for criado
    Então deve ser entregue in-app normalmente pela rede local
    E o push de navegador só será enviado após a reconexão
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Esta história é o canal de entrega |
| RN-005 | A operação local não depende de internet | Entrega in-app 100% local |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
# WebSocket, in-app:
{ "type": "alert.raised",
  "data": { "alertId": "...", "alertType": "ORDER_LATE",
            "severity": "HIGH", "entityId": "...",
            "message": "Pedido A47 da mesa 12 está há 21 minutos na fila." } }

# Cliente reconhece:
{ "type": "ack", "data": { "alertId": "..." } }

# Web Push (enviado pela nuvem):
POST /v1/notifications/subscribe
{ "endpoint": "...", "keys": { "p256dh": "...", "auth": "..." } }

GET /v1/notifications?status=unread
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Alerta e estado de entrega | `acknowledged_at`, `acknowledged_by`, `delivered_at` |
| `push_subscription` | Assinatura de push por usuário e dispositivo | `endpoint`, `keys`, `user_id`, `device_id` |

## 9. Comportamento offline

Entrega in-app funciona integralmente na rede local, pelo WebSocket do edge, com fallback de polling.

Push de navegador depende de internet e é enviado pela nuvem. Com a loja offline, o gestor que está fora não recebe push — limitação inerente que precisa ser comunicada. O alerta de atraso de sincronização (US-066), detectado pela nuvem, cobre parcialmente essa lacuna.

## 10. Interface e experiência

- Alerta in-app nunca bloqueia a tela — a operação não pode ser interrompida por notificação
- Severidade diferenciada por cor e por som; crítico se distingue de informativo sem leitura
- Permissão de push pedida no momento em que o valor está claro, não no primeiro acesso
- Central de notificações acessível de qualquer tela, com contador de pendentes

## 11. Métricas, alertas e observabilidade

- Taxa de entrega por canal
- Tempo entre criação e reconhecimento, por tipo e perfil
- Percentual de usuários com push ativado
- Alertas não reconhecidos por turno

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Entrega in-app em menos de 2 s |
| Integração | Push entregue com o navegador fechado |
| Integração | Reconhecimento registra o tempo de resposta |
| Caos offline | Entrega in-app funcionando com internet caída |

## 13. Dependências

**Depende de:** US-080  
**Habilita:** US-077, US-082

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

- Push de navegador tem suporte irregular entre navegadores e sistemas, especialmente em iOS. Validar no piloto com os dispositivos reais do cliente.

---

*US-081 · Épico E-08 · Pacote 004_DonaBetinha · Replay Studio.*
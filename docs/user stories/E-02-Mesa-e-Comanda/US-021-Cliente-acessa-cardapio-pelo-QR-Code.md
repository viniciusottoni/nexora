# US-021 · Cliente acessa cardapio pelo QR Code

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-02 |
| **Regras de negócio** | — |
| **ADRs** | ADR-009, ADR-010, ADR-027 |
| **Eventos** | EVT-020 |
| **Aplicações** | web-menu, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1),
> **quero** abrir o cardápio lendo o QR Code da mesa, sem instalar nada,
> **para** que eu possa pedir na hora, sem esperar alguém me atender.

## 2. Contexto e motivação

É o primeiro contato do cliente final com o produto, e o mais implacável: se demorar mais de dois segundos ou pedir cadastro, o cliente desiste e chama o garçom — e o sistema perde a razão de existir.

Daí três decisões: **PWA e não app nativo** (ADR-009), porque exigir instalação mata a conversão na mesa; **sem cadastro obrigatório**, porque ninguém cria conta para pedir uma pizza; e **marca do estabelecimento**, nunca da Replay (ADR-010).

A meta de desempenho é dura: cardápio carregado em menos de 2 segundos em 4G (doc. 02, seção 11).

## 3. Escopo

### 3.1 Dentro desta história

- Resolução do `qr_token` para mesa, sessão e tenant
- Emissão de token anônimo de sessão, com escopo mínimo e expiração junto da sessão da mesa
- Carregamento do cardápio do canal `DINE_IN` com branding do tenant
- Abertura automática de sessão de mesa se ainda não houver uma (com a US-022)
- Service Worker com cache do cardápio e das imagens
- Otimização de imagem e pré-renderização para atingir a meta de 2 s
- Instalação opcional do PWA, oferecida sem insistência

### 3.2 Fora desta história

- Envio do pedido (US-030)
- Identificação do cliente dentro da mesa (RF-SAL-13, Fase 2)
- Login por telefone e OTP — isso é do canal de delivery (Fase 4)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Acesso ao cardápio pelo QR Code

  Cenário: Acesso sem instalação
    Dado um cliente sentado na mesa 12
    Quando ler o QR Code com a câmera
    Então o cardápio deve abrir no navegador em até 2 segundos em 4G
    E não deve ser exigido cadastro nem instalação
    E a marca exibida deve ser a do estabelecimento

  Cenário: Sessão de mesa já aberta
    Dado que a mesa 12 já tem sessão aberta pelo garçom
    Quando o cliente ler o QR Code
    Então ele deve entrar na sessão existente
    E deve ver os itens já lançados na mesa

  Cenário: Primeira leitura da mesa
    Dado que a mesa 12 está livre
    Quando o cliente ler o QR Code
    Então uma sessão deve ser aberta com origem QR
    E o evento table.session.opened deve ser emitido

  Cenário: Token inválido ou rotacionado
    Quando um QR Code antigo for lido
    Então deve ser exibida mensagem orientando a chamar o garçom
    E nenhuma informação de outra mesa deve ser revelada

  Cenário: Retorno após fechar o navegador
    Dado um cliente que fechou o navegador durante a refeição
    Quando ler o QR Code novamente
    Então deve retornar à mesma sessão, com o consumo preservado

  Cenário: Operação sem internet da loja
    Dado que a internet da loja caiu, mas o Wi-Fi local funciona
    Quando o cliente ler o QR Code
    Então o cardápio deve abrir normalmente pelo servidor local
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | Resolução e cardápio servidos pelo edge |
| RN-015 | Isolamento entre estabelecimentos | Token anônimo tem escopo de uma única sessão de mesa |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-020 | `table.session.opened` | Sessão aberta pela leitura do QR | tableId, source=QR, guestCount | ↑ |

## 7. Contrato de API

```http
GET /v1/public/table/{qrToken}
→ 200 {
    "table":   { "id": "...", "label": "12", "area": "Salão" },
    "session": { "id": "...", "openedAt": "...", "status": "OPEN" },
    "sessionToken": "<token anônimo, escopo mínimo>",
    "currentItems": [ { "name": "...", "quantity": 1, "status": "FIRED" } ],
    "total": 8700
  }
→ 404 { "code": "INVALID_TABLE_TOKEN" }

GET /v1/public/branding
GET /v1/public/menu?channel=DINE_IN
```

> O `sessionToken` autoriza apenas ações da própria mesa: ver cardápio, criar pedido, chamar garçom, pedir a conta. Nunca leitura de outra mesa.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `dining_table` | Resolução pelo token | `qr_token`, `status` |
| `table_session` | Sessão de consumo | `table_id`, `opened_at`, `source`, `status`, `guest_count` |

## 9. Comportamento offline

Funciona integralmente na rede local: o `web-menu` é servido pelo Nginx do edge e a API é a local. O cliente no Wi-Fi da loja pede normalmente com a internet caída.

O Service Worker (Workbox) mantém cardápio e imagens em cache, o que reduz o tempo de carregamento em acessos subsequentes e sustenta a operação em contingência (RF-OFF-08).

Limitação conhecida: cliente em 4G, fora do Wi-Fi da loja, depende da internet da loja para alcançar o edge. Comunicar ao cliente na implantação.

## 10. Interface e experiência

- Zero fricção: nenhuma tela intermediária entre ler o código e ver o cardápio
- Marca do estabelecimento em primeiro plano; nenhuma marca da Replay visível ao cliente final
- Convite a instalar o PWA aparece uma vez, discreto, e nunca bloqueia o fluxo
- Estado de carregamento com esqueleto de conteúdo, não com giro indefinido
- Mensagem de token inválido orienta a ação (chamar o garçom), sem jargão técnico

## 11. Métricas, alertas e observabilidade

- Tempo de carregamento do cardápio (p90) em 4G — meta abaixo de 2 s
- Taxa de leitura de QR que resulta em pedido enviado — conversão do canal de mesa
- Proporção de sessões abertas por QR versus por garçom — indicador de adoção
- Contagem de tokens inválidos, indicando QR Codes desatualizados no salão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Resolução de token válido, rotacionado e inexistente |
| Integração | Cliente entra em sessão já aberta pelo garçom e vê os itens lançados |
| Desempenho | Carregamento em menos de 2 s em 4G simulado, com cardápio de 200 itens |
| Segurança | `sessionToken` não permite acesso a outra mesa nem a rota administrativa |
| Caos offline | Fluxo completo com a internet da loja derrubada |
| E2E | Da câmera do celular ao cardápio, sem instalação |

## 13. Dependências

**Depende de:** US-020, US-003, US-010  
**Habilita:** US-024, US-025, US-030

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

- **Risco T3** — Wi-Fi instável na área de mesas tem probabilidade alta e impacto alto. Mitigação de infraestrutura: AP dedicado à área de clientes, separado da VLAN operacional.
- Cliente com celular antigo ou sem leitor de QR nativo precisa do caminho alternativo pelo garçom — os dois caminhos são equivalentes por desenho.

---

*US-021 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*
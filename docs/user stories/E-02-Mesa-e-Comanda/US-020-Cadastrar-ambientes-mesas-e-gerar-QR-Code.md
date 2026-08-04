# US-020 · Cadastrar ambientes mesas e gerar QR Code

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-01 |
| **Regras de negócio** | — |
| **ADRs** | ADR-016 |
| **Eventos** | EVT-054 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar os ambientes e as mesas do meu salão, com QR Code por mesa,
> **para** que o cliente consiga pedir da própria mesa e eu consiga medir o salão por área.

## 2. Contexto e motivação

A mesa é a unidade de medição do salão: giro, ocupação, ticket médio e faturamento por m² todos derivam dela. O ambiente (salão interno, varanda, mezanino) permite comparar áreas — informação que costuma revelar que uma parte do salão rende muito menos que a outra.

O QR Code carrega um token opaco por mesa, não o número da mesa. Isso evita que alguém adivinhe a URL de outra mesa e veja o consumo alheio.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de ambiente com nome e posição
- CRUD de mesa com rótulo, capacidade e ambiente
- Geração de `qr_token` opaco e rotacionável por mesa
- Exportação dos QR Codes em PDF pronto para impressão
- Criação em lote ("criar mesas 1 a 20")
- Estado da mesa: livre, ocupada, aguardando conta, em limpeza

### 3.2 Fora desta história

- Arte personalizada do QR Code com a marca (Fase 5)
- Planta baixa visual do salão com posicionamento real
- Unir e separar mesas (RF-SAL-09, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Cadastro de mesas e QR Code

  Cenário: Criação em lote
    Dado o ambiente "Salão"
    Quando o gestor criar as mesas de 1 a 20 em lote
    Então devem existir 20 mesas com rótulos sequenciais
    E cada uma deve ter um qr_token único e opaco

  Cenário: Exportação para impressão
    Dado 20 mesas cadastradas
    Quando o gestor exportar os QR Codes
    Então deve ser gerado um PDF com um código por página, identificado pelo rótulo da mesa

  Cenário: Rotação de token
    Dado uma mesa cujo QR Code foi fotografado por um cliente
    Quando o gestor rotacionar o token
    Então o código anterior deve deixar de funcionar
    E um novo QR Code deve ser gerado para impressão

  Cenário: Token não adivinhável
    Dado o qr_token da mesa 12
    Quando alguém tentar deduzir o token da mesa 13
    Então não deve haver relação previsível entre eles

  Cenário: Exclusão de mesa com histórico
    Dado uma mesa com sessões encerradas no histórico
    Quando o gestor tentar excluí-la
    Então a exclusão deve ser recusada e a desativação oferecida
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Isolamento entre estabelecimentos | `qr_token` resolve para exatamente um tenant |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-054 | `tenant.config_updated` | Mesa ou ambiente criado/alterado | areaId, tableId | ↓ |

## 7. Contrato de API

```http
POST /v1/areas                { "name": "Salão", "position": 1 }
POST /v1/tables               { "areaId": "...", "label": "12", "seats": 4 }
POST /v1/tables/bulk          { "areaId": "...", "from": 1, "to": 20, "seats": 4 }
POST /v1/tables/{id}/rotate-token
GET  /v1/tables/qr-codes.pdf?areaId=...
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `area` | Ambiente do salão | `name`, `position`, `is_active` |
| `dining_table` | Mesa física | `area_id`, `label`, `seats`, `qr_token`, `status` |

> `qr_token` é gerado com entropia criptográfica e indexado — é a chave de entrada do cliente no sistema.

## 9. Comportamento offline

Somente leitura no edge. A resolução do QR Code acontece localmente, contra a réplica de `dining_table` — o cliente precisa conseguir abrir o cardápio da mesa mesmo com a internet da loja caída, desde que esteja no Wi-Fi local.

## 10. Interface e experiência

- Criação em lote é obrigatória — cadastrar 20 mesas uma a uma é atrito desnecessário no onboarding
- PDF de QR Codes com rótulo grande da mesa, para conferência na hora de colar
- Aviso na rotação de token de que os códigos impressos precisam ser substituídos

## 11. Métricas, alertas e observabilidade

- Mesas ativas por ambiente — denominador do cálculo de ocupação e giro
- Contagem de rotações de token, indicando possível problema de segurança física

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração de token com entropia adequada e sem sequência previsível |
| Integração | Criação em lote é transacional |
| Integração | Token rotacionado invalida o anterior imediatamente |
| Segurança | Token de mesa de um tenant não resolve em outro |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-021, US-022, US-023

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

- Quantidade e planta das mesas é material pendente do cliente (Visão Geral 20.2). Sem isso, o cadastro fica genérico e precisa ser refeito na implantação.

---

*US-020 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*
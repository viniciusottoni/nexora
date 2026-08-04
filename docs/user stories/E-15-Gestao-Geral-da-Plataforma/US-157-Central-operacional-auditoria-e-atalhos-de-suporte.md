# US-157 · Central operacional, auditoria e atalhos de suporte

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Quarto incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-16, RF-PLT-07, RF-PLT-08 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-021, ADR-022, ADR-023 |
| **Eventos** | Consome EVT-056 a EVT-059, EVT-074, EVT-081 a EVT-084 |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** priorizar clientes que exigem atenção e consultar seu histórico administrativo,
> **para** agir rapidamente com contexto, auditoria e acesso seguro às ferramentas de suporte.

## 2. Contexto e motivação

A E-14 é dona das capacidades profundas de saúde e suporte. Esta história não as duplica: cria a camada de composição na raiz e no detalhe do tenant. Ela responde “quem precisa de atenção agora, por quê e qual ação autorizada está disponível?”.

## 3. Escopo

### 3.1 Dentro desta história

- Indicadores globais de tenants por status, instalações por saúde, convites pendentes e provisionamentos parados
- Fila priorizada de atenção com motivo explicável
- Linha do tempo administrativa por tenant: criação, status, plano, proprietário, credenciais, domínio, suporte e incidentes
- Filtros por tipo, período, ator e correlação
- Links contextuais para diagnóstico da US-140, suporte da US-145 e atualização da US-146
- Reconhecimento/resolução de pendência administrativa sem apagar o fato original
- Exportação auditável de metadados administrativos
- Atualização periódica com horário da última coleta

### 3.2 Fora desta história

- Implementar novamente diagnóstico, suporte ou atualização do parque
- Acessar conteúdo de pedido, caixa, estoque ou financeiro
- Alterar fatos históricos
- Criar métricas comerciais do cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Central operacional da plataforma

  Cenário: Priorização explicável
    Dado tenants com instalação offline, convite expirado e provisionamento parado
    Quando a visão geral for aberta
    Então cada pendência deve mostrar severidade, motivo e tempo nessa condição
    E a ordenação deve priorizar criticidade sem esconder itens menos graves

  Cenário: Linha do tempo administrativa
    Dado um tenant com mudanças de plano, status e proprietário
    Quando o administrador abrir o histórico
    Então deve ver os fatos em ordem cronológica
    E cada item deve informar ator, origem, motivo e correlationId quando aplicável

  Cenário: Atalho de suporte
    Dado uma instalação degradada
    Quando o administrador solicitar acesso aos dados do cliente
    Então deve ser encaminhado ao fluxo autorizado da US-145
    E nenhum token de suporte deve ser criado silenciosamente

  Cenário: Falha parcial
    Dado que o serviço de saúde está temporariamente indisponível
    Quando a central carregar
    Então dados administrativos disponíveis devem continuar visíveis
    E a seção de saúde deve indicar falha e horário da última coleta
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação sensível é auditável | Linha do tempo é append-only e preserva autor/contexto |
| RN-015 | Isolamento total | A central mostra apenas metadado técnico/administrativo; suporte exige US-145 |

## 6. Eventos emitidos e consumidos

Consome `tenant.status_changed`, `tenant.plan_changed`, `tenant.owner_access_changed`, `installation.token_reissued`, `support.access.granted`, `sync.delayed`, `edge.offline_detected` e `edge.reconnected`.

Reconhecer uma pendência cria registro administrativo próprio, sem modificar ou excluir o evento de origem.

## 7. Contrato de API

```http
GET /v1/platform/attention?severity=CRITICAL,HIGH&limit=25&cursor=...
→ 200 { "data": [{ "tenantId": "...", "tenantName": "...", "type": "INSTALLATION_OFFLINE", "severity": "CRITICAL", "since": "...", "reason": "Sem contato há 18 min", "action": { "kind": "OPEN_DIAGNOSTICS", "href": "..." } }], "nextCursor": null }

GET /v1/platform/tenants/{id}/administrative-timeline?type=STATUS,PLAN&from=...&to=...
→ 200 { "data": [{ "type": "STATUS_CHANGED", "occurredAt": "...", "actor": { "id": "...", "name": "..." }, "reason": "...", "correlationId": "...", "summary": { "from": "ACTIVE", "to": "SUSPENDED" } }] }
```

## 8. Modelo de dados

Usa projeções derivadas de `audit_log`, `domain_event`, `tenant_status_history`, `tenant_plan_history`, `owner_invite`, `edge_installation`, `installation_incident` e `support_access`. A projeção não altera as fontes.

## 9. Comportamento offline

Exclusivo de nuvem. Dados antigos só podem aparecer com timestamp e indicador “desatualizado”; ações administrativas não ficam em fila no navegador.

## 10. Interface e experiência

- Cards de resumo levam diretamente à lista filtrada correspondente
- Fila de atenção explica a regra que gerou a prioridade
- Linha do tempo usa linguagem humana e permite revelar IDs técnicos
- Ações destrutivas não aparecem como atalhos de um clique
- Falhas parciais são isoladas por seção

## 11. Métricas, alertas e observabilidade

- Pendências por tipo, severidade e idade
- Tempo médio até reconhecimento e resolução
- Incidentes descobertos pela plataforma antes do contato do cliente
- Uso dos atalhos de diagnóstico/suporte
- Latência e defasagem das projeções

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Classificação e ordenação explicável de severidade |
| Integração | Projeção de eventos/históricos sem perda nem duplicidade |
| Segurança | Ausência de dado de negócio e exigência de US-145 |
| Resiliência | Falha parcial e timestamp de defasagem |
| E2E | Card global → lista filtrada → detalhe → diagnóstico/suporte autorizado |

## 13. Dependências

**Depende de:** US-090, US-140, US-145, US-146, US-150, US-152, US-153, US-155, US-156  
**Habilita:** operação proativa da plataforma em escala

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Matriz de severidade e SLA aprovada
- [ ] Fontes e defasagem aceitável definidas
- [ ] Atalhos e permissões revisados

**DoD**

- [ ] Resumo, fila e linha do tempo implementados
- [ ] Priorização explicável e testada
- [ ] Falhas parciais não bloqueiam a página
- [ ] Nenhum atalho contorna autorização ou auditoria
- [ ] Métricas de detecção e resolução instrumentadas

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA]** Definir SLAs e limiares que transformam um fato em item de atenção.
- Agregações devem ser projeções; consultar tabelas operacionais em tempo real pode comprometer desempenho e isolamento.

---

*US-157 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

# E-08 · Alertas e Notificacoes

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 4 |
| **Pontos** | 21 |
| **Sprints previstas** | Sprint 7 |
| **Aplicações afetadas** | api-edge, api-cloud, web-pos, web-kds, web-menu, web-admin |
| **Pacotes do monorepo** | packages/domain, packages/events |

---

## 1. Objetivo do épico

Implementar o requisito declarado literalmente pelo cliente: *"alerta para cada usuário envolvido no processo, desde a mesa, caixa, cozinha"* e *"deve ter alertas em cada etapa para cada usuário"*.

O princípio de desenho que impede isso de virar ruído está no RF-ALT-01: **notificar cada perfil apenas sobre eventos que exigem ação dele**. Alerta para todo mundo é alerta que ninguém atende — e uma equipe que aprende a ignorar notificação perde também as que importam.

A matriz de alertas proposta está na Visão Geral, seção 15, e é marcada como *a validar*.

## 2. Valor entregue

- Cada perfil recebe apenas o que exige ação dele
- Limiares configuráveis por estabelecimento, calibráveis no piloto
- Entrega in-app e por push de navegador, sem dependência de e-mail ou SMS
- Agrupamento de alertas repetidos, evitando poluição
- Base de medição de tempo de resposta a cada tipo de alerta

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-080](./US-080-Motor-de-alertas-com-limiares-configuraveis.md) | Motor de alertas com limiares configuraveis | M | 8 | RF-ALT-01, RF-ALT-02 |
| [US-081](./US-081-Entrega-in-app-e-push-de-navegador.md) | Entrega in-app e push de navegador | M | 5 | RF-ALT-03 |
| [US-082](./US-082-Direcionamento-por-perfil-e-por-acao.md) | Direcionamento por perfil e por acao | M | 5 | RF-ALT-01 |
| [US-083](./US-083-Agrupamento-de-alertas-repetidos.md) | Agrupamento de alertas repetidos | S | 3 | RF-ALT-04 |

## 4. Ordem de execução recomendada

1. US-080 — motor de alertas com limiares
2. US-082 — direcionamento por perfil e ação
3. US-081 — entrega in-app e push
4. US-083 — agrupamento de repetidos

## 5. Dependências do épico

**Depende de:** E-00, E-03  
**Habilita:** E-07, E-10, E-12

## 6. Definition of Done do épico

- [ ] Matriz de alertas da Visão Geral implementada e validada com o cliente
- [ ] Todos os limiares configuráveis por tenant
- [ ] Alerta chegando apenas ao perfil que precisa agir
- [ ] Push de navegador funcionando para gestor fora da loja
- [ ] Alertas funcionando integralmente offline dentro da rede local

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Excesso de alertas fazer a equipe ignorar todos | Alta | Alto | Direcionamento por perfil, agrupamento e calibração no piloto; medir taxa de ignorados (RF-ALT-06) |
| Matriz de alertas não validada com o cliente | Média | Médio | Marcada como "a validar" na Visão Geral 15 — confirmar antes da Sprint 7 |

---

*Épico E-08 · Pacote 004_DonaBetinha · Replay Studio.*
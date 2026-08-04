# US-128 · Exportacao para o contador

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-09 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** exportar os dados financeiros no formato que meu contador usa,
> **para** que ele pare de pedir planilha e eu pare de montar à mão.

## 2. Contexto e motivação

O cliente tem contador externo responsável apenas pela contabilidade formal. Hoje o repasse é manual.

A exportação reduz trabalho dos dois lados e aumenta a qualidade do dado que chega ao contador — sem prometer integração contábil, que está fora do escopo.

## 3. Escopo

### 3.1 Dentro desta história

- Exportação em CSV e planilha
- Conteúdo: receitas por período e forma, despesas por categoria, folha
- Filtro por período
- Formato tabular estável, documentado
- Registro da exportação em auditoria

### 3.2 Fora desta história

- Integração direta com sistema contábil
- SPED e obrigações acessórias
- Emissão fiscal (pendência)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Exportação para o contador

  Cenário: Exportação do período
    Dado o mês de julho fechado
    Quando o gestor exportar em CSV
    Então deve receber receitas, despesas por categoria e folha
    E o formato deve ser tabular e estável

  Cenário: Filtro por período
    Dado a seleção de um trimestre
    Quando a exportação for gerada
    Então deve conter apenas os lançamentos do período

  Cenário: Registro em auditoria
    Dado uma exportação realizada
    Quando concluída
    Então deve ficar registrada em audit_log com autor e período

  Cenário: Período incompleto
    Dado um período com pendências de lançamento
    Quando a exportação for solicitada
    Então deve haver aviso sobre a incompletude
    E o gestor deve poder prosseguir mesmo assim
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Exportação registrada em auditoria |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/finance/export?format=csv&period=2026-07
→ 200 (text/csv)

GET /v1/finance/export?format=xlsx&from=2026-07-01&to=2026-09-30
→ 200 (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `financial_entry` | Origem da exportação | Todos os lançamentos do período |
| `payroll` | Folha do período | `total_cost`, itens |
| `audit_log` | Registro da exportação | `action=FINANCE_EXPORTED` |

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Exportação em dois cliques, com período pré-selecionado no mês anterior
- Aviso de período incompleto sem bloquear a exportação
- Formato documentado, para que o contador saiba o que esperar

## 11. Métricas, alertas e observabilidade

- Frequência de exportação
- Períodos exportados

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Conteúdo do CSV corresponde aos lançamentos do período |
| Integração | Exportação registrada em auditoria |
| Validação | Formato validado com o contador do cliente |

## 13. Dependências

**Depende de:** US-127  
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

- Formato precisa ser validado com o contador real antes de ser considerado pronto — cada escritório tem sua preferência.

---

*US-128 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*
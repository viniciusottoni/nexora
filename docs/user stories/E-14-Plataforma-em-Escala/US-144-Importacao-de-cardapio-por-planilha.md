# US-144 · Importacao de cardapio por planilha

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | S — Should have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-CAT-12 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | EVT-050 |
| **Aplicações** | web-admin, web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9) e gestor (P8),
> **quero** importar o cardápio inteiro de uma planilha,
> **para** que a carga inicial deixe de ser o passo mais lento da implantação.

## 2. Contexto e motivação

A Visão Geral é direta sobre isso (11.2): a carga inicial é *a tarefa mais trabalhosa do onboarding e um item que deve ser padronizado e otimizado, porque se repetirá em cada novo estabelecimento*.

Cadastrar sessenta produtos com variações e modificadores um a um é o gargalo real da meta de cinco dias úteis.

## 3. Escopo

### 3.1 Dentro desta história

- Modelo de planilha documentado
- Importação de categorias, produtos, variações e preços
- Validação com relatório de erros por linha
- Pré-visualização antes de confirmar
- Importação incremental, atualizando existentes
- Registro da importação em auditoria

### 3.2 Fora desta história

- Importação de fichas técnicas (avaliar em fase posterior)
- Importação de imagens em lote
- Integração com sistemas de terceiros

## 4. Critérios de aceite

```gherkin
Funcionalidade: Importação de cardápio

  Cenário: Importação completa
    Dado uma planilha no modelo com 60 produtos
    Quando a importação for executada
    Então categorias, produtos, variações e preços devem ser criados
    E o resultado deve ser exibido com a contagem de cada tipo

  Cenário: Erros por linha
    Dado uma planilha com 3 linhas inválidas
    Quando a validação executar
    Então deve ser exibido o erro de cada linha, com o número
    E nenhuma linha deve ser importada até a correção

  Cenário: Pré-visualização
    Dado uma planilha válida
    Quando a pré-visualização for solicitada
    Então deve mostrar o que será criado e o que será atualizado
    E nada deve ser gravado antes da confirmação

  Cenário: Importação incremental
    Dado produtos já cadastrados
    Quando uma planilha com os mesmos códigos for importada
    Então os existentes devem ser atualizados, não duplicados

  Cenário: Registro em auditoria
    Dado uma importação concluída
    Quando registrada
    Então audit_log deve conter autor, arquivo e contagens
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Importação registrada em auditoria |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.created` / `product.updated` | Produtos importados | productId, source=IMPORT | ↓ |

## 7. Contrato de API

```http
GET  /v1/catalog/import/template
→ 200 (planilha modelo)

POST /v1/catalog/import/validate     (multipart)
→ { "valid": false,
    "errors": [ { "row": 12, "column": "price",
                  "message": "Valor inválido" } ],
    "preview": { "toCreate": 57, "toUpdate": 0 } }

POST /v1/catalog/import              (multipart)
→ 201 { "created": { "categories": 6, "products": 57, "variants": 132 },
        "updated": {...}, "skipped": 0 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `category` / `product` / `product_variant` / `price` | Criados ou atualizados | — |
| `audit_log` | Registro da importação | `action=MENU_IMPORTED` |

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Modelo de planilha para download, com exemplos preenchidos
- Erros apontando linha e coluna exatas — planilha grande com erro genérico é inutilizável
- Pré-visualização obrigatória antes de gravar
- Resultado com contagem por tipo de objeto criado

## 11. Métricas, alertas e observabilidade

- Tempo de carga de cardápio com e sem importação
- Taxa de sucesso na primeira tentativa
- Erros mais frequentes — insumo para melhorar o modelo de planilha

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de cada tipo de erro possível |
| Integração | Importação incremental atualiza sem duplicar |
| Integração | Nada é gravado quando há erro de validação |
| Integração | Registro em auditoria |

## 13. Dependências

**Depende de:** US-010, US-011, US-014  
**Habilita:** US-141

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

- Modelo de planilha complexo demais faz o cliente errar e desistir. Manter o mínimo de colunas obrigatórias e validar cedo.

---

*US-144 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*
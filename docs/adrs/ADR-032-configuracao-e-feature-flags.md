# ADR-032 · Configuração por tenant e feature flags

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-010, ADR-013, ADR-029 |
| **Requisitos afetados** | RF-PLT-04, RF-ALT-02, RN-016, RNF-MAN-02 |

---

## Contexto

O ADR-013 proíbe código específico por cliente e determina que toda diferença entre estabelecimentos seja **configuração**. Isso transfere o peso para o sistema de configuração: se ele for fraco, a pressão volta como exceção no código.

Há também uma necessidade distinta, frequentemente confundida com a primeira: **feature flags** para desenvolvimento — permitir integrar código incompleto em `main` (ADR-029) sem expô-lo, e liberar funcionalidade gradualmente no parque.

Configuração e feature flag parecem a mesma coisa e não são. Confundi-las produz um sistema em que ninguém sabe se um parâmetro é permanente ou temporário.

## Decisão

**Três níveis distintos, com ciclos de vida diferentes:**

| Nível | Natureza | Quem altera | Ciclo de vida |
|---|---|---|---|
| **Configuração** | Parâmetro de negócio | Gestor do tenant | Permanente |
| **Módulo** | Funcionalidade contratada | Replay (plano) | Permanente |
| **Feature flag** | Controle de lançamento | Replay (técnico) | **Temporário — removido após estabilizar** |

## Detalhamento

### Configuração de negócio

Vive em `tenant_config`, em blocos JSONB desserializados via `System.Text.Json` e validados por `FluentValidation`:

```json
{
  "operation": {
    "serviceFeePercent": 10,
    "serviceFeeOptional": true,
    "maxDiscountPercentWithoutApproval": 5,
    "halfAndHalfPricing": "HIGHEST",
    "stockDeductionMoment": "ITEM_READY",
    "businessDayStartHour": 5,
    "bottleneck": { "resource": "OVEN", "slots": 5, "avgCookMinutes": 7 }
  },
  "thresholds": {
    "orderWarnMinutes": 12,
    "orderCriticalMinutes": 18,
    "cmvDivergencePercent": 5,
    "syncDelayMinutes": 5
  }
}
```

Regras: schema versionado; toda chave tem valor padrão; alteração gera evento (EVT-054) e auditoria; a interface explica o efeito prático em linguagem de negócio.

### Módulos por plano

```json
{ "modules": { "dineIn": true, "kds": true, "delivery": false, "stock": true, "finance": false } }
```

Módulo desativado não aparece na interface e tem as rotas bloqueadas no servidor — não basta esconder o botão.

### Feature flags

```csharp
if (await featureManager.IsEnabledAsync("dynamic-promise-time", tenantId))
{
    promise = CalculateDynamicPromise(...);
}
else
{
    promise = CalculateFixedPromise(...);
}
```

Implementado com `Microsoft.FeatureManagement` (ou solução equivalente), com um `IFeatureFilter` custom que resolve o percentual do parque ou a lista de tenants a partir da configuração persistida — mesmo contrato do desenho original, trocando apenas a biblioteca.

| Regra | Valor |
|---|---|
| Toda flag tem **dono e data de expiração** |
| Vida máxima | 90 dias |
| Expirada | Alerta no CI |
| Ativação | Percentual do parque ou lista de tenants |
| Remoção | Obrigatória após estabilizar — a flag some junto com o caminho antigo |
| Revisão | Trimestral, na revisão de arquitetura |

Flag que vira permanente é sinal de que deveria ter sido **configuração** desde o início.

### Como decidir o nível

```
A diferença é de negócio e o cliente decide?      → Configuração
É funcionalidade contratada por plano?            → Módulo
É controle temporário de lançamento?              → Feature flag
É "só para esse cliente"?                         → NÃO — ver ADR-013
```

### Propagação

| Nível | Propagação |
|---|---|
| Configuração | `configVersion++` → edge puxa em até 30 s (ADR-007) |
| Módulo | Idem |
| Feature flag | Consultada na nuvem; cacheada 60 s; padrão seguro se indisponível |

O edge mantém cópia local da configuração — precisa funcionar offline.

### Precedência

```
valor do tenant  >  valor do plano  >  padrão do produto
```

Configuração ausente **nunca** quebra: cai no padrão. Isso permite adicionar chave nova sem migrar todos os tenants.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Configuração em tabelas normalizadas | Consultável; tipada | Cada chave nova vira migration | Rígido demais para produto em evolução |
| Tudo em feature flag | Um só mecanismo | Confunde parâmetro permanente com controle temporário | Flags nunca seriam removidas |
| Serviço externo de flags (LaunchDarkly) | Recurso completo | Custo; dependência externa; não funciona offline | Desproporcional; edge precisa de autonomia |
| Configuração em arquivo por instalação | Simples | Alterar exigiria acesso ao servidor da loja | Gestor não conseguiria ajustar nada |
| Sem feature flags | Menos código | Exigiria branches longas, contrariando ADR-029 | Trunk-based depende de flags |

## Consequências

**Positivas**

- ADR-013 fica sustentável — há sempre um caminho legítimo para atender demanda
- Gestor ajusta o próprio negócio sem acionar a Replay
- Chave nova entra sem migration (JSONB com padrão)
- Trunk-based viável, com integração contínua de código incompleto
- Lançamento gradual no parque reduz risco

**Negativas**

- JSONB é menos tipado — exige validação disciplinada
- Flags acumulam se a remoção não for cobrada
- Três níveis podem confundir quem não conhece a distinção
- Configuração incorreta pode travar a operação

**Mitigações**

- `FluentValidation` valida toda leitura e escrita de configuração
- Expiração de flag alerta no CI; revisão trimestral obrigatória
- Fluxograma de decisão (acima) na documentação de onboarding do time
- Configuração crítica tem validação semântica (ex.: taxa de serviço entre 0 e 20%)

## Como validar

- Toda chave de configuração tem validator `FluentValidation` e valor padrão
- Nenhuma flag com mais de 90 dias (verificação no CI)
- Teste: tenant sem a chave usa o padrão, sem erro
- Teste: módulo desativado bloqueia a rota no servidor, não só na interface
- Teste: edge offline opera com a configuração em cache

## Revisitar quando

- O número de chaves crescer a ponto de exigir interface de busca e categorização
- Surgir necessidade de configuração por loja dentro de um mesmo tenant (rede multi-unidade)

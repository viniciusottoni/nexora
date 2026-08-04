# ADR-029 · Branching, versionamento e release do parque

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps |
| **Relacionados** | ADR-002, ADR-019 |
| **Requisitos afetados** | RNF-IMP-03 a 05, RNF-MAN-06 |

---

## Contexto

O produto tem dois destinos de deploy com naturezas opostas:

- **Nuvem** — sob nosso controle, deploy a qualquer hora, rollback rápido
- **Parque de edges** — N servidores em N lojas, cada um com sua janela, possivelmente offline no momento do release

Um mesmo commit precisa gerar artefatos para os dois. E o edge de uma loja pode estar duas versões atrás da nuvem, o que exige compatibilidade de contrato entre versões.

## Decisão

**Trunk-based development com versionamento semântico e release por tag. Deploy contínuo na nuvem; deploy em janela, com rollback automático, no parque.**

## Detalhamento

### Branching

```
main ──●──●──●──●──●──●──●──►   sempre pronto para release
        \    /    \    /
         feat/    fix/          branches curtas (< 2 dias)
```

| Regra | Valor |
|---|---|
| Branch de feature | Máximo 2 dias de vida |
| Merge | Squash, com mensagem convencional |
| `main` | Sempre liberável |
| Branch de release | Não existe — a tag é o release |
| Hotfix | Branch a partir da tag, correção, tag nova |

Feature grande entra atrás de feature flag (ADR-032), não em branch longa.

### Versionamento

```
MAJOR.MINOR.PATCH        ex.: 1.4.2

MAJOR   quebra de contrato de API ou incompatibilidade de edge
MINOR   funcionalidade nova, compatível
PATCH   correção, compatível
```

Uma única versão para todo o monorepo. Simplifica drasticamente o suporte: perguntar "qual versão você está usando" tem uma resposta só.

### Matriz de compatibilidade

| Componente | Compatibilidade exigida |
|---|---|
| Edge ↔ Nuvem (sync) | Nuvem aceita edge até **2 MINOR** atrás |
| Front ↔ API | Front avisa e recarrega se a API for mais nova |
| Migration | Compatível para trás por 1 versão (ADR-019) |
| Contrato de evento | Consumidor tolera versões antigas (doc. 04, §8) |

Edge com mais de 2 MINOR de atraso é bloqueado no sync e alerta a Replay — situação anômala que exige intervenção.

### Fluxo de release

```
Tag v1.4.2 em main
   │
   ├─► Build de imagens (api-cloud, api-edge, web-*, print-service)
   ├─► Testes E2E contra staging
   ├─► Deploy na nuvem            (imediato, rolling, sem downtime)
   └─► Publicação para o parque   (edges buscam na própria janela)
         ├─ backup → migration → subida → health check
         └─ falha  → rollback automático + alerta
```

### Deploy na nuvem

| Item | Valor |
|---|---|
| Estratégia | Rolling |
| Downtime | Zero |
| Rollback | Imagem anterior, em menos de 5 min |
| Migration | Aplicada antes, sempre compatível para trás |
| Canário | Acima de 20 lojas, 10% do tráfego por 30 min |

### Deploy no parque

| Item | Valor |
|---|---|
| Iniciativa | **Pull** — o edge busca; nunca push |
| Janela | Configurável por tenant, fora da operação |
| Ordem | Por ondas: lojas internas → 10% → 50% → 100% |
| Intervalo entre ondas | 24 h |
| Rollback | Automático por falha de health check |
| Bloqueio | Manual, por instalação, se necessário |

As ondas existem porque um defeito que só aparece em produção não pode atingir o parque inteiro na mesma noite.

### Ambientes

| Ambiente | Origem | Uso |
|---|---|---|
| Desenvolvimento | Local | Dia a dia |
| CI | Efêmero por PR | Pipeline |
| Staging | `main` a cada merge | E2E e homologação |
| Caos | `main` diário | Testes de rede e falha |
| Produção nuvem | Tag | Clientes |
| Produção edge | Tag, por onda | Lojas |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Git Flow | Estruturado | Branches longas; merges dolorosos; atrasa integração | Excesso para time pequeno com deploy contínuo |
| Versionamento independente por pacote | Granular | Matriz de compatibilidade explode; suporte fica confuso | "Qual versão você usa?" precisa ter uma resposta |
| Deploy push para o parque | Controle central | Loja pode estar offline ou em operação | Pull respeita a realidade da loja |
| Sem ondas | Mais simples | Defeito atinge todo o parque de uma vez | Risco alto |
| Edge sempre na última versão obrigatória | Sem matriz de compatibilidade | Loja offline ficaria bloqueada | Inviável |

## Consequências

**Positivas**

- Integração contínua real; sem merge doloroso
- Uma versão só, fácil de comunicar e suportar
- Parque atualiza sozinho, sem técnico no local
- Ondas limitam o alcance de um defeito

**Negativas**

- Trunk-based exige testes confiáveis e disciplina de PR pequena
- Feature flags acumulam se não forem removidas
- Ondas fazem o parque conviver com versões diferentes por alguns dias

**Mitigações**

- Pipeline de PR abaixo de 10 min, para não incentivar branch longa
- Revisão trimestral de feature flags obsoletas (ADR-032)
- Compatibilidade de 2 MINOR cobre com folga a janela das ondas
- Painel de plataforma mostra a distribuição de versões do parque

## Como validar

- Nenhuma branch com mais de 2 dias (métrica do repositório)
- Teste de compatibilidade: edge na versão N−2 sincroniza com nuvem na versão N
- Ensaio de rollback trimestral, em nuvem e em edge
- Nenhuma loja com mais de 2 MINOR de atraso

## Revisitar quando

- O parque crescer a ponto de exigir mais ondas ou canário mais elaborado
- Um cliente exigir controle próprio sobre quando atualizar

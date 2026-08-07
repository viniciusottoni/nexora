# ADR-019 · Migrations e compatibilidade do parque instalado

| | |
|---|---|
| **Status** | Substituído |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps |
| **Substituído por** | [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md) |
| **Relacionados** | ADR-005, ADR-029, ADR-033 |
| **Requisitos afetados** | RNF-MAN-04, RNF-MAN-05, RNF-IMP-02, RNF-IMP-03 |

---

> ⚠️ **Substituído em 06/08/2026 pelo [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md).** Sem parque de bancos distribuídos, a complexidade de compatibilidade "duas versões" tratada aqui deixa de ser um problema de escala — migração volta a ser rotina de banco único, com práticas usuais de zero-downtime deploy (RNF-MAN-04/05 permanecem como boa prática geral, não como necessidade estrutural). Conteúdo mantido como registro histórico.

## Contexto

Migration em um sistema com servidor único é rotina. Em um **parque distribuído** é outra coisa: cada loja tem seu próprio banco, pode estar em versão diferente, pode estar offline no momento do release e não tem ninguém tecnicamente capacitado no local para intervir se algo falhar.

Um erro de migration numa sexta-feira à noite significa uma pizzaria parada, sem ninguém para consertar.

## Decisão

**Migrations sempre compatíveis para trás por pelo menos uma versão, aplicadas automaticamente em janela configurada, com health check e rollback automático.**

Migration de **schema** e migration de **dados** são coisas separadas e nunca andam juntas.

## Detalhamento

### Regras de compatibilidade

| Operação | Regra |
|---|---|
| Adicionar coluna | Sempre `NULLABLE` ou com `DEFAULT`; nunca `NOT NULL` sem default |
| Remover coluna | **Duas versões**: v1 para de usar → v2 remove |
| Renomear coluna | **Nunca renomeia**: adiciona a nova, copia, para de usar a antiga, remove depois |
| Adicionar índice | `CREATE INDEX CONCURRENTLY` |
| Alterar tipo | Nova coluna + backfill + troca; nunca `ALTER TYPE` em tabela grande |
| Adicionar enum | Permitido (`ADD VALUE`) |
| Remover valor de enum | Duas versões |
| Adicionar constraint | `NOT VALID` primeiro, `VALIDATE` depois |
| Migration de dados | Job separado, nunca dentro da migration de schema |

### Por que a regra das duas versões

O edge pode estar rodando a versão anterior quando a migration chega. Se a migration remover uma coluna que o código antigo ainda usa, a aplicação quebra entre a migration e a atualização do container.

```
v1.4  código para de escrever em `old_column`   (coluna ainda existe)
v1.5  migration remove `old_column`             (nada mais a usa)
```

### Fluxo de atualização do edge

```
1. Edge consulta a nuvem  → GET /v1/sync/health → expectedVersion
2. Fora do horário de operação (janela configurada por tenant)
3. Backup do banco local                        ← obrigatório antes de tudo
4. docker pull das novas imagens
5. dotnet ef database update
6. Sobe a nova versão
7. Health check: API responde, SignalR sobe, banco consistente
8a. OK       → confirma versão na nuvem
8b. Falhou   → rollback automático (imagem anterior + restore do backup)
              → alerta imediato à Replay
```

### Janela de atualização

```json
{ "maintenanceWindow": { "start": "04:00", "end": "06:00", "timezone": "America/Sao_Paulo" } }
```

Nenhuma atualização ocorre em horário de operação. Se a janela for perdida (loja offline), tenta na próxima.

### Migrations de dados

Rodam como job assíncrono após a subida da versão, em lotes, com possibilidade de pausa:

```csharp
// Nexora.Tools.DataMigrations/BackfillBusinessDay.cs
await ProcessInBatchesAsync(batchSize: 1000, async batch => { ... });
```

Motivo: uma migration de dados que trave por 20 minutos bloqueia o deploy e pode estourar a janela.

### Validação antes do parque

Toda migration é aplicada, em CI, sobre um **dump real anonimizado de produção** antes de qualquer release chegar às lojas.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Migration manual por loja | Controle total | Impossível em escala; exige técnico no local | Inviabiliza a Fase 5 |
| Sem regra de compatibilidade | Mais rápido de escrever | Quebra durante a janela de atualização | Risco de loja parada |
| Migration só na nuvem, edge sem schema próprio | Simples | Edge não teria banco local | Viola ADR-001 |
| Blue-green no edge | Zero downtime | Exige o dobro de recurso no mini-PC | Desproporcional ao hardware da loja |

## Consequências

**Positivas**

- Atualização do parque sem intervenção humana no local
- Falha de migration não deixa loja parada — rollback automático
- Compatibilidade para trás elimina a janela de quebra
- Backup obrigatório antes de cada atualização

**Negativas**

- Remoção de coluna leva dois ciclos de release
- Mais disciplina na escrita da migration
- Backup antes de cada atualização consome tempo e espaço

**Mitigações**

- Checklist de migration na revisão de PR
- Verificação automática no CI de padrões proibidos (`DROP COLUMN` na mesma versão que parou de usar, `NOT NULL` sem default)
- Backup incremental para reduzir tempo

## Como validar

- CI aplica toda migration sobre dump real anonimizado
- Teste de atualização: versão N−1 com a migration da versão N aplicada — aplicação continua funcionando
- Ensaio de rollback em ambiente de caos, trimestralmente
- Nenhuma loja em versão mais de duas atrás (monitorado no painel de plataforma)

## Revisitar quando

- O parque crescer a ponto de exigir atualização em ondas (canário por grupo de lojas)

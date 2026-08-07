# ADR-027 · Estratégia de offline no dispositivo

| | |
|---|---|
| **Status** | Substituído |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md) |
| **Relacionados** | ADR-001, ADR-009, ADR-011, ADR-020, ADR-028 |
| **Requisitos afetados** | RF-OFF-08, RNF-DIS-06, RNF-OFF-01 |

---

> ⚠️ **Substituído em 06/08/2026 pelo [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md).** Decisão de negócio: sem internet, o sistema fica indisponível — igual a qualquer cardápio web comum, sem fila de ações nem cache de contingência. As duas camadas de resiliência descritas aqui (cache de leitura e fila de escrita) são removidas, não apenas a parte específica de queda do edge. Conteúdo mantido como registro histórico.

## Contexto

O ADR-001 resolve a queda de **internet**: o edge continua servindo a loja. Mas restam duas falhas que ele não cobre:

1. **O dispositivo perde o Wi-Fi** — o garçom anda até o fundo do salão e sai da cobertura
2. **O próprio edge cai** — falha de hardware, energia ou processo

O segundo caso foi identificado como risco T1 na arquitetura (impacto crítico). Sem tratamento, a loja para completamente até alguém intervir.

## Decisão

**Duas camadas de resiliência no dispositivo:**

1. **Cache de leitura** — catálogo, mesas e configuração ficam disponíveis offline
2. **Fila de escrita** — ações são enfileiradas localmente e reenviadas quando a conexão volta

O escopo é deliberadamente **limitado**: o dispositivo não vira um segundo servidor.

## Detalhamento

### Camadas

| Camada | Tecnologia | Conteúdo |
|---|---|---|
| Assets | Service Worker (Workbox) | HTML, JS, CSS, fontes, ícones |
| Dados de leitura | IndexedDB (Dexie) | Catálogo, branding, mesas, configuração |
| Fila de escrita | IndexedDB | Ações pendentes com `Idempotency-Key` |
| Estado de sessão | Memória + IndexedDB | Pedido em edição, mesa aberta |

### Fila de ações

```ts
interface QueuedAction {
  id: string;                 // UUIDv7 — também é a Idempotency-Key (ADR-020)
  endpoint: string;
  method: 'POST' | 'PATCH';
  payload: unknown;
  occurredAt: string;         // horário REAL da ação (ADR-034)
  attempts: number;
  status: 'PENDING' | 'SENDING' | 'FAILED';
}
```

Ao reconectar, a fila é drenada **em ordem**, com `X-Occurred-At` preservando o horário original — para que a métrica fique correta (RN-020).

### Regra central de segurança

> **A fila só aceita ações que não dependem de estado do servidor para serem válidas.**

| Enfileirável | Não enfileirável |
|---|---|
| Criar pedido | Fechar conta (depende do total consolidado) |
| Adicionar item | Aplicar desconto (exige autorização online) |
| Avançar item no KDS | Fechar caixa (exige conferência) |
| Chamar garçom | Cancelar item iniciado (exige autorização) |
| Solicitar conta | Registrar pagamento |

Ações não enfileiráveis exibem mensagem clara: *"Sem conexão com o servidor. Esta ação precisa de conexão."*

### Contingência de falha do edge

Se o edge estiver inacessível (não apenas a internet), o dispositivo entra em **modo contingência**:

```
┌────────────────────────────────────────────┐
│ ⚠ Sem conexão com o servidor da loja       │
│ Pedidos serão enviados quando reconectar.  │
│ Pendentes: 7                               │
└────────────────────────────────────────────┘
```

O garçom continua lançando pedidos com o cardápio em cache. Eles **não chegam à cozinha** até o edge voltar — o que é uma limitação real e precisa estar explícita na tela, para que a equipe acione o procedimento manual.

Isso não substitui o runbook de contingência (ADR-033); é uma ponte para falhas curtas, de minutos.

### Limites deliberados

| Limite | Valor | Motivo |
|---|---|---|
| Ações na fila | 200 | Acima disso, o problema é operacional, não técnico |
| Idade máxima da fila | 4 h | Ação muito antiga provavelmente perdeu validade |
| Tamanho do cache de catálogo | 5 MB | Fotos ficam em CDN, com cache separado |
| Retenção de dados após logout | Zero | Segurança |

### Indicação visual — requisito, não enfeite

| Estado | Indicação |
|---|---|
| Online | Sem indicação (o normal não precisa de aviso) |
| Sem internet, edge OK | Ícone discreto — a operação está normal |
| Sem edge | **Faixa persistente** com contador de pendências |
| Fila drenando | Progresso |
| Falha permanente | Alerta com ação sugerida |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sem offline no cliente | Simples | Qualquer oscilação de Wi-Fi interrompe o garçom | Frustrante em uso real |
| Réplica completa do banco no dispositivo | Máxima autonomia | Complexidade de sincronização entre N dispositivos; sem fonte de verdade | Recria o problema que o ADR-001 resolveu |
| CRDT no cliente | Convergência automática | Semântica de negócio não é comutativa | Mesmo motivo do ADR-007 |
| Só cache de leitura, sem fila | Mais simples | Garçom perde o pedido que estava montando | Perda de trabalho é inaceitável |

## Consequências

**Positivas**

- Oscilação de Wi-Fi deixa de interromper o trabalho
- Falha curta do edge não faz o garçom perder o pedido em edição
- Idempotência (ADR-020) torna a drenagem da fila segura
- Horário real preservado — métrica não se corrompe

**Negativas**

- Complexidade adicional no frontend
- Estado offline pode confundir se a sinalização for ruim
- Fila com falha permanente exige intervenção manual
- Cache desatualizado pode mostrar produto que já esgotou

**Mitigações**

- Sinalização explícita, testada com usuários reais (teste U-01)
- Fila com falha permanente oferece "reenviar" e "descartar", com confirmação
- Catálogo revalidado a cada reconexão (ADR-028)
- Limite de idade evita reenviar ação sem sentido

## Como validar

- Teste: desligar Wi-Fi do dispositivo no meio do lançamento — pedido é enfileirado e enviado ao reconectar
- Teste: parar o edge — dispositivo entra em contingência com faixa visível
- Teste: fila drenada em ordem, com horários originais preservados
- Teste: ação não enfileirável exibe mensagem clara, sem enfileirar

## Revisitar quando

- A falha do edge se mostrar frequente o suficiente para justificar autonomia maior no dispositivo

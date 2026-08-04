# ADR-026 · Impressão térmica por serviço no edge

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps |
| **Relacionados** | ADR-009, ADR-001, ADR-025 |
| **Requisitos afetados** | RF-CXA-12, RF-CXA-57 |

---

## Contexto

O ADR-009 escolheu PWA para todas as interfaces. A limitação conhecida é impressão: o navegador não acessa impressora térmica diretamente, e `window.print()` produz um resultado inadequado para bobina de 80 mm — margens erradas, fonte errada, sem comando de guilhotina.

Mesmo com o objetivo declarado de *"não ter papel passando para cima e para baixo"*, alguns papéis permanecem necessários: comprovante de consumo ao cliente, fechamento de caixa e, futuramente, documento fiscal (ADR-025).

## Decisão

**Um serviço de impressão roda no servidor local (edge) e fala com as impressoras via ESC/POS. As aplicações web solicitam impressão pela API — nunca imprimem diretamente.**

## Detalhamento

### Arquitetura

```
web-pos (navegador)
   │ POST /v1/print
   ▼
api-edge ──► fila de impressão ──► print-service (container no edge)
                                        │ ESC/POS
                     ┌──────────────────┼──────────────────┐
                     ▼                  ▼                  ▼
              Impressora caixa   Impressora cozinha   Impressora balcão
              (USB ou rede)      (rede)               (rede)
```

### Contrato

```http
POST /v1/print
{
  "printerId": "caixa-01",
  "template": "RECEIPT",
  "data": { "sessionId": "...", "copies": 1 }
}
→ 202 { "jobId": "...", "status": "QUEUED" }
```

A resposta é assíncrona: impressão nunca bloqueia o fechamento da conta. Falha de impressora não impede a venda.

### Cadastro de impressoras

```json
{
  "printers": [
    { "id": "caixa-01",   "name": "Caixa",   "connection": "usb",  "path": "/dev/usb/lp0", "width": 48 },
    { "id": "cozinha-01", "name": "Cozinha", "connection": "network", "host": "192.168.1.50", "port": 9100, "width": 42 }
  ]
}
```

Configuração por tenant (ADR-032), não código.

### Modelos de impressão

| Modelo | Uso |
|---|---|
| `RECEIPT` | Comprovante de consumo ao cliente |
| `BILL_PREVIEW` | Pré-conta na mesa |
| `CASH_CLOSING` | Fechamento de caixa |
| `ORDER_TICKET` | Comanda de produção — **fallback se o KDS cair** |
| `DELIVERY_LABEL` | Etiqueta de entrega (Fase 4) |
| `FISCAL` | Documento fiscal (ADR-025) |

### O modelo `ORDER_TICKET` é rede de segurança

Se o KDS falhar no meio do serviço, a impressão de comanda permite que a cozinha continue trabalhando enquanto o problema é resolvido. Não é o modo de operação desejado — é contingência. Fica desativada por padrão e é acionável em um toque pelo gerente.

### Fila e resiliência

| Situação | Comportamento |
|---|---|
| Impressora sem papel ou offline | Job fica na fila; alerta ao operador; venda segue |
| Reconexão da impressora | Fila é drenada automaticamente |
| Job com mais de 30 min na fila | Descartado com registro |
| Reimpressão | Endpoint dedicado, registrado em auditoria |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| `window.print()` do navegador | Zero infraestrutura | Formatação inadequada para bobina; sem guilhotina; exige diálogo | Inutilizável na prática |
| Aplicativo Electron no caixa | Acesso direto ao hardware | Mais um artefato para distribuir e atualizar no parque | Contraria ADR-009 sem necessidade |
| Agente instalado em cada máquina | Flexível | N agentes por loja para instalar e atualizar | O edge já está lá; centralizar é mais simples |
| Impressora com API HTTP própria | Sem serviço intermediário | Restringe os modelos de impressora suportados | Limita a escolha do cliente |
| Impressão via nuvem | Centralizado | Depende de internet; viola ADR-001 | Impressão precisa funcionar offline |

## Consequências

**Positivas**

- PWA mantido em todas as interfaces
- Um único ponto de configuração de impressoras por loja
- Impressão assíncrona não bloqueia a venda
- Fila torna falha de impressora um incômodo, não um bloqueio
- Fallback de comanda impressa protege contra falha do KDS

**Negativas**

- Mais um container no edge para operar e monitorar
- Compatibilidade ESC/POS varia entre fabricantes
- Impressora USB exige mapeamento de dispositivo no container

**Mitigações**

- Lista de modelos homologados, testados antes de recomendar ao cliente
- Health check da impressora no heartbeat da instalação (ADR-022)
- Preferência por impressora de rede na recomendação de hardware — evita mapeamento USB

## Como validar

- Impressão de comprovante em impressora homologada, com corte automático
- Impressora desligada: venda conclui, job fica na fila, alerta é exibido
- Impressora religada: fila drena sozinha
- Teste de fallback: KDS desligado, comanda impressa ativada em um toque

## Revisitar quando

- Um cliente exigir modelo de impressora não compatível com ESC/POS
- A emissão fiscal (ADR-025) impuser requisito específico de impressão

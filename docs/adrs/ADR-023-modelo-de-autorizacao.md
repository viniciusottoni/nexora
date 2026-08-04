# ADR-023 · Modelo de autorização RBAC com elevação pontual

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-004, ADR-013, ADR-014 |
| **Requisitos afetados** | RF-IAM-02, RF-IAM-07, RF-PED-05, RF-CXA-05, RN-011 |

---

## Contexto

O produto tem nove perfis (doc. 01, §3) com necessidades bem distintas, e a diretriz de produto replicável exige que **cada estabelecimento possa definir quem pode o quê** — uma pizzaria pequena pode deixar o garçom aplicar desconto; uma maior, não.

Ao mesmo tempo, há um padrão operacional universal em restaurantes: o **gerente autoriza pontualmente** uma ação do operador, sem assumir o terminal. Cancelar item que já está no forno, aplicar desconto acima do limite, fechar caixa com divergência. Modelar isso como "trocar de usuário" quebraria o fluxo e faria o operador perder a tela em que estava.

## Decisão

**RBAC com permissões granulares, papéis configuráveis por tenant, e mecanismo de elevação pontual (`authorizationToken`) para ações sensíveis.**

## Detalhamento

### Estrutura

```
usuário ──N:N── papel ──1:N── permissão
                  │
             configurável por tenant
```

```json
{
  "code": "WAITER",
  "name": "Garçom",
  "permissions": [
    "table:open", "table:close_request", "table:transfer",
    "order:create", "order:read", "order:add_item",
    "order:cancel_queued",
    "kds:read"
  ]
}
```

### Convenção de permissão

```
<recurso>:<ação>[_<qualificador>]

order:cancel_queued    cancelar item ainda em fila
order:cancel_started   cancelar item já iniciado  ← sensível
cash:discount_limited  desconto até o limite configurado
cash:discount_any      desconto sem limite         ← sensível
```

O qualificador é o que permite graduar poder sem multiplicar papéis.

### Papéis de sistema (padrão em toda instalação)

| Código | Nome | Observação |
|---|---|---|
| `OWNER` | Proprietário | Todas as permissões do tenant |
| `MANAGER` | Gerente | Operação completa + autorizações |
| `CASHIER` | Caixa | Caixa e comandas |
| `WAITER` | Garçom | Salão |
| `KITCHEN` | Cozinha | KDS |
| `STOCK` | Estoque | Insumos e compras |
| `COURIER` | Entregador | Entregas atribuídas |

São **modelos**, não imutáveis: o tenant pode ajustar permissões, criar papéis e renomear. O que não muda é o conjunto de permissões existentes — isso é produto (ADR-013).

### Elevação pontual

```
Garçom tenta cancelar item em produção
   │
   ▼ 403 AUTHORIZATION_REQUIRED  (ADR-021)
   │
   ▼ Cliente abre diálogo de PIN
   │
   ▼ POST /v1/auth/authorize { action, pin, context }
   │  → authorizationToken (válido 120 s, para aquela ação e contexto)
   │
   ▼ Repete a requisição com X-Authorization-Token
   │
   ▼ Executa · auditoria registra executor E autorizador
```

O token é **vinculado à ação e ao contexto específicos** — não é uma sessão temporária de gerente. Autorizar o cancelamento do item X não autoriza cancelar o item Y.

### Ações que exigem elevação (configurável por tenant)

| Ação | Permissão |
|---|---|
| Cancelar item já iniciado | `order:cancel_started` |
| Desconto acima do limite | `cash:discount_any` |
| Fechar caixa com divergência acima do limite | `cash:close_divergent` |
| Ajuste manual de estoque | `stock:adjust` |
| Estorno de pagamento | `payment:refund` |
| Fechar conta com item pendente | `order:close_with_pending` |
| Alterar preço em pedido aberto | `order:override_price` |

### Verificação

```ts
@Post(':id/cancel')
@RequirePermission('order:cancel_started', { elevatable: true })
async cancelItem(...) { }
```

O guard verifica: (1) o usuário tem a permissão? Se sim, prossegue. (2) Se não, e a ação é elevável, exige `X-Authorization-Token` válido para aquela ação e contexto. (3) Caso contrário, 403.

### Duas camadas independentes

| Camada | Protege contra |
|---|---|
| RBAC na aplicação | Ação não permitida ao papel |
| RLS no banco (ADR-004) | Acesso a dados de outro tenant |

São ortogonais e ambas obrigatórias. RBAC não protege isolamento; RLS não protege permissão.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Papéis fixos no código | Simples | Cada cliente tem organização diferente | Viola ADR-013 |
| ABAC (baseado em atributos) | Muito expressivo | Complexidade alta; difícil de explicar ao gestor | Desproporcional |
| Trocar de sessão para autorizar | Simples de implementar | Operador perde a tela; gerente precisa deslogar depois | Quebra o fluxo operacional |
| Autorização em outro dispositivo | Mais seguro | Gerente teria de estar no terminal dele | Inviável no salão |
| Permissões só por papel, sem qualificador | Menos permissões | Exigiria muitos papéis para graduar poder | Explosão de papéis |

## Consequências

**Positivas**

- Cada estabelecimento organiza permissões conforme sua realidade
- Elevação pontual espelha o comportamento real do restaurante
- Auditoria registra executor **e** autorizador — atende RF-AUD com precisão
- Papéis padrão aceleram a implantação de novo cliente

**Negativas**

- Catálogo de permissões precisa ser mantido e documentado
- Configuração incorreta pode travar a operação
- Elevação adiciona um passo ao fluxo

**Mitigações**

- Papéis padrão cobrem 95% dos casos — o tenant raramente precisa mexer
- Tela de permissões mostra o efeito prático de cada uma em linguagem de negócio
- Validação impede remover todas as permissões críticas do papel `OWNER`
- Token de elevação com validade curta (120 s) e escopo restrito

## Como validar

- Teste de matriz: cada endpoint × cada papel padrão, verificando permitido/negado
- Teste: ação elevável sem token retorna 403 com `requiresAuthorization: true`
- Teste: token de elevação de um contexto não serve para outro
- Teste: auditoria contém `actorId` e `authorizedBy` em toda ação elevada

## Revisitar quando

- Surgir necessidade de regra dependente de atributo (ex.: "gerente só autoriza na própria loja em rede multi-unidade")

# US-152 · Visão 360 e acesso aos módulos do estabelecimento

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Segundo incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-11 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-004, ADR-010, ADR-021, ADR-023 |
| **Eventos** | Consome EVT-054, EVT-055, EVT-083, EVT-084 |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** uma visão administrativa completa de um estabelecimento,
> **para** entender cadastro, responsáveis, lojas, instalação e próximos passos em um único lugar.

## 2. Contexto e motivação

Uma linha no diretório identifica o cliente, mas não responde por que ele exige atenção. O detalhe precisa reunir metadados administrativos sem atravessar a fronteira que protege pedidos, pagamentos, estoque e financeiro.

“Acessar módulos” significa navegar para superfícies autorizadas ou solicitar suporte via US-145; nunca impersonar silenciosamente o cliente.

## 3. Escopo

### 3.1 Dentro desta história

- Cabeçalho com nome, slug, status, plano, modelo, domínio e datas
- Resumo de branding e configuração, com links para funções já existentes
- Proprietário e estado do convite
- Lista resumida de lojas e instalações
- Checklist de implantação com pendências e próxima ação recomendada
- Links para saúde, histórico administrativo, suporte e atualização do parque
- URLs públicas/administrativas resolvidas a partir da configuração real
- Banner explícito quando o cadastro estiver incompleto ou inconsistente

### 3.2 Fora desta história

- Exibição direta de vendas, pedidos, caixa, estoque ou financeiro
- Login automático como usuário do tenant
- Diagnóstico técnico completo (US-140)
- Concessão de suporte sem motivo, prazo e auditoria (US-145)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Visão 360 do estabelecimento

  Cenário: Cadastro saudável
    Dado um tenant ativo com proprietário, loja e instalação saudável
    Quando o administrador abrir seu detalhe
    Então deve ver os principais metadados e o checklist concluído
    E os atalhos devem apontar para recursos daquele mesmo tenant

  Cenário: Provisionamento incompleto
    Dado um tenant criado cujo token não foi consumido
    Quando abrir o detalhe
    Então deve ver a instalação como pendente
    E deve receber a próxima ação segura disponível

  Cenário: Tentativa de abrir dado de negócio
    Dado que o administrador não possui token de suporte válido
    Quando tentar acessar conteúdo interno do cliente
    Então o sistema deve iniciar o fluxo da US-145
    E nunca deve conceder acesso implícito

  Cenário: Recurso inexistente
    Dado um ID de tenant inexistente ou removido logicamente
    Quando abrir a URL de detalhe
    Então deve receber 404 sem informação adicional sensível
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Ações sensíveis são auditadas | Atalhos que mudam contexto ou iniciam suporte registram o ator |
| RN-015 | Isolamento total | O detalhe limita-se a metadados administrativos e exige US-145 para dados do cliente |

## 6. Eventos emitidos e consumidos

| ID | Evento | Uso |
|---|---|---|
| EVT-054 | `tenant.config_updated` | Atualiza versão/configuração resumida |
| EVT-055 | `tenant.branding_updated` | Atualiza identidade e domínio exibidos |
| EVT-083/084 | Edge offline/reconectado | Atualiza saúde e próxima ação |

Não emite evento apenas por visualização; acessos sensíveis seguem auditoria.

## 7. Contrato de API

```http
GET /v1/platform/tenants/{id}/overview
→ 200 {
  "tenant": { "id": "...", "name": "...", "slug": "...", "status": "ACTIVE", "plan": "COMPLETO", "template": "PIZZERIA", "domain": null },
  "owner": { "name": "...", "email": "...", "inviteStatus": "ACCEPTED" },
  "stores": [{ "id": "...", "name": "Matriz", "timezone": "America/Sao_Paulo" }],
  "installations": [{ "id": "...", "label": "...", "status": "ACTIVE", "health": "OK" }],
  "deployment": { "completed": 9, "total": 9, "nextAction": null },
  "links": { "publicMenu": "...", "admin": "...", "health": "..." }
}
```

## 8. Modelo de dados

Agrega `tenant`, `tenant_config`, `app_user`, `owner_invite`, `store`, `edge_installation` e projeção do checklist. Não consulta tabelas operacionais de pedido ou financeiro.

## 9. Comportamento offline

Exclusivo de nuvem. Links que dependem de instalação offline permanecem visíveis, mas desabilitados com motivo e última comunicação conhecida.

## 10. Interface e experiência

- Resumo no topo; seções Cadastro, Responsáveis, Lojas, Instalações e Histórico
- Próxima ação mais importante destacada, sem esconder outras pendências
- IDs técnicos copiáveis, mas visualmente secundários
- Links externos mostram destino e nunca trocam identidade silenciosamente
- Metadados ausentes exibem “Não configurado”, não espaço vazio ambíguo

## 11. Métricas, alertas e observabilidade

- Tempo para resolver uma pendência a partir do detalhe
- Frequência de tenants com checklist incompleto
- Links quebrados ou configurações inconsistentes
- Aberturas do fluxo de suporte por origem

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Contrato | Compatibilidade entre DTO .NET e schema TypeScript |
| Integração | Agregação correta e ausência de dados operacionais |
| Segurança | Cross-tenant, 404 e exigência do fluxo de suporte |
| E2E | Diretório → detalhe → próxima ação → retorno preservado |
| Resiliência | Falha de uma seção não derruba o restante do detalhe |

## 13. Dependências

**Depende de:** US-003, US-151, US-140  
**Habilita:** US-153, US-154, US-155, US-156, US-157

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Matriz de campos administrativos aprovada
- [ ] Limite entre metadado e dado de negócio revisado por segurança
- [ ] Estratégia de links por ambiente definida

**DoD**

- [ ] Todas as seções e estados parciais implementados
- [ ] Nenhuma query acessa agregado operacional do tenant
- [ ] Links e permissões testados por ambiente
- [ ] Checklist deriva de fatos persistidos, não de estado local da tela
- [ ] Acesso sensível encaminha para US-145

## 15. Riscos, premissas e pendências

- A “visão 360” deve permanecer administrativa; incorporar KPIs do cliente quebraria a garantia de isolamento.
- **[PENDÊNCIA]** Definir quais URLs dos aplicativos existem em cada ambiente antes de habilitar links públicos.

---

*US-152 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

# US-151 · Diretório de estabelecimentos com busca e filtros

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Primeiro incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-10 |
| **Regras de negócio** | RN-015 |
| **ADRs** | ADR-004, ADR-021, ADR-023 |
| **Eventos** | Não se aplica |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** listar, buscar, filtrar e ordenar os estabelecimentos,
> **para** reencontrar qualquer cliente e entender seu estado atual sem consulta técnica.

## 2. Contexto e motivação

A US-002 já definiu `GET /v1/platform/tenants`, mas não definiu uma interface de listagem. O diretório é o ponto de entrada operacional para todas as histórias seguintes e deve funcionar com dezenas ou milhares de tenants sem carregar toda a base de uma vez.

## 3. Escopo

### 3.1 Dentro desta história

- Tabela/lista paginada por cursor
- Busca por nome, slug, domínio, documento e e-mail do proprietário
- Filtros por status, plano, modelo, saúde da instalação e data de criação
- Ordenação por nome, criação, última atividade administrativa e criticidade
- Colunas: nome, slug, plano, status, proprietário, lojas, instalações e última atualização
- Preservação de filtros na URL para compartilhamento e retorno do detalhe
- Estado vazio geral e estado “nenhum resultado” distintos
- Exportação CSV apenas dos metadados exibíveis, sujeita à mesma autorização

### 3.2 Fora desta história

- Edição em massa de status ou plano
- Dados de vendas, pedidos, caixa ou financeiro
- Diagnóstico técnico aprofundado (US-140)
- Acesso de suporte ao conteúdo do tenant (US-145)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Diretório de estabelecimentos

  Cenário: Listagem inicial
    Dado que existem estabelecimentos em diferentes estados
    Quando o administrador abrir o diretório
    Então deve ver uma página de resultados ordenada por criticidade e atualização
    E cada linha deve abrir o detalhe do estabelecimento

  Cenário: Busca combinada com filtros
    Dado que existem clientes com nomes semelhantes
    Quando buscar "betinha" e filtrar status ACTIVE
    Então apenas correspondências ativas devem aparecer
    E busca e filtros devem permanecer refletidos na URL

  Cenário: Base grande
    Dado que existem mais resultados que o limite da página
    Quando avançar a listagem
    Então a paginação por cursor não deve repetir nem omitir registros

  Cenário: Usuário sem autorização global
    Dado um usuário comum de estabelecimento
    Quando chamar o endpoint de diretório
    Então deve receber 403
    E nenhum nome, contagem ou metadado global deve ser retornado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Isolamento entre estabelecimentos | O diretório é exceção exclusiva do papel P9 e não expõe dados de negócio |

## 6. Eventos emitidos e consumidos

Não emite eventos. Mudanças refletidas na lista vêm dos fatos administrativos persistidos pelas histórias responsáveis.

## 7. Contrato de API

```http
GET /v1/platform/tenants?query=betinha&status=ACTIVE&plan=COMPLETO&health=DEGRADED&sort=attention&limit=25&cursor=...
→ 200 {
  "data": [
    {
      "id": "...", "name": "Dona Betinha", "slug": "dona-betinha",
      "status": "ACTIVE", "plan": "COMPLETO", "ownerEmail": "...",
      "storesCount": 1, "installationsCount": 1, "health": "OK",
      "createdAt": "...", "updatedAt": "..."
    }
  ],
  "nextCursor": "...",
  "appliedFilters": { "query": "betinha", "status": ["ACTIVE"] }
}
```

O contrato usa códigos de status normalizados em caixa alta; valores internos do enum não podem vazar como `Trial` ou número.

## 8. Modelo de dados

| Tabela/view | Papel | Campos relevantes |
|---|---|---|
| `tenant` | Fonte principal | `id`, `name`, `slug`, `status`, `plan`, `domain`, timestamps |
| `app_user` / `user_role` | Proprietário atual | e-mail e papel OWNER |
| `store` | Contagem de lojas | `tenant_id`, `deleted_at` |
| `edge_installation` | Contagem e saúde resumida | status, último contato |
| View de diretório | Agregação otimizada | Apenas metadados administrativos autorizados |

## 9. Comportamento offline

Somente leitura de nuvem. Se a conexão cair, a tela preserva filtros e informa que não foi possível atualizar; resultados antigos não são apresentados como atuais sem marcação explícita.

## 10. Interface e experiência

- Busca com debounce e botão de limpar
- Filtros em chips removíveis, contagem de ativos e URL reproduzível
- Status nunca indicado somente por cor
- Colunas essenciais permanecem em telas menores; demais dados vão para expansão/detalhe
- Ação primária “Novo estabelecimento” permanece visível

## 11. Métricas, alertas e observabilidade

- Latência p95 da listagem e das buscas
- Filtros mais utilizados e consultas sem resultado
- Quantidade de tenants por status e plano
- Erros de contrato com `traceId`

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Serialização de filtros, ordenação e cursor |
| Integração | Busca combinada, paginação estável e normalização de status |
| Segurança | `403` sem vazamento para usuário não P9 |
| Performance | Página de 25 itens dentro do orçamento com base representativa |
| E2E | Buscar → filtrar → abrir detalhe → voltar preservando contexto |

## 13. Dependências

**Depende de:** US-001, US-002, US-150  
**Habilita:** US-152, US-153, US-154, US-155, US-156

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Campos pesquisáveis e filtráveis aprovados
- [ ] Estratégia de paginação definida
- [ ] Volume de referência acordado
- [ ] Contrato de status normalizado definido

**DoD**

- [ ] Lista, busca, filtros, ordenação e paginação implementados
- [ ] URL preserva o estado da consulta
- [ ] Índices e plano de execução verificados
- [ ] Testes de autorização e contrato passando
- [ ] Exportação não contém campo fora do escopo administrativo

## 15. Riscos, premissas e pendências

- Busca textual sem índice adequado degrada com o crescimento; medir antes de escolher `ILIKE`, trigram ou serviço externo.
- Documento e e-mail são dados pessoais: mascarar quando o perfil não exigir visualização integral.

---

*US-151 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

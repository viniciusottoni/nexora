# US-150 · Estrutura e navegação do painel de plataforma

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Primeiro incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-09 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-015, ADR-021, ADR-023 |
| **Eventos** | Não se aplica |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** entrar em uma área administrativa com página inicial e navegação previsível,
> **para** acessar as funções globais sem depender de URLs, Swagger ou banco de dados.

## 2. Contexto e motivação

O `web-platform` nasceu na US-002 com um formulário único. Esse recorte era suficiente para provar provisionamento, mas não constitui uma aplicação administrativa. Quando o cadastro termina — ou quando retorna sucesso HTTP e falha na interpretação do cliente — não existe uma raiz para reencontrar o estabelecimento.

Esta história estabelece o shell permanente da plataforma. O formulário de novo estabelecimento passa a ser uma rota e uma ação dentro do produto, não a própria página inicial.

## 3. Escopo

### 3.1 Dentro desta história

- Rota inicial autenticada `/` com resumo da plataforma
- Navegação principal para Visão geral, Estabelecimentos, Instalações e Auditoria/Suporte
- Rota `/estabelecimentos/novo` para o fluxo existente da US-002
- Cabeçalho com identidade do administrador, ambiente e encerramento de sessão
- Breadcrumb e título coerentes em rotas de lista e detalhe
- Proteção de rotas pela policy de administrador da plataforma
- Estados globais de carregamento, sessão expirada, acesso negado, recurso inexistente e falha de API
- Navegação por teclado, foco visível e landmarks semânticos

### 3.2 Fora desta história

- Conteúdo completo das páginas de estabelecimentos (US-151/US-152)
- Diagnóstico de instalação (US-140)
- Acesso a dados de negócio do cliente (US-145)
- Personalização visual do painel por tenant; o painel pertence à plataforma Replay

## 4. Critérios de aceite

```gherkin
Funcionalidade: Estrutura do painel de plataforma

  Cenário: Entrada pela raiz
    Dado um administrador de plataforma autenticado
    Quando acessar a raiz do web-platform
    Então deve ver a visão geral da plataforma
    E deve existir navegação para Estabelecimentos, Instalações e Auditoria/Suporte
    E "Novo estabelecimento" deve ser uma ação, não o conteúdo único da raiz

  Cenário: Acesso direto a uma rota protegida
    Dado um usuário sem a policy PlatformAdmin
    Quando acessar uma rota administrativa global
    Então deve receber acesso negado sem qualquer dado de tenant
    E a tentativa deve ser observável

  Cenário: Sessão expirada
    Dado que o refresh token não é mais válido
    Quando uma navegação exigir dados protegidos
    Então a sessão local deve ser encerrada
    E o usuário deve voltar ao login sem loop de redirecionamento

  Cenário: Navegação acessível
    Dado que o administrador usa apenas teclado
    Quando percorrer menu, conteúdo e ações
    Então a ordem de foco deve ser lógica
    E a rota atual deve ser anunciada e indicada além de cor
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Ação sensível registra autor, horário e contexto | Falhas de autorização e saídas de sessão são observáveis |
| RN-015 | Isolamento total entre estabelecimentos | O shell não injeta tenant implícito nem revela dados antes da autorização |

## 6. Eventos emitidos e consumidos

Não cria evento de domínio. Telemetria de navegação e falha de autorização é tratada como observabilidade, sem payload de negócio.

## 7. Contrato de API

```http
GET /v1/platform/summary
→ 200 {
  "tenants": { "total": 12, "active": 9, "attention": 2 },
  "installations": { "healthy": 8, "degraded": 1, "offline": 1 },
  "pendingInvites": 2,
  "generatedAt": "..."
}
```

Erros seguem ADR-021. Resposta `401` tenta renovação uma vez; `403` nunca dispara fallback para rota menos protegida.

## 8. Modelo de dados

Nenhuma tabela nova. O resumo usa agregações de `tenant`, `edge_installation` e `owner_invite`, sempre sem dado operacional de pedido, pagamento ou financeiro.

## 9. Comportamento offline

O painel é exclusivo de nuvem. Sem conexão, mantém apenas a estrutura visual e informa que os dados não podem ser atualizados; nenhuma mutação fica enfileirada localmente.

## 10. Interface e experiência

- Sidebar persistente em desktop e drawer acessível em telas menores
- A rota atual deve permanecer visível após recarregar a página
- CTA “Novo estabelecimento” disponível na visão geral e no diretório
- Skeleton preserva a geometria; falha parcial não derruba toda a página
- Ambiente local/homologação/produção deve estar claramente identificado

## 11. Métricas, alertas e observabilidade

- Tempo até primeira renderização útil da visão geral
- Taxa de `401`, refresh bem-sucedido e `403` por rota
- Rotas administrativas mais utilizadas
- Erros de navegação e carregamentos parciais com `traceId`

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Mapa de rotas, item ativo e decisão de redirecionamento |
| Integração | Policy PlatformAdmin em todos os endpoints globais |
| E2E | Login → raiz → lista → novo estabelecimento → retorno à lista |
| Acessibilidade | Teclado, foco, landmarks, nome acessível e contraste |
| Segurança | Usuário de tenant nunca renderiza dado global |

## 13. Dependências

**Depende de:** US-004  
**Habilita:** US-151, US-152, US-157

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Arquitetura de informação e mapa de rotas aprovados
- [ ] Policies por rota definidas
- [ ] Estados de erro e sessão desenhados
- [ ] Contrato de `/v1/platform/summary` revisado

**DoD**

- [ ] Raiz, navegação e proteção de rotas implementadas
- [ ] Acessibilidade automatizada e manual validada
- [ ] Testes E2E e de autorização passando
- [ ] Nenhum dado global armazenado em cache persistente inseguro
- [ ] OpenAPI e documentação atualizados

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA]** Confirmar nomes finais e agrupamento dos itens de navegação com a equipe que operará a plataforma.
- O resumo deve permanecer pequeno; análises aprofundadas pertencem às páginas específicas.

---

*US-150 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*

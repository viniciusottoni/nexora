# US-007 · Pipeline de CI-CD com travas de governanca

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | — |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-013, ADR-019, ADR-029 |
| **Eventos** | — |
| **Aplicações** | infra/cloud, monorepo |
| **Autoridade do dado** | — |

---

## 1. História

> **Como** time de desenvolvimento,
> **quero** que o pipeline reprove automaticamente violações das decisões arquiteturais,
> **para** que a governança do produto não dependa de alguém lembrar dela na revisão de código.

## 2. Contexto e motivação

O risco 6 da Visão Geral é o mais silencioso do projeto: *customização por código para cada cliente destrói a escalabilidade*. Governança escrita em documento não impede nada. Governança no CI impede.

Esta história transforma o ADR-013 em uma trava executável: um PR que compare literalmente um identificador de tenant, ou que embuta valor de marca de um cliente, falha o build com uma mensagem que aponta o ADR.

O mesmo vale para o contrato de API e para o isolamento multi-tenant — as duas outras coisas que quebram sem aviso.

## 3. Escopo

### 3.1 Dentro desta história

- Workflow do GitHub Actions: lint, typecheck, testes unitários, testes de integração, build
- Trava de governança ADR-013: falha se o código comparar identificador de tenant ou embutir marca de cliente
- Execução obrigatória da suíte de isolamento multi-tenant em todo PR
- Snapshot versionado do OpenAPI: quebra de contrato falha o CI
- Verificação de migrations compatíveis com versão anterior (ADR-019)
- Publicação de imagens Docker versionadas para edge e nuvem
- Estratégia de branch e versionamento semântica (ADR-029)

### 3.2 Fora desta história

- Deploy automático para produção (exige aprovação manual)
- Atualização do parque de lojas (US-146)
- Testes de carga em cada PR — rodam em cadência própria (doc. 10)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Travas de governança no pipeline

  Cenário: PR bloqueado por violação de isolamento
    Dado um PR cujo código compara literalmente um identificador de tenant
    Quando o CI executar
    Então o build deve falhar
    E a mensagem deve apontar o ADR-013 e a linha exata

  Cenário: PR bloqueado por marca embutida
    Dado um PR que embute a cor ou o nome de um cliente específico no código
    Quando o CI executar
    Então o build deve falhar indicando que identidade é configuração (ADR-010)

  Cenário: Quebra de contrato de API
    Dado um PR que remove um campo obrigatório de um DTO publicado
    Quando o CI comparar com o snapshot do OpenAPI
    Então o build deve falhar
    E deve indicar que a mudança exige nova versão de path (/v2)

  Cenário: Migration incompatível
    Dado uma migration que remove coluna ainda usada pela versão anterior
    Quando o CI executar a verificação de compatibilidade
    Então deve falhar indicando o ADR-019
    E deve sugerir o padrão de expansão e contração em duas etapas

  Cenário: Suíte de isolamento obrigatória
    Dado um PR que adiciona uma tabela com tenant_id sem política RLS
    Quando o CI executar
    Então o teste de isolamento deve falhar
    E o merge deve ficar bloqueado

  Cenário: Build verde publica imagens
    Dado um merge na branch principal com todos os checks verdes
    Quando o pipeline concluir
    Então devem ser publicadas imagens versionadas de api-edge e api-cloud
    E a versão deve seguir versionamento semântico
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica de negócio é configuração, nunca código de cliente | Verificação estática automatizada no pipeline, com falha bloqueante |
| RN-015 | Isolamento total entre estabelecimentos | Suíte de isolamento obrigatória em todo PR |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

_Esta história não expõe endpoint novo; consome contratos já definidos no documento 05._

## 8. Modelo de dados

_Não se aplica a esta história._

## 9. Comportamento offline

Não se aplica — infraestrutura de desenvolvimento, sem componente de runtime na loja.

O pipeline, no entanto, é o que garante que o comportamento offline não regrida: a suíte de caos offline descrita no documento 10 roda em cadência agendada e reprova a release se o fluxo operacional quebrar sem internet.

## 10. Interface e experiência

- Mensagem de falha do CI escrita para quem vai corrigir: o que quebrou, onde, qual ADR e como resolver
- Tempo total do pipeline abaixo de 10 minutos para não desincentivar PRs pequenos
- Checks obrigatórios configurados na proteção de branch, não apenas informativos

## 11. Métricas, alertas e observabilidade

- Tempo médio do pipeline e taxa de falha por tipo de check
- Contagem de violações do ADR-013 barradas — indicador de pressão por customização
- Cobertura de testes por pacote, com atenção especial a `packages/domain`

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Meta-teste | PR de exemplo violando o ADR-013 é efetivamente reprovado |
| Meta-teste | PR de exemplo quebrando o OpenAPI é reprovado |
| Meta-teste | Migration incompatível é reprovada |
| Integração | Imagens publicadas sobem corretamente no compose do edge |

## 13. Dependências

**Depende de:** US-001  
**Habilita:** todas as demais histórias — é a rede de segurança do projeto

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

- Trava de governança com falso positivo alto gera pressão do time para desabilitá-la. Mitigação: regra específica, mensagem clara e mecanismo de exceção documentado com justificativa registrada.
- Pipeline lento é o principal motivo pelo qual times param de rodar testes localmente. Manter abaixo de 10 minutos é requisito, não meta.

---

*US-007 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*
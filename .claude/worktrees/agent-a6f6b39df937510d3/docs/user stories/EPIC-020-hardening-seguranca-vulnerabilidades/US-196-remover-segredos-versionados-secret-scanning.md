---
title: US-196 — Remover credenciais versionadas e ativar prevenção de vazamento
sidebar_position: 196
---

# US-196 — Remover credenciais versionadas e ativar prevenção de vazamento

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-196 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Engenharia, DevOps e Segurança |
| Plano | Todos |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | GitHub, provedor de deploy, provedores externos |
| Status | Planejada |

## 2. História do usuário

Como **responsável técnico pelo AWAKEN**,

quero **remover credenciais versionadas e impedir novos vazamentos**,

para **proteger APIs, infraestrutura, dados e custos operacionais do projeto**.

## 3. Contexto

A revisão identificou credencial preenchida em arquivo de configuração de desenvolvimento. Credenciais não podem ficar versionadas, mesmo em repositório privado. Qualquer credencial já exposta deve ser tratada como comprometida e substituída no provedor correspondente.

## 4. Objetivo

Eliminar credenciais reais do repositório e implantar mecanismos automáticos para detectar novos vazamentos antes do merge.

## 5. Escopo

### Entra nesta US

- Substituir credenciais reais por placeholders não funcionais.
- Substituir credenciais já expostas nos provedores externos.
- Garantir que configurações locais sensíveis fiquem fora do versionamento.
- Criar documentação segura de configuração por ambiente.
- Configurar verificação automática de vazamento no CI.
- Garantir que logs de build não imprimam valores sensíveis.

### Fora desta US

- Troca de fornecedor de exercícios.
- Cofre corporativo avançado.
- Rotação automática programada para todos os provedores.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nenhuma credencial real pode ser versionada. |
| RN-002 | Credencial exposta deve ser substituída antes de qualquer release. |
| RN-003 | Arquivos de exemplo devem usar placeholders claramente inválidos. |
| RN-004 | O CI deve falhar ao detectar provável credencial. |
| RN-005 | A documentação deve orientar configuração segura por ambiente. |
| RN-006 | Logs não podem exibir valores sensíveis. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário final | Não impactado. |
| Dev local | Usa configuração local não versionada. |
| CI/CD | Usa variáveis protegidas do provedor. |
| Produção | Usa variáveis seguras do ambiente de deploy. |
| Admin interno | Não acessa valor bruto de credenciais pelo app. |

## 8. Fluxo principal

1. Identificar credenciais versionadas.
2. Substituir valores no provedor externo.
3. Remover valores reais do repositório.
4. Ajustar `.gitignore` e arquivos de exemplo.
5. Configurar verificação automática no CI.
6. Documentar configuração segura por ambiente.
7. Validar que o projeto sobe apenas com variáveis seguras configuradas.

## 9. Fluxos alternativos

### Histórico Git contém valor sensível

O valor exposto deve ser substituído no provedor. Limpeza de histórico pode ser avaliada, mas não substitui a troca da credencial.

### Ambiente sem configuração obrigatória

A aplicação deve falhar de forma clara e segura, sem fallback para valor versionado.

## 10. Estados esperados

- configuração ausente;
- configuração segura;
- configuração inválida;
- vazamento detectado no CI;
- credencial substituída;
- documentação atualizada.

## 11. Impacto no Frontend Flutter

- Garantir que chaves públicas permitidas sejam explicitamente classificadas como públicas.
- Evitar versionar arquivos locais de build com valores sensíveis.

## 12. Impacto no Backend

- Remover valores reais de arquivos `appsettings`.
- Ler configurações sensíveis via ambiente seguro.
- Integrar com a US-197 para validação de startup.

## 13. Impacto no Banco de Dados

Não há alteração obrigatória de schema.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Protege integrações pagas e reduz risco de abuso de quota ou cobrança indevida.

## 16. Impacto em Internacionalização

Não aplicável.

## 17. Contrato técnico sugerido

Arquivos esperados:

```txt
.github/workflows/security.yml
.github/dependabot.yml
.gitignore
backend/src/Awaken.Api/appsettings.json
backend/src/Awaken.Api/appsettings.Development.json
docs/configuracao-segura.md
```

## 18. Eventos de Analytics

Não aplicável.

## 19. Critérios de aceite

### CA-001 — Nenhuma credencial real versionada

Dado o repositório completo,
Quando a verificação automática roda,
Então nenhum valor sensível real é encontrado.

### CA-002 — Credencial exposta substituída

Dado que um valor sensível foi exposto,
Quando a US for concluída,
Então o valor antigo não é mais aceito pelo provedor externo.

### CA-003 — CI bloqueia vazamento

Dado um commit com provável credencial,
Quando o CI roda,
Então o job falha antes do merge.

### CA-004 — Dev local documentado

Dado um novo desenvolvedor,
Quando lê a documentação,
Então sabe configurar o ambiente sem editar arquivos versionados com valores reais.

## 20. Critérios de teste para QA

- rodar verificação automática no repositório;
- validar startup sem configuração obrigatória;
- validar startup com configuração segura;
- revisar `.gitignore`;
- revisar logs de CI sem valores sensíveis.

## ✅ Decisão registrada

Credenciais reais não pertencem ao repositório. Toda credencial exposta deve ser substituída, e o CI passa a bloquear novos vazamentos.
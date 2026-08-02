---
title: US-175 — Tela de configurações completa e funcional
sidebar_position: 175
---

# US-175 — Tela de configurações completa e 100% funcional

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-175 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | EPIC-002, EPIC-003, EPIC-015 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**,
quero **que todos os itens da tela de configurações sejam funcionais**,
para **gerenciar conta, assinatura, suporte e informações legais sem encontrar botões mortos**.

## 3. Contexto

O README aponta tiles como Contato, FAQ e Sobre sem `onTap`, além de lacunas em assinatura, termos, privacidade, restaurar compras e versão.

## 4. Objetivo

Completar a tela de configurações com destinos reais, estados de erro e textos localizados.

## 5. Escopo

### Entra nesta US

- Gerenciar assinatura.
- Restaurar compras.
- Termos de uso.
- Política de privacidade.
- Sobre/versão do app.
- FAQ/ajuda.
- Contato ou abertura de ticket via US-176.
- Logout e exclusão de conta quando aplicável.
- Remover tiles sem ação.

### Fora desta US

- Painel admin.
- Chat em tempo real.
- Central de ajuda completa.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda opção visível em configurações deve ter ação funcional. |
| RN-002 | Não pode existir item de menu sem destino. |
| RN-003 | Ações comerciais devem respeitar RevenueCat/loja. |
| RN-004 | Links legais devem abrir conteúdo atualizado. |
| RN-005 | Configurações devem funcionar com acesso ativo e expirado quando fizer sentido. |

## 7. Fluxo principal

1. Usuário abre configurações.
2. App carrega status de conta/assinatura.
3. Todos os tiles exibidos possuem destino real.
4. Usuário consegue executar ações de conta, suporte e legal.

## 8. Impacto Flutter

- Revisar lista de tiles.
- Implementar `onTap` em todos os itens.
- Criar estados de loading/erro para restore e links.
- Exibir versão/build do app.

## 9. Impacto Backend

- Pode expor dados de conta e links legais.
- Suporte/ticket fica integrado à US-176.

## 10. Critérios de aceite

### CA-001 — Sem tile morto

Dado que a tela de configurações está aberta,
quando o usuário tocar em qualquer item visível,
então deve haver ação funcional ou feedback claro.

### CA-002 — Restore funcional

Dado que o usuário toca em restaurar compras,
quando o fluxo finalizar,
então o app deve informar sucesso ou erro real.

## 11. Decisão registrada

> Configurações é área de confiança: nenhum item visível pode ser decorativo ou sem destino.

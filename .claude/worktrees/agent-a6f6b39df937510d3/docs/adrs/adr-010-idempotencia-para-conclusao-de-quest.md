# ADR-010 — Estratégia de idempotência para conclusão de quest

Status: Aceito

## Contexto

O app terá suporte parcial a uso offline e pode reenviar uma conclusão de quest quando a conexão voltar. Além disso, o usuário pode tocar mais de uma vez no botão de concluir, ou a rede pode repetir uma requisição.

## Decisão

Toda operação crítica de conclusão de quest deve usar idempotência.

## Implementação

- O app deve gerar um `idempotencyKey` por tentativa de conclusão.
- O backend deve persistir a chave com usuário, operação e resultado.
- Requisições repetidas com a mesma chave retornam o mesmo resultado.
- A atualização de quest, XP, atributos e streak deve ocorrer em uma única transação.
- Usar lock curto por usuário e quest para evitar corrida.
- Registrar tentativas duplicadas para observabilidade.

## Consequências

A experiência fica segura mesmo com rede instável. A complexidade aumenta levemente no backend, mas evita bugs graves de XP duplicado.

## Critérios de aceite

- Duplo clique em concluir não duplica XP.
- Retry do app não duplica XP.
- Requisição repetida retorna resposta consistente.
- Testes cobrem concorrência básica.

# ADR-009 — Backend como autoridade de XP, rank e streak

Status: Aceito

## Contexto

XP, rank, level, atributos e streak são o coração da gamificação do AWAKEN. Se o app calcular esses dados como fonte final, usuários poderiam duplicar XP, manipular estado offline ou gerar inconsistência entre dispositivos.

## Decisão

O backend será a autoridade final de XP, rank, level, atributos, streak, limites do plano e conclusão de quest.

## Implementação

- O app pode mostrar previsão visual de XP.
- O backend calcula e persiste XP oficial.
- Toda conclusão de quest deve passar pela API.
- Usar transação para atualizar quest, XP, atributos e streak.
- Registrar `xp_transactions` para histórico imutável.
- Registrar auditoria para ajustes administrativos.
- Usar idempotência para evitar duplicidade.

## Consequências

A progressão fica confiável e consistente. O app deve lidar com estados como “sincronizando”, “pendente de confirmação” e “falha ao sincronizar”.

## Critérios de aceite

- Concluir quest offline não concede XP até sincronizar.
- Reenvio da mesma conclusão não duplica XP.
- Rank e level retornam do backend.
- Histórico de XP permite auditoria.

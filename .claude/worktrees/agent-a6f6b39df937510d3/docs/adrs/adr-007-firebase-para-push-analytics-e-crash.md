# ADR-007 — Firebase para push, analytics e crash

Status: Aceito

## Contexto

O AWAKEN precisa medir retenção, ativação, conclusão de quests, conversão premium, compartilhamento de card e estabilidade do app. Também precisa enviar notificações push para lembrete de treino, streak em risco, rank up e eventos de engajamento.

## Decisão

Usar Firebase para Cloud Messaging, Analytics e Crashlytics.

## Implementação

- Usar `firebase_core`, `firebase_messaging`, `firebase_analytics` e `firebase_crashlytics` no Flutter.
- Criar projeto Firebase separado por ambiente, quando possível.
- Registrar eventos de produto com nomes padronizados.
- Enviar token FCM do dispositivo para o backend.
- Usar Firebase Admin SDK no backend para envio de push.
- Registrar falhas de envio em `notification_delivery_logs`.
- Não enviar dados sensíveis em notificações.

## Consequências

O produto ganha visibilidade de uso e estabilidade desde o MVP. A equipe deve manter governança dos eventos para evitar analytics poluído e deve respeitar consentimento e privacidade.

## Critérios de aceite

- Crashlytics captura falhas reais do app.
- Eventos principais são enviados.
- Push abre a tela correta.
- Usuário consegue desativar notificações.

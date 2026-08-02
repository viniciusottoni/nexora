# ADR-014 — Política de dados pessoais e LGPD

Status: Aceito

## Contexto

O AWAKEN coleta dados pessoais e dados físicos, como nome, e-mail, idade, altura, peso, objetivo, limitações físicas, disponibilidade e histórico de treino. Esses dados exigem cuidado desde o MVP.

## Decisão

Adotar Privacy by Design e tratar dados pessoais com minimização, transparência, segurança e retenção controlada.

## Implementação

- Coletar apenas dados necessários para personalizar treinos.
- Explicar finalidade dos dados no onboarding.
- Manter política de privacidade acessível.
- Permitir exclusão de conta quando legalmente possível.
- Não usar dados reais em ambientes de desenvolvimento.
- Restringir acesso administrativo.
- Registrar eventos sensíveis de acesso e alteração.
- Evitar dados sensíveis em logs, notificações e analytics.

## Consequências

O produto nasce mais confiável e preparado para escalar. A equipe deve validar documentos legais antes do go-live.

## Critérios de aceite

- Política de privacidade existe.
- Usuário entende o uso dos dados.
- Dados pessoais não aparecem em logs técnicos.
- Existe fluxo planejado para exclusão de conta.

# ADR-015 — Política de logs sem dados sensíveis

Status: Aceito

## Contexto

Logs são essenciais para investigar erros, falhas de API, problemas de assinatura, geração de treino e eventos de progressão. Porém logs não podem expor senha, tokens, dados pessoais completos, limitações físicas detalhadas ou payloads sensíveis.

## Decisão

Adotar logs estruturados sem dados sensíveis.

## Implementação

- Usar Serilog no backend.
- Usar correlation id em toda requisição.
- Registrar endpoint, status code, tempo, usuário técnico e erro resumido.
- Nunca registrar senha, token, refresh token ou chave de API.
- Evitar payload completo de onboarding e perfil físico.
- Mascara de e-mail quando necessário.
- Crashlytics no app deve receber contexto mínimo.
- Prompts e respostas externas devem ser sanitizados antes de log.

## Consequências

A investigação técnica continua possível sem aumentar exposição de dados. A equipe deve revisar logs durante QA e antes do go-live.

## Critérios de aceite

- Logs não exibem token.
- Logs não exibem senha.
- Logs não exibem payload físico completo.
- Erros possuem correlation id.

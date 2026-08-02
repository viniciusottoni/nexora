# ADR-001 — Flutter como frontend mobile oficial

Status: Aceito

## Contexto

O AWAKEN é um app mobile com estética dark, anime e gamificada. O produto depende de uma experiência visual forte, com animações de XP, level up, rank up, cards com brilho, transições rápidas e sensação de evolução constante.

## Decisão

Usar Flutter como tecnologia oficial do frontend mobile do AWAKEN.

## Diretrizes de implementação

- Criar o app em `apps/mobile`.
- Usar Dart com null safety.
- Organizar o app por features.
- Usar Riverpod para estado.
- Usar go_router para navegação.
- Usar Dio para HTTP.
- Usar Drift para cache local.
- Usar flutter_secure_storage para tokens.
- Usar Firebase Analytics e Crashlytics.
- Usar RevenueCat para assinaturas.

## Consequências

O time ganha controle visual e uma base preparada para Android e iOS. A equipe deve aceitar a curva de aprendizado de Dart e manter disciplina para evitar widgets gigantes e lógica de negócio dentro da interface.

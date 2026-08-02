# ADR-017 — Estratégia de expansão para iOS

Status: Aceito

## Contexto

O MVP do AWAKEN será lançado primeiro no Android para validar produto, retenção e monetização. A expansão para iOS deve ser planejada desde o início para evitar retrabalho.

## Decisão

Preparar o app Flutter para iOS desde o início, mas lançar iOS somente após estabilização e validação do Android.

## Implementação

- Manter pasta `ios/` no projeto Flutter.
- Evitar plugins sem suporte confiável a iOS.
- Preparar Apple Sign-In para fase iOS.
- Usar RevenueCat para reduzir retrabalho de assinatura.
- Separar configurações por flavor.
- Testar layout em safe areas e tamanhos de iPhone.
- Usar conta Apple Developer apenas na fase de publicação.

## Consequências

O projeto mantém caminho claro para expansão sem atrasar o MVP Android. O time precisará de Mac/Xcode para build e publicação iOS.

## Critérios de aceite

- O projeto Flutter mantém compatibilidade iOS.
- Bibliotecas escolhidas têm suporte iOS.
- Fluxo de assinatura é compatível com App Store.
- Apple Sign-In é incluído antes do lançamento iOS.

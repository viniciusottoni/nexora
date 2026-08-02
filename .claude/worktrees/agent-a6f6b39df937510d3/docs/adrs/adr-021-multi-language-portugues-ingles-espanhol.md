# ADR-021 — Multi-language: português, inglês, espanhol e francês

Status: Aceito

## Contexto

O AWAKEN nasce com foco inicial no mercado brasileiro, mas o conceito do produto possui potencial internacional. A proposta de app fitness gamificado com estética anime/RPG, quests, XP, rank e evolução pessoal é compreensível em diferentes mercados. Se o app for criado apenas em português, a expansão futura para inglês e espanhol exigirá retrabalho em telas, textos, eventos, notificações, erros, conteúdo de treino e assets.

## Decisão

O AWAKEN deve nascer com suporte multi-language para quatro idiomas desde a fundação:

- Português do Brasil: `pt-BR`;
- Inglês: `en` ou `en-US`;
- Espanhol: `es` ou `es-ES`/`es-419`, conforme decisão de localização;
- Francês: `fr` ou `fr-FR`.

O idioma padrão inicial será português do Brasil, mas a arquitetura do app, backend e conteúdo deve estar preparada desde o MVP para inglês, espanhol e francês.

## Diretrizes de implementação no Flutter

- Usar o sistema oficial de internacionalização do Flutter com arquivos ARB.
- Criar os arquivos:
  - `apps/mobile/lib/l10n/app_pt.arb`;
  - `apps/mobile/lib/l10n/app_en.arb`;
  - `apps/mobile/lib/l10n/app_es.arb`;
  - `apps/mobile/lib/l10n/app_fr.arb`.
- Não escrever textos fixos diretamente em widgets.
- Centralizar labels, mensagens, botões, títulos, erros e empty states nos arquivos de tradução.
- Usar chaves semânticas, por exemplo:
  - `dailyQuestTitle`;
  - `startQuestButton`;
  - `onboardingGoalGainMuscle`;
  - `subscriptionMonthlyTitle`;
  - `errorUnexpected`.
- Garantir que os textos suportem expansão de tamanho, especialmente espanhol, inglês e francês.
- Evitar layout rígido baseado em tamanho exato de texto.
- Testar as telas principais nos quatro idiomas.

## Diretrizes de implementação no backend

- APIs devem aceitar preferência de idioma do usuário.
- O perfil do usuário deve armazenar o idioma preferido.
- Notificações push devem usar templates localizados.
- E-mails transacionais devem usar templates localizados.
- Mensagens de erro públicas devem ser localizáveis.
- Conteúdos gerados ou retornados para o app devem respeitar o idioma do usuário.
- A geração de treinos por IA deve receber o idioma como parâmetro controlado pelo backend.

## Idioma de conteúdo e domínio

Devem ser localizados:

- onboarding;
- objetivos de treino;
- níveis de experiência;
- limitações físicas;
- equipamentos;
- nomes e instruções de exercícios;
- títulos e descrições de quests;
- mensagens de XP, level up e rank up;
- textos de assinatura;
- notificações push;
- e-mails;
- mensagens de erro;
- termos públicos do app.

## Regras obrigatórias

- O produto não deve misturar idiomas na mesma tela.
- O idioma do usuário deve ser definido automaticamente pelo locale do aparelho, mas pode ser alterado nas configurações.
- Se uma tradução estiver ausente, o fallback deve ser português do Brasil no MVP.
- Textos de analytics devem manter nomes técnicos estáveis em inglês, sem traduzir nomes de eventos.
- Dados de domínio enviados pela API devem usar códigos estáveis, e o app traduz a apresentação quando possível.

## Consequências

A implementação inicial fica um pouco mais trabalhosa, mas evita retrabalho grande na expansão internacional. O produto fica preparado para validar Brasil, Estados Unidos/mercado global, América Latina e mercados francófonos (França, Bélgica, Suíça, África francófona, Canadá) com menos alterações estruturais.

## Critérios de aceite

- O app possui arquivos ARB para português, inglês, espanhol e francês.
- Nenhuma tela P0 possui texto fixo fora da camada de localização.
- O usuário pode ter idioma preferido salvo no perfil.
- Push e e-mail possuem estratégia de template por idioma.
- A geração de treino recebe idioma do usuário.
- QA valida fluxo principal em português, inglês, espanhol e francês.

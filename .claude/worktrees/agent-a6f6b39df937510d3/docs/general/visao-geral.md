# AWAKEN — Detalhamento Inicial do Sistema e Backlog MVP

## 1. Visão do Produto

O **AWAKEN** é um aplicativo fitness gamificado que transforma treino físico em uma jornada de evolução pessoal.

A proposta central é:

> O Duolingo do treino físico, com alma de anime.

O usuário não apenas treina. Ele cria um perfil, recebe quests diárias, completa treinos, ganha XP, evolui atributos, sobe de level, mantém streak, desbloqueia ranks e compartilha seu progresso como um “Hunter”.

---

## 2. Objetivo do MVP

O objetivo do MVP é validar se usuários brasileiros conseguem criar uma rotina consistente de treino quando o treino é apresentado como uma jornada gamificada, simples, motivadora e em português.

O MVP também deve validar o novo modelo comercial do AWAKEN:

> **Teste gratuito de 7 dias. Depois disso, o usuário precisa assinar o plano mensal ou anual para continuar usando.**

O MVP deve provar:

* que o usuário entende rapidamente a proposta do AWAKEN;
* que o onboarding consegue gerar um treino adequado ao perfil;
* que a quest diária motiva o usuário a treinar;
* que XP, rank, level e streak aumentam retenção;
* que o teste gratuito de 7 dias entrega valor suficiente para gerar conversão;
* que o bloqueio após o trial é percebido como claro, honesto e esperado;
* que o card de perfil gera vontade de compartilhar;
* que o app é estável o suficiente para lançamento público.

---

## 3. Problema que o Produto Resolve

Muitos usuários querem treinar, mas abandonam depois de poucos dias porque os apps tradicionais são frios, genéricos, pouco motivadores ou exigem conhecimento prévio.

O AWAKEN resolve isso ao:

* transformar treino em missão;
* dar recompensa visual e emocional ao progresso;
* adaptar o treino ao nível, equipamento, tempo e limitações do usuário;
* evitar paywall surpresa;
* permitir teste gratuito real de 7 dias antes da assinatura;
* deixar claro que, após o trial, o acesso exige plano mensal ou anual;
* preservar o progresso do usuário mesmo quando o acesso estiver bloqueado;
* usar linguagem, estética e progressão inspiradas em games e anime.

---

## 4. Público-Alvo do MVP

### Público primário

Homens e mulheres de 16 a 30 anos, fãs de anime, games, RPG, Solo Leveling, shonen ou cultura geek, que querem começar ou retomar uma rotina de treino.

### Público secundário

Pessoas de 30 a 50 anos que gostam de gamificação, querem consistência e preferem uma experiência mais divertida do que apps fitness tradicionais.

---

## 5. Fase do Produto

| Campo                    | Valor                          |
| ------------------------ | ------------------------------ |
| Fase                     | MVP Android Fitness Gamificado |
| Plataforma inicial       | Android                        |
| Plataforma futura        | iOS                            |
| Frontend                 | Flutter                        |
| Backend                  | ASP.NET Core                   |
| Banco principal          | PostgreSQL                     |
| Cache / Operacional      | Redis                          |
| Assinaturas              | RevenueCat                     |
| Analytics / Crash / Push | Firebase                       |
| Idioma padrão            | PT-BR                          |
| Idiomas preparados       | PT-BR, EN, ES                  |

---

## 6. Princípios do MVP

| Princípio                     | Decisão                                                                                         |
| ----------------------------- | ----------------------------------------------------------------------------------------------- |
| Trial transparente            | O usuário entende antes do onboarding que terá 7 dias grátis e depois precisará assinar.        |
| Assinatura obrigatória        | Após o trial, o acesso ao app fica bloqueado até assinatura mensal ou anual.                    |
| Zero dark patterns            | Sem paywall surpresa, sem avaliação antes de uso real, sem trial escondido.                     |
| PT-BR desde o dia 1           | Toda tela P0 nasce em português brasileiro.                                                     |
| Personalização real           | O treino precisa respeitar objetivo, nível, equipamento, tempo e limitações.                    |
| Estabilidade acima de excesso | Melhor ter menos features funcionando bem do que muitas instáveis.                              |
| Gamificação positiva          | Falhar não deve humilhar o usuário; o foco é retorno e consistência.                            |
| Mobile-first                  | Toda decisão deve priorizar uso rápido no celular.                                              |
| MVP vendável                  | O app precisa demonstrar valor no trial e converter para plano mensal ou anual após 7 dias.    |

---

## 7. Perfis de Usuário

| Perfil                        | Descrição                                                                                       |
| ----------------------------- | ----------------------------------------------------------------------------------------------- |
| Visitante                     | Usuário não autenticado que abriu o app pela primeira vez.                                      |
| Usuário em Trial              | Usuário autenticado dentro dos 7 dias gratuitos de teste.                                       |
| Premium Mensal                | Usuário com assinatura mensal ativa.                                                           |
| Premium Anual                 | Usuário com assinatura anual ativa.                                                            |
| Trial expirado                | Usuário que concluiu os 7 dias gratuitos e ainda não assinou.                                   |
| Assinatura expirada           | Usuário que já assinou, mas perdeu acesso por cancelamento, vencimento ou falha de pagamento.   |
| Admin interno                 | Perfil operacional interno para gestão futura. No MVP, sem painel completo.                    |
| Suporte interno               | Perfil futuro para consulta e suporte. No MVP, sem painel completo.                            |
| Sistema/Worker                | Rotinas automáticas de trial, assinatura, streak, notificações, logs e sincronização.           |
| Serviços externos             | RevenueCat, Firebase, IA, storage e push notifications.                                        |

> Observação: o perfil **Free Hunter** deixa de existir no MVP. O acesso gratuito é temporário e limitado ao trial de 7 dias.

---

## 8. Escopo do MVP

### Entra no MVP

* Splash e primeira experiência visual.
* Cadastro e login.
* Tela de comunicação do trial antes do onboarding.
* Teste gratuito de 7 dias.
* Planos mensal e anual.
* Bloqueio de acesso após expiração do trial.
* Reativação de acesso após assinatura.
* Preservação do progresso mesmo com trial ou assinatura expirada.
* Onboarding completo.
* Perfil físico e preferências de treino.
* Registro de limitações físicas.
* Catálogo inicial de exercícios.
* Geração de quest diária baseada no perfil para usuários com acesso ativo.
* Treino compatível com equipamentos e limitações informadas.
* Treino personalizado para usuários em trial ou assinantes.
* Edição de treino antes de iniciar.
* Execução da quest.
* Conclusão da quest.
* XP, level, rank, atributos e streak.
* Perfil do Hunter.
* Card compartilhável durante trial ou assinatura ativa.
* Histórico básico.
* Nutrição básica simples, se couber no ciclo final do MVP.
* Push de lembrete básico.
* Push ou aviso de fim de trial, se couber no ciclo final do MVP.
* Firebase Analytics.
* Crashlytics.
* Logs básicos de backend.
* Internacionalização preparada para PT-BR, EN e ES.
* App estável para publicação Android.

### Fora do MVP

* Plano gratuito permanente.
* Free Hunter permanente.
* Uso ilimitado do app sem assinatura após os 7 dias.
* Ranking entre amigos.
* Rede social interna.
* Chat/comunidade.
* Wearables.
* Versão web.
* IA avançada ultra-personalizada.
* Nutrição completa com macros detalhados.
* Gráficos avançados de evolução.
* Master Quests completas.
* Sistema completo de badges.
* Marketplace de treinos.
* Avatar 3D.
* Admin panel completo.
* Treinos com vídeos próprios gravados.

---

## 9. Jornada Principal do Usuário no MVP

1. Usuário abre o app.
2. Visualiza splash e proposta do AWAKEN.
3. Escolhe idioma ou usa PT-BR como padrão.
4. Vê a tela pricing, entende o trial de 7 dias e escolhe mensal ou anual ali.
5. A escolha feita na pricing é salva e, em seguida, o usuário cria conta (seleciona nacionalidade, informa nome, e-mail e senha) ou faz login (EPIC-002).
6. Trial inicia automaticamente após o cadastro; a escolha salva na pricing fica vinculada à conta para a compra posterior (usuários com plano pago ativo têm acesso restaurado ao fazer login).
7. Responde onboarding de perfil físico e treino (8 etapas).
8. Informa objetivo, nível de experiência, tempo de treino, dados físicos, tipo de corpo, tempo disponível, limitações físicas e dores.
9. Confirma resumo do perfil.
10. Recebe sua primeira quest diária.
11. Pode editar o treino antes de iniciar.
12. Inicia a quest.
13. Marca exercícios como concluídos.
14. Finaliza o treino.
15. Recebe XP, evolução de atributos e atualização de streak. Se houver dungeon concluída, o item fica armazenado até assinar (se estiver em trial).
16. Visualiza perfil do Hunter.
17. Compartilha o card.
18. Retorna nos dias seguintes para manter streak durante o trial.
19. Ao fim dos 7 dias, caso não tenha assinado, visualiza o paywall obrigatório com o plano já escolhido na pricing e o CTA de compra.
20. Confirma o plano salvo e conclui a compra para continuar usando.
21. Após assinar, recupera acesso ao progresso, itens armazenados e continua a jornada.

---

## 10. Matriz Trial vs Assinatura no MVP

| Funcionalidade                    | Visitante | Trial 7 dias | Premium Mensal | Premium Anual | Trial/Assinatura expirada |
| --------------------------------- | --------: | -----------: | --------------: | -------------: | ------------------------: |
| Ver proposta do app               |       Sim |          Sim |             Sim |            Sim |                       Sim |
| Criar conta                       |       Sim |          Sim |             Sim |            Sim |                       Sim |
| Iniciar trial (sem cartão)        |       Sim |          Não |             Não |            Não |                       Não |
| Onboarding completo (8 etapas)    |       Não |          Sim |             Sim |            Sim |                       Não |
| Quest diária                      |       Não |          Sim |             Sim |            Sim |                       Não |
| Treinos compatíveis com perfil    |       Não |          Sim |             Sim |            Sim |                       Não |
| Editar treino antes de iniciar    |       Não |          Sim |             Sim |            Sim |                       Não |
| Executar treino                   |       Não |          Sim |             Sim |            Sim |                       Não |
| XP, rank e level                  |       Não |          Sim |             Sim |            Sim |                       Não |
| Atributos (6)                     |       Não |          Sim |             Sim |            Sim |                       Não |
| Streak                            |       Não |          Sim |             Sim |            Sim |                       Não |
| Itens de dungeon (receber/usar)   |       Não |      Não (armazenados) |        Sim |           Sim |                       Não |
| Card compartilhável               |       Não |          Sim |             Sim |            Sim |                       Não |
| Card de perfil animado            |       Não |          Não |             Não |            Sim |                       Não |
| Histórico                         |       Não |          Sim |             Sim |            Sim |                  Limitado |
| Nutrição básica                   |       Não |          Sim |             Sim |            Sim |                       Não |
| Prioridade no suporte             |       Não |          Não |             Não |            Sim |                       Não |
| Assinar plano mensal/anual        |       Sim |          Sim |             Sim |            Sim |                       Sim |
| Recuperar acesso após pagamento   |       Não |          Não |             Sim |            Sim |                       Sim |

---

## 10.1. Regras Comerciais do Trial e Assinatura

| ID | Regra |
|---|---|
| RN-COM-001 | Todo novo usuário tem direito a um único trial de 7 dias, sem necessidade de cartão. |
| RN-COM-002 | A tela pricing apresenta o trial de 7 dias antes do cadastro, de forma clara e sem dark pattern, e é o único canal de escolha do plano mensal ou anual. |
| RN-COM-003 | O backend deve ser a fonte de verdade para início, fim e status do trial e da assinatura. |
| RN-COM-004 | O trial inicia automaticamente após o cadastro, sem necessidade de pagamento, e a escolha feita na pricing fica salva para uso posterior. |
| RN-COM-005 | Após o cadastro, o sistema usa a escolha salva na pricing para direcionar a compra via RevenueCat. A assinatura só é ativada após confirmação do pagamento. |
| RN-COM-006 | Após 7 dias sem assinatura, o acesso a onboarding, quests, treino, XP, perfil completo, card e nutrição deve ser bloqueado. |
| RN-COM-007 | Após o trial expirar, o usuário deve conseguir acessar apenas telas de assinatura, conta, termos, privacidade e suporte mínimo. |
| RN-COM-008 | O usuário pode escolher plano mensal (R$ 14,90/mês) ou anual (R$ 99,90/ano). |
| RN-COM-009 | Ao assinar, o acesso deve ser reativado imediatamente. |
| RN-COM-010 | O progresso, histórico, XP, rank, level, atributos e streak não devem ser apagados quando o trial ou a assinatura expirar. |
| RN-COM-011 | O mesmo usuário não pode reiniciar trial usando a mesma conta. |
| RN-COM-012 | A tela que explica o trial deve ser exibida antes do cadastro, de forma clara e sem dark pattern. |
| RN-COM-013 | O paywall exibido após expiração do trial deve confirmar o plano salvo e seguir a compra do RevenueCat, sem permitir nova escolha fora da pricing. |
| RN-COM-014 | Durante o trial, itens de dungeons não podem ser recebidos nem usados. Ficam armazenados e são liberados automaticamente ao assinar qualquer plano. |
| RN-COM-015 | O plano anual deve exibir desconto de 45% e equivalente mensal (R$ 8,32/mês) de forma destacada. |
| RN-COM-016 | O plano anual concede exclusivos ao assinante: card de perfil animado e prioridade no suporte. |
| RN-COM-017 | A pricing screen é o único canal para escolher o revenue; o paywall apenas confirma e executa a compra do plano já salvo. |

---

# 11. Épicos do MVP

## EPIC-001 — Fundação Mobile e Experiência Base

Preparar a base do aplicativo Flutter, identidade visual inicial, navegação, tema dark, internacionalização e estados globais.

## EPIC-002 — Autenticação e Conta do Usuário

Permitir que o usuário crie conta, entre no app, mantenha sessão segura e gerencie ações básicas da conta.

## EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso

Implementar modelo comercial transparente com teste gratuito de 7 dias (sem cartão), planos mensal (R$ 14,90/mês) e anual (R$ 99,90/ano, −45%), bloqueio após expiração e integração com RevenueCat. Durante o trial, itens de dungeons ficam armazenados e são liberados ao assinar.

## EPIC-004 — Onboarding e Perfil Inicial do Hunter

Coletar as informações necessárias para gerar treinos compatíveis com o usuário em um fluxo de 8 etapas: objetivo, nível de experiência, tempo de treino, dados físicos, tipo de corpo (seleção visual), tempo disponível, limitações físicas e dores físicas.

## EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade

Criar base mínima de exercícios, variantes, equipamentos, dificuldade e restrições.

## EPIC-006 — Geração da Quest Diária

Gerar uma quest diária funcional com treino compatível com o perfil do usuário.

## EPIC-007 — Edição de Treino Antes da Quest

Permitir que o usuário visualize e ajuste o treino antes de começar, com edição gratuita só no recorte entre Treino Regenerativo e Programa Específico. No MVP, os programas iniciais são Caminho do Saitama, com progressão por rank, e Perfect 2, com apenas 2 exercícios ideais por grupo muscular. Regerar dentro do programa personalizado individual exige o Pergaminho da Reforja.

## EPIC-008 — Execução da Quest e Registro do Treino

Permitir que o usuário execute, marque progresso e conclua a quest diária, dungeon ou raid.

## EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak

Aplicar a gamificação central do AWAKEN.

## EPIC-010 — Perfil do Hunter e Card Compartilhável

Exibir evolução do usuário e permitir compartilhamento externo.

## EPIC-011 — Histórico Básico e Log de Batalha

Registrar quests concluídas e mostrar histórico simples ao usuário.

## EPIC-012 — Nutrição Básica

Oferecer acompanhamento simples de água e calorias gastas no dia até o momento no MVP, se couber no ciclo final, com visualização na Home logo abaixo do card de rank e antes das quests, com copos de água ajustáveis.

## EPIC-013 — Notificações e Retenção

Enviar lembretes simples para manter streak, retorno diário e consciência sobre o fim do trial.

## EPIC-014 — Analytics, Crash, Logs e Observabilidade

Coletar eventos, falhas e logs para medir ativação, retenção, estabilidade, expiração de trial e conversão para assinatura.

## EPIC-015 — Segurança, Privacidade e LGPD

Garantir consentimento, privacidade, exclusão de conta e proteção dos dados sensíveis.

## EPIC-016 — Release Android e Qualidade MVP

Preparar publicação, testes internos, critérios de estabilidade e versão inicial na Google Play.

---

# 12. Backlog de User Stories do MVP

## EPIC-001 — Fundação Mobile e Experiência Base

| ID     | User Story                                                                                                                             | Prioridade | Perfil    | Plano | Status    |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------- | ---------- | --------- | ----- | --------- |
| US-001 | Como visitante, quero visualizar uma splash screen com a identidade AWAKEN, para entender que estou entrando em uma experiência épica. | P0         | Visitante | Todos | Planejada |
| US-002 | Como usuário, quero navegar por uma estrutura base de telas, para usar o app sem confusão.                                             | P0         | Todos     | Todos | Planejada |
| US-003 | Como usuário, quero uma interface dark, legível e imersiva, para sentir a proposta anime/gamificada sem perder clareza.                | P0         | Todos     | Todos | Planejada |
| US-004 | Como usuário, quero que o app esteja em PT-BR, com estrutura preparada para EN e ES, para usar o produto no meu idioma.                | P0         | Todos     | Todos | Planejada |
| US-005 | Como usuário, quero ver estados de carregamento, erro, vazio e sucesso, para entender o que está acontecendo em cada tela.             | P0         | Todos     | Todos | Planejada |
| US-006 | Como usuário, quero que o app funcione bem em celulares Android mínimos definidos, para ter uma experiência estável.                   | P0         | Todos     | Todos | Planejada |

---

## EPIC-002 — Autenticação e Conta do Usuário

| ID     | User Story                                                                                                       | Prioridade | Perfil    | Plano | Status    |
| ------ | ---------------------------------------------------------------------------------------------------------------- | ---------- | --------- | ----- | --------- |
| US-007 | Como visitante, quero criar uma conta com e-mail e senha, para salvar meu progresso.                             | P0         | Visitante | Todos | Planejada |
| US-008 | Como visitante, quero entrar com e-mail e senha, para acessar meu perfil.                                        | P0         | Visitante | Todos | Planejada |
| US-009 | Como visitante, quero entrar com Google, para acelerar meu cadastro.                                             | P0         | Visitante | Todos | Planejada |
| US-010 | Como usuário autenticado, quero manter minha sessão ativa com segurança, para não precisar fazer login toda vez. | P0         | Todos     | Todos | Planejada |
| US-011 | Como usuário, quero sair da minha conta, para proteger meu acesso em aparelhos compartilhados.                   | P0         | Todos     | Todos | Planejada |
| US-012 | Como usuário, quero recuperar minha senha, para voltar a acessar minha conta caso esqueça.                       | P1         | Todos     | Todos | Planejada |
| US-013 | Como usuário, quero excluir minha conta, para exercer meu direito de remoção dos dados.                          | P1         | Todos     | Todos | Planejada |

---

## EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso

| ID     | User Story                                                                                                                                                                          | Prioridade | Perfil                        | Plano                 | Status    |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------------------------- | --------------------- | --------- |
| US-014 | Como visitante, quero entender antes do onboarding que posso testar o AWAKEN por 7 dias e depois precisarei assinar, para iniciar com transparência.                                | P0         | Visitante                     | Trial/Mensal/Anual    | Planejada |
| US-015 | Como visitante, quero iniciar meu teste gratuito de 7 dias, para experimentar o AWAKEN antes de decidir assinar.                                                                    | P0         | Visitante                     | Trial                 | Planejada |
| US-016 | Como sistema, quero registrar início e fim do trial de 7 dias, para controlar corretamente o acesso gratuito temporário.                                                            | P0         | Sistema                       | Trial                 | Planejada |
| US-017 | Como usuário, quero visualizar os benefícios dos planos mensal e anual de forma clara, para decidir qual assinatura faz mais sentido.                                                | P0         | Todos                         | Mensal/Anual          | Planejada |
| US-018 | Como sistema, quero sincronizar entitlement com RevenueCat, para liberar, bloquear ou reativar o acesso corretamente.                                                               | P0         | Sistema                       | Trial/Mensal/Anual    | Planejada |
| US-019 | Como assinante mensal ou anual, quero ter meu acesso reconhecido no app, para usar o AWAKEN sem fricção.                                                                            | P0         | Premium Mensal/Premium Anual  | Mensal/Anual          | Planejada |
| US-020 | Como usuário com trial ou assinatura expirada, quero visualizar um paywall obrigatório, para assinar mensal ou anual e recuperar meu acesso.                                        | P0         | Trial expirado/Assinatura expirada | Mensal/Anual      | Planejada |
| US-021 | Como usuário, quero que o paywall seja exibido de forma honesta e previsível, para não sentir que fui enganado após investir tempo no app.                                          | P0         | Todos                         | Trial/Mensal/Anual    | Planejada |
| US-116 | Como usuário em trial, quero visualizar quantos dias gratuitos ainda tenho, para saber quando precisarei assinar.                                                                   | P0         | Usuário em Trial              | Trial                 | Planejada |
| US-117 | Como usuário em trial, quero receber avisos quando meu teste estiver próximo do fim, para decidir se vou assinar antes de perder acesso.                                            | P1         | Usuário em Trial              | Trial                 | Planejada |
| US-118 | Como usuário com trial expirado, quero assinar o plano mensal, para continuar usando o AWAKEN com pagamento recorrente mensal.                                                     | P0         | Trial expirado                | Mensal                | Planejada |
| US-119 | Como usuário com trial expirado, quero assinar o plano anual, para continuar usando o AWAKEN com melhor custo-benefício.                                                           | P0         | Trial expirado                | Anual                 | Planejada |
| US-120 | Como usuário que assinou após trial expirado, quero recuperar imediatamente meu acesso, para continuar minha evolução de onde parei.                                                | P0         | Trial expirado                | Mensal/Anual          | Planejada |
| US-121 | Como usuário com trial ou assinatura expirada, quero que meu progresso fique salvo, para não perder minha evolução caso eu assine depois.                                          | P0         | Trial expirado/Assinatura expirada | Mensal/Anual      | Planejada |
| US-122 | Como sistema, quero impedir que o mesmo usuário reinicie trial indevidamente, para proteger o modelo comercial do AWAKEN.                                                          | P0         | Sistema                       | Trial                 | Planejada |

---

## EPIC-004 — Onboarding e Perfil Inicial do Hunter

| ID     | User Story                                                                                                           | Prioridade | Perfil                   | Plano              | Status    |
| ------ | -------------------------------------------------------------------------------------------------------------------- | ---------- | ------------------------ | ------------------ | --------- |
| US-022 | Como novo usuário, quero iniciar o onboarding após entender o trial e os planos, para configurar meu perfil com transparência. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-023 | Como usuário com acesso ativo, quero informar meu objetivo principal (ganhar massa, perder peso, condicionamento, força ou manter a forma), para receber treinos coerentes com minha meta. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-024 | Como usuário com acesso ativo, quero informar meu nível de experiência (sedentário, iniciante, intermediário ou avançado), para não receber treinos difíceis ou fáceis demais. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-140 | Como usuário com acesso ativo, quero informar há quanto tempo treino (de "não treino" a "mais de 3 anos"), para que o sistema entenda meu histórico e ajuste a progressão. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-025 | Como usuário com acesso ativo, quero informar idade, altura, peso e sexo biológico, para melhorar a recomendação inicial. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-141 | Como usuário com acesso ativo, quero selecionar meu tipo de corpo atual por meio de silhuetas visuais (magro, normal, gordo ou atlético/forte), para personalizar meu perfil e a geração do treino. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-028 | Como usuário com acesso ativo, quero informar meu tempo disponível por treino (5-10, 10-20, 20-30, 30-40 ou 40-50 min), para receber quests compatíveis com minha rotina. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-142 | Como usuário com acesso ativo, quero informar minhas limitações físicas, para que o sistema filtre exercícios contraindicados do catálogo e não me coloque em risco. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-030 | Como usuário com acesso ativo, quero informar minhas dores físicas (Pescoço, Ombro, Pulso, Costas, Lombar ou Joelhos), para evitar exercícios que agravem as regiões afetadas. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-032 | Como usuário com acesso ativo, quero revisar meu perfil antes de concluir, para corrigir erros.                      | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-033 | Como usuário com acesso ativo, quero salvar meu perfil inicial, para gerar minha primeira quest.                     | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-034 | Como usuário com acesso ativo, quero editar meu perfil após o onboarding, para atualizar minha realidade sem refazer tudo. | P1     | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |

---

## EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade

| ID     | User Story                                                                                                                  | Prioridade | Perfil  | Plano | Status    |
| ------ | --------------------------------------------------------------------------------------------------------------------------- | ---------- | ------- | ----- | --------- |
| US-035 | Como sistema, quero ter um catálogo inicial de exercícios, para montar treinos sem depender 100% de IA.                     | P0         | Sistema | Todos | Planejada |
| US-036 | Como sistema, quero classificar exercícios por grupo muscular, para equilibrar os treinos.                                  | P0         | Sistema | Todos | Planejada |
| US-037 | Como sistema, quero classificar exercícios por equipamento necessário, para respeitar o que o usuário possui.               | P0         | Sistema | Todos | Planejada |
| US-038 | Como sistema, quero classificar exercícios por dificuldade, para adaptar ao nível do usuário.                               | P0         | Sistema | Todos | Planejada |
| US-039 | Como sistema, quero mapear variantes fáceis e difíceis dos exercícios, para ajustar treinos de iniciantes e intermediários. | P0         | Sistema | Todos | Planejada |
| US-040 | Como sistema, quero mapear contraindicações básicas, para evitar exercícios incompatíveis com limitações informadas.        | P0         | Sistema | Todos | Planejada |
| US-041 | Como usuário, quero ver instruções simples do exercício, para executar com mais segurança.                                  | P0         | Todos   | Todos | Planejada |

---

## EPIC-006 — Geração da Quest Diária

| ID     | User Story                                                                                                                | Prioridade | Perfil                        | Plano              | Status    |
| ------ | ------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------------------------- | ------------------ | --------- |
| US-042 | Como usuário com acesso ativo, quero receber uma quest diária baseada no meu perfil, para saber exatamente o que treinar hoje. | P0      | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-043 | Como sistema, quero bloquear geração de quest para trial expirado ou assinatura expirada, para cumprir o modelo comercial. | P0         | Sistema                       | Trial/Mensal/Anual | Planejada |
| US-044 | Como usuário em trial ou assinante, quero receber quest personalizada, para perceber valor real antes e depois da assinatura. | P0       | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-045 | Como sistema, quero impedir exercícios incompatíveis com limitações e equipamentos, para proteger a confiança do usuário. | P0         | Sistema                       | Todos              | Planejada |
| US-046 | Como sistema, quero usar fallback por templates quando a IA falhar, para não deixar o usuário com acesso ativo sem treino. | P0        | Sistema                       | Trial/Mensal/Anual | Planejada |
| US-047 | Como usuário com acesso ativo, quero que minha quest do dia fique salva, para não perder o treino ao fechar o app.         | P0         | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-048 | Como usuário com acesso ativo, quero regenerar a quest dentro de limites justos, para ajustar um treino ruim sem abusar do sistema. | P1  | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-049 | Como sistema, quero registrar o motivo da geração da quest, para auditar se ela respeitou o perfil do usuário.            | P1         | Sistema                       | Todos              | Planejada |

---

## EPIC-007 — Edição de Treino Antes da Quest

No MVP, a edição sem item fica limitada ao recorte entre Treino Regenerativo e Programa Específico. Os programas iniciais são Caminho do Saitama, com progressão por rank, e Perfect 2, com apenas 2 exercícios ideais por grupo muscular. Regerar dentro do programa personalizado individual exige o Pergaminho da Reforja.

| ID     | User Story                                                                                                        | Prioridade | Perfil                   | Plano              | Status    |
| ------ | ----------------------------------------------------------------------------------------------------------------- | ---------- | ------------------------ | ------------------ | --------- |
| US-050 | Como usuário com acesso ativo, quero visualizar o treino antes de iniciar, para decidir se quero executá-lo.       | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-051 | Como usuário com acesso ativo, quero substituir um exercício antes de iniciar, para adaptar o treino à minha realidade. | P0      | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-052 | Como usuário com acesso ativo, quero ajustar séries, repetições ou tempo antes de iniciar, para adequar a intensidade. | P0      | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-053 | Como sistema, quero validar alterações do treino, para impedir combinações incompatíveis com o perfil.            | P0         | Sistema                  | Todos              | Planejada |
| US-054 | Como usuário com acesso ativo, quero salvar preferências de edição, para o sistema respeitar minhas escolhas nas próximas semanas. | P1 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-055 | Como sistema, quero bloquear edição de treino para trial expirado ou assinatura expirada, para cumprir a regra de acesso pago. | P0 | Sistema                  | Trial/Mensal/Anual | Planejada |

---

## EPIC-008 — Execução da Quest e Registro do Treino

Executar, acompanhar e registrar quests diárias, dungeons e raids com um contrato comum de progresso, conclusão, XP e recompensa.

| ID     | User Story                                                                                       | Prioridade | Perfil                   | Plano              | Status    |
| ------ | ------------------------------------------------------------------------------------------------ | ---------- | ------------------------ | ------------------ | --------- |
| US-056 | Como usuário com acesso ativo, quero iniciar a quest, para começar meu treino do dia.             | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-057 | Como usuário com acesso ativo, quero acompanhar exercício por exercício, para saber o que fazer em seguida. | P0  | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-058 | Como usuário com acesso ativo, quero marcar exercício como concluído, para registrar meu progresso. | P0       | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-059 | Como usuário com acesso ativo, quero pausar e retomar a quest, para lidar com interrupções reais. | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-060 | Como usuário com acesso ativo, quero cancelar uma quest em andamento, para sair sem gerar progresso indevido. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-061 | Como usuário com acesso ativo, quero concluir a quest, para receber XP e atualizar meu progresso. | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-062 | Como sistema, quero registrar o treino concluído, para manter histórico e alimentar gamificação. | P0         | Sistema                  | Todos              | Planejada |
| US-063 | Como usuário com acesso ativo, quero ver uma tela de recompensa após concluir, para sentir evolução imediata. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |

---

## EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak

| ID     | User Story                                                                                             | Prioridade | Perfil                   | Plano              | Status    |
| ------ | ------------------------------------------------------------------------------------------------------ | ---------- | ------------------------ | ------------------ | --------- |
| US-064 | Como usuário com acesso ativo, quero ganhar XP ao concluir exercícios, para sentir progressão.         | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-065 | Como sistema, quero calcular XP por esforço, dificuldade e conclusão, para recompensar de forma justa. | P0         | Sistema                  | Todos              | Planejada |
| US-066 | Como usuário com acesso ativo, quero subir de level ao acumular XP, para perceber evolução contínua.   | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-067 | Como usuário com acesso ativo, quero evoluir de rank, para ter uma meta aspiracional de longo prazo.   | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-068 | Como usuário com acesso ativo, quero evoluir atributos, para entender em que área estou melhorando.    | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-069 | Como usuário com acesso ativo, quero manter streak ao treinar em dias consecutivos, para criar hábito. | P0         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-070 | Como sistema, quero preservar streak com regra clara de virada de dia, para evitar injustiça.          | P0         | Sistema                  | Todos              | Planejada |
| US-071 | Como usuário com acesso ativo, quero receber feedback visual de level up, para reforçar a sensação de conquista. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-072 | Como usuário com trial ou assinatura expirada, quero que meu progresso já conquistado não seja apagado, para poder continuar após assinar. | P0 | Trial expirado/Assinatura expirada | Mensal/Anual | Planejada |

---

## EPIC-010 — Perfil do Hunter e Card Compartilhável

| ID     | User Story                                                                                                         | Prioridade | Perfil                        | Plano              | Status    |
| ------ | ------------------------------------------------------------------------------------------------------------------ | ---------- | ----------------------------- | ------------------ | --------- |
| US-073 | Como usuário com acesso ativo, quero visualizar meu perfil Hunter, para acompanhar minha evolução.                  | P0         | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-074 | Como usuário com acesso ativo, quero ver rank, level, XP, streak e atributos no perfil, para entender meu estado atual. | P0    | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-075 | Como sistema, quero definir uma classe inicial do usuário, para reforçar identidade gamificada.                    | P1         | Sistema                       | Todos              | Planejada |
| US-076 | Como usuário com acesso ativo, quero usar avatar básico ou imagem de perfil, para personalizar minimamente meu card. | P1       | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-077 | Como usuário com acesso ativo, quero gerar um card compartilhável, para divulgar meu progresso fora do app.         | P0         | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-078 | Como usuário com acesso ativo, quero compartilhar meu card por WhatsApp, Instagram ou outros apps, para gerar viralização orgânica. | P0 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-079 | Como usuário em trial, quero ter um card compartilhável funcional durante o teste, para perceber valor antes de assinar. | P0     | Usuário em Trial              | Trial              | Planejada |
| US-080 | Como assinante mensal ou anual, quero ter visual premium no card, para perceber diferenciação estética.             | P1         | Premium Mensal/Premium Anual  | Mensal/Anual       | Planejada |

---

## EPIC-011 — Histórico Básico e Log de Batalha

| ID     | User Story                                                                                                        | Prioridade | Perfil                        | Plano              | Status    |
| ------ | ----------------------------------------------------------------------------------------------------------------- | ---------- | ----------------------------- | ------------------ | --------- |
| US-081 | Como usuário com acesso ativo, quero ver quests concluídas recentemente, para acompanhar minha consistência.       | P0         | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-082 | Como usuário em trial, quero ver meu histórico durante os 7 dias gratuitos, para entender meu progresso antes de assinar. | P0   | Usuário em Trial              | Trial              | Planejada |
| US-083 | Como assinante mensal ou anual, quero ver histórico completo, para acompanhar evolução sem limite curto.           | P1         | Premium Mensal/Premium Anual  | Mensal/Anual       | Planejada |
| US-084 | Como usuário com acesso ativo, quero ver XP recebido em cada quest, para entender minha progressão.                | P0         | Usuário em Trial/Premium      | Trial/Mensal/Anual | Planejada |
| US-085 | Como sistema, quero registrar logs de conclusão de quest, para manter consistência entre histórico e gamificação. | P0         | Sistema                       | Todos              | Planejada |

---

## EPIC-012 — Nutrição Básica

| ID     | User Story                                                                                                         | Prioridade | Perfil                   | Plano              | Status    |
| ------ | ------------------------------------------------------------------------------------------------------------------ | ---------- | ------------------------ | ------------------ | --------- |
| US-086 | Como usuário com acesso ativo, quero ver meta diária simples de água, para cuidar do básico da minha rotina.       | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-087 | Como usuário com acesso ativo, quero registrar consumo de água, para acompanhar minha hidratação.                  | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-088 | Como usuário com acesso ativo, quero ver o gasto calórico estimado do dia até o momento, para acompanhar meu consumo energético. | P1 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-089 | Como usuário com acesso ativo, quero visualizar a nutrição básica na Home, para ver água e calorias antes das quests. | P1 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-090 | Como usuário com acesso ativo, quero ver a água em copos ajustáveis, para entender melhor minha hidratação diária. | P1 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |

---

## EPIC-013 — Notificações e Retenção

| ID     | User Story                                                                                               | Prioridade | Perfil                   | Plano              | Status    |
| ------ | -------------------------------------------------------------------------------------------------------- | ---------- | ------------------------ | ------------------ | --------- |
| US-091 | Como usuário com acesso ativo, quero permitir notificações, para receber lembretes de treino.            | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-092 | Como usuário com acesso ativo, quero receber lembrete da quest diária, para não esquecer de treinar.     | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-093 | Como usuário com acesso ativo, quero receber alerta de streak em risco, para ter chance de manter minha sequência. | P1 | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-094 | Como usuário, quero configurar horário preferido de lembrete, para receber notificações em momento útil. | P1         | Usuário em Trial/Premium | Trial/Mensal/Anual | Planejada |
| US-095 | Como sistema, quero evitar notificações excessivas, para não gerar irritação ou abandono.                | P1         | Sistema                  | Todos              | Planejada |
| US-123 | Como usuário em trial, quero receber aviso de proximidade do fim do teste, para decidir se vou assinar antes do bloqueio. | P1 | Usuário em Trial | Trial | Planejada |
| US-124 | Como usuário com trial expirado, quero receber comunicação clara de reativação, para entender que posso voltar assinando mensal ou anual. | P1 | Trial expirado | Mensal/Anual | Planejada |

---

## EPIC-014 — Analytics, Crash, Logs e Observabilidade

| ID     | User Story                                                                                                 | Prioridade | Perfil     | Plano              | Status    |
| ------ | ---------------------------------------------------------------------------------------------------------- | ---------- | ---------- | ------------------ | --------- |
| US-096 | Como produto, quero rastrear eventos de onboarding, para medir ativação.                                   | P0         | Produto    | Todos              | Planejada |
| US-097 | Como produto, quero rastrear geração, início e conclusão de quest, para medir engajamento.                 | P0         | Produto    | Trial/Mensal/Anual | Planejada |
| US-098 | Como produto, quero rastrear XP, level up e streak, para medir impacto da gamificação.                     | P0         | Produto    | Trial/Mensal/Anual | Planejada |
| US-099 | Como produto, quero rastrear visualização de trial, planos e paywall, para medir conversão.                | P0         | Produto    | Trial/Mensal/Anual | Planejada |
| US-100 | Como engenharia, quero registrar crashes no Firebase Crashlytics, para corrigir falhas críticas.           | P0         | Engenharia | Todos              | Planejada |
| US-101 | Como engenharia, quero logs com correlationId no backend, para investigar erros de API.                    | P0         | Engenharia | Todos              | Planejada |
| US-102 | Como produto, quero identificar queda em funis críticos, para priorizar melhorias pós-lançamento.          | P1         | Produto    | Todos              | Planejada |
| US-125 | Como produto, quero rastrear início, contagem e expiração do trial, para medir conversão após 7 dias.      | P0         | Produto    | Trial              | Planejada |
| US-126 | Como produto, quero rastrear escolha de plano mensal ou anual, para avaliar preferência e receita prevista. | P0        | Produto    | Mensal/Anual       | Planejada |

---

## EPIC-015 — Segurança, Privacidade e LGPD

| ID     | User Story                                                                                              | Prioridade | Perfil  | Plano | Status    |
| ------ | ------------------------------------------------------------------------------------------------------- | ---------- | ------- | ----- | --------- |
| US-103 | Como usuário, quero aceitar termos de uso e política de privacidade, para usar o app com clareza legal. | P0         | Todos   | Todos | Planejada |
| US-104 | Como usuário, quero entender que o app não substitui orientação médica, para evitar uso inadequado.     | P0         | Todos   | Todos | Planejada |
| US-105 | Como sistema, quero proteger tokens e sessão do usuário, para evitar acesso indevido.                   | P0         | Sistema | Todos | Planejada |
| US-106 | Como sistema, quero validar dados sensíveis no backend, para impedir entrada inválida ou perigosa.      | P0         | Sistema | Todos | Planejada |
| US-107 | Como usuário, quero solicitar exclusão de conta, para remover meus dados pessoais.                      | P1         | Todos   | Todos | Planejada |
| US-108 | Como sistema, quero auditar ações sensíveis, para manter rastreabilidade.                               | P1         | Sistema | Todos | Planejada |

---

## EPIC-016 — Release Android e Qualidade MVP

| ID     | User Story                                                                                                                | Prioridade | Perfil     | Plano | Status    |
| ------ | ------------------------------------------------------------------------------------------------------------------------- | ---------- | ---------- | ----- | --------- |
| US-109 | Como engenharia, quero configurar ambientes de desenvolvimento, homologação e produção, para reduzir risco de lançamento. | P0         | Engenharia | Todos | Planejada |
| US-110 | Como engenharia, quero gerar build Android de teste interno, para validar o app antes do público.                         | P0         | Engenharia | Todos | Planejada |
| US-111 | Como QA, quero executar checklist de smoke test, para garantir que fluxos críticos funcionam.                             | P0         | QA         | Todos | Planejada |
| US-112 | Como QA, quero testar onboarding, quest, edição, conclusão e gamificação, para validar o núcleo do MVP.                   | P0         | QA         | Todos | Planejada |
| US-113 | Como QA, quero testar cenários de trial ativo, trial expirado, assinatura mensal, assinatura anual e assinatura expirada, para validar monetização.                      | P0         | QA         | Todos | Planejada |
| US-114 | Como engenharia, quero usar feature flags simples, para desligar recursos problemáticos sem republicar o app.             | P1         | Engenharia | Todos | Planejada |
| US-115 | Como produto, quero publicar versão inicial em teste aberto na Google Play, para coletar feedback real.                   | P0         | Produto    | Todos | Planejada |

---

# 13. Priorização Recomendada por Ordem de Implementação

## Bloco 1 — Base técnica e identidade

* US-001
* US-002
* US-003
* US-004
* US-005
* US-006
* US-109

## Bloco 2 — Conta, sessão e segurança inicial

* US-007
* US-008
* US-009
* US-010
* US-011
* US-103
* US-104
* US-105
* US-106

## Bloco 3 — Trial, assinatura e bloqueio de acesso

* US-014
* US-015
* US-016
* US-017
* US-018
* US-019
* US-020
* US-021
* US-116
* US-118
* US-119
* US-120
* US-121
* US-122

## Bloco 4 — Onboarding

* US-022
* US-023
* US-024
* US-140
* US-025
* US-141
* US-028
* US-142
* US-030
* US-032
* US-033

## Bloco 5 — Catálogo e geração de treino

* US-035
* US-036
* US-037
* US-038
* US-039
* US-040
* US-041
* US-042
* US-043
* US-044
* US-045
* US-046
* US-047

## Bloco 6 — Edição e execução da quest

* US-050
* US-051
* US-052
* US-053
* US-055
* US-056
* US-057
* US-058
* US-060
* US-061
* US-062
* US-063

## Bloco 7 — Gamificação

* US-064
* US-065
* US-066
* US-067
* US-068
* US-069
* US-070
* US-071
* US-072

## Bloco 8 — Perfil, card e histórico

* US-073
* US-074
* US-077
* US-078
* US-079
* US-081
* US-082
* US-084
* US-085

## Bloco 9 — Analytics, crash e qualidade

* US-096
* US-097
* US-098
* US-099
* US-100
* US-101
* US-110
* US-111
* US-112
* US-113
* US-115
* US-125
* US-126

## Bloco 10 — P1 do MVP, se houver tempo antes do lançamento

* US-012
* US-013
* US-031
* US-034
* US-048
* US-049
* US-054
* US-059
* US-075
* US-076
* US-080
* US-083
* US-086
* US-087
* US-088
* US-089
* US-090
* US-091
* US-092
* US-093
* US-094
* US-095
* US-102
* US-107
* US-108
* US-114
* US-117
* US-123
* US-124

---

# 14. Entidades Principais do MVP

## User

Representa a conta do usuário.

Campos principais:

* id
* email
* name
* authProvider
* createdAt
* updatedAt
* deletedAt

## UserProfile

Representa perfil físico e histórico de treino coletados no onboarding.

Campos principais:

* userId
* age
* heightCm
* weightKg
* biologicalSex
* goal
* experienceLevel
* trainingDuration
* bodyType
* availableMinutesPerWorkout
* physicalLimitations
* physicalPains
* onboardingCompletedAt

## Subscription

Representa status comercial, trial, assinatura e bloqueio de acesso.

Campos principais:

* userId
* plan
* status
* entitlement
* revenueCatCustomerId
* trialStartedAt
* trialEndsAt
* trialConsumedAt
* trialStatus
* subscriptionStartedAt
* subscriptionEndsAt
* expiresAt
* accessStatus
* lastRevenueCatSyncAt

Valores esperados para `plan`:

* trial
* monthly
* annual

Valores esperados para `accessStatus`:

* visitor
* trial_active
* trial_expired
* subscription_active
* subscription_expired
* blocked

## Exercise

Representa exercício do catálogo.

Campos principais:

* id
* name
* description
* muscleGroups
* equipmentRequired
* difficulty
* variants
* contraindicationTags
* attributeImpacts

## Quest

Representa quest diária gerada.

Campos principais:

* id
* userId
* questDate
* status
* source
* generatedFromProfileHash
* totalEstimatedMinutes
* xpPreview
* createdAt
* startedAt
* completedAt

## QuestExercise

Representa exercício dentro da quest.

Campos principais:

* id
* questId
* exerciseId
* order
* sets
* reps
* durationSeconds
* restSeconds
* notes
* status

## QuestLog

Representa conclusão e histórico.

Campos principais:

* id
* userId
* questId
* completedAt
* xpEarned
* attributesEarned
* streakBefore
* streakAfter
* rankBefore
* rankAfter
* levelBefore
* levelAfter

## HunterProgress

Representa progressão gamificada.

Campos principais:

* userId
* rank
* level
* currentXp
* totalXp
* streakCount
* longestStreak
* lastQuestCompletedDate

## HunterAttributes

Representa atributos do usuário.

Campos principais:

* userId
* strength
* agility
* endurance
* vitality
* focus

## NutritionLog

Representa registro nutricional básico.

Campos principais:

* id
* userId
* date
* waterMl
* caloriesSpentEstimated

---

# 15. Eventos de Analytics Iniciais

| Evento                         | Quando dispara                                                        |
| ------------------------------ | --------------------------------------------------------------------- |
| app_opened                     | Quando o app abre.                                                    |
| splash_viewed                  | Quando a splash é exibida.                                            |
| trial_offer_viewed             | Quando a tela de teste gratuito de 7 dias é exibida.                  |
| plans_viewed                   | Quando a tela de planos mensal/anual é exibida.                       |
| trial_started                  | Quando usuário inicia o teste gratuito.                               |
| trial_day_count_viewed         | Quando usuário visualiza quantos dias restam no trial.                |
| trial_day_3_reached            | Quando o usuário chega ao terceiro dia de trial.                      |
| trial_day_6_reached            | Quando o usuário chega ao sexto dia de trial.                         |
| trial_expired                  | Quando o trial expira.                                                |
| paywall_after_trial_viewed     | Quando o paywall obrigatório é exibido após o trial.                  |
| monthly_plan_selected          | Quando usuário seleciona plano mensal.                                |
| annual_plan_selected           | Quando usuário seleciona plano anual.                                 |
| subscription_started           | Quando assinatura é reconhecida.                                      |
| subscription_expired           | Quando assinatura expira.                                             |
| access_blocked                 | Quando acesso é bloqueado por trial ou assinatura expirada.           |
| access_restored                | Quando acesso é restaurado após assinatura.                           |
| login_started                  | Quando usuário inicia login.                                          |
| login_completed                | Quando login é concluído.                                             |
| onboarding_started             | Quando onboarding começa.                                             |
| onboarding_step_completed      | A cada etapa concluída.                                               |
| onboarding_completed           | Quando perfil inicial é salvo.                                        |
| quest_generated                | Quando uma quest é criada.                                            |
| quest_generation_blocked       | Quando geração de quest é bloqueada por falta de assinatura ativa.    |
| quest_generation_failed        | Quando geração falha.                                                 |
| quest_viewed                   | Quando usuário visualiza quest.                                       |
| workout_edited                 | Quando usuário edita treino.                                          |
| quest_started                  | Quando usuário inicia treino.                                         |
| exercise_completed             | Quando exercício é marcado.                                           |
| quest_completed                | Quando quest é concluída.                                             |
| xp_earned                      | Quando XP é aplicado.                                                 |
| level_up                       | Quando usuário sobe de level.                                         |
| rank_up                        | Quando usuário sobe de rank.                                          |
| streak_updated                 | Quando streak é atualizada.                                           |
| hunter_profile_viewed          | Quando perfil é aberto.                                               |
| hunter_card_shared             | Quando card é compartilhado.                                          |
| crash_detected                 | Quando Crashlytics captura falha.                                     |

---

# 16. Definition of Ready para Detalhar uma User Story

Uma User Story estará pronta para detalhamento individual quando tiver:

* ID definido;
* épico definido;
* prioridade definida;
* perfil principal definido;
* plano impactado definido;
* objetivo claro;
* dependência principal mapeada;
* impacto em Flutter identificado;
* impacto em backend identificado;
* impacto em banco identificado;
* impacto em gamificação identificado;
* impacto em monetização identificado, quando houver;
* impacto em internacionalização identificado;
* critérios de aceite a serem escritos.

---

# 17. Definition of Done do MVP

O MVP só deve ser considerado pronto quando:

* usuário conseguir criar conta;
* usuário conseguir ver a explicação do teste gratuito de 7 dias antes do onboarding;
* usuário conseguir iniciar trial de 7 dias;
* sistema conseguir registrar início e fim do trial;
* usuário conseguir completar onboarding durante trial ou assinatura ativa;
* sistema bloquear acesso quando o trial expirar sem assinatura;
* usuário conseguir assinar plano mensal ou anual após o trial;
* sistema reativar acesso após assinatura;
* sistema preservar progresso quando trial ou assinatura expirar;
* sistema gerar quest compatível com perfil para usuários com acesso ativo;
* sistema não sugerir equipamento ausente;
* sistema não sugerir exercício incompatível com limitação física;
* usuário com acesso ativo conseguir editar treino antes de iniciar;
* usuário com acesso ativo conseguir iniciar e concluir quest;
* XP, rank, level, atributos e streak atualizarem corretamente;
* perfil Hunter exibir progresso para usuários com acesso ativo;
* card compartilhável funcionar durante trial ou assinatura ativa;
* histórico básico funcionar;
* eventos críticos forem enviados ao Firebase;
* eventos de trial, paywall, assinatura e bloqueio forem enviados ao Firebase;
* crashes forem capturados;
* backend tiver logs mínimos;
* app estiver em PT-BR;
* estrutura de EN e ES estiver preparada;
* smoke test passar em dispositivos Android mínimos;
* nenhum bug crítico impedir cadastro, login, trial, onboarding, quest, conclusão, assinatura ou reativação de acesso.

---

# 18. Decisão Registrada

O MVP do AWAKEN deve priorizar a experiência central de transformação do treino em quest diária gamificada, dentro de um modelo comercial de teste gratuito temporário.

A fronteira do MVP é:

> cadastrar, iniciar trial de 7 dias, entender o usuário, gerar treino compatível, permitir edição, executar quest, recompensar com XP/rank/streak, exibir perfil Hunter, permitir compartilhamento e converter para assinatura mensal ou anual após o fim do teste.

A decisão comercial registrada é:

> o AWAKEN não terá plano gratuito permanente no MVP. O usuário poderá testar gratuitamente por 7 dias. Depois disso, deverá assinar o plano mensal ou anual para continuar usando.

Tudo que não reforça diretamente ativação, retenção, estabilidade, trial transparente, conversão para assinatura ou progressão gamificada deve ficar para Pós-MVP.

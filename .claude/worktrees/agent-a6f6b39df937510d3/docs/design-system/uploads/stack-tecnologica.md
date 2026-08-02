# STACK TECNOLÓGICA — APP FITNESS GAMIFICADO
> Android primeiro, iOS depois. Foco em MVP rápido com capacidade de escalar.

---

## ⚠️ DECISÃO CRÍTICA ANTES DE TUDO: MAUI ou Flutter?

A pesquisa em fontes atuais (2026) mostra um ponto importante para **este projeto específico**:

| Critério | .NET MAUI | Flutter |
|---|---|---|
| Animações pesadas (partículas, level-up, cards) | ⚠️ Médio | ✅ Excelente |
| Consistência visual cross-platform | ⚠️ Depende do SO | ✅ Pixel-perfect igual em todos |
| Performance de UI rica | ⚠️ Delega ao SO | ✅ Renderiza direto na GPU |
| Comunidade e libs de gamificação | ⚠️ Pequena | ✅ Grande |
| Curva de aprendizado | ✅ Fácil (C#) | ⚠️ Dart (nova linguagem) |
| Ecossistema .NET (backend integrado) | ✅ Natural | ⚠️ Separado |
| Maturidade para apps enterprise | ✅ Forte | ⚠️ Cresce |
| Android → iOS expansion | ✅ Sim | ✅ Sim |

### Veredicto honesto

> **Este app tem estética anime, animações de level-up, partículas de XP, cards que brilham por rank, transições épicas.** Flutter foi feito exatamente para isso. MAUI entrega na nativa do SO — o que é ótimo para apps empresariais, mas limita o controle visual que este projeto exige.

**Recomendação:**

- **Equipe já sabe C# / .NET?** → MAUI. A produtividade supera a desvantagem de animações.
- **Equipe nova ou sem preferência?** → **Flutter. É a escolha técnica correta para este projeto.**
- **Quer fazer os dois amanhã?** → Flutter agora + backend .NET separado. Melhor dos dois mundos.

O documento descreve **as duas stacks completas** para você decidir. Backend é o **mesmo para os dois**.

---

## OPÇÃO A — FLUTTER (Recomendado para este projeto)

### Por que Flutter ganha aqui
- Renderizador **Impeller**: sem jank na primeira animação, 60–120fps consistente
- Controla cada pixel — o card de rank brilha exatamente como você desenhou, igual no Android e iOS
- Biblioteca **Flame** (game engine sobre Flutter) — perfeita para efeitos de partícula, XP, dungeons
- `flutter_animate` para transições cinematográficas com 2 linhas de código
- Hot reload instantâneo — testa a animação em segundos

### Stack Frontend — Flutter

```
Flutter 3.x (Dart)
├── UI Framework
│   ├── flutter_animate          # animações declarativas (level-up, XP bar, transitions)
│   ├── flame                    # engine 2D — partículas, efeitos de rank
│   ├── lottie                   # animações JSON (After Effects → app)
│   └── cached_network_image     # avatares e arte sem travar UI
│
├── State Management
│   ├── Riverpod 2.x             # estado reativo, testável, sem boilerplate
│   └── flutter_hooks            # lógica de UI reutilizável
│
├── Navegação
│   └── go_router                # roteamento declarativo, deep links, tabs
│
├── Local Storage
│   ├── drift (SQLite)           # banco local offline-first
│   ├── flutter_secure_storage   # tokens JWT seguros
│   └── shared_preferences       # configs simples
│
├── Networking
│   ├── dio                      # HTTP client + interceptors
│   └── retrofit                 # geração de código para API calls
│
├── Auth
│   ├── google_sign_in           # login Google
│   └── sign_in_with_apple       # login Apple (obrigatório para iOS)
│
├── Notificações
│   └── firebase_messaging       # push notifications (FCM)
│
├── Analytics & Crashes
│   ├── firebase_analytics       # eventos, funis, retenção
│   └── firebase_crashlytics     # crash reports
│
├── Monetização
│   └── purchases_flutter        # RevenueCat SDK — subscriptions Android+iOS
│
└── Compartilhamento
    ├── screenshot               # captura o card de perfil como imagem
    └── share_plus               # compartilha o card no WhatsApp, IG, TikTok
```

### IDE & Ferramentas — Flutter
- **IDE:** Android Studio ou VS Code com plugin Flutter/Dart
- **Device Preview:** Flutter Inspector + `device_preview` package
- **Testes:** `flutter_test` + `mocktail`
- **CI/CD:** GitHub Actions + `fastlane` (build e deploy automático)

---

## OPÇÃO B — .NET MAUI (Se equipe já domina C#)

### Quando faz sentido
- Dev principal já tem experiência C#/.NET
- Quer reutilizar lógica com o backend (shared models, validações)
- Prioriza desenvolvimento rápido sobre perfeição de animações
- Aceita que efeitos de partícula serão mais simples no MVP

### Stack Frontend — MAUI

```
.NET MAUI 9 (C#)
├── UI Framework
│   ├── SkiaSharp                # 2D customizado — cards de rank, gráficos de stats
│   ├── Lottie for MAUI          # animações JSON (level-up, XP)
│   ├── MAUI Community Toolkit   # behaviors, animações, converters
│   └── MauiIcons                # biblioteca de ícones
│
├── State / Architecture
│   ├── CommunityToolkit.Mvvm    # MVVM puro, sem boilerplate
│   └── ReactiveUI (opcional)    # se quiser reatividade mais avançada
│
├── Navegação
│   └── Shell Navigation (nativo MAUI) # tabs, flyout, deep links
│
├── Local Storage
│   ├── SQLite-net-pcl           # banco local
│   ├── SecureStorage API        # tokens
│   └── Preferences API          # configs
│
├── Networking
│   ├── HttpClient nativo        # chamadas API
│   └── Refit                    # geração de código para REST APIs
│
├── Auth
│   ├── Microsoft.Identity.Client (MSAL) # suporte multi-provider
│   └── Plugin.GoogleClient      # Google Sign-In
│
├── Notificações
│   └── Plugin.Firebase.CloudMessaging  # FCM
│
├── Analytics
│   └── Firebase SDK para MAUI   # analytics + crashlytics
│
├── Monetização
│   └── Plugin.InAppBilling      # Google Play Billing + App Store
│   OU RevenueCat .NET SDK       # mais simples de gerenciar (recomendado)
│
└── Compartilhamento
    └── Share API (nativo MAUI)  # compartilha card gerado com SkiaSharp
```

### IDE & Ferramentas — MAUI
- **IDE:** Visual Studio 2022+ (Windows) ou VS Code com C# Dev Kit
- **Emulador:** Android Emulator integrado no VS
- **Testes:** xUnit + Moq
- **CI/CD:** GitHub Actions + `dotnet publish`

---

## BACKEND (Mesmo para Flutter e MAUI)

### Stack Servidor

```
ASP.NET Core 9 Web API (C#)
├── ORM
│   └── Entity Framework Core 9 + PostgreSQL
│
├── Cache
│   └── Redis                    # streaks, sessões, leaderboard
│
├── Autenticação
│   ├── ASP.NET Identity         # users, roles
│   └── JWT Bearer tokens        # stateless auth
│
├── IA / Treinos
│   └── OpenAI API (GPT-4o)      # geração de treinos personalizados
│   OU Azure OpenAI              # se quiser dentro do ecossistema MS
│
├── Notificações Push
│   └── Firebase Admin SDK       # envia FCM pelo servidor
│
├── Storage (imagens, avatares)
│   └── Cloudflare R2            # mais barato que S3 para MVP
│   OU Azure Blob Storage        # se stack Azure
│
└── Email (boas-vindas, receipts)
    └── Resend.com               # simples, developer-friendly
```

### Banco de Dados — Modelo simplificado

```
Tabelas principais:
- users           (id, email, name, created_at)
- profiles        (user_id, class, rank, level, xp, streak, weight, height)
- workouts        (id, user_id, exercises[], generated_at, completed_at)
- exercises       (id, name, muscle_group, difficulty, variants[])
- quest_logs      (id, user_id, date, exercises_done[], xp_earned)
- subscriptions   (user_id, plan, status, expires_at)
- attributes      (user_id, strength, agility, endurance, vitality, focus)
```

---

## INFRAESTRUTURA

### MVP (baixo custo, escala quando precisar)

```
┌─────────────────────────────────────────┐
│  App (Android)                          │
│  Flutter / MAUI                         │
└──────────────┬──────────────────────────┘
               │ HTTPS REST API
┌──────────────▼──────────────────────────┐
│  Backend — Railway.app                  │
│  ASP.NET Core 9 Web API                 │
│  ~$5/mês para MVP                       │
└──────────────┬──────────────────────────┘
               │
    ┌──────────┼──────────┐
    ▼          ▼          ▼
PostgreSQL    Redis    Cloudflare R2
(Railway)  (Railway)   (storage)
```

**Por que Railway no MVP:**
- Deploy com 1 comando (`railway up`)
- PostgreSQL + Redis inclusos
- Preço escalonável — começa em $5/mês
- Sem gerenciar infra. Foco no produto.

### Escala futura (quando monetizar)
- Railway → migrar para **Azure Container Apps** ou **Render**
- Adicionar CDN (Cloudflare) na frente da API
- Read replicas no PostgreSQL para queries pesadas de leaderboard

---

## MONETIZAÇÃO TÉCNICA — RevenueCat

**Por que RevenueCat e não implementar direto?**

Implementar Google Play Billing + App Store In-App Purchase do zero é complexo, propenso a bugs e o maior motivo de perda de receita em apps. RevenueCat abstrai tudo:

```
RevenueCat SDK
├── Gerencia Google Play Billing + App Store automaticamente
├── Dashboard unificado de receita (sem entrar em 2 consoles)
├── A/B test de preços
├── Gestão de entitlements (free vs premium) sem código
├── Webhooks para backend (saber quando assinou/cancelou)
└── SDK Flutter: purchases_flutter
    SDK MAUI: RevenueCat .NET SDK
```

**Planos sugeridos via RevenueCat:**

```json
{
  "free": {
    "entitlement": "free_hunter",
    "features": ["daily_quest", "xp_system", "profile_card"]
  },
  "premium_monthly": {
    "entitlement": "s_rank",
    "price_brl": 14.90,
    "features": ["all"]
  },
  "premium_annual": {
    "entitlement": "s_rank",
    "price_brl": 99.90,
    "features": ["all"]
  }
}
```

---

## ROADMAP TÉCNICO — Android → iOS

### Fase 1 — Android MVP (meses 1–3)
- [ ] Setup projeto Flutter/MAUI
- [ ] Autenticação (email + Google)
- [ ] Onboarding + perfil do usuário
- [ ] Geração de treino via IA
- [ ] Sistema de XP, rank, streak
- [ ] Quest diária + log de conclusão
- [ ] Card de perfil compartilhável
- [ ] RevenueCat + Google Play Billing
- [ ] Firebase Analytics + Crashlytics
- [ ] Deploy na Google Play (internal testing → open testing → production)

### Fase 2 — Estabilização Android (meses 3–5)
- [ ] Feedback dos primeiros usuários
- [ ] Fix bugs críticos
- [ ] A/B test de preço (RevenueCat)
- [ ] Otimizar conversão free → premium
- [ ] Adicionar nutrição básica
- [ ] Push notifications de streak

### Fase 3 — Expansão iOS (meses 5–7, após monetização estável)
- **Flutter:** 95% do código já funciona. Ajustes de UI para guidelines Apple + Apple Sign-In obrigatório.
- **MAUI:** Mesmo percentual. Precisa de Mac para compilar (Xcode obrigatório).
- Conta Apple Developer: $99/ano (obrigatório)
- RevenueCat já gerencia App Store Billing — zero retrabalho

### Fase 4 — Features V2 (mês 6+)
- [ ] Gráficos de progresso por exercício
- [ ] Master Quests semanais com recompensas de atributo
- [ ] Sistema de badges
- [ ] Customização de avatar
- [ ] Social (ranking entre amigos)

---

## DECISÃO FINAL — MATRIZ DE ESCOLHA

```
Você ou seu dev principal já domina C#/.NET?
    ├── SIM → MAUI
    │         Prós: reutilizar skill, backend integrado, Visual Studio
    │         Aceite: animações mais simples no MVP
    │
    └── NÃO → Flutter
              Prós: melhor UX visual, maior comunidade, libs de gamificação
              Aceite: aprender Dart (curva ~2 semanas para JS/TS devs)
```

**Recomendação final:**

Se o objetivo é lançar rápido com visual épico e o dev não tem C# já dominado: **Flutter + ASP.NET Core backend separado**. É a combinação que entrega a estética anime com performance de 120fps e ainda usa o melhor backend da indústria (.NET) para a API.

---

## RESUMO DE CUSTOS — MVP

| Serviço | Custo mensal |
|---|---|
| Railway (backend + DB + Redis) | ~$15 |
| Cloudflare R2 (storage) | ~$0 no início |
| Firebase (analytics, push) | Gratuito (Spark plan) |
| RevenueCat | Gratuito até $2.500/mês de receita |
| OpenAI API (geração de treinos) | ~$10–30 (depende do uso) |
| Google Play Console (one-time) | $25 (único) |
| Apple Developer (fase 2) | $99/ano |
| **Total MVP Android** | **~$50–60/mês** |

---

*Documento gerado em: Junho 2026*
*Versão: 1.0 — Stack Tecnológica*
*Referência: projeto-conceito.md*

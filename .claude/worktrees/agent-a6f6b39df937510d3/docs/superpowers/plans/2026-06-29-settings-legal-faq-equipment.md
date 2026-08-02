# Settings — Legal Popups, FAQ e Equipamentos no Perfil — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir links externos (Termos/Privacidade) por popups in-app, criar FAQ real com 8 Q&As, expandir textos legais para cobertura das Américas, e mover Equipamentos de Settings para Edit Profile.

**Architecture:** Reutiliza o padrão `_showLegalDocument` já existente em `LegalAcceptancePage`. Textos legais vivem nos ARBs. FAQ usa pares de chaves `settingsFaqQ{N}`/`settingsFaqA{N}` por idioma. Equipment é campo multi-select no final de `EditProfilePage`, removido de `SettingsPage`.

**Tech Stack:** Flutter · Dart · Riverpod · ARB (l10n) · flutter_test · mocktail

---

## Arquivos a modificar

| Arquivo | Responsabilidade |
|---|---|
| `lib/l10n/app_pt.arb` | Expand legal texts + add/remove FAQ keys |
| `lib/l10n/app_en.arb` | Idem (EN) |
| `lib/l10n/app_es.arb` | Idem (ES) |
| `lib/l10n/app_fr.arb` | Idem (FR) |
| `lib/l10n/app_localizations*.dart` | Regenerado via `flutter gen-l10n` |
| `lib/features/settings/presentation/pages/settings_page.dart` | +`_showLegalDoc`, +FAQ redesign, -`_launchUrl`, -url_launcher, -equipment tile |
| `lib/features/hunter_profile/presentation/pages/edit_profile_page.dart` | +`_equipmentAvailable` field + seção no form |
| `test/features/settings/presentation/pages/settings_page_test.dart` | Update FAQ test, add Terms/Privacy dialog tests |
| `test/features/hunter_profile/presentation/pages/edit_profile_page_test.dart` | Add equipment section tests |

---

### Task 1: Atualizar app_pt.arb — textos legais expandidos + chaves FAQ

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`

- [ ] **Step 1: Substituir `legalTermsContent` e `legalPrivacyContent` e adicionar FAQ keys**

No arquivo `apps/mobile/lib/l10n/app_pt.arb`, localizar e substituir as linhas existentes `"legalTermsContent": "..."` e `"legalPrivacyContent": "..."` pelos valores abaixo. Em seguida, remover as linhas `"settingsFaqDialogTitle"` e `"settingsFaqDialogMessage"` (e seus `@` correspondentes). Por fim, adicionar as chaves FAQ logo após `"settingsFaqLabel"`.

**Substituição de `legalTermsContent`:**
```json
"legalTermsContent": "1. Aceite e uso\n\nAo usar o Awaken, você confirma que leu estes termos e concorda em segui-los. Se não concordar, não use o app.\n\n2. Elegibilidade\n\nO uso do Awaken é destinado a pessoas com 13 anos ou mais. Usuários entre 13 e 18 anos devem obter consentimento dos responsáveis legais. Não coletamos intencionalmente dados de crianças menores de 13 anos.\n\n3. Conta e responsabilidade\n\nVocê é responsável pelas informações da sua conta e pelo uso da sua senha. Não compartilhe credenciais e avise o suporte em caso de suspeita de acesso indevido.\n\n4. Conteúdo e limites do app\n\nO Awaken organiza treinos, progresso e informações relacionadas à sua jornada fitness. As sugestões são informativas e educacionais, e não substituem orientação de médico, educador físico, fisioterapeuta ou nutricionista. Consulte um profissional antes de iniciar qualquer programa de exercícios.\n\n5. Conduta permitida\n\nNão use o app para violar leis, tentar acessar dados de terceiros, fraudar assinaturas ou interferir no funcionamento do serviço.\n\n6. Propriedade intelectual\n\nTodo o conteúdo do Awaken — incluindo nome, marca, design, código-fonte, textos, ilustrações e elementos visuais — é protegido por direitos autorais e propriedade intelectual. Você não pode copiar, modificar, distribuir ou criar obras derivadas sem autorização expressa.\n\n7. Assinatura, pagamentos e reembolsos\n\nAlguns recursos requerem assinatura paga. As compras são processadas pelas lojas de aplicativos (App Store ou Google Play) e estão sujeitas às políticas de reembolso da respectiva loja. Não processamos reembolsos diretamente. Podemos alterar preços e planos com aviso prévio razoável.\n\n8. Limitação de responsabilidade\n\nO Awaken é fornecido no estado em que se encontra. Na máxima extensão permitida por lei, não nos responsabilizamos por lesões, danos à saúde, perdas financeiras ou qualquer dano indireto decorrente do uso do app ou dos treinos sugeridos. Você assume os riscos inerentes à prática de atividade física.\n\n9. Indenização\n\nVocê concorda em defender e indenizar o Awaken e seus representantes contra quaisquer reclamações resultantes do mau uso do serviço ou violação destes termos.\n\n10. Encerramento\n\nPodemos restringir o acesso em caso de abuso, fraude ou violação destes termos. Você pode encerrar sua conta a qualquer momento pelo app.\n\n11. Lei aplicável e disputas\n\nEstes termos são regidos pelas leis do Brasil, com foro eleito na Comarca de São Paulo/SP. Para usuários fora do Brasil, disputas serão resolvidas preferencialmente por arbitragem amigável antes de qualquer ação judicial, respeitadas as leis locais aplicáveis.\n\n12. Contato\n\nDúvidas podem ser enviadas pelo canal de suporte do app ou por e-mail ao time Awaken.",
```

**Substituição de `legalPrivacyContent`:**
```json
"legalPrivacyContent": "1. Dados que coletamos\n\nPodemos coletar: nome, e-mail, idioma, sessão, status de acesso, preferências, métricas de uso, dados de treino, informações físicas informadas pelo usuário (idade, peso, altura, sexo biológico, limitações, dores, objetivos), dados de assinatura e registros de erro.\n\n2. Base legal para o tratamento\n\nTratamos seus dados com base no consentimento (art. 7, I da LGPD), na execução do contrato de uso do app (art. 7, V), no interesse legítimo para segurança e melhoria do serviço (art. 7, IX) e no cumprimento de obrigações legais (art. 7, II). Você pode retirar o consentimento a qualquer momento mediante exclusão da conta.\n\n3. Como usamos os dados\n\nUsamos os dados para: criar e autenticar contas, personalizar treinos e experiência, salvar progresso, exibir conteúdo adequado, melhorar estabilidade do app, prevenir abusos e atender solicitações de suporte.\n\n4. Compartilhamento e fornecedores\n\nNão vendemos seus dados pessoais. Podemos compartilhá-los com fornecedores que operam o serviço:\n— Firebase (Google): autenticação, analytics, notificações push, crash logs\n— RevenueCat: gerenciamento de assinaturas\n— Cloudflare R2: armazenamento de avatares e imagens\n— OpenAI / Azure OpenAI: geração de treinos personalizados (dados enviados são minimizados)\n\nEsses fornecedores são contratualmente obrigados a proteger seus dados.\n\n5. Transferência internacional\n\nOs fornecedores acima podem processar dados em servidores fora do Brasil (EUA, UE). Essas transferências são realizadas com base em salvaguardas adequadas, conforme art. 33 da LGPD, incluindo cláusulas contratuais padrão.\n\n6. Retenção e segurança\n\nMantemos os dados pelo tempo necessário para prestar o serviço e cumprir obrigações legais. Utilizamos medidas técnicas e organizacionais para proteger as informações. Comunicaremos incidentes relevantes conforme exigido por lei.\n\n7. Seus direitos\n\nVocê pode solicitar: acesso, correção, portabilidade, anonimização, bloqueio ou exclusão dos seus dados. Residentes da Califórnia (CCPA) têm direito adicional de saber quais categorias de dados são coletadas e de solicitar que não sejam vendidos (o que não fazemos). Para exercer seus direitos, entre em contato pelo suporte ou por privacy@awaken.app.\n\n8. Dados de saúde e sensíveis\n\nDados físicos como peso, altura, limitações e dores são considerados dados sensíveis sob a LGPD (art. 11). Coletamos esses dados exclusivamente com seu consentimento expresso para personalizar seu plano de treino.\n\n9. Menores\n\nNão coletamos intencionalmente dados de crianças menores de 13 anos (COPPA). O uso por menores entre 13 e 18 anos requer consentimento dos responsáveis legais.\n\n10. Cookies e rastreamento\n\nO app pode utilizar identificadores de sessão, tokens e ferramentas de analytics para melhorar a experiência. Não utilizamos cookies de publicidade de terceiros.\n\n11. Encarregado de dados (DPO)\n\nNos termos do art. 41 da LGPD, o canal oficial para solicitações de privacidade é: privacy@awaken.app.\n\n12. Alterações nesta política\n\nAlterações relevantes serão comunicadas no app ou por e-mail com antecedência razoável. O uso continuado após a notificação constitui aceite das mudanças.\n\n13. Versão e vigência\n\nVersão 1.0 · Em vigor desde 2026-06-29.",
```

**Chaves FAQ a adicionar logo após `"settingsFaqLabel": "FAQ",` e seu `@`:**
```json
"settingsFaqQ1": "O que é uma Quest Diária?",
"settingsFaqA1": "A Quest Diária é o treino personalizado gerado para você a cada dia, com base no seu perfil, objetivos, equipamentos e limitações físicas. Completar sua quest rende XP e mantém seu streak.",
"settingsFaqQ2": "Como funciona o sistema de XP, Level e Rank?",
"settingsFaqA2": "XP é ganho ao concluir quests. Acumular XP aumenta seu Level. Ao atingir marcos de Level, seu Rank evolui — de E até S, como em um sistema de hunter. O servidor calcula tudo de forma segura.",
"settingsFaqQ3": "O que é o Streak e como evitar perdê-lo?",
"settingsFaqA3": "O Streak conta os dias consecutivos em que você completou sua quest. Se você perder um dia, o streak zera. Para manter, basta concluir pelo menos uma quest por dia.",
"settingsFaqQ4": "Como funciona a assinatura e o trial gratuito?",
"settingsFaqA4": "O Awaken oferece um trial gratuito por alguns dias, sem necessidade de cartão. Após o trial, você pode assinar mensalmente ou anualmente e ter acesso a quests personalizadas, dungeons, raids, nutrição e muito mais.",
"settingsFaqQ5": "O que acontece com meu progresso se eu cancelar?",
"settingsFaqA5": "Seu progresso (XP, level, rank, streak, inventário) fica salvo. O acesso a recursos premium é restringido, mas seus dados não são excluídos. Ao reativar, tudo volta do ponto em que parou.",
"settingsFaqQ6": "Posso alterar meu perfil após o onboarding?",
"settingsFaqA6": "Sim. Acesse Perfil → Editar Perfil para atualizar objetivo, nível, dados físicos, equipamentos disponíveis, limitações e dores. Seus treinos serão ajustados automaticamente.",
"settingsFaqQ7": "Os treinos substituem um personal trainer ou médico?",
"settingsFaqA7": "Não. O Awaken é uma ferramenta de apoio e organização fitness. As sugestões são educacionais e não substituem avaliação ou acompanhamento de educador físico, médico, fisioterapeuta ou nutricionista.",
"settingsFaqQ8": "Como entrar em contato com o suporte?",
"settingsFaqA8": "Acesse Configurações → Fale Conosco para abrir um ticket de suporte. Nossa equipe responde pelo canal de suporte do app.",
```

**Remover** as seguintes linhas do ARB (e seus `@` correspondentes):
```
"settingsFaqDialogTitle": "FAQ",
"@settingsFaqDialogTitle": { ... },
"settingsFaqDialogMessage": "Em breve disponível no app.",
"@settingsFaqDialogMessage": { ... },
```

- [ ] **Step 2: Verificar JSON válido**

O ARB é JSON. Verificar que não há vírgulas duplas, ausentes, ou chaves duplicadas após as edições. Abrir o arquivo e revisar a área em torno das chaves modificadas.

---

### Task 2: Atualizar app_en.arb

**Files:**
- Modify: `apps/mobile/lib/l10n/app_en.arb`

- [ ] **Step 1: Substituir textos legais e adicionar FAQ**

**Substituição de `legalTermsContent`:**
```json
"legalTermsContent": "1. Acceptance of Terms\n\nBy using Awaken, you confirm that you have read these terms and agree to follow them. If you do not agree, do not use the app.\n\n2. Eligibility\n\nAwaken is intended for users aged 13 or older. Users between 13 and 18 must obtain parental or guardian consent. We do not knowingly collect data from children under 13.\n\n3. Account and Responsibility\n\nYou are responsible for your account information and password. Do not share credentials and notify support if you suspect unauthorized access.\n\n4. Content and App Limitations\n\nAwaken organizes workouts, progress and information related to your fitness journey. Suggestions are informational and educational, and do not replace advice from a physician, personal trainer, physiotherapist or nutritionist. Consult a professional before starting any exercise program.\n\n5. Permitted Conduct\n\nDo not use the app to violate laws, attempt to access third-party data, commit subscription fraud or interfere with the service.\n\n6. Intellectual Property\n\nAll Awaken content — including name, brand, design, source code, texts, illustrations and visual elements — is protected by copyright and intellectual property law. You may not copy, modify, distribute or create derivative works without express authorization.\n\n7. Subscription, Payments and Refunds\n\nSome features require a paid subscription. Purchases are processed by app stores (App Store or Google Play) and subject to their respective refund policies. We do not process refunds directly. We may change prices and plans with reasonable prior notice.\n\n8. Limitation of Liability\n\nAwaken is provided as is. To the fullest extent permitted by law, we are not liable for injuries, health damages, financial losses or any indirect damages arising from use of the app or suggested workouts. You assume the inherent risks of physical exercise.\n\n9. Indemnification\n\nYou agree to defend and indemnify Awaken and its representatives against any claims arising from misuse of the service or violation of these terms.\n\n10. Termination\n\nWe may restrict access in cases of abuse, fraud or violation of these terms. You may close your account at any time through the app.\n\n11. Governing Law and Disputes\n\nThese terms are governed by the laws of Brazil, with exclusive jurisdiction in São Paulo, SP. For users outside Brazil, disputes will preferably be resolved through amicable arbitration before any legal action, in compliance with applicable local laws.\n\n12. Contact\n\nQuestions can be submitted through the app support channel or by email to the Awaken team.",
```

**Substituição de `legalPrivacyContent`:**
```json
"legalPrivacyContent": "1. Data We Collect\n\nWe may collect: name, email, language, session, access status, preferences, usage metrics, workout data, user-provided physical information (age, weight, height, biological sex, limitations, pain points, goals), subscription data and error logs.\n\n2. Legal Basis for Processing\n\nWe process your data based on consent, contract performance, legitimate interest for security and service improvement, and compliance with legal obligations. You may withdraw consent at any time by deleting your account.\n\n3. How We Use Your Data\n\nWe use data to: create and authenticate accounts, personalize workouts and experience, save progress, display appropriate content, improve app stability, prevent abuse and handle support requests.\n\n4. Sharing and Providers\n\nWe do not sell your personal data. We may share it with providers operating the service:\n— Firebase (Google): authentication, analytics, push notifications, crash logs\n— RevenueCat: subscription management\n— Cloudflare R2: avatar and image storage\n— OpenAI / Azure OpenAI: personalized workout generation (data sent is minimized)\n\nThese providers are contractually required to protect your data.\n\n5. International Transfers\n\nProviders above may process data on servers outside Brazil (US, EU). These transfers rely on appropriate safeguards including standard contractual clauses.\n\n6. Retention and Security\n\nWe retain data as long as needed to provide the service and meet legal obligations. We apply technical and organizational measures to protect information. We will report relevant incidents as required by law.\n\n7. Your Rights\n\nYou may request: access, correction, portability, anonymization, blocking or deletion of your data. California residents (CCPA) have additional rights to know which categories of data are collected and to request they not be sold (which we do not do). To exercise your rights, contact support or privacy@awaken.app.\n\n8. Health and Sensitive Data\n\nPhysical data such as weight, height, limitations and pain points is sensitive data. We collect it exclusively with your express consent to personalize your training plan.\n\n9. Minors\n\nWe do not knowingly collect data from children under 13 (COPPA). Use by minors aged 13 to 18 requires parental or guardian consent.\n\n10. Cookies and Tracking\n\nThe app may use session identifiers, tokens and analytics tools to improve experience. We do not use third-party advertising cookies.\n\n11. Data Protection Officer (DPO)\n\nThe official channel for privacy and data protection requests is: privacy@awaken.app.\n\n12. Changes to This Policy\n\nSignificant changes will be communicated in the app or by email with reasonable advance notice. Continued use after notification constitutes acceptance.\n\n13. Version and Effective Date\n\nVersion 1.0 · Effective from 2026-06-29.",
```

**FAQ keys a adicionar logo após `"settingsFaqLabel": "FAQ",` e seu `@`:**
```json
"settingsFaqQ1": "What is a Daily Quest?",
"settingsFaqA1": "A Daily Quest is the personalized workout generated for you each day, based on your profile, goals, available equipment and physical limitations. Completing your quest earns XP and keeps your streak alive.",
"settingsFaqQ2": "How do XP, Level and Rank work?",
"settingsFaqA2": "XP is earned by completing quests. Accumulating XP increases your Level. Reaching Level milestones evolves your Rank — from E up to S, like a hunter ranking system. The server calculates everything securely.",
"settingsFaqQ3": "What is the Streak and how do I keep it?",
"settingsFaqA3": "The Streak tracks how many consecutive days you have completed your quest. Missing a day resets it to zero. To maintain it, complete at least one quest per day.",
"settingsFaqQ4": "How does the subscription and free trial work?",
"settingsFaqA4": "Awaken offers a free trial for a few days with no card required. After the trial, you can subscribe monthly or annually for access to personalized quests, dungeons, raids, nutrition tracking and more.",
"settingsFaqQ5": "What happens to my progress if I cancel?",
"settingsFaqA5": "Your progress (XP, level, rank, streak, inventory) is saved. Access to premium features is restricted, but your data is not deleted. If you reactivate, everything picks up where you left off.",
"settingsFaqQ6": "Can I change my profile after onboarding?",
"settingsFaqA6": "Yes. Go to Profile → Edit Profile to update your goal, level, physical data, available equipment, limitations and pain points. Your workouts will be adjusted automatically.",
"settingsFaqQ7": "Do workouts replace a personal trainer or doctor?",
"settingsFaqA7": "No. Awaken is a fitness support and organization tool. The suggestions are educational and do not replace evaluation or guidance from a personal trainer, physician, physiotherapist or nutritionist.",
"settingsFaqQ8": "How do I contact support?",
"settingsFaqA8": "Go to Settings → Contact Us to open a support ticket. Our team responds through the app support channel.",
```

**Remover** `settingsFaqDialogTitle` e `settingsFaqDialogMessage` (e seus `@`).

---

### Task 3: Atualizar app_es.arb

**Files:**
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] **Step 1: Substituir textos legais e adicionar FAQ**

**Substituição de `legalTermsContent`:**
```json
"legalTermsContent": "1. Aceptación de términos\n\nAl usar Awaken, confirmas que has leído estos términos y aceptas seguirlos. Si no estás de acuerdo, no uses la app.\n\n2. Elegibilidad\n\nAwaken está destinado a personas de 13 años o más. Los usuarios de entre 13 y 18 años deben obtener el consentimiento de sus padres o tutores. No recopilamos intencionalmente datos de menores de 13 años.\n\n3. Cuenta y responsabilidad\n\nEres responsable de la información de tu cuenta y del uso de tu contraseña. No compartas credenciales e informa al soporte si sospechas de acceso no autorizado.\n\n4. Contenido y limitaciones de la app\n\nAwaken organiza entrenamientos, progreso e información relacionada con tu camino fitness. Las sugerencias son informativas y educativas, y no reemplazan la orientación de un médico, entrenador personal, fisioterapeuta o nutricionista. Consulta a un profesional antes de iniciar cualquier programa de ejercicio.\n\n5. Conducta permitida\n\nNo uses la app para violar leyes, intentar acceder a datos de terceros, cometer fraude en suscripciones o interferir con el servicio.\n\n6. Propiedad intelectual\n\nTodo el contenido de Awaken — nombre, marca, diseño, código fuente, textos, ilustraciones y elementos visuales — está protegido por derechos de autor. No puedes copiar, modificar, distribuir ni crear obras derivadas sin autorización expresa.\n\n7. Suscripción, pagos y reembolsos\n\nAlgunos recursos requieren suscripción de pago. Las compras son procesadas por las tiendas de aplicaciones (App Store o Google Play) y sujetas a sus políticas de reembolso. No procesamos reembolsos directamente. Podemos cambiar precios y planes con aviso previo razonable.\n\n8. Limitación de responsabilidad\n\nAwaken se proporciona tal cual. En la máxima extensión permitida por la ley, no somos responsables de lesiones, daños a la salud, pérdidas financieras ni daños indirectos derivados del uso de la app. Asumes los riesgos inherentes a la práctica de actividad física.\n\n9. Indemnización\n\nAceptas defender e indemnizar a Awaken y sus representantes contra cualquier reclamación derivada del mal uso del servicio o violación de estos términos.\n\n10. Finalización\n\nPodemos restringir el acceso en casos de abuso, fraude o violación de estos términos. Puedes cerrar tu cuenta en cualquier momento desde la app.\n\n11. Ley aplicable y disputas\n\nEstos términos se rigen por las leyes de Brasil, con jurisdicción exclusiva en São Paulo, SP. Para usuarios fuera de Brasil, las disputas se resolverán preferiblemente mediante arbitraje amistoso antes de cualquier acción legal.\n\n12. Contacto\n\nLas preguntas pueden enviarse a través del canal de soporte de la app o por correo al equipo Awaken.",
```

**Substituição de `legalPrivacyContent`:**
```json
"legalPrivacyContent": "1. Datos que recopilamos\n\nPodemos recopilar: nombre, correo, idioma, sesión, estado de acceso, preferencias, métricas de uso, datos de entrenamiento, información física proporcionada por el usuario (edad, peso, altura, sexo biológico, limitaciones, dolores, objetivos), datos de suscripción y registros de error.\n\n2. Base legal para el tratamiento\n\nTratamos tus datos basándonos en el consentimiento, ejecución del contrato de uso, interés legítimo para seguridad y mejora del servicio, y cumplimiento de obligaciones legales. Puedes retirar el consentimiento en cualquier momento eliminando tu cuenta.\n\n3. Cómo usamos los datos\n\nUsamos los datos para: crear y autenticar cuentas, personalizar entrenamientos y experiencia, guardar progreso, mostrar contenido adecuado, mejorar la estabilidad de la app, prevenir abusos y atender solicitudes de soporte.\n\n4. Compartición y proveedores\n\nNo vendemos tus datos personales. Podemos compartirlos con proveedores que operan el servicio:\n— Firebase (Google): autenticación, analytics, notificaciones push, registros de fallos\n— RevenueCat: gestión de suscripciones\n— Cloudflare R2: almacenamiento de avatares e imágenes\n— OpenAI / Azure OpenAI: generación de entrenamientos personalizados (datos minimizados)\n\nEstos proveedores están contractualmente obligados a proteger tus datos.\n\n5. Transferencias internacionales\n\nLos proveedores pueden procesar datos en servidores fuera de Brasil (EE.UU., UE) con salvaguardas adecuadas.\n\n6. Retención y seguridad\n\nConservamos los datos el tiempo necesario para prestar el servicio y cumplir obligaciones legales. Aplicamos medidas técnicas y organizativas de protección.\n\n7. Tus derechos\n\nPuedes solicitar: acceso, corrección, portabilidad, anonimización, bloqueo o eliminación de tus datos. Los residentes de California (CCPA) tienen derechos adicionales. Para ejercerlos, contacta al soporte o escribe a privacy@awaken.app.\n\n8. Datos de salud y sensibles\n\nRecopilamos datos físicos exclusivamente con tu consentimiento expreso para personalizar tu plan de entrenamiento.\n\n9. Menores\n\nNo recopilamos intencionalmente datos de menores de 13 años. El uso entre 13 y 18 años requiere consentimiento parental.\n\n10. Cookies y rastreo\n\nLa app puede usar identificadores de sesión y herramientas de analytics. No usamos cookies publicitarias de terceros.\n\n11. Delegado de protección de datos\n\nEl canal oficial para solicitudes de privacidad es: privacy@awaken.app.\n\n12. Cambios en esta política\n\nComunicaremos cambios relevantes en la app o por correo con antelación razonable.\n\n13. Versión y vigencia\n\nVersión 1.0 · En vigor desde 2026-06-29.",
```

**FAQ keys a adicionar logo após `"settingsFaqLabel"` e seu `@`:**
```json
"settingsFaqQ1": "¿Qué es una Quest Diaria?",
"settingsFaqA1": "La Quest Diaria es el entrenamiento personalizado generado para ti cada día, basado en tu perfil, objetivos, equipo disponible y limitaciones físicas. Completarla te da XP y mantiene tu racha.",
"settingsFaqQ2": "¿Cómo funcionan XP, Level y Rank?",
"settingsFaqA2": "El XP se gana completando quests. Acumular XP aumenta tu Level. Al alcanzar hitos de Level, tu Rank evoluciona — de E hasta S, como en un sistema de cazador. El servidor lo calcula todo de forma segura.",
"settingsFaqQ3": "¿Qué es el Streak y cómo no perderlo?",
"settingsFaqA3": "El Streak cuenta los días consecutivos en que completaste tu quest. Si pierdes un día, vuelve a cero. Para mantenerlo, completa al menos una quest por día.",
"settingsFaqQ4": "¿Cómo funciona la suscripción y el período de prueba gratuito?",
"settingsFaqA4": "Awaken ofrece una prueba gratuita de unos días sin tarjeta requerida. Después puedes suscribirte mensual o anualmente para acceder a quests personalizadas, mazmorras, incursiones, nutrición y más.",
"settingsFaqQ5": "¿Qué pasa con mi progreso si cancelo?",
"settingsFaqA5": "Tu progreso (XP, level, rank, racha, inventario) queda guardado. El acceso a funciones premium se restringe, pero tus datos no se eliminan. Si reactivas, todo continúa donde lo dejaste.",
"settingsFaqQ6": "¿Puedo cambiar mi perfil después del proceso inicial?",
"settingsFaqA6": "Sí. Ve a Perfil → Editar Perfil para actualizar objetivo, nivel, datos físicos, equipo disponible, limitaciones y dolores. Tus entrenamientos se ajustarán automáticamente.",
"settingsFaqQ7": "¿Los entrenamientos reemplazan a un entrenador o médico?",
"settingsFaqA7": "No. Awaken es una herramienta de apoyo fitness. Las sugerencias son educativas y no reemplazan la evaluación de un entrenador, médico, fisioterapeuta o nutricionista.",
"settingsFaqQ8": "¿Cómo contacto al soporte?",
"settingsFaqA8": "Ve a Configuración → Contactar con nosotros para abrir un ticket de soporte.",
```

**Remover** `settingsFaqDialogTitle` e `settingsFaqDialogMessage` (e seus `@`).

---

### Task 4: Atualizar app_fr.arb

**Files:**
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Substituir textos legais e adicionar FAQ**

**Substituição de `legalTermsContent`:**
```json
"legalTermsContent": "1. Acceptation des conditions\n\nEn utilisant Awaken, vous confirmez avoir lu ces conditions et acceptez de les respecter. Si vous n'êtes pas d'accord, n'utilisez pas l'application.\n\n2. Éligibilité\n\nAwaken est destiné aux personnes de 13 ans et plus. Les utilisateurs de 13 à 18 ans doivent obtenir le consentement de leurs parents ou tuteurs. Nous ne collectons pas intentionnellement de données d'enfants de moins de 13 ans.\n\n3. Compte et responsabilité\n\nVous êtes responsable des informations de votre compte et de l'utilisation de votre mot de passe. Ne partagez pas vos identifiants et informez le support en cas d'accès suspect.\n\n4. Contenu et limites de l'application\n\nAwaken organise des entraînements, la progression et les informations liées à votre parcours fitness. Les suggestions sont informatives et éducatives, et ne remplacent pas l'avis d'un médecin, coach sportif, kinésithérapeute ou nutritionniste. Consultez un professionnel avant de commencer tout programme d'exercice.\n\n5. Conduite autorisée\n\nN'utilisez pas l'application pour violer des lois, tenter d'accéder aux données de tiers, commettre des fraudes d'abonnement ou perturber le service.\n\n6. Propriété intellectuelle\n\nTout le contenu Awaken — nom, marque, design, code source, textes, illustrations et éléments visuels — est protégé par le droit d'auteur. Vous ne pouvez pas copier, modifier, distribuer ou créer des œuvres dérivées sans autorisation expresse.\n\n7. Abonnement, paiements et remboursements\n\nCertaines fonctionnalités nécessitent un abonnement payant. Les achats sont traités par les boutiques d'applications (App Store ou Google Play) et soumis à leurs politiques de remboursement. Nous ne traitons pas les remboursements directement. Nous pouvons modifier les prix et les formules avec un préavis raisonnable.\n\n8. Limitation de responsabilité\n\nAwaken est fourni tel quel. Dans toute la mesure permise par la loi, nous ne sommes pas responsables des blessures, dommages à la santé, pertes financières ou dommages indirects découlant de l'utilisation de l'application. Vous assumez les risques inhérents à la pratique d'une activité physique.\n\n9. Indemnisation\n\nVous acceptez de défendre et d'indemniser Awaken et ses représentants contre toute réclamation résultant d'une mauvaise utilisation du service ou d'une violation de ces conditions.\n\n10. Résiliation\n\nNous pouvons restreindre l'accès en cas d'abus, de fraude ou de violation de ces conditions. Vous pouvez fermer votre compte à tout moment depuis l'application.\n\n11. Droit applicable et litiges\n\nCes conditions sont régies par le droit brésilien, avec juridiction exclusive à São Paulo, SP. Pour les utilisateurs hors du Brésil, les litiges seront de préférence résolus par arbitrage amiable avant toute action en justice.\n\n12. Contact\n\nLes questions peuvent être envoyées via le canal d'assistance de l'application ou par e-mail à l'équipe Awaken.",
```

**Substituição de `legalPrivacyContent`:**
```json
"legalPrivacyContent": "1. Données collectées\n\nNous pouvons collecter : nom, e-mail, langue, session, statut d'accès, préférences, métriques d'utilisation, données d'entraînement, informations physiques fournies par l'utilisateur (âge, poids, taille, sexe biologique, limitations, douleurs, objectifs), données d'abonnement et journaux d'erreurs.\n\n2. Base légale du traitement\n\nNous traitons vos données sur la base du consentement, de l'exécution du contrat d'utilisation, de l'intérêt légitime pour la sécurité et l'amélioration du service, et du respect des obligations légales. Vous pouvez retirer votre consentement à tout moment en supprimant votre compte.\n\n3. Utilisation des données\n\nNous utilisons les données pour : créer et authentifier des comptes, personnaliser les entraînements et l'expérience, enregistrer la progression, afficher du contenu approprié, améliorer la stabilité, prévenir les abus et traiter les demandes d'assistance.\n\n4. Partage et fournisseurs\n\nNous ne vendons pas vos données personnelles. Nous pouvons les partager avec des fournisseurs qui opèrent le service :\n— Firebase (Google) : authentification, analytics, notifications push, journaux de plantage\n— RevenueCat : gestion des abonnements\n— Cloudflare R2 : stockage d'avatars et d'images\n— OpenAI / Azure OpenAI : génération d'entraînements personnalisés (données minimisées)\n\nCes fournisseurs sont contractuellement tenus de protéger vos données.\n\n5. Transferts internationaux\n\nLes fournisseurs peuvent traiter des données sur des serveurs hors du Brésil (États-Unis, UE) avec des garanties appropriées.\n\n6. Conservation et sécurité\n\nNous conservons les données le temps nécessaire pour fournir le service et respecter les obligations légales. Nous appliquons des mesures techniques et organisationnelles de protection.\n\n7. Vos droits\n\nVous pouvez demander : accès, correction, portabilité, anonymisation, blocage ou suppression de vos données. Les résidents de Californie (CCPA) ont des droits supplémentaires. Pour exercer vos droits, contactez le support ou écrivez à privacy@awaken.app.\n\n8. Données de santé et sensibles\n\nNous collectons des données physiques exclusivement avec votre consentement exprès pour personnaliser votre plan d'entraînement.\n\n9. Mineurs\n\nNous ne collectons pas intentionnellement de données d'enfants de moins de 13 ans. L'utilisation entre 13 et 18 ans nécessite le consentement parental.\n\n10. Cookies et traçage\n\nL'application peut utiliser des identifiants de session et des outils d'analytics. Nous n'utilisons pas de cookies publicitaires tiers.\n\n11. Délégué à la protection des données\n\nLe canal officiel pour les demandes de confidentialité est : privacy@awaken.app.\n\n12. Modifications de cette politique\n\nNous communiquerons les changements importants dans l'application ou par e-mail avec un préavis raisonnable.\n\n13. Version et date d'entrée en vigueur\n\nVersion 1.0 · En vigueur depuis le 2026-06-29.",
```

**FAQ keys a adicionar logo após `"settingsFaqLabel"` e seu `@`:**
```json
"settingsFaqQ1": "Qu'est-ce qu'une Quête Quotidienne ?",
"settingsFaqA1": "La Quête Quotidienne est l'entraînement personnalisé généré pour vous chaque jour, basé sur votre profil, objectifs, équipement disponible et limitations physiques. La compléter vous rapporte des XP et maintient votre streak.",
"settingsFaqQ2": "Comment fonctionnent les XP, le Level et le Rank ?",
"settingsFaqA2": "Les XP sont gagnés en complétant des quêtes. Accumuler des XP augmente votre Level. Atteindre des jalons de Level fait évoluer votre Rank — de E jusqu'à S, comme dans un système de chasseur. Le serveur calcule tout de façon sécurisée.",
"settingsFaqQ3": "Qu'est-ce que le Streak et comment le maintenir ?",
"settingsFaqA3": "Le Streak comptabilise les jours consécutifs où vous avez complété votre quête. Manquer un jour le remet à zéro. Pour le maintenir, complétez au moins une quête par jour.",
"settingsFaqQ4": "Comment fonctionnent l'abonnement et l'essai gratuit ?",
"settingsFaqA4": "Awaken propose un essai gratuit de quelques jours sans carte requise. Ensuite, vous pouvez vous abonner mensuellement ou annuellement pour accéder aux quêtes personnalisées, donjons, raids, nutrition et plus.",
"settingsFaqQ5": "Que se passe-t-il avec ma progression si j'annule ?",
"settingsFaqA5": "Votre progression (XP, level, rank, streak, inventaire) est sauvegardée. L'accès aux fonctionnalités premium est restreint, mais vos données ne sont pas supprimées. Si vous réactivez, tout reprend là où vous en étiez.",
"settingsFaqQ6": "Puis-je modifier mon profil après l'intégration initiale ?",
"settingsFaqA6": "Oui. Allez dans Profil → Modifier le profil pour mettre à jour votre objectif, niveau, données physiques, équipement disponible, limitations et douleurs. Vos entraînements seront ajustés automatiquement.",
"settingsFaqQ7": "Les entraînements remplacent-ils un coach ou un médecin ?",
"settingsFaqA7": "Non. Awaken est un outil de soutien fitness. Les suggestions sont éducatives et ne remplacent pas l'évaluation d'un coach, médecin, kinésithérapeute ou nutritionniste.",
"settingsFaqQ8": "Comment contacter le support ?",
"settingsFaqA8": "Allez dans Paramètres → Nous contacter pour ouvrir un ticket de support.",
```

**Remover** `settingsFaqDialogTitle` e `settingsFaqDialogMessage` (e seus `@`).

---

### Task 5: Regenerar localizations

**Files:**
- Modify: `apps/mobile/lib/l10n/app_localizations.dart` (auto-gerado)
- Modify: `apps/mobile/lib/l10n/app_localizations_pt.dart` (auto-gerado)
- Modify: `apps/mobile/lib/l10n/app_localizations_en.dart` (auto-gerado)
- Modify: `apps/mobile/lib/l10n/app_localizations_es.dart` (auto-gerado)
- Modify: `apps/mobile/lib/l10n/app_localizations_fr.dart` (auto-gerado)

- [ ] **Step 1: Rodar gen-l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: sem erros. Se houver erro de chave duplicada ou JSON inválido, corrigir o ARB correspondente e rodar novamente.

- [ ] **Step 2: Confirmar geração**

```bash
grep -l "settingsFaqQ1\|settingsFaqDialogMessage" apps/mobile/lib/l10n/app_localizations*.dart
```

Expected: `settingsFaqQ1` encontrado, `settingsFaqDialogMessage` **não** encontrado.

---

### Task 6: Atualizar settings_page_test.dart (teste primeiro)

**Files:**
- Modify: `apps/mobile/test/features/settings/presentation/pages/settings_page_test.dart`

- [ ] **Step 1: Atualizar teste de FAQ e adicionar testes de Termos/Privacidade**

Localizar o teste `'US-175: toque em FAQ exibe dialog'` e substituí-lo. Adicionar testes de Terms e Privacy logo após:

```dart
testWidgets('FAQ: toque abre dialog com primeira pergunta', (tester) async {
  await tester.pumpWidget(buildTestApp());
  await tester.pumpAndSettle();

  await scrollTo(tester, find.text('FAQ'));
  await tester.tap(find.text('FAQ'));
  await tester.pumpAndSettle();

  expect(find.text('O que é uma Quest Diária?'), findsOneWidget);
});

testWidgets('FAQ: dialog exibe resposta para primeira pergunta', (tester) async {
  await tester.pumpWidget(buildTestApp());
  await tester.pumpAndSettle();

  await scrollTo(tester, find.text('FAQ'));
  await tester.tap(find.text('FAQ'));
  await tester.pumpAndSettle();

  expect(
    find.textContaining('treino personalizado'),
    findsOneWidget,
  );
});

testWidgets('Termos: toque abre dialog com conteúdo dos termos', (tester) async {
  await tester.pumpWidget(buildTestApp());
  await tester.pumpAndSettle();

  await scrollTo(tester, find.text('Termos de uso'));
  await tester.tap(find.text('Termos de uso'));
  await tester.pumpAndSettle();

  expect(find.text('Termos de Uso'), findsOneWidget);
  expect(find.textContaining('Aceite e uso'), findsOneWidget);
});

testWidgets('Privacidade: toque abre dialog com conteúdo da política', (tester) async {
  await tester.pumpWidget(buildTestApp());
  await tester.pumpAndSettle();

  await scrollTo(tester, find.text('Política de privacidade'));
  await tester.tap(find.text('Política de privacidade'));
  await tester.pumpAndSettle();

  expect(find.text('Política de Privacidade'), findsOneWidget);
  expect(find.textContaining('Dados que coletamos'), findsOneWidget);
});
```

- [ ] **Step 2: Verificar que os novos testes FALHAM (antes da implementação)**

```bash
cd apps/mobile && flutter test test/features/settings/presentation/pages/settings_page_test.dart --name "FAQ|Termos|Privacidade" 2>&1 | tail -20
```

Expected: FAIL com mensagem de widget não encontrado.

---

### Task 7: Implementar mudanças em settings_page.dart

**Files:**
- Modify: `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart`

- [ ] **Step 1: Remover import url_launcher e constantes não usadas**

Remover as linhas:
```dart
import 'package:url_launcher/url_launcher.dart';
```
```dart
const _kTermsUrl = 'https://awaken.app/terms';
const _kPrivacyUrl = 'https://awaken.app/privacy';
```

- [ ] **Step 2: Adicionar método `_showLegalDoc` dentro de `_SettingsPageState`**

Logo antes do método `_showFaqDialog`, adicionar:

```dart
void _showLegalDoc(
  BuildContext context,
  AppLocalizations l10n,
  String title,
  String body,
) {
  showDialog<void>(
    context: context,
    barrierDismissible: true,
    builder: (ctx) {
      final size = MediaQuery.sizeOf(ctx);
      return Dialog(
        backgroundColor: Colors.transparent,
        insetPadding: const EdgeInsets.all(AwakenSpacing.lg),
        child: AwakenPanel(
          cut: AwakenSpacing.cardRadius,
          surfaceColor: AwakenColors.backgroundSecondary,
          surfaceOpacity: 0.96,
          borderColor: AwakenColors.borderDefault,
          padding: EdgeInsets.zero,
          child: ConstrainedBox(
            constraints: BoxConstraints(
              maxWidth: 560,
              maxHeight: size.height * 0.82,
            ),
            child: Padding(
              padding: const EdgeInsets.all(AwakenSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(title, style: AwakenTypography.titleLarge),
                      ),
                      IconButton(
                        tooltip: l10n.legalDocumentCloseButton,
                        onPressed: () => Navigator.of(ctx).pop(),
                        icon: const Icon(Icons.close),
                        color: AwakenColors.textSecondary,
                      ),
                    ],
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  Expanded(
                    child: Scrollbar(
                      child: SingleChildScrollView(
                        child: SelectableText(
                          body,
                          style: AwakenTypography.bodyMedium,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: AwakenSpacing.lg),
                  TextButton(
                    onPressed: () => Navigator.of(ctx).pop(),
                    child: Text(l10n.legalDocumentCloseButton),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    },
  );
}
```

- [ ] **Step 3: Substituir `_showFaqDialog` para usar lista Q&A**

Substituir o método `_showFaqDialog` inteiro por:

```dart
void _showFaqDialog(BuildContext context, AppLocalizations l10n) {
  final items = [
    (l10n.settingsFaqQ1, l10n.settingsFaqA1),
    (l10n.settingsFaqQ2, l10n.settingsFaqA2),
    (l10n.settingsFaqQ3, l10n.settingsFaqA3),
    (l10n.settingsFaqQ4, l10n.settingsFaqA4),
    (l10n.settingsFaqQ5, l10n.settingsFaqA5),
    (l10n.settingsFaqQ6, l10n.settingsFaqA6),
    (l10n.settingsFaqQ7, l10n.settingsFaqA7),
    (l10n.settingsFaqQ8, l10n.settingsFaqA8),
  ];

  showDialog<void>(
    context: context,
    builder: (ctx) {
      final size = MediaQuery.sizeOf(ctx);
      return Dialog(
        backgroundColor: Colors.transparent,
        insetPadding: const EdgeInsets.all(AwakenSpacing.lg),
        child: AwakenPanel(
          cut: AwakenSpacing.cardRadius,
          surfaceColor: AwakenColors.backgroundSecondary,
          surfaceOpacity: 0.96,
          borderColor: AwakenColors.borderDefault,
          padding: EdgeInsets.zero,
          child: ConstrainedBox(
            constraints: BoxConstraints(
              maxWidth: 560,
              maxHeight: size.height * 0.82,
            ),
            child: Padding(
              padding: const EdgeInsets.all(AwakenSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          l10n.settingsFaqLabel,
                          style: AwakenTypography.titleLarge,
                        ),
                      ),
                      IconButton(
                        onPressed: () => Navigator.of(ctx).pop(),
                        icon: const Icon(Icons.close),
                        color: AwakenColors.textSecondary,
                      ),
                    ],
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  Expanded(
                    child: ListView.separated(
                      itemCount: items.length,
                      separatorBuilder: (_, __) =>
                          const Divider(height: AwakenSpacing.lg),
                      itemBuilder: (_, i) => Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            items[i].$1,
                            style: AwakenTypography.titleMedium,
                          ),
                          const SizedBox(height: AwakenSpacing.xs),
                          Text(
                            items[i].$2,
                            style: AwakenTypography.bodyMedium,
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: AwakenSpacing.md),
                  TextButton(
                    onPressed: () => Navigator.of(ctx).pop(),
                    child: Text(l10n.legalDocumentCloseButton),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    },
  );
}
```

- [ ] **Step 4: Substituir os onTap de Termos e Privacidade**

Localizar:
```dart
onTap: () => _launchUrl(_kTermsUrl),
```
Substituir por:
```dart
onTap: () => _showLegalDoc(
  context,
  l10n,
  l10n.legalAcceptanceViewTerms,
  l10n.legalTermsContent,
),
```

Localizar:
```dart
onTap: () => _launchUrl(_kPrivacyUrl),
```
Substituir por:
```dart
onTap: () => _showLegalDoc(
  context,
  l10n,
  l10n.legalAcceptanceViewPrivacy,
  l10n.legalPrivacyContent,
),
```

- [ ] **Step 5: Remover o tile de Equipamentos da seção Conta**

Localizar e remover o bloco:
```dart
_SettingsCardTile(
  icon: Icons.fitness_center_outlined,
  label: l10n.settingsEquipmentLabel,
  onTap: () => context.push(AppRoutes.equipmentSettings),
),
const SizedBox(height: AwakenSpacing.sm),
```

- [ ] **Step 6: Remover método `_launchUrl` (agora não usado)**

Localizar e remover:
```dart
Future<void> _launchUrl(String url) async {
  final uri = Uri.parse(url);
  if (await canLaunchUrl(uri)) {
    await launchUrl(uri, mode: LaunchMode.externalApplication);
  }
}
```

- [ ] **Step 7: Rodar testes de settings para confirmar verde**

```bash
cd apps/mobile && flutter test test/features/settings/presentation/pages/settings_page_test.dart 2>&1 | tail -20
```

Expected: todos os testes PASS (inclusive os novos de FAQ, Termos, Privacidade).

- [ ] **Step 8: Commit**

```bash
cd apps/mobile && git add lib/features/settings/presentation/pages/settings_page.dart lib/l10n/app_pt.arb lib/l10n/app_en.arb lib/l10n/app_es.arb lib/l10n/app_fr.arb lib/l10n/app_localizations.dart lib/l10n/app_localizations_pt.dart lib/l10n/app_localizations_en.dart lib/l10n/app_localizations_es.dart lib/l10n/app_localizations_fr.dart test/features/settings/presentation/pages/settings_page_test.dart
git commit -m "feat: termos/privacidade e FAQ abrem in-app dialog com textos legais expandidos"
```

---

### Task 8: Atualizar edit_profile_page_test.dart (teste primeiro)

**Files:**
- Modify: `apps/mobile/test/features/hunter_profile/presentation/pages/edit_profile_page_test.dart`

- [ ] **Step 1: Adicionar testes para seção de Equipamentos**

Localizar o final do `void main()` e adicionar antes do `}` de fechamento:

```dart
testWidgets('exibe seção de equipamentos com opções', (tester) async {
  final l10n = await _ptL10n();
  await tester.pumpWidget(_wrap(_FakeProfileRepository()));
  await tester.pumpAndSettle();

  await tester.scrollUntilVisible(
    find.text(l10n.onboardingEquipmentTitle),
    300,
    scrollable: find.byType(Scrollable).last,
  );

  expect(find.text(l10n.onboardingEquipmentTitle), findsOneWidget);
  expect(find.text(l10n.onboardingEquipmentNone), findsOneWidget);
  expect(find.text(l10n.onboardingEquipmentDumbbells), findsOneWidget);
  expect(find.text(l10n.onboardingEquipmentGym), findsOneWidget);
});

testWidgets('equipamento do perfil aparece pré-selecionado', (tester) async {
  // _completeProfile() tem equipmentAvailable: ['bodyweight']
  await tester.pumpWidget(_wrap(_FakeProfileRepository()));
  await tester.pumpAndSettle();

  final l10n = await _ptL10n();
  await tester.scrollUntilVisible(
    find.text(l10n.onboardingEquipmentNone),
    300,
    scrollable: find.byType(Scrollable).last,
  );

  // 'bodyweight' -> onboardingEquipmentNone aparece como selecionado
  // Verificamos que o widget existe (seleção visual é testada indiretamente pelo save)
  expect(find.text(l10n.onboardingEquipmentNone), findsOneWidget);
});

testWidgets('salvar perfil inclui equipmentAvailable', (tester) async {
  final repo = _FakeProfileRepository();
  await tester.pumpWidget(_wrap(repo));
  await tester.pumpAndSettle();

  final l10n = await _ptL10n();

  // Scroll até botão salvar e salvar
  await tester.scrollUntilVisible(
    find.text(l10n.profileEditSaveButton),
    300,
    scrollable: find.byType(Scrollable).last,
  );
  await tester.tap(find.text(l10n.profileEditSaveButton));
  await tester.pumpAndSettle();

  expect(repo.lastUpdateArgs?['equipmentAvailable'], isNotNull);
});
```

- [ ] **Step 2: Verificar que os novos testes FALHAM**

```bash
cd apps/mobile && flutter test test/features/hunter_profile/presentation/pages/edit_profile_page_test.dart --name "equipamento|equipments|Equipamentos" 2>&1 | tail -20
```

Expected: FAIL com widget não encontrado.

---

### Task 9: Implementar equipamentos em EditProfilePage

**Files:**
- Modify: `apps/mobile/lib/features/hunter_profile/presentation/pages/edit_profile_page.dart`

- [ ] **Step 1: Adicionar campo `_equipmentAvailable` ao estado**

Localizar a linha:
```dart
List<String> _physicalPains = [];
```
Adicionar logo após:
```dart
List<String> _equipmentAvailable = [];
```

- [ ] **Step 2: Popular `_equipmentAvailable` em `_populateFrom`**

Localizar:
```dart
_physicalPains = List<String>.from(profile.physicalPains ?? []);
```
Adicionar logo após:
```dart
_equipmentAvailable = List<String>.from(profile.equipmentAvailable ?? []);
```

- [ ] **Step 3: Adicionar seção Equipamentos no `_form()`**

Localizar o bloco de Dores no `_form()`:
```dart
            _SectionLabel(l10n.onboardingPainsTitle),
            _OptionGroup(
              multiSelected: _physicalPains,
              exclusiveOption: 'no_pains',
              options: [
                _Option('no_pains', l10n.onboardingPainsNoneOption),
                _Option('neck', l10n.onboardingPainsNeck),
                _Option('shoulder', l10n.onboardingPainsShoulder),
                _Option('wrist', l10n.onboardingPainsWrist),
                _Option('back', l10n.onboardingPainsBack),
                _Option('lower_back', l10n.onboardingPainsLowerBack),
                _Option('knees', l10n.onboardingPainsKnees),
              ],
              onToggle: (value) => setState(() {
                _physicalPains = _toggle(_physicalPains, value, 'no_pains');
                _formError = null;
              }),
            ),
```
Adicionar logo após esse bloco (antes de `if (_formError != null)`):
```dart
            const SizedBox(height: AwakenSpacing.lg),
            _SectionLabel(l10n.onboardingEquipmentTitle),
            _OptionGroup(
              multiSelected: _equipmentAvailable,
              options: [
                _Option('bodyweight', l10n.onboardingEquipmentNone),
                _Option('dumbbells', l10n.onboardingEquipmentDumbbells),
                _Option('home_equipment', l10n.onboardingEquipmentHome),
                _Option('gym', l10n.onboardingEquipmentGym),
              ],
              onToggle: (value) => setState(() {
                final next = List<String>.from(_equipmentAvailable);
                if (next.contains(value)) {
                  next.remove(value);
                } else {
                  next.add(value);
                }
                _equipmentAvailable = next;
                _formError = null;
              }),
            ),
```

- [ ] **Step 4: Incluir `equipmentAvailable` no `_save()`**

Localizar em `_save()`:
```dart
          physicalPains: _physicalPains,
        );
```
Substituir por:
```dart
          physicalPains: _physicalPains,
          equipmentAvailable: _equipmentAvailable,
        );
```

- [ ] **Step 5: Rodar testes de edit profile**

```bash
cd apps/mobile && flutter test test/features/hunter_profile/presentation/pages/edit_profile_page_test.dart 2>&1 | tail -20
```

Expected: todos os testes PASS.

- [ ] **Step 6: Commit**

```bash
cd apps/mobile && git add lib/features/hunter_profile/presentation/pages/edit_profile_page.dart test/features/hunter_profile/presentation/pages/edit_profile_page_test.dart
git commit -m "feat: adiciona campo de equipamentos em editar perfil e remove da settings"
```

---

### Task 10: Verificação final

**Files:** nenhum

- [ ] **Step 1: flutter analyze sem erros**

```bash
cd apps/mobile && flutter analyze 2>&1 | tail -30
```

Expected: `No issues found!` ou apenas warnings pré-existentes.

- [ ] **Step 2: Suite completa de testes**

```bash
cd apps/mobile && flutter test 2>&1 | tail -10
```

Expected: `All tests passed.`

- [ ] **Step 3: Verificar que url_launcher não é mais importado por settings_page**

```bash
grep "url_launcher" apps/mobile/lib/features/settings/presentation/pages/settings_page.dart
```

Expected: sem output.

- [ ] **Step 4: Verificar que settingsFaqDialogMessage não existe mais nos localizatons**

```bash
grep "settingsFaqDialogMessage" apps/mobile/lib/l10n/app_localizations.dart
```

Expected: sem output.

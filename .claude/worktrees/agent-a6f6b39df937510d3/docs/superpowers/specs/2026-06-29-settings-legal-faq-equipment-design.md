# Settings — Legal Popups, FAQ e Equipamentos no Perfil

Data: 2026-06-29  
Status: Aprovado

---

## Contexto

Três problemas identificados na settings page:

1. **Termos/Privacidade** não abrem nada: `_launchUrl` tenta lançar `awaken.app/terms` e `awaken.app/privacy`, mas o domínio não existe ainda, então nada acontece.
2. **FAQ** exibe placeholder *"Em breve disponível no app."* — sem conteúdo real.
3. **Equipamentos disponíveis** vive como página separada (Settings → Equipamentos), quando deveria estar em Editar Perfil junto com os demais dados do hunter.

Adicionalmente, os textos legais existentes nos ARBs (`legalTermsContent` / `legalPrivacyContent`) precisam de cláusulas complementares para cobertura jurídica nas Américas (Brasil LGPD, EUA COPPA/CCPA, Canadá PIPEDA).

---

## Decisões de Design

### 1. Termos e Privacidade — Dialog in-app (Abordagem A)

A `LegalAcceptancePage` já implementa `_showLegalDocument(title, body, eventName)` com dialog scrollável (`AwakenPanel` + `SingleChildScrollView` + `SelectableText`). A settings page simplesmente não aproveita isso.

**Solução:** extrair o padrão como método privado `_showLegalDoc(context, l10n, title, body)` dentro de `_SettingsPageState`. Chamar com `l10n.legalTermsContent` e `l10n.legalPrivacyContent` — os mesmos textos já aceitos pelo usuário no `LegalAcceptancePage`.

Remover: import `url_launcher`, constantes `_kTermsUrl` / `_kPrivacyUrl`, método `_launchUrl`.

### 2. Textos legais expandidos — todos os 4 idiomas

Expandir `legalTermsContent` e `legalPrivacyContent` nos ARBs (pt, en, es, fr).

#### Termos de Uso — adições

| Cláusula nova | Justificativa |
|---|---|
| Limitação de responsabilidade | App fitness ≠ conselho médico. Afasta responsabilidade civil em BR, EUA, CA, MX |
| Propriedade intelectual | Protege marca AWAKEN, design, código |
| Restrição de idade (13+) | COPPA (EUA): proibição de coleta de menores sem consentimento parental |
| Reembolsos | Delegação expressa às lojas (Apple/Google); evita chargeback |
| Lei aplicável e foro | São Paulo/BR como foro eleito; arbitragem para demais Américas |

#### Política de Privacidade — adições

| Cláusula nova | Justificativa |
|---|---|
| Base legal (LGPD art. 7) | Exigência expressa da LGPD; consentimento + interesse legítimo |
| Fornecedores nomeados | Firebase, RevenueCat, Cloudflare R2, OpenAI/Azure — LGPD art. 37 + PIPEDA |
| Transferência internacional | Dados saem do Brasil; LGPD art. 33 exige menção explícita |
| Direitos CCPA (Califórnia) | Direito de "não vender" + categorias de dados coletados |
| Dados de saúde | Sensíveis pela LGPD art. 11; consentimento específico declarado |
| Encarregado/DPO | LGPD art. 41; canal de contato privacy@awaken.app |
| Versão e data de vigência | Proof of which version user accepted |

### 3. FAQ — Dialog com Q&A estruturado

O dialog atual (`_showFaqDialog`) exibe um único `Text(l10n.settingsFaqDialogMessage)`. Redesenhado para:

- Header com título
- `ListView` scrollável de pares Q/A: pergunta em `AwakenTypography.titleMedium`, resposta em `AwakenTypography.bodyMedium`
- Botão Fechar

**Novos ARB keys** (×4 idiomas): `settingsFaqQ1`–`settingsFaqQ8`, `settingsFaqA1`–`settingsFaqA8`.  
Remover: `settingsFaqDialogMessage` / `settingsFaqDialogTitle` (substituídos).

**8 perguntas definidas:**

| # | Pergunta (PT) |
|---|---|
| 1 | O que é uma Quest Diária? |
| 2 | Como funciona o sistema de XP, Level e Rank? |
| 3 | O que é o Streak e como evitar perder? |
| 4 | Como funciona a assinatura e o trial gratuito? |
| 5 | O que acontece com meu progresso se eu cancelar? |
| 6 | Posso alterar meu perfil após o onboarding? |
| 7 | Os treinos substituem um personal trainer ou médico? |
| 8 | Como entrar em contato com o suporte? |

### 4. Equipamentos em Editar Perfil

**`EditProfilePage`:**
- Adicionar `List<String> _equipmentAvailable = []` ao estado
- Popular em `_populateFrom(profile)`: `_equipmentAvailable = List<String>.from(profile.equipmentAvailable ?? [])`
- Adicionar seção "Equipamentos" no final do `_form()` (após Dores), reutilizando `_OptionGroup` com `onToggle` (multi-select, sem exclusive option)
- Opções: `bodyweight`, `dumbbells`, `home_equipment`, `gym` (labels já existem nos ARBs)
- Passar `equipmentAvailable: _equipmentAvailable` no `_save()` → `updateProfile`
- Sem validação obrigatória (campo opcional; vazio = apenas peso corporal)

**`SettingsPage`:**
- Remover o `_SettingsCardTile` de "Equipamentos disponíveis" da seção Conta

**`EquipmentSettingsPage`:**
- Arquivo permanece intacto (não deletar route/page)
- Apenas sem navegação apontando para ele — remoção não-destrutiva

---

## Arquivos Afetados

| Arquivo | Mudança |
|---|---|
| `lib/features/settings/presentation/pages/settings_page.dart` | +`_showLegalDoc`, -`_launchUrl`, -url_launcher, remove tile equipamentos, redesenha `_showFaqDialog` |
| `lib/features/hunter_profile/presentation/pages/edit_profile_page.dart` | +`_equipmentAvailable`, seção Equipamentos no form e no `_save` |
| `lib/l10n/app_pt.arb` | Expand legal texts, +FAQ keys, remove `settingsFaqDialogMessage` |
| `lib/l10n/app_en.arb` | Idem |
| `lib/l10n/app_es.arb` | Idem |
| `lib/l10n/app_fr.arb` | Idem |
| `lib/l10n/app_localizations.dart` + `_en/_pt/_es/_fr.dart` | Regenerado via `flutter gen-l10n` |

---

## Fora do Escopo

- Não alterar `LegalAcceptancePage` nem o fluxo de aceite inicial
- Não criar versão em URL real (`awaken.app/terms`)
- Não remover `EquipmentSettingsPage` do código/route
- Não adicionar acordeon expandível no FAQ (text layout simples é suficiente para MVP)
- Não validar equipamento como obrigatório no edit profile

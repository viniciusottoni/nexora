# Configuração Segura de Ambiente — AWAKEN

> **Regra de ouro:** nenhum valor real de credencial, senha ou chave de API deve
> aparecer em qualquer arquivo rastreado pelo Git. Arquivos de exemplo contêm
> apenas placeholders que **não funcionam**.

---

## 1. Visão geral

O projeto AWAKEN separa configuração em três camadas:

| Camada | Arquivo / mecanismo | Commitado? |
|--------|---------------------|------------|
| Padrões seguros (sem segredos) | `appsettings.json` | Sim |
| Overrides de desenvolvimento (sem segredos) | `appsettings.Development.json` | Sim |
| Segredos locais do desenvolvedor | `appsettings.Local.json` | **NÃO** |
| CI/CD | GitHub Actions Secrets / variáveis de ambiente | **NÃO** |

---

## 2. Configuração do ambiente local

### 2.1 Backend (.NET)

1. **Copie o template de variáveis:**

   ```bash
   cp .env.example .env.local
   ```

   O arquivo `.env.local` está no `.gitignore` e nunca será commitado.

2. **Crie `backend/src/Awaken.Api/appsettings.Local.json`** com os valores reais:

   ```json
   {
     "ConnectionStrings": {
       "PostgreSQL": "Host=localhost;Port=5432;Database=awaken;Username=awaken;Password=SUA_SENHA_AQUI",
       "Redis": "localhost:6380"
     },
     "Jwt": {
       "Secret": "SUA_CHAVE_JWT_COM_MINIMO_32_CHARS_AQUI"
     },
     "AdminJwt": {
       "Secret": "SUA_CHAVE_ADMIN_JWT_COM_MINIMO_32_CHARS"
     },
     "ExerciseProvider": {
       "ApiKey": "SUA_CHAVE_RAPIDAPI_AQUI"
     },
     "OpenAI": {
       "ApiKey": "SUA_CHAVE_OPENAI_AQUI"
     },
     "Cloudflare": {
       "R2AccountId": "SEU_ACCOUNT_ID",
       "R2AccessKey": "SUA_ACCESS_KEY",
       "R2SecretKey": "SUA_SECRET_KEY"
     },
     "Firebase": {
       "ProjectId": "SEU_PROJECT_ID",
       "ServiceAccountKeyPath": "/caminho/local/para/serviceAccount.json"
     }
   }
   ```

   O padrão `appsettings.Local.json` é carregado automaticamente pelo .NET em
   desenvolvimento e está no `.gitignore`.

3. **Suba o banco e o Redis com Docker:**

   ```bash
   docker-compose up -d
   ```

4. **Rode a API:**

   ```bash
   cd backend
   dotnet run --project src/Awaken.Api/Awaken.Api.csproj
   ```

### 2.2 Flutter (mobile)

Os arquivos de configuração do Firebase e dart_defines **nunca** devem ser
commitados. Eles são injetados manualmente em desenvolvimento ou pelo CI.

Arquivos ignorados pelo `.gitignore`:

```
apps/mobile/dart_defines/production.env
apps/mobile/dart_defines/staging.env
apps/mobile/google-services.json
apps/mobile/android/app/google-services.json
apps/mobile/ios/GoogleService-Info.plist
```

Para desenvolvimento local, obtenha esses arquivos com um membro do time que
tenha acesso ao Firebase Console e ao gerenciador de senhas do projeto.

---

## 3. CI/CD — GitHub Actions

Em CI, **nenhum arquivo de segredos é commitado**. Todos os valores sensíveis são
injetados como **GitHub Actions Secrets** e acessados via variáveis de ambiente.

### 3.1 Secrets configurados no repositório

Acesse `Settings → Secrets and variables → Actions` no GitHub e configure:

| Secret | Descrição |
|--------|-----------|
| `JWT_SECRET` | Chave JWT de produção (≥ 32 chars) |
| `ADMIN_JWT_SECRET` | Chave JWT admin de produção (≥ 32 chars) |
| `POSTGRES_PASSWORD` | Senha do PostgreSQL de produção |
| `REDIS_CONNECTION` | Connection string Redis de produção |
| `OPENAI_API_KEY` | Chave da OpenAI |
| `EXERCISEDB_API_KEY` | Chave da ExerciseDB (RapidAPI) |
| `CLOUDFLARE_R2_ACCESS_KEY` | Chave de acesso Cloudflare R2 |
| `CLOUDFLARE_R2_SECRET_KEY` | Chave secreta Cloudflare R2 |
| `FIREBASE_SERVICE_ACCOUNT_JSON` | JSON da service account Firebase (base64) |
| `REVENUECAT_API_KEY` | Chave da RevenueCat |
| `GOOGLE_CLIENT_ID` | Client ID Google OAuth |

### 3.2 Como os Secrets são usados nos workflows

```yaml
env:
  Jwt__Secret: ${{ secrets.JWT_SECRET }}
  ConnectionStrings__PostgreSQL: ${{ secrets.POSTGRES_CONNECTION_STRING }}
```

**Regras dos workflows:**
- Nunca usar `echo` ou `print` com variáveis de ambiente que contenham segredos
- Nunca logar o contexto completo de `env` em steps de debug
- Usar `${{ secrets.NOME }}` — GitHub mascara automaticamente nos logs

---

## 4. Arquivos que NUNCA devem ser commitados

| Arquivo / padrão | Motivo |
|------------------|--------|
| `appsettings.Local.json` | Segredos locais do desenvolvedor |
| `appsettings.*.Local.json` | Variante do padrão acima |
| `.env`, `.env.*` (exceto `.env.example`) | Variáveis de ambiente com valores reais |
| `*.secrets.json` | Arquivos de segredos genéricos |
| `secrets/` | Diretório de segredos |
| `apps/mobile/android/app/google-services.json` | Credenciais Firebase Android |
| `apps/mobile/ios/GoogleService-Info.plist` | Credenciais Firebase iOS |
| `apps/mobile/dart_defines/production.env` | Config de produção Flutter |
| `apps/mobile/dart_defines/staging.env` | Config de staging Flutter |
| `*.pem`, `*.key` | Certificados e chaves privadas |

Todos esses padrões estão no `.gitignore` da raiz do repositório.

---

## 5. Geração de segredos seguros

Para gerar uma chave JWT segura (≥ 32 chars aleatórios):

```bash
# Linux/macOS
openssl rand -base64 48

# PowerShell (Windows)
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

---

## 6. Verificação — scanning de segredos

O CI roda scanning automático de segredos em todo push e pull request via
Gitleaks (`.github/workflows/security.yml`). Se um segredo for detectado:

1. O pipeline falha e o commit é bloqueado no PR
2. **Revogue imediatamente** a credencial vazada no provedor (mesmo que o PR
   não tenha sido mergeado — o histórico Git preserva o conteúdo)
3. Gere uma nova credencial
4. Use `git filter-repo` ou contate o time de segurança para limpar o histórico

---

## 7. Dúvidas

Consulte o ADR-014 (`docs/adrs/ADR-014-lgpd-dados-pessoais.md`) para políticas
de dados pessoais, e ADR-015 (`docs/adrs/ADR-015-logs-dados-sensiveis.md`) para
regras sobre o que não pode aparecer em logs.

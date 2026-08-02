# Multi-stage build do Nexora.Api.Edge (.NET 10, ADR-036/037/039) — porta da imagem que antes
# buildava a antiga aplicação Node/Prisma equivalente (removida do monorepo). Build context é a
# RAIZ do monorepo (mesma convenção da imagem anterior — infra/edge/docker-compose.yml não
# referencia `build:`, só a imagem final; quem builda de verdade é o pipeline de CI de US-007,
# executando `docker build -f infra/cloud/api-edge.Dockerfile .` a partir da raiz).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Arquivos de configuração da solution primeiro (cache de camada do Docker: só invalida o restore
# quando algum .csproj/props/config muda, não a cada alteração de código-fonte).
COPY backend/global.json backend/NuGet.config backend/Directory.Build.props backend/Directory.Packages.props backend/Nexora.slnx ./backend/
COPY backend/src ./backend/src

RUN dotnet restore backend/src/Nexora.Api.Edge/Nexora.Api.Edge.csproj
RUN dotnet publish backend/src/Nexora.Api.Edge/Nexora.Api.Edge.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:3000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 3000

# Rota de saúde real do Nexora.Api.Edge (InstallationController, GET /v1/health) — mesmo caminho
# checado pelo healthcheck do serviço api-edge em infra/edge/docker-compose.yml.
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD wget -q -O - "http://127.0.0.1:3000/v1/health" || exit 1

# Usuário não-root "app", já provido pela imagem base do .NET (equivalente ao "node" da imagem
# anterior) — compatível com o `group_add: [EDGE_CONTAINER_GID]` do compose, que dá a esse usuário
# acesso de leitura aos volumes de chave/estado montados pelo host.
USER app

# ENTRYPOINT fixo em vez de CMD: o serviço "migrator" do compose sobrescreve só os ARGUMENTOS
# (command: ['--migrate']) para rodar em modo de migration-only e sair — ver Program.cs.
ENTRYPOINT ["dotnet", "Nexora.Api.Edge.dll"]

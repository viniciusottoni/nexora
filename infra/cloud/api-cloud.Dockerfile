# Multi-stage build do Nexora.Api.Cloud (.NET 10, ADR-036/037/039) — porta da imagem que antes
# buildava a antiga aplicação Node/Prisma equivalente (removida do monorepo). Build context é a
# RAIZ do monorepo, mesma convenção de infra/cloud/api-edge.Dockerfile.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/global.json backend/NuGet.config backend/Directory.Build.props backend/Directory.Packages.props backend/Nexora.slnx ./backend/
COPY backend/src ./backend/src

RUN dotnet restore backend/src/Nexora.Api.Cloud/Nexora.Api.Cloud.csproj
RUN dotnet publish backend/src/Nexora.Api.Cloud/Nexora.Api.Cloud.csproj \
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

# Sem HEALTHCHECK: diferente do Api.Edge (InstallationController expõe GET /v1/health real),
# Nexora.Api.Cloud hoje não tem nenhum endpoint de liveness genérico — só GET /v1/sync/health
# (InstallationsController), que é saúde de UMA instalação autenticada, não do processo. Apontar
# o HEALTHCHECK para um caminho inexistente marcaria o container como unhealthy para sempre, um
# bug silencioso pior que não ter healthcheck. Adicionar o endpoint fica fora do escopo desta
# tarefa (US-006 é o servidor EDGE; este Dockerfile só existe porque a auditoria pediu os dois
# junto — nenhum docker-compose deste repo builda/roda api-cloud hoje).
USER app
ENTRYPOINT ["dotnet", "Nexora.Api.Cloud.dll"]

FROM node:22.22.3-bookworm-slim AS web-build
WORKDIR /src/apps/web

COPY apps/web/package.json apps/web/package-lock.json ./
RUN npm ci
COPY apps/web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS api-build
WORKDIR /src

COPY AgenticJobSearch.sln ./
COPY src/AgenticJobSearch.Domain/AgenticJobSearch.Domain.csproj src/AgenticJobSearch.Domain/
COPY src/AgenticJobSearch.Application/AgenticJobSearch.Application.csproj src/AgenticJobSearch.Application/
COPY src/AgenticJobSearch.Infrastructure/AgenticJobSearch.Infrastructure.csproj src/AgenticJobSearch.Infrastructure/
COPY src/AgenticJobSearch.Api/AgenticJobSearch.Api.csproj src/AgenticJobSearch.Api/
RUN dotnet restore src/AgenticJobSearch.Api/AgenticJobSearch.Api.csproj

COPY src/ src/
RUN dotnet publish src/AgenticJobSearch.Api/AgenticJobSearch.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

COPY --from=web-build /src/apps/web/dist/web/browser/ /app/publish/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=api-build --chown=$APP_UID:$APP_UID /app/publish/ ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AgenticJobSearch.Api.dll"]

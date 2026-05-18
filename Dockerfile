# Single parameterized Dockerfile — builds any project in the solution.
# Usage (via docker-compose args):
#   PROJECT  = csproj folder name, e.g. SecPerf.Api.Minimal
#   DLL_NAME = published DLL name  (usually same as PROJECT)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy full solution so shared projects (Domain, Application, Infrastructure) are available
COPY . .

ARG PROJECT
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained

# ── Runtime image ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ARG DLL_NAME
ENV DLL=${DLL_NAME}
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet $DLL.dll"]

# Builds the React frontend and .NET API into a single image.
# The API serves the built frontend from wwwroot, so only one Railway service is needed.

FROM node:22-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src/api
COPY api/SecretSanta.Api/SecretSanta.Api.csproj SecretSanta.Api/
RUN dotnet restore SecretSanta.Api
COPY api/SecretSanta.Api/ SecretSanta.Api/
RUN dotnet publish SecretSanta.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /src/web/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "SecretSanta.Api.dll"]

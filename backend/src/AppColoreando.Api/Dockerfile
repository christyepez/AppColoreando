# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/AppColoreando.Domain/AppColoreando.Domain.csproj src/AppColoreando.Domain/
COPY src/AppColoreando.Application/AppColoreando.Application.csproj src/AppColoreando.Application/
COPY src/AppColoreando.Infrastructure/AppColoreando.Infrastructure.csproj src/AppColoreando.Infrastructure/
COPY src/AppColoreando.Api/AppColoreando.Api.csproj src/AppColoreando.Api/
RUN --mount=type=cache,id=appcoloreando-nuget,target=/root/.nuget/packages,sharing=locked dotnet restore src/AppColoreando.Api/AppColoreando.Api.csproj
COPY . .
RUN --mount=type=cache,id=appcoloreando-nuget,target=/root/.nuget/packages,sharing=locked dotnet publish src/AppColoreando.Api/AppColoreando.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AppColoreando.Api.dll"]

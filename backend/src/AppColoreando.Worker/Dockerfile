# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/AppColoreando.Domain/AppColoreando.Domain.csproj src/AppColoreando.Domain/
COPY src/AppColoreando.Application/AppColoreando.Application.csproj src/AppColoreando.Application/
COPY src/AppColoreando.Infrastructure/AppColoreando.Infrastructure.csproj src/AppColoreando.Infrastructure/
COPY src/AppColoreando.Worker/AppColoreando.Worker.csproj src/AppColoreando.Worker/
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore src/AppColoreando.Worker/AppColoreando.Worker.csproj
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish src/AppColoreando.Worker/AppColoreando.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AppColoreando.Worker.dll"]

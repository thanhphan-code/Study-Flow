FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props ./
COPY src/backend/StudyFlow.API/StudyFlow.API.csproj src/backend/StudyFlow.API/
COPY src/backend/StudyFlow.Application/StudyFlow.Application.csproj src/backend/StudyFlow.Application/
COPY src/backend/StudyFlow.Domain/StudyFlow.Domain.csproj src/backend/StudyFlow.Domain/
COPY src/backend/StudyFlow.Infrastructure/StudyFlow.Infrastructure.csproj src/backend/StudyFlow.Infrastructure/
RUN dotnet restore src/backend/StudyFlow.API/StudyFlow.API.csproj

COPY src/backend/ src/backend/
RUN dotnet publish src/backend/StudyFlow.API/StudyFlow.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

COPY --from=build --chown=app:app /app/publish ./
RUN mkdir -p /app/uploads && chown -R app:app /app/uploads
USER app

ENTRYPOINT ["dotnet", "StudyFlow.API.dll"]

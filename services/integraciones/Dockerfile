# Imagen del microservicio Integraciones. Mismo esquema que Empleos: compilar con el SDK y ejecutar con el runtime.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Integraciones.Application/Integraciones.Application.csproj src/Integraciones.Application/
COPY src/Integraciones.Infrastructure/Integraciones.Infrastructure.csproj src/Integraciones.Infrastructure/
COPY src/Integraciones.Api/Integraciones.Api.csproj src/Integraciones.Api/
RUN dotnet restore src/Integraciones.Api/Integraciones.Api.csproj

COPY src/ src/
RUN dotnet publish src/Integraciones.Api/Integraciones.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

# Credenciales de Gmail y URL de Empleos por variables de entorno: Gmail__ClientId, Gmail__ClientSecret,
# Gmail__RefreshToken, Empleos__BaseUrl y Empleos__ApiKey.
ENTRYPOINT ["dotnet", "Integraciones.Api.dll"]

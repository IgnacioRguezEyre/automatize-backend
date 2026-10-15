# Imagen del microservicio Empleos. Dos etapas: una con el SDK para compilar y otra, mucho más pequeña,
# solo con el runtime de ASP.NET para ejecutar.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: mientras no cambien las dependencias, Docker reutiliza la capa del restore.
COPY src/Empleos.Domain/Empleos.Domain.csproj src/Empleos.Domain/
COPY src/Empleos.Application/Empleos.Application.csproj src/Empleos.Application/
COPY src/Empleos.Infrastructure/Empleos.Infrastructure.csproj src/Empleos.Infrastructure/
COPY src/Empleos.Api/Empleos.Api.csproj src/Empleos.Api/
RUN dotnet restore src/Empleos.Api/Empleos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Empleos.Api/Empleos.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# La API escucha en el 8080 y se ejecuta con un usuario sin privilegios (viene definido en la imagen de Microsoft).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

# La cadena de conexión no va en la imagen: se pasa al arrancar con la variable ConnectionStrings__Jobs.
ENTRYPOINT ["dotnet", "Empleos.Api.dll"]

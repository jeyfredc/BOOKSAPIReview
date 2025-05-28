# Imagen base con .NET 8 SDK
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos del proyecto
COPY . .

# Restaurar paquetes y compilar
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ejecutar la app
ENTRYPOINT ["dotnet", "BooksAPIReviews.dll"]

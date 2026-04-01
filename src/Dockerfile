# Dockerfile para LogisticsAndDeliveries.WebApi (multistage, .NET8)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
ENV CI=true

# Copiar archivos de proyecto para aprovechar la cache de Docker
COPY ["LogisticsAndDeliveries/LogisticsAndDeliveries.Core/LogisticsAndDeliveries.Core.csproj", "LogisticsAndDeliveries/LogisticsAndDeliveries.Core/"]
COPY ["LogisticsAndDeliveries/LogisticsAndDeliveries.Domain/LogisticsAndDeliveries.Domain.csproj", "LogisticsAndDeliveries/LogisticsAndDeliveries.Domain/"]
COPY ["LogisticsAndDeliveries/LogisticsAndDeliveries.Application/LogisticsAndDeliveries.Application.csproj", "LogisticsAndDeliveries/LogisticsAndDeliveries.Application/"]
COPY ["LogisticsAndDeliveries/LogisticsAndDeliveries.Infrastructure/LogisticsAndDeliveries.Infrastructure.csproj", "LogisticsAndDeliveries/LogisticsAndDeliveries.Infrastructure/"]
COPY ["LogisticsAndDeliveries/LogisticsAndDeliveries.WebApi/LogisticsAndDeliveries.WebApi.csproj", "LogisticsAndDeliveries/LogisticsAndDeliveries.WebApi/"]
RUN dotnet restore "LogisticsAndDeliveries/LogisticsAndDeliveries.WebApi/LogisticsAndDeliveries.WebApi.csproj"

# Copiar el resto del código y publicar
COPY . .
WORKDIR /src/LogisticsAndDeliveries/LogisticsAndDeliveries.WebApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Imagen de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LogisticsAndDeliveries.WebApi.dll"]

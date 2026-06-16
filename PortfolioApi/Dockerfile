# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PortfolioApi.csproj", "./"]
RUN dotnet restore "PortfolioApi.csproj"
COPY . .
RUN dotnet publish "PortfolioApi.csproj" -c Release -o /app/publish

# Estágio Final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Garante que a porta 8080 seja usada (padrão do Render)
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PortfolioApi.dll"]
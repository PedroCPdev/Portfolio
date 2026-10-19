FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PortfolioApi.csproj", "./"]
RUN dotnet restore "PortfolioApi.csproj"
COPY . .
RUN dotnet publish "PortfolioApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Usuário sem privilégio que a imagem aspnet já traz (UID 1654). A 8080 é porta alta, então não
# precisa de root para escutar.
USER $APP_UID
ENTRYPOINT ["dotnet", "PortfolioApi.dll"]

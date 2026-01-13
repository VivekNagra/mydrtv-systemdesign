FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MyDRTV.sln ./
COPY shared/MyDrTv.Contracts/MyDrTv.Contracts.csproj shared/MyDrTv.Contracts/
COPY src/RecommendationService/RecommendationService.csproj src/RecommendationService/

RUN dotnet restore src/RecommendationService/RecommendationService.csproj

COPY . .
RUN dotnet publish src/RecommendationService/RecommendationService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RecommendationService.dll"]

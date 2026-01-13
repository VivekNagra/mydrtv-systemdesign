FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MyDRTV.sln ./
COPY shared/MyDrTv.Contracts/MyDrTv.Contracts.csproj shared/MyDrTv.Contracts/
COPY src/IdentityService/IdentityService.csproj src/IdentityService/
RUN dotnet restore src/IdentityService/IdentityService.csproj

COPY . .
RUN dotnet publish src/IdentityService/IdentityService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "IdentityService.dll"]

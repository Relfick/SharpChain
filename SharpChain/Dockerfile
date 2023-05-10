FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SharpChain/SharpChain.csproj", "SharpChain/"]
RUN dotnet restore "SharpChain/SharpChain.csproj"
COPY . .
WORKDIR "/src/SharpChain"
RUN dotnet build "SharpChain.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SharpChain.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SharpChain.dll"]

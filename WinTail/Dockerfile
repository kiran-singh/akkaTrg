FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WinTail/WinTail.csproj", "WinTail/"]
RUN dotnet restore "WinTail/WinTail.csproj"
COPY . .
WORKDIR "/src/WinTail"
RUN dotnet build "WinTail.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WinTail.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WinTail.dll"]

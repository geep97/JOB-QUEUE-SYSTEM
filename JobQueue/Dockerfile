FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["JobQueue/JobQueue.csproj", "JobQueue/"]
RUN dotnet restore "JobQueue/JobQueue.csproj"

COPY . .
WORKDIR /src/JobQueue
RUN dotnet publish "JobQueue.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "JobQueue.dll"]
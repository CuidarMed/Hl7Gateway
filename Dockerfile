FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos del proyecto
COPY Hl7Gateway.csproj .
RUN dotnet restore

COPY . .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .

EXPOSE 5000 2575

ENTRYPOINT ["dotnet", "Hl7Gateway.dll"]


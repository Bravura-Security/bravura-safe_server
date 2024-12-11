FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

LABEL com.bitwarden.product="bitwarden"

WORKDIR /app
COPY obj/build-output/publish .

ENTRYPOINT ["sh", "-c", "dotnet /app/MsSqlMigratorUtility.dll \"${MSSQL_CONN_STRING}\" ${@}", "--" ]

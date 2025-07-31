
# --------- Build stage ----------
    # l�m  vi?c vs iamge v� g�n t�n stage n�y l� bui;d
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore 
COPY . . 
RUN dotnet publish  -c Release -o /app/publish


# --------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish . 
# Copy k?t qu? bi�n d?ch t? stage build sang stage runtime

EXPOSE 7173
ENTRYPOINT ["dotnet", "E-learning.dll"]

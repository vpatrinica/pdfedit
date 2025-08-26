# Stage 1: Build Environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching
COPY PdfEdit.sln .
COPY src/PdfEdit.Api/PdfEdit.Api.csproj src/PdfEdit.Api/
COPY src/PdfEdit.Client/PdfEdit.Client.csproj src/PdfEdit.Client/
COPY src/PdfEdit.Shared/PdfEdit.Shared.csproj src/PdfEdit.Shared/

# Restore dependencies
RUN dotnet restore "PdfEdit.sln"

# Copy the rest of the source code
COPY . .

# Publish the application
WORKDIR "/src/src/PdfEdit.Api"
RUN dotnet publish "PdfEdit.Api.csproj" -c Release -o /app/publish

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80 for the combined app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "PdfEdit.Api.dll"]
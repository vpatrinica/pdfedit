# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/PdfEdit.Api/PdfEdit.Api.csproj", "src/PdfEdit.Api/"]
COPY ["src/PdfEdit.Shared/PdfEdit.Shared.csproj", "src/PdfEdit.Shared/"]

# Restore dependencies
RUN dotnet restore "src/PdfEdit.Api/PdfEdit.Api.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/PdfEdit.Api"
RUN dotnet build "PdfEdit.Api.csproj" -c Release -o /app/build
RUN dotnet publish "PdfEdit.Api.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "PdfEdit.Api.dll"]
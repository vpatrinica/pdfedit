# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/PdfEdit.Client/PdfEdit.Client.csproj", "src/PdfEdit.Client/"]
COPY ["src/PdfEdit.Shared/PdfEdit.Shared.csproj", "src/PdfEdit.Shared/"]

# Restore dependencies
RUN dotnet restore "src/PdfEdit.Client/PdfEdit.Client.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/PdfEdit.Client"
RUN dotnet build "PdfEdit.Client.csproj" -c Release -o /app/build
RUN dotnet publish "PdfEdit.Client.csproj" -c Release -o /app/publish

# Use Nginx to serve the static files
FROM nginx:alpine AS runtime
WORKDIR /var/www/web

# Copy the published Blazor app
COPY --from=build /app/publish/wwwroot .

# Copy nginx configuration
COPY src/PdfEdit.Client/nginx.conf /etc/nginx/nginx.conf

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
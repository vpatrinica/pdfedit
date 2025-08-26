@echo off
REM PDF Editor - Podman Deployment Script
REM This script uses Podman to build and run the PDF Editor application

setlocal enabledelayedexpansion

echo 🐋 Starting PDF Editor with Podman...

REM Check if podman is installed
where podman >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ Error: Podman is not installed. Please install Podman first.
    echo    Visit: https://podman.io/getting-started/installation
    exit /b 1
)

REM Check if podman-compose is available
where podman-compose >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using podman-compose...
    podman-compose -f podman-compose.yml up --build %*
    goto :success
)

REM Check if podman compose is available
podman compose version >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using podman compose...
    podman compose -f podman-compose.yml up --build %*
    goto :success
)

echo ⚠️  Warning: Neither podman-compose nor 'podman compose' found.
echo    Falling back to individual podman commands...

REM Create network
echo 🌐 Creating network...
podman network exists pdfedit-network >nul 2>nul || podman network create pdfedit-network

REM Build and run client
echo 🏗️  Building client container...
podman build -t pdfedit-client:latest -f src/PdfEdit.Client/Dockerfile .
if %errorlevel% neq 0 exit /b 1

echo 🚀 Starting client container...
podman run -d --name pdfedit-client --network pdfedit-network -p 80:80 pdfedit-client:latest
if %errorlevel% neq 0 exit /b 1

REM Build and run API
echo 🏗️  Building API container...
podman build -t pdfedit-api:latest -f src/PdfEdit.Api/Dockerfile .
if %errorlevel% neq 0 exit /b 1

echo 🚀 Starting API container...
podman run -d --name pdfedit-api --network pdfedit-network -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production -e ASPNETCORE_URLS=http://+:8080 pdfedit-api:latest
if %errorlevel% neq 0 exit /b 1

echo ✅ PDF Editor is starting up...
echo    Client: http://localhost
echo    API: http://localhost:8080
echo.
echo To stop the application, run: podman stop pdfedit-client pdfedit-api
echo To remove containers, run: podman rm pdfedit-client pdfedit-api
exit /b 0

:success
echo ✅ PDF Editor is starting up...
echo    Client: http://localhost
echo    API: http://localhost:8080
echo.
echo To stop the application, run: scripts\stop-podman.bat
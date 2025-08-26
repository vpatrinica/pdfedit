@echo off
REM PDF Editor - Docker Stop Script
REM This script stops the PDF Editor containers

setlocal enabledelayedexpansion

echo 🛑 Stopping PDF Editor containers...

REM Check if docker-compose is available
where docker-compose >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using docker-compose...
    docker-compose down
    goto :success
)

REM Check if docker compose is available
docker compose version >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using docker compose...
    docker compose down
    goto :success
)

echo ❌ Error: Docker Compose is not available.
exit /b 1

:success
echo ✅ PDF Editor stopped successfully!
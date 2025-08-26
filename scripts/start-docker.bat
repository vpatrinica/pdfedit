@echo off
REM PDF Editor - Docker Deployment Script
REM This script uses Docker Compose to build and run the PDF Editor application

setlocal enabledelayedexpansion

echo 🐳 Starting PDF Editor with Docker...

REM Check if docker is installed
where docker >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ Error: Docker is not installed. Please install Docker first.
    echo    Visit: https://docs.docker.com/get-docker/
    exit /b 1
)

REM Check if docker-compose is available
where docker-compose >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using docker-compose...
    docker-compose up --build %*
    goto :success
)

REM Check if docker compose is available
docker compose version >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using docker compose...
    docker compose up --build %*
    goto :success
)

echo ❌ Error: Docker Compose is not available.
echo    Please install docker-compose or use Docker with compose plugin.
exit /b 1

:success
echo ✅ PDF Editor is starting up...
echo    Client: http://localhost
echo    API: http://localhost:8080
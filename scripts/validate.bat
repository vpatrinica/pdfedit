@echo off
REM PDF Editor - Container Validation Script
REM This script validates the container configuration without building

setlocal enabledelayedexpansion

echo 🔍 Validating PDF Editor container configurations...

REM Check if podman is installed
where podman >nul 2>nul
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('podman --version 2^>nul') do set PODMAN_VERSION=%%i
    echo ✅ Podman is available: !PODMAN_VERSION!
    
    REM Check compose support
    where podman-compose >nul 2>nul
    if %errorlevel% equ 0 (
        for /f "tokens=*" %%i in ('podman-compose --version 2^>nul') do set PODMAN_COMPOSE_VERSION=%%i
        echo ✅ podman-compose is available: !PODMAN_COMPOSE_VERSION!
        echo 📋 Validating podman-compose.yml...
        podman-compose -f podman-compose.yml config >nul 2>nul && echo ✅ podman-compose.yml is valid
    ) else (
        podman compose version >nul 2>nul
        if !errorlevel! equ 0 (
            echo ✅ podman compose is available
            echo 📋 Validating podman-compose.yml...
            podman compose -f podman-compose.yml config >nul 2>nul && echo ✅ podman-compose.yml is valid
        ) else (
            echo ⚠️  Neither podman-compose nor 'podman compose' found
            echo    Individual podman commands will be used
        )
    )
) else (
    echo ❌ Podman is not available
)

echo.

REM Check if docker is installed
where docker >nul 2>nul
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('docker --version 2^>nul') do set DOCKER_VERSION=%%i
    echo ✅ Docker is available: !DOCKER_VERSION!
    
    REM Check compose support
    where docker-compose >nul 2>nul
    if %errorlevel% equ 0 (
        for /f "tokens=*" %%i in ('docker-compose --version 2^>nul') do set DOCKER_COMPOSE_VERSION=%%i
        echo ✅ docker-compose is available: !DOCKER_COMPOSE_VERSION!
        echo 📋 Validating docker-compose.yml...
        docker-compose config >nul 2>nul && echo ✅ docker-compose.yml is valid
    ) else (
        docker compose version >nul 2>nul
        if !errorlevel! equ 0 (
            echo ✅ docker compose is available
            echo 📋 Validating docker-compose.yml...
            docker compose config >nul 2>nul && echo ✅ docker-compose.yml is valid
        ) else (
            echo ❌ Docker Compose is not available
        )
    )
) else (
    echo ❌ Docker is not available
)

echo.

REM Check scripts
echo 📁 Checking deployment scripts...
for %%s in (start-podman.sh stop-podman.sh start-docker.sh stop-docker.sh start-podman.bat stop-podman.bat start-docker.bat stop-docker.bat) do (
    if exist "scripts\%%s" (
        echo ✅ scripts\%%s exists
    ) else (
        echo ❌ scripts\%%s is missing
    )
)

echo.
echo 🎯 Container validation complete!
echo.
echo 💡 Recommended usage:
echo    Podman (default): scripts\start-podman.bat
echo    Docker:           scripts\start-docker.bat
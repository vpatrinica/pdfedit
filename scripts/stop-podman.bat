@echo off
REM PDF Editor - Podman Stop Script
REM This script stops the PDF Editor containers

setlocal enabledelayedexpansion

echo 🛑 Stopping PDF Editor containers...

REM Check if podman-compose is available
where podman-compose >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using podman-compose...
    podman-compose -f podman-compose.yml down
    goto :success
)

REM Check if podman compose is available
podman compose version >nul 2>nul
if %errorlevel% equ 0 (
    echo 📦 Using podman compose...
    podman compose -f podman-compose.yml down
    goto :success
)

echo 🧹 Stopping individual containers...

REM Stop containers
podman ps --format "{{.Names}}" | findstr /C:"pdfedit-client" >nul
if %errorlevel% equ 0 (
    echo ⏹️  Stopping client container...
    podman stop pdfedit-client
)

podman ps --format "{{.Names}}" | findstr /C:"pdfedit-api" >nul
if %errorlevel% equ 0 (
    echo ⏹️  Stopping API container...
    podman stop pdfedit-api
)

REM Remove containers
podman ps -a --format "{{.Names}}" | findstr /C:"pdfedit-client" >nul
if %errorlevel% equ 0 (
    echo 🗑️  Removing client container...
    podman rm pdfedit-client
)

podman ps -a --format "{{.Names}}" | findstr /C:"pdfedit-api" >nul
if %errorlevel% equ 0 (
    echo 🗑️  Removing API container...
    podman rm pdfedit-api
)

:success
echo ✅ PDF Editor stopped successfully!
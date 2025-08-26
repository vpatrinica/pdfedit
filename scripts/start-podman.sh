#!/bin/bash

# PDF Editor - Podman Deployment Script
# This script uses Podman to build and run the PDF Editor application

set -e

echo "🐋 Starting PDF Editor with Podman..."

# Check if podman is installed
if ! command -v podman &> /dev/null; then
    echo "❌ Error: Podman is not installed. Please install Podman first."
    echo "   Visit: https://podman.io/getting-started/installation"
    exit 1
fi

# Check if podman-compose is available
if command -v podman-compose &> /dev/null; then
    echo "📦 Using podman-compose..."
    podman-compose -f podman-compose.yml up --build "$@"
elif podman compose version &> /dev/null; then
    echo "📦 Using podman compose..."
    podman compose -f podman-compose.yml up --build "$@"
else
    echo "⚠️  Warning: Neither podman-compose nor 'podman compose' found."
    echo "   Falling back to individual podman commands..."
    
    # Create network
    echo "🌐 Creating network..."
    podman network exists pdfedit-network || podman network create pdfedit-network
    
    # Build and run client
    echo "🏗️  Building client container..."
    podman build -t pdfedit-client:latest -f src/PdfEdit.Client/Dockerfile .
    
    echo "🚀 Starting client container..."
    podman run -d --name pdfedit-client --network pdfedit-network -p 80:80 pdfedit-client:latest
    
    # Build and run API
    echo "🏗️  Building API container..."
    podman build -t pdfedit-api:latest -f src/PdfEdit.Api/Dockerfile .
    
    echo "🚀 Starting API container..."
    podman run -d --name pdfedit-api --network pdfedit-network -p 8080:8080 \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e ASPNETCORE_URLS=http://+:8080 \
        pdfedit-api:latest
    
    echo "✅ PDF Editor is starting up..."
    echo "   Client: http://localhost"
    echo "   API: http://localhost:8080"
    echo ""
    echo "To stop the application, run: podman stop pdfedit-client pdfedit-api"
    echo "To remove containers, run: podman rm pdfedit-client pdfedit-api"
    exit 0
fi

echo "✅ PDF Editor is starting up..."
echo "   Client: http://localhost"
echo "   API: http://localhost:8080"
echo ""
echo "To stop the application, run: ./scripts/stop-podman.sh"
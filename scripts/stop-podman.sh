#!/bin/bash

# PDF Editor - Podman Stop Script
# This script stops the PDF Editor containers

set -e

echo "🛑 Stopping PDF Editor containers..."

# Check if podman-compose is available
if command -v podman-compose &> /dev/null; then
    echo "📦 Using podman-compose..."
    podman-compose -f podman-compose.yml down
elif podman compose version &> /dev/null; then
    echo "📦 Using podman compose..."
    podman compose -f podman-compose.yml down
else
    echo "🧹 Stopping individual containers..."
    
    # Stop containers
    if podman ps --format "{{.Names}}" | grep -q "pdfedit-client"; then
        echo "⏹️  Stopping client container..."
        podman stop pdfedit-client
    fi
    
    if podman ps --format "{{.Names}}" | grep -q "pdfedit-api"; then
        echo "⏹️  Stopping API container..."
        podman stop pdfedit-api
    fi
    
    # Remove containers
    if podman ps -a --format "{{.Names}}" | grep -q "pdfedit-client"; then
        echo "🗑️  Removing client container..."
        podman rm pdfedit-client
    fi
    
    if podman ps -a --format "{{.Names}}" | grep -q "pdfedit-api"; then
        echo "🗑️  Removing API container..."
        podman rm pdfedit-api
    fi
fi

echo "✅ PDF Editor stopped successfully!"
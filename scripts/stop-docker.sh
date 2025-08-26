#!/bin/bash

# PDF Editor - Docker Stop Script
# This script stops the PDF Editor containers

set -e

echo "🛑 Stopping PDF Editor containers..."

# Check if docker-compose is available
if command -v docker-compose &> /dev/null; then
    echo "📦 Using docker-compose..."
    docker-compose down
elif docker compose version &> /dev/null; then
    echo "📦 Using docker compose..."
    docker compose down
else
    echo "❌ Error: Docker Compose is not available."
    exit 1
fi

echo "✅ PDF Editor stopped successfully!"
#!/bin/bash

# PDF Editor - Container Validation Script
# This script validates the container configuration without building

set -e

echo "🔍 Validating PDF Editor container configurations..."

# Check if podman is installed
if command -v podman &> /dev/null; then
    echo "✅ Podman is available: $(podman --version)"
    
    # Check compose support
    if command -v podman-compose &> /dev/null; then
        echo "✅ podman-compose is available: $(podman-compose --version)"
        echo "📋 Validating podman-compose.yml..."
        podman-compose -f podman-compose.yml config > /dev/null && echo "✅ podman-compose.yml is valid"
    elif podman compose version &> /dev/null; then
        echo "✅ podman compose is available"
        echo "📋 Validating podman-compose.yml..."
        podman compose -f podman-compose.yml config > /dev/null && echo "✅ podman-compose.yml is valid"
    else
        echo "⚠️  Neither podman-compose nor 'podman compose' found"
        echo "   Individual podman commands will be used"
    fi
else
    echo "❌ Podman is not available"
fi

echo ""

# Check if docker is installed
if command -v docker &> /dev/null; then
    echo "✅ Docker is available: $(docker --version)"
    
    # Check compose support
    if command -v docker-compose &> /dev/null; then
        echo "✅ docker-compose is available: $(docker-compose --version)"
        echo "📋 Validating docker-compose.yml..."
        docker-compose config > /dev/null && echo "✅ docker-compose.yml is valid"
    elif docker compose version &> /dev/null; then
        echo "✅ docker compose is available"
        echo "📋 Validating docker-compose.yml..."
        docker compose config > /dev/null && echo "✅ docker-compose.yml is valid"
    else
        echo "❌ Docker Compose is not available"
    fi
else
    echo "❌ Docker is not available"
fi

echo ""

# Check scripts
echo "📁 Checking deployment scripts..."
for script in start-podman.sh stop-podman.sh start-docker.sh stop-docker.sh; do
    if [ -f "scripts/$script" ] && [ -x "scripts/$script" ]; then
        echo "✅ scripts/$script is executable"
    else
        echo "❌ scripts/$script is missing or not executable"
    fi
done

echo ""
echo "🎯 Container validation complete!"
echo ""
echo "💡 Recommended usage:"
echo "   Podman (default): ./scripts/start-podman.sh"
echo "   Docker:           ./scripts/start-docker.sh"
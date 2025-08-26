#!/bin/bash

# PDF Editor - Docker Deployment Script
# This script uses Docker Compose to build and run the PDF Editor application

set -e

echo "🐳 Starting PDF Editor with Docker..."

# Check if docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Error: Docker is not installed. Please install Docker first."
    echo "   Visit: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check if docker-compose is available
if command -v docker-compose &> /dev/null; then
    echo "📦 Using docker-compose..."
    docker-compose up --build "$@"
elif docker compose version &> /dev/null; then
    echo "📦 Using docker compose..."
    docker compose up --build "$@"
else
    echo "❌ Error: Docker Compose is not available."
    echo "   Please install docker-compose or use Docker with compose plugin."
    exit 1
fi

echo "✅ PDF Editor is starting up..."
echo "   Client: http://localhost"
echo "   API: http://localhost:8080"
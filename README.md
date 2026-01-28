# Media Asset Manager

A .NET 8 Web API for managing media assets with cloud storage integration.

## Overview

Media Asset Manager provides a RESTful API to organize, store, and retrieve media content. Built with clean architecture principles, it separates concerns across distinct layers while integrating with Backblaze B2 for cloud storage and PostgreSQL for metadata management.

## Features

- RESTful API for media asset management
- Cloud storage integration (Backblaze B2)
- PostgreSQL database with Entity Framework Core
- Query filtering, sorting, and pagination
- Clean architecture with proper layer separation
- Response DTOs to protect domain model
- Structured logging with Serilog
- OpenAPI/Swagger documentation

## Architecture

The solution follows clean architecture principles with clear separation of concerns:

```
MediaAssetManager/
├── MediaAssetManager.API/            # Presentation layer (Controllers, DTOs)
├── MediaAssetManager.Services/       # Application layer (Business logic)
├── MediaAssetManager.Core/           # Domain layer (Entities, Interfaces)
└── MediaAssetManager.Infrastructure/ # Data access layer (Repositories, EF Core)
```

**Dependency Flow:** API → Services → Core ← Infrastructure

## Tech Stack

- .NET 8 (LTS)
- ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL with Npgsql
- Serilog for structured logging
- Backblaze B2 (S3-compatible storage)
- AWS SDK for S3

## Development Status

This project is in active development. See [docs/TODO.md](docs/TODO.md) for planned improvements and future features.

## API Endpoints

- `GET /api/mediaassets` - List media assets (with filtering, sorting, pagination)
- `GET /api/mediaassets/{id}` - Get specific media asset

More endpoints coming soon.

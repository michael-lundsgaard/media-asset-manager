# AI Agent Instructions for Media Asset Manager

## Project Overview

**Media Asset Manager** is a .NET 8 Web API for managing video gaming clips with Backblaze B2 cloud storage integration.

### Architecture
- **Clean Architecture** pattern with distinct layers:
  - `MediaAssetManager.Core` - Domain entities and interfaces
  - `MediaAssetManager.Infrastructure` - Data access (EF Core + PostgreSQL/Supabase)
  - `MediaAssetManager.Services` - Business logic and external service integrations
  - `MediaAssetManager.API` - RESTful API endpoints

### Tech Stack
- **.NET 8** Web API
- **Entity Framework Core** with PostgreSQL
- **Supabase** for database hosting
- **Backblaze B2** for object storage
- **Clean Architecture** with dependency injection

---

## Key Documentation

### Configuration & Setup
- **[docs/CONFIGURATION.md](docs/CONFIGURATION.md)** - Essential reading for:
  - User secrets setup (development)
  - Environment variables (production)
  - Database connection strings
  - Backblaze B2 storage configuration
  - Security best practices

---

## Common Tasks

### Database Operations
- **Connection**: Uses PostgreSQL via Supabase (see CONFIGURATION.md)
- **Context Factory**: `MediaAssetManager.Infrastructure/Data/MediaAssetContextFactory.cs`
- **Migrations**: Located in `MediaAssetManager.Infrastructure/Migrations`

### API Controllers
- **MediaAssetsController**: Main CRUD operations for media assets
- **TestController**: Testing endpoints (development only)

### Core Entities
- **MediaAsset**: Primary entity in `MediaAssetManager.Core/Entities/MediaAsset.cs`

---

## Getting Started (For AI Agents)

### 1. Understanding Configuration
Before making changes, always review:
- `appsettings.json` - Base configuration structure
- `appsettings.Development.json` - Development settings
- **docs/CONFIGURATION.md** - Secrets and sensitive data setup

### 2. Code Modifications
When modifying code:
- Follow existing patterns and naming conventions
- Respect the Clean Architecture boundaries
- Do not add sensitive data to configuration files
- Keep controller logic thin, business logic in Services
- Entity definitions stay in Core layer

### 3. Adding New Features
- **New Entity**: Add to `MediaAssetManager.Core/Entities/`
- **New Service**: Add to `MediaAssetManager.Services/`
- **New API Endpoint**: Add controller to `MediaAssetManager.API/Controllers/`
- **Data Access**: Add repository/context changes to `MediaAssetManager.Infrastructure/`

---

## Important Notes

### Security
- **NEVER** add real secrets, passwords, or API keys to configuration files
- Always use User Secrets (dev) or Environment Variables (prod)
- Reference **docs/CONFIGURATION.md** for proper secrets management

### Dependencies
- Project uses dependency injection throughout
- Services registered in `Program.cs` (MediaAssetManager.API)
- Follow existing DI patterns when adding new services

### Database
- EF Core migrations managed in Infrastructure project
- Always create migrations when changing entities
- Test with Supabase PostgreSQL instance

---

## Project Purpose

This API manages gaming video clips with metadata:
- Upload media to Backblaze B2 storage
- Store metadata in PostgreSQL database
- Retrieve and manage gaming content
- Organize clips by game, platform, tags, etc.

---

## Project Structure Quick Reference

MediaAssetManager/
├── MediaAssetManager.Core/            # Domain layer (entities, interfaces)
├── MediaAssetManager.Infrastructure/  # Data access (EF Core, repositories)
├── MediaAssetManager.Services/        # Business logic & external services
├── MediaAssetManager.API/             # Web API controllers & startup
├── docs/                              # Documentation
│   └── CONFIGURATION.md               # Setup & secrets management
├── CLAUDE.md                          # This file (AI agent instructions)
└── README.md                          # Project readme

---

## Tips for AI Agents

1. **Always read existing code** before making changes to understand patterns
2. **Check CONFIGURATION.md** when dealing with settings or connections
3. **Respect layer boundaries** - do not reference Infrastructure from Core
4. **Use existing services** - check Services layer before creating new integrations
5. **Follow .NET conventions** - use async/await, proper naming, XML comments where appropriate

---

## Contributing

When making changes:
- Ensure code compiles successfully
- Follow existing code style and patterns
- Update relevant documentation if needed
- Test changes locally when possible
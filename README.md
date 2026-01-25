# Media Asset Manager

A .NET 8 Web API for managing video clips with cloud storage integration.

## Overview

Media Asset Manager provides a RESTful API to organize, store, and retrieve gaming content. Upload your clips to Backblaze B2 cloud storage while maintaining rich metadata in a PostgreSQL database.

## Features

- **Upload & Store** - Upload media to Backblaze B2 cloud storage
- **Metadata Management** - Store and query media metadata in PostgreSQL
- **Clean Architecture** - Well-organized codebase with clear separation of concerns
- **Secure Configuration** - User secrets and environment variable support
- **Cloud-Ready** - Integrates with Supabase (PostgreSQL) and Backblaze B2

## Tech Stack

- **.NET 8** - Latest LTS version of .NET
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core** - ORM for database access
- **PostgreSQL** - Primary database (hosted on Supabase)
- **Backblaze B2** - Cloud object storage for media files

## Project Structure

```
MediaAssetManager/
├── MediaAssetManager.Core/           # Domain entities and interfaces
├── MediaAssetManager.Infrastructure/ # Data access and EF Core
├── MediaAssetManager.Services/       # Business logic & external services
├── MediaAssetManager.API/            # API controllers and configuration
└── docs/                             # Documentation
    └── CONFIGURATION.md              # Setup and configuration guide
```

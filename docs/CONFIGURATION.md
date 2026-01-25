# Configuration & Secrets Setup

## Overview
This project uses **User Secrets** (development) and **Environment Variables** (production) for sensitive configuration.

## Development Setup

### 1. Configure User Secrets
User secrets are stored outside your project directory and are never committed to git.

```bash
cd MediaAssetManager.API

# Set Database Connection String (Supabase with IPv4 pooler)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "User Id=postgres.fjjyfdyqpswcpxsvcrut;Password=<password>;Server=aws-1-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres"

# Set Backblaze B2 Configuration
dotnet user-secrets set "B2:AccountId" "<your-account-id>"
dotnet user-secrets set "B2:ApplicationKey" "<your-application-key>"
dotnet user-secrets set "B2:BucketId" "<your-bucket-id>"
dotnet user-secrets set "B2:BucketName" "<your-bucket-name>"
```

### 2. Verify User Secrets
```bash
dotnet user-secrets list --project MediaAssetManager.API
```

## Configuration Structure

### appsettings.json
- **Committed to git** ?
- Contains placeholder values showing required structure
- Safe default values (localhost for development)

### appsettings.Development.json
- **Committed to git** ?
- Development-specific non-sensitive config (logging levels, etc.)
- Does NOT contain secrets

### User Secrets (Development)
- **Never committed to git** ?
- Contains actual sensitive values for local development
- Location: `%APPDATA%\Microsoft\UserSecrets\213fd626-d700-4601-a2e3-67c86dc0cdac\secrets.json`

### Environment Variables (Production)
- Set in your hosting environment (Azure, AWS, Docker, etc.)
- Same key names as appsettings structure
- Example: `ConnectionStrings__DefaultConnection`

## Required Configuration Keys

### Database
```
ConnectionStrings:DefaultConnection
```

### Backblaze B2 Storage
```
B2:AccountId
B2:ApplicationKey
B2:BucketId
B2:BucketName
```

## Production Deployment

For production, set environment variables instead of user secrets:

**Linux/Docker:**
```bash
export ConnectionStrings__DefaultConnection="User Id=postgres.<project-ref>;Password=<password>;Server=aws-1-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres"
export B2__AccountId="your-account-id"
export B2__ApplicationKey="your-key"
```

**Azure App Service:** Use Application Settings in the portal

**Docker Compose:**
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=...
  - B2__AccountId=...
```

## Security Notes

?? **Never commit these to git:**
- Real database connection strings
- API keys or application keys
- Passwords or tokens
- Any file with `.local.json` extension

? **Safe to commit:**
- appsettings.json (with placeholders)
- appsettings.Development.json (without secrets)
- This README

## Troubleshooting

**EF Core migrations can't find connection string:**
- Ensure user secrets are configured (the `MediaAssetContextFactory` reads them)
- Verify you're in the API project directory when running migrations
- Run `dotnet user-secrets list --project MediaAssetManager.API` to verify

**IPv6 connection issues with Supabase:**
- Use the IPv4 pooler URL (not the direct database URL)
- Format: `aws-0-eu-central-1.pooler.supabase.com` (with `-0-`)

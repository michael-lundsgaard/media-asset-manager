# TODO

This document tracks planned improvements and future features for the Media Asset Manager project.

## General Improvements

### Global Exception Handler
Add middleware to catch and handle unhandled exceptions consistently across the API.

**Implementation:**
- Add `UseExceptionHandler` middleware in `Program.cs`
- Return standardized `ErrorResponseDto` for all unhandled exceptions
- Log exceptions with correlation IDs for debugging
- Prevent leaking sensitive information (stack traces, connection strings) in production

**Benefits:**
- Consistent error response format
- Better security (no information leakage)
- Improved debugging with proper logging

### Testing
**Unit Tests**
- Add tests for Services layer (business logic)
- Add tests for Repository layer (data access)
- Mock dependencies using interfaces
- Target >80% code coverage

**Integration Tests**
- Test API endpoints end-to-end
- Use in-memory database or test containers
- Verify request/response contracts
- Test error scenarios and edge cases

### Documentation
**XML Documentation**
- Document all public APIs (controllers, services, repositories)
- Add `<summary>` tags for methods and classes
- Add `<param>` and `<returns>` tags
- Enable XML documentation generation in project files
- Improves Swagger/OpenAPI documentation

### Validation
**FluentValidation**
- Consider replacing/supplementing DataAnnotations with FluentValidation
- Provides more complex validation rules
- Better separation of validation logic
- More readable and maintainable validation code

### Health Checks
**Database Health Check**
- Verify PostgreSQL connection is active
- Check if database is accessible

**External Services Health Check**
- Check Backblaze B2 connectivity
- Verify cloud storage availability

**Implementation:**
- Add `Microsoft.Extensions.Diagnostics.HealthChecks` package
- Create custom health checks for external dependencies
- Expose `/health` endpoint with detailed status

### HATEOAS Links
Implement Hypermedia links in `PaginatedResponse<T>` for better API discoverability.

**Current State:**
- `PaginationLinks` class exists but is not integrated into `PaginatedResponse<T>`
- Links would provide navigation URLs (self, first, previous, next, last)

**Implementation:**
- Add `Links` property to `PaginatedResponse<T>`
- Generate URLs based on current request context
- Include links for: self, first, previous (if not first page), next (if not last page), last
- Improves REST API maturity level (Richardson Maturity Model Level 3)

**Benefits:**
- Better API discoverability
- Clients can navigate without building URLs
- Follows REST best practices
- Self-documenting API

### API Versioning
Implement versioning strategy for API endpoints.

**Options:**
- URL-based: `/api/v1/mediaassets`
- Header-based: `Api-Version: 1.0`
- Query string: `/api/mediaassets?api-version=1.0`

**Package:** `Asp.Versioning.Http`

### Authentication & Authorization
**JWT Authentication**
- Implement token-based authentication
- Add user registration and login endpoints
- Protect endpoints with `[Authorize]` attribute

**OAuth2 Integration**
- Support external identity providers (Google, Microsoft, GitHub)
- Use OAuth2/OpenID Connect
- Consider using Identity Server or Auth0

**Authorization**
- Implement role-based access control (RBAC)
- Add policy-based authorization
- Separate read/write permissions

### Caching
**In-Memory Caching**
- Cache frequently accessed data (e.g., popular assets)
- Use `IMemoryCache` for simple scenarios
- Implement cache invalidation strategy

**Distributed Caching (Redis)**
- For multi-instance deployments
- Share cache across API instances
- Better scalability
- Use `IDistributedCache` abstraction

### Rate Limiting
Prevent API abuse and ensure fair usage.

**Options:**
- IP-based rate limiting
- User/API key-based limits
- Endpoint-specific limits

**Package:** `AspNetCoreRateLimit` or built-in .NET 7+ rate limiting middleware

### Logging & Monitoring
**Request/Response Logging**
- Log incoming requests and outgoing responses
- Include headers, body (sanitized), response time
- Use middleware for automatic logging

**Correlation IDs**
- Generate unique ID for each request
- Pass through entire call chain
- Include in all log entries
- Makes debugging distributed systems easier

**Implementation:**
- Add correlation ID middleware
- Store in `HttpContext.Items`
- Include in response headers
- Log with structured logging

### Advanced Patterns
**CQRS (Command Query Responsibility Segregation)**
Consider implementing if write operations become complex.

**When to use:**
- Different read and write models needed
- Complex business logic on writes
- Different scaling requirements for reads vs writes

**Tools:**
- MediatR for command/query handling
- Separate read and write databases (optional)

**Domain Events**
Implement if you need to decouple domain logic and enable event-driven architecture.

**Use cases:**
- Trigger side effects after entity changes
- Audit logging
- Integration with external systems
- Notifications

**Implementation:**
- Add event publishing mechanism
- Create domain event handlers
- Consider event sourcing for audit trail

### Deployment
- Containerize with Docker
- Create docker-compose for local development
- Set up CI/CD pipeline (GitHub Actions)
- Deploy to Azure/AWS/GCP

### Monitoring
- Application Insights or similar APM tool
- Performance metrics
- Error tracking
- User analytics

### Database
- Add database migration strategy
- Implement soft deletes
- Add audit fields (CreatedAt, UpdatedAt, DeletedAt)
- Consider read replicas for scaling

### Security
- HTTPS enforcement
- CORS configuration
- Security headers (HSTS, CSP, X-Frame-Options)
- Input sanitization
- SQL injection prevention (already handled by EF Core)
- Secrets management (Azure Key Vault, AWS Secrets Manager)

## Notes

- Prioritize items based on actual needs
- Don't over-engineer early
- Add complexity only when necessary
- Keep the codebase maintainable
- Focus on delivering value first

**Last Updated:** 2025-01-28

# MonitoringGrid API Versioning Guide

## Overview
The MonitoringGrid API supports multiple versions to ensure backward compatibility while introducing new features and improvements. This guide explains the versioning strategy, available versions, and migration paths.

## Versioning Strategy

### Semantic Versioning
We follow semantic versioning principles:
- **Major Version**: Breaking changes that require client updates
- **Minor Version**: New features that are backward compatible
- **Patch Version**: Bug fixes and minor improvements

### URL-based Versioning
API versions are specified in the URL path:
```
/api/v{version}/endpoint
```

## Available API Versions

### Version 1.0 (Legacy)
**Base URL**: `/api/v1.0/`
**Status**: Maintenance mode
**Description**: Original API implementation with direct controller actions

**Key Features:**
- Direct database access through repositories
- Basic CRUD operations
- Simple error handling
- Limited validation

**Example Endpoints:**
```
GET    /api/v1.0/kpi
POST   /api/v1.0/kpi
PUT    /api/v1.0/kpi/{id}
DELETE /api/v1.0/kpi/{id}
```

**Deprecation Notice**: Version 1.0 will be deprecated in Q3 2025. Please migrate to v2.0 or v3.0.

### Version 2.0 (CQRS)
**Base URL**: `/api/v2.0/`
**Status**: Current stable
**Description**: CQRS-based implementation with MediatR

**Key Features:**
- Command Query Responsibility Segregation (CQRS)
- MediatR for command/query handling
- Domain events integration
- Enhanced validation with FluentValidation
- Performance monitoring
- Response caching
- Bulk operations

**Example Endpoints:**
```
GET    /api/v2.0/kpi                    # Query: GetKpisQuery
POST   /api/v2.0/kpi                    # Command: CreateKpiCommand
PUT    /api/v2.0/kpi/{id}               # Command: UpdateKpiCommand
DELETE /api/v2.0/kpi/{id}               # Command: DeleteKpiCommand
POST   /api/v2.0/kpi/{id}/execute       # Command: ExecuteKpiCommand
POST   /api/v2.0/kpi/bulk               # Command: BulkKpiOperationCommand
GET    /api/v2.0/kpi/dashboard          # Query: GetKpiDashboardQuery
```

**New Features in v2.0:**
- Bulk operations for multiple KPIs
- Enhanced filtering and pagination
- Real-time execution monitoring
- Improved error handling
- Domain event notifications

### Version 3.0 (Result Pattern)
**Base URL**: `/api/v3.0/`
**Status**: Preview/Beta
**Description**: Result Pattern implementation with functional error handling

**Key Features:**
- Result<T> pattern for functional error handling
- No exceptions for business logic errors
- Type-safe error handling
- Composable operations
- Enhanced type safety

**Example Endpoints:**
```
GET    /api/v3.0/kpi                    # Returns Result<List<KpiDto>>
POST   /api/v3.0/kpi                    # Returns Result<KpiDto>
PUT    /api/v3.0/kpi/{id}               # Returns Result<KpiDto>
DELETE /api/v3.0/kpi/{id}               # Returns Result<Unit>
```

**New Features in v3.0:**
- Functional error handling
- Composable operations
- Enhanced type safety
- Better error context
- Reduced exception overhead

## Version Comparison

| Feature | v1.0 | v2.0 | v3.0 |
|---------|------|------|------|
| CQRS | ❌ | ✅ | ✅ |
| Domain Events | ❌ | ✅ | ✅ |
| Result Pattern | ❌ | ❌ | ✅ |
| Bulk Operations | ❌ | ✅ | ✅ |
| Enhanced Validation | ❌ | ✅ | ✅ |
| Performance Monitoring | ❌ | ✅ | ✅ |
| Response Caching | ❌ | ✅ | ✅ |
| Real-time Events | ❌ | ✅ | ✅ |
| Functional Error Handling | ❌ | ❌ | ✅ |

## Migration Guide

### From v1.0 to v2.0

**Breaking Changes:**
- None - v2.0 maintains the same request/response DTOs

**Migration Steps:**
1. Update base URL from `/api/v1.0/` to `/api/v2.0/`
2. No code changes required for basic CRUD operations
3. Optionally leverage new features like bulk operations

**Example Migration:**
```javascript
// Before (v1.0)
const response = await fetch('/api/v1.0/kpi');

// After (v2.0)
const response = await fetch('/api/v2.0/kpi');
```

### From v2.0 to v3.0

**Breaking Changes:**
- Response format changes to Result<T> pattern
- Error handling changes

**Migration Steps:**
1. Update base URL from `/api/v2.0/` to `/api/v3.0/`
2. Update response handling to work with Result<T> pattern
3. Update error handling logic

**Example Migration:**
```javascript
// Before (v2.0)
try {
  const response = await fetch('/api/v2.0/kpi');
  const kpis = await response.json();
  // Handle kpis
} catch (error) {
  // Handle error
}

// After (v3.0)
const response = await fetch('/api/v3.0/kpi');
const result = await response.json();

if (result.isSuccess) {
  const kpis = result.value;
  // Handle kpis
} else {
  const error = result.error;
  // Handle error
}
```

## Content Negotiation

All API versions support the same content types:
- **Request**: `application/json`
- **Response**: `application/json`

## Authentication

All API versions use the same authentication mechanism:
- **Bearer Token**: JWT tokens in Authorization header
- **API Key**: Custom API key authentication (optional)

## Rate Limiting

Rate limiting applies across all API versions:
- **Default**: 1000 requests per hour per client
- **Authenticated**: 5000 requests per hour per user
- **Premium**: 10000 requests per hour per user

## Error Handling

### v1.0 Error Format
```json
{
  "message": "Error description",
  "statusCode": 400
}
```

### v2.0 Error Format
```json
{
  "type": "ValidationError",
  "title": "One or more validation errors occurred",
  "status": 400,
  "detail": "The request contains invalid data",
  "instance": "/api/v2.0/kpi",
  "errors": {
    "Frequency": ["Frequency must be greater than 0"]
  },
  "traceId": "0HN7GLLP5N1J7:00000001"
}
```

### v3.0 Error Format
```json
{
  "isSuccess": false,
  "value": null,
  "error": {
    "code": "ValidationError",
    "message": "One or more validation errors occurred",
    "details": {
      "Frequency": ["Frequency must be greater than 0"]
    }
  }
}
```

## Deprecation Policy

- **Notice Period**: 6 months advance notice for deprecation
- **Support Period**: 12 months support after deprecation notice
- **Migration Support**: Documentation and tools provided for migration

## Version Support Status

| Version | Status | Support Until | Recommended Action |
|---------|--------|---------------|-------------------|
| v1.0 | Maintenance | Q3 2025 | Migrate to v2.0 |
| v2.0 | Active | Ongoing | Current stable version |
| v3.0 | Preview | TBD | Evaluate for future use |

## Best Practices

1. **Always specify version** in your API calls
2. **Monitor deprecation notices** in API responses
3. **Test thoroughly** when migrating between versions
4. **Use the latest stable version** for new projects
5. **Plan migrations** well in advance of deprecation dates

## Getting Help

- **Documentation**: Available in Swagger UI for each version
- **Support**: Contact support@monitoringgrid.com
- **Migration Assistance**: Available for enterprise customers
- **Community**: GitHub discussions and issues

## Changelog

### v3.0.0-preview (Current)
- Added Result<T> pattern
- Functional error handling
- Enhanced type safety

### v2.0.0 (2024-12-01)
- CQRS implementation
- Domain events
- Enhanced validation
- Bulk operations
- Performance monitoring

### v1.0.0 (2024-06-01)
- Initial API release
- Basic CRUD operations
- Simple authentication

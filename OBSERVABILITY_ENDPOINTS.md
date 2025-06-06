# MonitoringGrid API - Observability Endpoints

## 🔍 **Available Observability Endpoints**

### **Health Checks**
```
GET /health
```
**Response**: Comprehensive health status including:
- Database connectivity
- KPI system health (active/stale KPIs, alerts)
- Database performance metrics
- External services status

**Example Response**:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database connection successful"
    },
    "kpi-system": {
      "status": "Healthy",
      "description": "KPI system healthy - 15 active KPIs",
      "data": {
        "activeKpis": 15,
        "staleKpis": 2,
        "recentAlerts": 1,
        "criticalAlerts": 0
      }
    },
    "database-performance": {
      "status": "Healthy",
      "description": "Database performance good: 45ms",
      "data": {
        "queryTimeMs": 45,
        "timestamp": "2024-01-15T01:34:56Z"
      }
    }
  }
}
```

### **Prometheus Metrics**
```
GET /metrics
```
**Response**: Prometheus-formatted metrics including:

#### **KPI Metrics**
- `monitoringgrid_kpi_executions_total` - Total KPI executions by name, status, owner
- `monitoringgrid_kpi_execution_duration_seconds` - KPI execution duration histogram
- `monitoringgrid_active_kpis` - Number of active KPIs
- `monitoringgrid_stale_kpis` - Number of stale KPIs

#### **Alert Metrics**
- `monitoringgrid_alerts_triggered_total` - Total alerts triggered by KPI, severity, owner
- `monitoringgrid_active_alerts` - Active alerts by severity
- `monitoringgrid_alert_resolution_duration_seconds` - Alert resolution time

#### **API Performance Metrics**
- `monitoringgrid_api_requests_total` - API requests by method, endpoint, status
- `monitoringgrid_api_request_duration_seconds` - API request duration
- `monitoringgrid_rate_limit_exceeded_total` - Rate limit violations

#### **Database Metrics**
- `monitoringgrid_database_query_duration_seconds` - Database query performance
- `monitoringgrid_database_errors_total` - Database errors by type

#### **Business Metrics**
- `monitoringgrid_system_health_score` - Overall system health (0-100)
- `monitoringgrid_bulk_operations_total` - Bulk operations by type and status

**Example Metrics Output**:
```
# HELP monitoringgrid_kpi_executions_total Total number of KPI executions
# TYPE monitoringgrid_kpi_executions_total counter
monitoringgrid_kpi_executions_total{kpi_name="TransactionSuccessRate",status="success",owner="admin"} 45

# HELP monitoringgrid_system_health_score Overall system health score (0-100)
# TYPE monitoringgrid_system_health_score gauge
monitoringgrid_system_health_score 87.5
```

---

## 🔧 **Enhanced API Features**

### **Performance Headers**
All API responses now include performance headers:
- `X-Response-Time-Ms`: Request processing time in milliseconds
- `X-Request-Id`: Unique request identifier for tracing

### **Rate Limiting Headers**
Rate-limited endpoints include:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Window`: Time window (e.g., "1m")

### **Structured Error Responses**
All errors now return consistent JSON format:
```json
{
  "statusCode": 404,
  "message": "Resource not found",
  "details": "KPI with ID 999 was not found",
  "traceId": "00-1234567890abcdef-fedcba0987654321-01",
  "timestamp": "2024-01-15T01:34:56Z"
}
```

---

## 📊 **Observability Integration Examples**

### **KPI Execution with Tracing**
```http
POST /api/v1/kpi/123/execute
```

**Generated Traces**:
- `ExecuteKpi` - Main execution span
- `ExecuteStoredProcedure` - Database operation span
- `ProcessAlert` - Alert processing span (if triggered)

**Generated Metrics**:
- KPI execution count and duration
- Database query performance
- Alert metrics (if triggered)

**Generated Logs**:
```
[INFO] KPI execution started: 123 - TransactionSuccessRate (Owner: admin) [TraceId: 00-abc123...]
[INFO] KPI execution completed successfully: 123 - TransactionSuccessRate in 1250ms. Result: Current: 94.50, Historical: 95.00, Deviation: 0.53% [TraceId: 00-abc123...]
```

### **Dashboard with Caching and Metrics**
```http
GET /api/v1/kpi/dashboard
```

**Features**:
- Response cached for 60 seconds
- Performance monitoring applied
- System health score calculated and recorded
- Dashboard aggregation traced

---

## 🚀 **Integration with Monitoring Tools**

### **Prometheus + Grafana**
1. Configure Prometheus to scrape `/metrics` endpoint
2. Import MonitoringGrid dashboard templates
3. Set up alerts based on system health score and KPI metrics

### **Application Performance Monitoring (APM)**
- OpenTelemetry traces can be exported to:
  - Jaeger
  - Zipkin
  - Azure Application Insights
  - AWS X-Ray
  - Google Cloud Trace

### **Log Aggregation**
- Structured logs with trace IDs for correlation
- Compatible with:
  - ELK Stack (Elasticsearch, Logstash, Kibana)
  - Splunk
  - Azure Monitor
  - AWS CloudWatch

---

## 📈 **Monitoring Best Practices**

### **Key Metrics to Monitor**
1. **System Health Score** - Overall system status
2. **KPI Execution Success Rate** - Business process health
3. **API Response Times** - User experience
4. **Database Query Performance** - Infrastructure health
5. **Alert Resolution Time** - Operational efficiency

### **Alerting Thresholds**
- System health score < 80
- KPI execution failure rate > 5%
- API response time > 2 seconds
- Database query time > 1 second
- Unresolved critical alerts > 5

### **Dashboard Recommendations**
1. **Executive Dashboard** - High-level KPIs and system health
2. **Operations Dashboard** - Real-time system performance
3. **Development Dashboard** - API performance and error rates
4. **Business Dashboard** - KPI trends and alert patterns

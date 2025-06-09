# Scheduler and KPI Types Implementation

## Overview

This implementation adds comprehensive scheduling capabilities and multiple KPI types to the MonitoringGrid system. The solution includes reusable React components, TypeScript utilities, and enhanced KPI management functionality.

## 🚀 **What's Been Implemented**

### 1. **Generic Scheduler Component** ✅
- **Location**: `src/components/Common/SchedulerComponent.tsx`
- **Features**:
  - Multiple schedule types: Interval, Cron, One-time
  - Visual cron expression builder with presets
  - Interval presets (1 min to 24 hours)
  - Advanced options (start/end dates, timezone)
  - Real-time validation and preview
  - Next execution time calculation

### 2. **KPI Type Selector Component** ✅
- **Location**: `src/components/Common/KpiTypeSelector.tsx`
- **Features**:
  - Four KPI types with detailed descriptions
  - Dynamic configuration based on selected type
  - Threshold and comparison operator settings
  - Built-in validation and recommendations
  - Usage examples and guidance

### 3. **Enhanced Type Definitions** ✅
- **Location**: `src/types/api.ts`
- **New Types**:
  - `ScheduleConfiguration` - Complete scheduling configuration
  - `ScheduleType` - Enum for schedule types
  - `KpiType` - Enum for KPI monitoring types
  - `KpiTypeDefinition` - Metadata for KPI types
  - Enhanced `KpiDto` and `CreateKpiRequest` with new fields

### 4. **Utility Functions** ✅
- **Scheduler Utils** (`src/utils/schedulerUtils.ts`):
  - Cron expression validation
  - Schedule description generation
  - Next execution calculation
  - Configuration validation
  - Common presets and patterns

- **KPI Type Utils** (`src/utils/kpiTypeUtils.ts`):
  - KPI type definitions and metadata
  - Configuration validation
  - Recommended settings
  - Example use cases
  - Comparison operators

### 5. **Enhanced KPI Creation Form** ✅
- **Location**: `src/pages/KPI/KpiCreate.tsx`
- **Enhancements**:
  - Integrated scheduler component
  - KPI type selector
  - Enhanced validation
  - Support for new fields
  - Backward compatibility

### 6. **Demo Page** ✅
- **Location**: `src/pages/Demo/SchedulerDemo.tsx`
- **Features**:
  - Interactive component demonstration
  - Real-time validation feedback
  - Configuration previews
  - Combined configuration display

## 📋 **KPI Types Implemented**

### 1. **Success Rate Monitoring**
- **Purpose**: Monitor success percentages vs historical averages
- **Use Cases**: Transaction success, API response rates, login success
- **Required Fields**: deviation, lastMinutes
- **Default SP**: `monitoring.usp_MonitorTransactions`

### 2. **Transaction Volume Monitoring**
- **Purpose**: Track transaction counts vs historical patterns
- **Use Cases**: Daily transactions, API calls, user registrations
- **Required Fields**: deviation, minimumThreshold, lastMinutes
- **Default SP**: `monitoring.usp_MonitorTransactionVolume`

### 3. **Threshold Monitoring**
- **Purpose**: Simple threshold-based alerts
- **Use Cases**: System resources, queue lengths, error counts
- **Required Fields**: thresholdValue, comparisonOperator
- **Default SP**: `monitoring.usp_MonitorThreshold`

### 4. **Trend Analysis**
- **Purpose**: Detect gradual changes and patterns
- **Use Cases**: Performance degradation, capacity planning
- **Required Fields**: deviation, lastMinutes
- **Default SP**: `monitoring.usp_MonitorTrends`

## 🔧 **Schedule Types Supported**

### 1. **Interval Scheduling**
- Fixed intervals from 1 minute to 1 week
- Quick presets for common intervals
- Human-readable descriptions

### 2. **Cron Expression Scheduling**
- Full cron expression support (5-field format)
- Common pattern presets
- Expression validation
- Human-readable descriptions

### 3. **One-Time Scheduling**
- Single execution at specified date/time
- Future date validation
- Timezone support

## 🎯 **Next Steps: Backend Implementation**

### 1. **Database Schema Updates**
```sql
-- Add new columns to KPIs table
ALTER TABLE monitoring.KPIs ADD 
    KpiType NVARCHAR(50) DEFAULT 'success_rate',
    ScheduleConfiguration NVARCHAR(MAX), -- JSON
    ThresholdValue DECIMAL(18,2) NULL,
    ComparisonOperator NVARCHAR(10) NULL;

-- Create KPI Types lookup table
CREATE TABLE monitoring.KpiTypes (
    KpiTypeId NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    RequiredFields NVARCHAR(MAX), -- JSON array
    DefaultStoredProcedure NVARCHAR(255)
);
```

### 2. **New Stored Procedures**
Create the missing stored procedures:
- `monitoring.usp_MonitorTransactionVolume`
- `monitoring.usp_MonitorThreshold`
- `monitoring.usp_MonitorTrends`

### 3. **Enhanced KPI Execution Service**
```csharp
// Add to IKpiExecutionService
Task<KpiExecutionResult> ExecuteKpiByTypeAsync(KPI kpi, CancellationToken cancellationToken);
bool EvaluateThreshold(decimal value, decimal threshold, string comparisonOperator);
Task<TrendAnalysisResult> AnalyzeTrendAsync(KPI kpi, CancellationToken cancellationToken);
```

### 4. **Scheduling Service Integration**
```csharp
// Add Quartz.NET integration
public interface IKpiSchedulingService
{
    Task ScheduleKpiAsync(KPI kpi);
    Task UpdateKpiScheduleAsync(KPI kpi);
    Task UnscheduleKpiAsync(int kpiId);
    Task<List<ScheduledKpiInfo>> GetScheduledKpisAsync();
}
```

### 5. **API Controller Updates**
```csharp
// Add to KpiController
[HttpPost("{id}/schedule")]
public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleConfiguration schedule);

[HttpGet("types")]
public async Task<IActionResult> GetKpiTypes();

[HttpPost("{id}/test-threshold")]
public async Task<IActionResult> TestThreshold(int id, [FromBody] ThresholdTestRequest request);
```

## 🧪 **Testing the Implementation**

### 1. **Component Testing**
```bash
# Navigate to frontend directory
cd MonitoringGrid.Frontend

# Install dependencies (if needed)
npm install

# Start development server
npm run dev
```

### 2. **Access Demo Page**
- Navigate to `/demo/scheduler` (you'll need to add the route)
- Test different schedule configurations
- Try various KPI types
- Validate configurations

### 3. **Integration Testing**
- Test KPI creation with new fields
- Verify form validation
- Check data persistence (once backend is updated)

## 📚 **Usage Examples**

### 1. **Creating a Transaction Volume KPI**
```typescript
const kpiConfig = {
  indicator: "Daily Transaction Volume",
  kpiType: KpiType.TransactionVolume,
  scheduleConfiguration: {
    scheduleType: ScheduleType.Cron,
    cronExpression: "0 9 * * *", // Daily at 9 AM
    isEnabled: true,
    timezone: "UTC"
  },
  thresholdValue: 1000,
  comparisonOperator: "lt", // Alert if less than 1000
  deviation: 20, // 20% deviation tolerance
  minimumThreshold: 100
};
```

### 2. **Creating a Threshold-Based KPI**
```typescript
const kpiConfig = {
  indicator: "CPU Usage Monitor",
  kpiType: KpiType.Threshold,
  scheduleConfiguration: {
    scheduleType: ScheduleType.Interval,
    intervalMinutes: 5,
    isEnabled: true
  },
  thresholdValue: 80,
  comparisonOperator: "gt" // Alert if greater than 80%
};
```

## 🔍 **Key Features**

### ✅ **Implemented**
- Generic, reusable scheduler component
- Multiple KPI types with validation
- Enhanced type safety with TypeScript
- Comprehensive utility functions
- Interactive demo page
- Backward compatibility
- Extensive documentation

### 🚧 **Requires Backend Work**
- Database schema updates
- New stored procedures
- Enhanced execution service
- Quartz.NET scheduling integration
- API endpoint updates

## 🎉 **Benefits**

1. **Flexibility**: Support for multiple monitoring patterns
2. **Usability**: Intuitive UI with presets and validation
3. **Scalability**: Easy to add new KPI types and schedule patterns
4. **Maintainability**: Well-structured, documented code
5. **Type Safety**: Full TypeScript support
6. **Reusability**: Generic components for future use

This implementation provides a solid foundation for advanced KPI monitoring with flexible scheduling capabilities. The frontend is complete and ready for integration with the enhanced backend services.

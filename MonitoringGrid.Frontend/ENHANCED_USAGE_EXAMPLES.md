# Enhanced Frontend Integration - Usage Examples

## 🚀 **Ready-to-Use Enhanced Features**

### 1. System Analytics Dashboard

```typescript
import React from 'react';
import { useSystemAnalytics, useSystemHealth } from '@/hooks/useEnhancedApi';

const EnhancedSystemDashboard = () => {
  const { data: analytics, loading } = useSystemAnalytics(30);
  const { data: health } = useSystemHealth(true, 30000);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <h1>System Analytics (Last 30 Days)</h1>
      
      {/* System Health */}
      <div>
        <h2>System Health: {health?.overallHealthScore?.toFixed(1)}%</h2>
        <p>Status: {health?.systemStatus}</p>
        <p>Issues: {health?.issues?.length || 0}</p>
      </div>

      {/* Key Metrics */}
      <div>
        <h2>Key Metrics</h2>
        <p>Total KPIs: {analytics?.totalKpis}</p>
        <p>Active KPIs: {analytics?.activeKpis}</p>
        <p>Total Executions: {analytics?.totalExecutions}</p>
        <p>Total Alerts: {analytics?.totalAlerts}</p>
        <p>Resolution Rate: {analytics?.totalAlerts ? 
          ((analytics.resolvedAlerts / analytics.totalAlerts) * 100).toFixed(1) : 0}%</p>
      </div>

      {/* Top Performing KPIs */}
      <div>
        <h2>Top Performing KPIs</h2>
        {analytics?.topPerformingKpis?.map((kpi, index) => (
          <div key={index}>
            <strong>{kpi.indicator}</strong> - Score: {kpi.performanceScore.toFixed(1)}
            <br />Owner: {kpi.owner}
          </div>
        ))}
      </div>

      {/* System Recommendations */}
      {health?.recommendations && health.recommendations.length > 0 && (
        <div>
          <h2>System Recommendations</h2>
          {health.recommendations.map((rec, index) => (
            <div key={index} style={{ padding: '8px', backgroundColor: '#e3f2fd', margin: '4px 0' }}>
              {rec}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
```

### 2. Real-time Monitoring Component

```typescript
import React, { useEffect, useState } from 'react';
import { useRealtimeStatus, useLiveDashboard } from '@/hooks/useEnhancedApi';
import { useSignalR } from '@/services/signalRService';

const RealTimeMonitor = () => {
  const { data: status } = useRealtimeStatus(true, 5000);
  const { data: dashboard } = useLiveDashboard(true, 10000);
  const { isConnected, on, off } = useSignalR();
  const [realtimeEvents, setRealtimeEvents] = useState<any[]>([]);

  useEffect(() => {
    const handleStatusUpdate = (newStatus: any) => {
      setRealtimeEvents(prev => [{
        id: Date.now(),
        type: 'status_update',
        timestamp: new Date(),
        message: `Status updated - ${newStatus.activeKpis} active KPIs`
      }, ...prev.slice(0, 9)]);
    };

    const handleKpiExecuted = (data: any) => {
      setRealtimeEvents(prev => [{
        id: Date.now(),
        type: 'kpi_executed',
        timestamp: new Date(),
        message: `KPI executed: ${data.Indicator} - ${data.Result?.isSuccessful ? 'Success' : 'Failed'}`
      }, ...prev.slice(0, 9)]);
    };

    if (isConnected) {
      on('onStatusUpdate', handleStatusUpdate);
      on('onKpiExecuted', handleKpiExecuted);
    }

    return () => {
      off('onStatusUpdate');
      off('onKpiExecuted');
    };
  }, [isConnected, on, off]);

  return (
    <div>
      <h1>Real-time Monitoring</h1>
      
      {/* Connection Status */}
      <div style={{ 
        padding: '8px', 
        backgroundColor: isConnected ? '#e8f5e8' : '#ffeaea',
        marginBottom: '16px'
      }}>
        Status: {isConnected ? '🟢 Connected' : '🔴 Disconnected'}
      </div>

      {/* Real-time Metrics */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px', marginBottom: '16px' }}>
        <div>
          <h3>Active KPIs</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{status?.activeKpis || 0}</div>
        </div>
        <div>
          <h3>Due KPIs</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{status?.dueKpis || 0}</div>
        </div>
        <div>
          <h3>Recent Alerts</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{status?.recentAlerts || 0}</div>
        </div>
        <div>
          <h3>System Load</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{status?.systemLoad?.toFixed(1) || 0}%</div>
        </div>
      </div>

      {/* Recent Executions */}
      <div style={{ marginBottom: '16px' }}>
        <h2>Recent Executions</h2>
        {dashboard?.recentExecutions?.slice(0, 5).map((execution, index) => (
          <div key={index} style={{ 
            padding: '8px', 
            border: '1px solid #ddd', 
            marginBottom: '4px',
            backgroundColor: execution.isSuccessful ? '#e8f5e8' : '#ffeaea'
          }}>
            <strong>{execution.indicator}</strong> - 
            {execution.isSuccessful ? ' ✅ Success' : ' ❌ Failed'} - 
            {execution.executionTimeMs}ms - 
            {new Date(execution.timestamp).toLocaleTimeString()}
          </div>
        ))}
      </div>

      {/* Live Event Stream */}
      <div>
        <h2>Live Events</h2>
        {realtimeEvents.map((event) => (
          <div key={event.id} style={{ 
            padding: '8px', 
            border: '1px solid #ddd', 
            marginBottom: '4px',
            backgroundColor: '#f5f5f5'
          }}>
            <strong>{event.type}</strong> - {event.message} - 
            <small>{event.timestamp.toLocaleTimeString()}</small>
          </div>
        ))}
        {realtimeEvents.length === 0 && (
          <div style={{ padding: '16px', textAlign: 'center', color: '#666' }}>
            Waiting for real-time events...
          </div>
        )}
      </div>
    </div>
  );
};
```

### 3. Enhanced KPI Management

```typescript
import React from 'react';
import { useKpis, useKpiPerformanceAnalytics, useRealtimeKpiExecution } from '@/hooks/useEnhancedApi';

const EnhancedKpiManager = () => {
  const { data: kpis, loading, executeKpi } = useKpis();
  const { executeKpi: executeRealtime, loading: executingRealtime } = useRealtimeKpiExecution();
  const [selectedKpiId, setSelectedKpiId] = React.useState<number | null>(null);
  
  const { data: analytics } = useKpiPerformanceAnalytics(selectedKpiId || 0, 30);

  const handleExecuteKpi = async (kpiId: number, realtime: boolean = false) => {
    try {
      if (realtime) {
        await executeRealtime(kpiId);
      } else {
        await executeKpi(kpiId);
      }
      alert('KPI executed successfully!');
    } catch (error) {
      alert('Failed to execute KPI: ' + error);
    }
  };

  if (loading) return <div>Loading KPIs...</div>;

  return (
    <div>
      <h1>Enhanced KPI Management</h1>
      
      {/* KPI List */}
      <div style={{ marginBottom: '24px' }}>
        <h2>KPIs ({kpis?.length || 0})</h2>
        {kpis?.map((kpi) => (
          <div key={kpi.kpiId} style={{ 
            border: '1px solid #ddd', 
            padding: '16px', 
            marginBottom: '8px',
            backgroundColor: kpi.isActive ? '#f9f9f9' : '#f0f0f0'
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3>{kpi.indicator}</h3>
                <p>Owner: {kpi.owner}</p>
                <p>Frequency: {kpi.frequency} min | Priority: {kpi.priority === 1 ? 'SMS + Email' : 'Email Only'}</p>
                <p>Status: {kpi.isActive ? '🟢 Active' : '🔴 Inactive'}</p>
                {kpi.lastRun && <p>Last Run: {new Date(kpi.lastRun).toLocaleString()}</p>}
              </div>
              
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <button 
                  onClick={() => handleExecuteKpi(kpi.kpiId)}
                  disabled={executingRealtime}
                  style={{ padding: '8px 16px' }}
                >
                  Execute
                </button>
                <button 
                  onClick={() => handleExecuteKpi(kpi.kpiId, true)}
                  disabled={executingRealtime}
                  style={{ padding: '8px 16px', backgroundColor: '#1976d2', color: 'white' }}
                >
                  Real-time Execute
                </button>
                <button 
                  onClick={() => setSelectedKpiId(kpi.kpiId)}
                  style={{ padding: '8px 16px' }}
                >
                  View Analytics
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* KPI Analytics */}
      {selectedKpiId && analytics && (
        <div style={{ border: '2px solid #1976d2', padding: '16px', backgroundColor: '#f5f5f5' }}>
          <h2>Analytics for KPI {selectedKpiId}</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
            <div>
              <h4>Total Executions</h4>
              <div style={{ fontSize: '20px', fontWeight: 'bold' }}>{analytics.totalExecutions}</div>
            </div>
            <div>
              <h4>Success Rate</h4>
              <div style={{ fontSize: '20px', fontWeight: 'bold', color: '#4caf50' }}>
                {analytics.successRate.toFixed(1)}%
              </div>
            </div>
            <div>
              <h4>Total Alerts</h4>
              <div style={{ fontSize: '20px', fontWeight: 'bold', color: '#f44336' }}>
                {analytics.totalAlerts}
              </div>
            </div>
            <div>
              <h4>Performance Score</h4>
              <div style={{ fontSize: '20px', fontWeight: 'bold', color: '#1976d2' }}>
                {analytics.performanceScore.toFixed(1)}
              </div>
            </div>
          </div>
          
          <div style={{ marginTop: '16px' }}>
            <h4>Trend Direction: {analytics.trendDirection}</h4>
            <p>Average Deviation: {analytics.averageDeviation.toFixed(2)}%</p>
            <p>Average Execution Time: {analytics.averageExecutionTime.toFixed(0)}ms</p>
          </div>

          {analytics.recommendations.length > 0 && (
            <div style={{ marginTop: '16px' }}>
              <h4>Recommendations:</h4>
              {analytics.recommendations.map((rec, index) => (
                <div key={index} style={{ 
                  padding: '8px', 
                  backgroundColor: '#e3f2fd', 
                  margin: '4px 0',
                  borderLeft: '4px solid #1976d2'
                }}>
                  {rec}
                </div>
              ))}
            </div>
          )}
          
          <button 
            onClick={() => setSelectedKpiId(null)}
            style={{ marginTop: '16px', padding: '8px 16px' }}
          >
            Close Analytics
          </button>
        </div>
      )}
    </div>
  );
};
```

### 4. Enhanced Alert Management

```typescript
import React from 'react';
import { useCriticalAlerts, useUnresolvedAlerts, useManualAlert } from '@/hooks/useEnhancedApi';

const EnhancedAlertManager = () => {
  const { data: criticalAlerts } = useCriticalAlerts(true, 15000);
  const { data: unresolvedAlerts } = useUnresolvedAlerts(true, 20000);
  const { sendAlert, loading: sendingAlert } = useManualAlert();

  const handleSendManualAlert = async () => {
    try {
      const result = await sendAlert(
        1, // KPI ID
        'Manual test alert',
        'This is a test alert sent from the frontend',
        2 // Priority
      );
      alert(`Alert sent successfully! Alert ID: ${result.alertId}`);
    } catch (error) {
      alert('Failed to send alert: ' + error);
    }
  };

  return (
    <div>
      <h1>Enhanced Alert Management</h1>
      
      {/* Quick Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', marginBottom: '24px' }}>
        <div style={{ padding: '16px', border: '1px solid #f44336', backgroundColor: '#ffebee' }}>
          <h3>Critical Alerts</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#f44336' }}>
            {criticalAlerts?.length || 0}
          </div>
          <p>Require immediate attention</p>
        </div>
        <div style={{ padding: '16px', border: '1px solid #ff9800', backgroundColor: '#fff3e0' }}>
          <h3>Unresolved Alerts</h3>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#ff9800' }}>
            {unresolvedAlerts?.length || 0}
          </div>
          <p>Pending resolution</p>
        </div>
        <div style={{ padding: '16px', border: '1px solid #1976d2', backgroundColor: '#e3f2fd' }}>
          <h3>Manual Alert</h3>
          <button 
            onClick={handleSendManualAlert}
            disabled={sendingAlert}
            style={{ 
              padding: '8px 16px', 
              backgroundColor: '#1976d2', 
              color: 'white',
              border: 'none',
              borderRadius: '4px'
            }}
          >
            {sendingAlert ? 'Sending...' : 'Send Test Alert'}
          </button>
        </div>
      </div>

      {/* Critical Alerts */}
      {criticalAlerts && criticalAlerts.length > 0 && (
        <div style={{ marginBottom: '24px' }}>
          <h2 style={{ color: '#f44336' }}>🚨 Critical Alerts</h2>
          {criticalAlerts.map((alert, index) => (
            <div key={index} style={{ 
              padding: '16px', 
              border: '2px solid #f44336', 
              backgroundColor: '#ffebee',
              marginBottom: '8px'
            }}>
              <h4>{alert.message}</h4>
              <p>KPI: {alert.kpiIndicator} | Owner: {alert.kpiOwner}</p>
              <p>Triggered: {new Date(alert.triggerTime).toLocaleString()}</p>
              {alert.deviationPercent && (
                <p>Deviation: {Math.abs(alert.deviationPercent).toFixed(2)}%</p>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Unresolved Alerts */}
      {unresolvedAlerts && unresolvedAlerts.length > 0 && (
        <div>
          <h2 style={{ color: '#ff9800' }}>⚠️ Unresolved Alerts</h2>
          {unresolvedAlerts.map((alert, index) => (
            <div key={index} style={{ 
              padding: '16px', 
              border: '1px solid #ff9800', 
              backgroundColor: '#fff3e0',
              marginBottom: '8px'
            }}>
              <h4>{alert.message}</h4>
              <p>KPI: {alert.kpiIndicator} | Owner: {alert.kpiOwner}</p>
              <p>Triggered: {new Date(alert.triggerTime).toLocaleString()}</p>
              {alert.deviationPercent && (
                <p>Deviation: {Math.abs(alert.deviationPercent).toFixed(2)}%</p>
              )}
            </div>
          ))}
        </div>
      )}

      {/* No Alerts */}
      {(!criticalAlerts || criticalAlerts.length === 0) && 
       (!unresolvedAlerts || unresolvedAlerts.length === 0) && (
        <div style={{ 
          padding: '32px', 
          textAlign: 'center', 
          backgroundColor: '#e8f5e8',
          border: '1px solid #4caf50'
        }}>
          <h2 style={{ color: '#4caf50' }}>✅ All Clear!</h2>
          <p>No critical or unresolved alerts at this time.</p>
        </div>
      )}
    </div>
  );
};
```

## 🎯 **Usage Instructions**

1. **Import the hooks** in your React components
2. **Use the data** returned by the hooks
3. **Handle loading states** appropriately
4. **Set up SignalR** for real-time updates
5. **Configure refresh intervals** based on your needs

## 🚀 **Key Benefits**

- **Type Safety**: Full TypeScript support
- **Real-time Updates**: Automatic data refresh and SignalR events
- **Error Handling**: Comprehensive error management
- **Performance**: Optimized with configurable refresh intervals
- **Scalability**: Ready for enterprise-scale deployments

These examples demonstrate the **full power of our enhanced frontend integration** - providing enterprise-grade monitoring capabilities with real-time updates and comprehensive analytics! 🎉

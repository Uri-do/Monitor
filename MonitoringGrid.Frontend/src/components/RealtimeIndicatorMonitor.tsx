import React, { useState, useEffect } from 'react';
import { Box, Card, CardContent, Typography, Chip, LinearProgress, Alert, Button } from '@mui/material';
import { PlayArrow, CheckCircle, Error, AccessTime, Refresh } from '@mui/icons-material';
import { signalRService } from '@/services/signalRService';
import { useRealtime } from '@/contexts/RealtimeContext';
import type { IndicatorExecutionStarted, IndicatorExecutionCompleted } from '@/services/signalRService';

interface IndicatorExecution {
  indicatorId: number;
  indicatorName: string;
  owner: string;
  startTime: string;
  status: 'running' | 'completed' | 'failed';
  duration?: number;
  value?: number;
  errorMessage?: string;
  thresholdBreached?: boolean;
}

const RealtimeIndicatorMonitor: React.FC = () => {
  const [executions, setExecutions] = useState<IndicatorExecution[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<string>('Disconnected');
  const [connectionId, setConnectionId] = useState<string | null>(null);
  const [lastError, setLastError] = useState<string | null>(null);
  const { isEnabled, isConnected, enableRealtime } = useRealtime();

  useEffect(() => {
    console.log('🔧 RealtimeIndicatorMonitor: Setting up SignalR connection...');

    // Monitor connection status
    const handleConnectionStateChanged = (state: string) => {
      console.log('🔧 RealtimeIndicatorMonitor: Connection state changed to:', state);
      setConnectionStatus(state);
      setConnectionId(signalRService.getConnectionId());
      if (state === 'Connected') {
        setLastError(null);
      }
    };

    // Handle indicator execution started
    const handleIndicatorExecutionStarted = (data: IndicatorExecutionStarted) => {
      console.log('🚀 RealtimeIndicatorMonitor: Indicator execution started:', data);

      setExecutions(prev => {
        // Remove any existing execution for this indicator
        const filtered = prev.filter(exec => exec.indicatorId !== data.indicatorID);

        // Add new execution
        return [...filtered, {
          indicatorId: data.indicatorID,
          indicatorName: data.indicatorName,
          owner: data.owner,
          startTime: data.startTime,
          status: 'running'
        }];
      });
    };

    // Handle indicator execution completed
    const handleIndicatorExecutionCompleted = (data: IndicatorExecutionCompleted) => {
      console.log('✅ RealtimeIndicatorMonitor: Indicator execution completed:', data);

      setExecutions(prev =>
        prev.map(exec =>
          exec.indicatorId === data.indicatorId
            ? {
                ...exec,
                status: data.success ? 'completed' : 'failed',
                duration: data.duration,
                value: data.value,
                errorMessage: data.errorMessage,
                thresholdBreached: data.thresholdBreached
              }
            : exec
        )
      );

      // Remove completed executions after 10 seconds
      setTimeout(() => {
        setExecutions(prev => prev.filter(exec => exec.indicatorId !== data.indicatorId));
      }, 10000);
    };

    // Set up SignalR event handlers
    console.log('🔧 RealtimeIndicatorMonitor: Setting up event handlers...');
    signalRService.on('onConnectionStateChanged', handleConnectionStateChanged);
    signalRService.on('onIndicatorExecutionStarted', handleIndicatorExecutionStarted);
    signalRService.on('onIndicatorExecutionCompleted', handleIndicatorExecutionCompleted);

    // Start SignalR connection via RealtimeContext
    console.log('🔧 RealtimeIndicatorMonitor: Starting SignalR connection via RealtimeContext...');
    enableRealtime()
      .then(() => {
        console.log('✅ RealtimeIndicatorMonitor: SignalR connection started successfully via RealtimeContext');
        console.log('🔧 RealtimeIndicatorMonitor: Connection state:', signalRService.getConnectionState());
        console.log('🔧 RealtimeIndicatorMonitor: Connection ID:', signalRService.getConnectionId());
        setConnectionStatus(signalRService.getConnectionState());
        setConnectionId(signalRService.getConnectionId());
      })
      .catch(error => {
        console.error('❌ RealtimeIndicatorMonitor: Failed to start SignalR connection via RealtimeContext:', error);
        setLastError(error.message || 'Connection failed');
        setConnectionStatus('Failed');
      });

    // Cleanup
    return () => {
      console.log('🔧 RealtimeIndicatorMonitor: Cleaning up event handlers...');
      signalRService.off('onConnectionStateChanged');
      signalRService.off('onIndicatorExecutionStarted');
      signalRService.off('onIndicatorExecutionCompleted');
    };
  }, []);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running':
        return <PlayArrow color="primary" />;
      case 'completed':
        return <CheckCircle color="success" />;
      case 'failed':
        return <Error color="error" />;
      default:
        return <AccessTime />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'running':
        return 'primary';
      case 'completed':
        return 'success';
      case 'failed':
        return 'error';
      default:
        return 'default';
    }
  };

  const handleManualConnect = async () => {
    console.log('🔧 Manual connection test started...');
    setLastError(null);
    try {
      // Use RealtimeContext to enable connection
      await enableRealtime();
      console.log('✅ Manual connection successful via RealtimeContext');
    } catch (error: any) {
      console.error('❌ Manual connection failed:', error);
      setLastError(error.message || 'Manual connection failed');
    }
  };

  const handleTestConnection = async () => {
    console.log('🔧 Testing SignalR connection...');
    try {
      // Set up test message handler
      const handleTestMessage = (data: any) => {
        console.log('✅ Received test message:', data);
        alert(`Test successful! Connection ID: ${data.connectionId}`);
      };

      signalRService.on('onTestMessage', handleTestMessage);

      // Call test method on hub
      await signalRService.invoke('TestConnection');

      // Clean up handler after a delay
      setTimeout(() => {
        signalRService.off('onTestMessage');
      }, 5000);
    } catch (error: any) {
      console.error('❌ Test connection failed:', error);
      alert(`Test failed: ${error.message}`);
    }
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Real-time Indicator Monitor
        </Typography>
        
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          <Typography variant="body2">SignalR Status:</Typography>
          <Chip
            label={connectionStatus}
            color={connectionStatus === 'Connected' ? 'success' : 'error'}
            size="small"
          />
        </Box>

        <Box display="flex" alignItems="center" gap={1} mb={1}>
          <Typography variant="body2" color="text.secondary">
            RealtimeContext: Enabled={isEnabled.toString()}, Connected={isConnected.toString()}
          </Typography>
        </Box>

        {connectionId && (
          <Box display="flex" alignItems="center" gap={1} mb={1}>
            <Typography variant="body2" color="text.secondary">
              Connection ID: {connectionId.substring(0, 8)}...
            </Typography>
          </Box>
        )}

        {lastError && (
          <Box mb={2}>
            <Typography variant="body2" color="error.main">
              Error: {lastError}
            </Typography>
          </Box>
        )}

        {connectionStatus !== 'Connected' && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            SignalR is not connected. Real-time updates will not be available.
            <Box sx={{ mt: 1, display: 'flex', gap: 1 }}>
              <Button
                size="small"
                startIcon={<Refresh />}
                onClick={handleManualConnect}
                variant="outlined"
              >
                Retry Connection
              </Button>
              <Button
                size="small"
                onClick={handleTestConnection}
                variant="outlined"
                color="secondary"
              >
                Test Connection
              </Button>
            </Box>
          </Alert>
        )}

        {executions.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No indicator executions in progress. Start an indicator to see real-time updates.
          </Typography>
        ) : (
          <Box>
            {executions.map((execution) => (
              <Box key={execution.indicatorId} sx={{ mb: 2, p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                <Box display="flex" alignItems="center" gap={1} mb={1}>
                  {getStatusIcon(execution.status)}
                  <Typography variant="subtitle2">
                    {execution.indicatorName}
                  </Typography>
                  <Chip 
                    label={execution.status} 
                    color={getStatusColor(execution.status) as any}
                    size="small"
                  />
                </Box>
                
                <Typography variant="body2" color="text.secondary">
                  Owner: {execution.owner}
                </Typography>
                
                <Typography variant="body2" color="text.secondary">
                  Started: {new Date(execution.startTime).toLocaleTimeString()}
                </Typography>

                {execution.status === 'running' && (
                  <Box sx={{ mt: 1 }}>
                    <LinearProgress />
                    <Typography variant="caption" color="text.secondary">
                      Executing...
                    </Typography>
                  </Box>
                )}

                {execution.status === 'completed' && (
                  <Box sx={{ mt: 1 }}>
                    <Typography variant="body2" color="success.main">
                      ✅ Completed in {execution.duration}ms
                    </Typography>
                    {execution.value !== undefined && (
                      <Typography variant="body2">
                        Value: {execution.value}
                      </Typography>
                    )}
                    {execution.thresholdBreached && (
                      <Typography variant="body2" color="warning.main">
                        ⚠️ Threshold breached
                      </Typography>
                    )}
                  </Box>
                )}

                {execution.status === 'failed' && (
                  <Box sx={{ mt: 1 }}>
                    <Typography variant="body2" color="error.main">
                      ❌ Failed after {execution.duration}ms
                    </Typography>
                    {execution.errorMessage && (
                      <Typography variant="body2" color="error.main">
                        Error: {execution.errorMessage}
                      </Typography>
                    )}
                  </Box>
                )}
              </Box>
            ))}
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default RealtimeIndicatorMonitor;

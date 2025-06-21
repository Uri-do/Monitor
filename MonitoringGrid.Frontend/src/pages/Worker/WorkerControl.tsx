import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Alert,
  Chip,
  CircularProgress,
  Divider,
  Stack,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Refresh as RefreshIcon,
  Circle as StatusIcon,
} from '@mui/icons-material';
import { toast } from 'react-toastify';
import { workerApi } from '@/services/api';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';

interface WorkerStatus {
  isRunning: boolean;
  processId?: number;
  status: string;
  message: string;
  uptime?: string;
  lastActivity?: string;
  processedCount?: number;
  successCount?: number;
  failureCount?: number;
}

const WorkerControl: React.FC = () => {
  const [workerStatus, setWorkerStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Use real-time dashboard hook for live updates
  const { workerStatus: realtimeWorkerStatus, runningIndicators, isConnected } = useRealtimeDashboard();

  // Fetch worker status
  const fetchWorkerStatus = async () => {
    try {
      setLoading(true);
      setError(null);
      const status = await workerApi.getStatus(true, true, false);
      
      if (status) {
        setWorkerStatus({
          isRunning: status.isRunning || false,
          processId: status.processId,
          status: status.status || 'Unknown',
          message: status.message || 'No status available',
          uptime: status.uptime,
          lastActivity: status.lastActivity,
          processedCount: status.processedCount || 0,
          successCount: status.successCount || 0,
          failureCount: status.failureCount || 0,
        });
      } else {
        setWorkerStatus({
          isRunning: false,
          status: 'Stopped',
          message: 'Worker is not running',
        });
      }
    } catch (err: any) {
      console.error('Failed to fetch worker status:', err);
      setError(err.message || 'Failed to fetch worker status');
      setWorkerStatus({
        isRunning: false,
        status: 'Error',
        message: 'Failed to get status',
      });
    } finally {
      setLoading(false);
    }
  };

  // Start worker
  const handleStart = async () => {
    try {
      setActionLoading('start');
      const result = await workerApi.start();
      
      if (result.success) {
        toast.success(result.message || 'Worker started successfully');
        // Refresh status after a short delay
        setTimeout(fetchWorkerStatus, 2000);
      } else {
        toast.error(result.message || 'Failed to start worker');
      }
    } catch (err: any) {
      console.error('Failed to start worker:', err);
      toast.error(err.message || 'Failed to start worker');
    } finally {
      setActionLoading(null);
    }
  };

  // Stop worker
  const handleStop = async () => {
    try {
      setActionLoading('stop');
      const result = await workerApi.stop();
      
      if (result.success) {
        toast.success(result.message || 'Worker stopped successfully');
        // Refresh status after a short delay
        setTimeout(fetchWorkerStatus, 2000);
      } else {
        toast.error(result.message || 'Failed to stop worker');
      }
    } catch (err: any) {
      console.error('Failed to stop worker:', err);
      toast.error(err.message || 'Failed to stop worker');
    } finally {
      setActionLoading(null);
    }
  };

  // Restart worker
  const handleRestart = async () => {
    try {
      setActionLoading('restart');
      const result = await workerApi.restart();
      
      if (result.success) {
        toast.success(result.message || 'Worker restarted successfully');
        // Refresh status after a short delay
        setTimeout(fetchWorkerStatus, 3000);
      } else {
        toast.error(result.message || 'Failed to restart worker');
      }
    } catch (err: any) {
      console.error('Failed to restart worker:', err);
      toast.error(err.message || 'Failed to restart worker');
    } finally {
      setActionLoading(null);
    }
  };

  // Initial load and periodic refresh
  useEffect(() => {
    fetchWorkerStatus();
    
    // Refresh every 30 seconds (rely on SignalR for real-time updates)
    const interval = setInterval(fetchWorkerStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  // Update status from real-time data if available
  useEffect(() => {
    if (realtimeWorkerStatus && realtimeWorkerStatus.services?.length > 0) {
      const indicatorService = realtimeWorkerStatus.services.find(s => s.name === 'IndicatorMonitoringWorker');
      if (indicatorService) {
        setWorkerStatus(prev => ({
          ...prev,
          isRunning: indicatorService.status === 'Running',
          status: indicatorService.status,
          uptime: indicatorService.uptime,
          lastActivity: indicatorService.lastActivity,
          processedCount: indicatorService.processedCount,
          successCount: indicatorService.successCount,
          failureCount: indicatorService.failureCount,
          message: indicatorService.description || prev?.message || 'Worker service status',
        }));
      }
    }
  }, [realtimeWorkerStatus]);

  const getStatusColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'running':
        return 'success';
      case 'stopped':
        return 'default';
      case 'error':
        return 'error';
      default:
        return 'warning';
    }
  };

  const getStatusIcon = (isRunning: boolean) => {
    return (
      <StatusIcon 
        sx={{ 
          color: isRunning ? 'success.main' : 'grey.500',
          fontSize: 16,
          mr: 1
        }} 
      />
    );
  };

  if (loading && !workerStatus) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Worker Control
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Simple control panel for starting and stopping worker processes
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Worker Status Card */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Worker Status
              </Typography>
              
              {workerStatus && (
                <Stack spacing={2}>
                  <Box display="flex" alignItems="center">
                    {getStatusIcon(workerStatus.isRunning)}
                    <Chip 
                      label={workerStatus.status}
                      color={getStatusColor(workerStatus.status)}
                      size="small"
                    />
                    {isConnected && (
                      <Chip 
                        label="Live"
                        color="success"
                        size="small"
                        sx={{ ml: 1 }}
                      />
                    )}
                  </Box>
                  
                  <Typography variant="body2" color="text.secondary">
                    {workerStatus.message}
                  </Typography>

                  {workerStatus.processId && (
                    <Typography variant="body2">
                      Process ID: {workerStatus.processId}
                    </Typography>
                  )}

                  {workerStatus.uptime && (
                    <Typography variant="body2">
                      Uptime: {workerStatus.uptime}
                    </Typography>
                  )}

                  {workerStatus.lastActivity && (
                    <Typography variant="body2">
                      Last Activity: {new Date(workerStatus.lastActivity).toLocaleString()}
                    </Typography>
                  )}

                  {(workerStatus.processedCount !== undefined) && (
                    <Box>
                      <Typography variant="body2">
                        Processed: {workerStatus.processedCount} | 
                        Success: {workerStatus.successCount} | 
                        Failed: {workerStatus.failureCount}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Control Actions Card */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Actions
              </Typography>

              <Stack spacing={2}>
                <Button
                  variant="contained"
                  color="success"
                  startIcon={actionLoading === 'start' ? <CircularProgress size={20} /> : <StartIcon />}
                  onClick={handleStart}
                  disabled={!!actionLoading || workerStatus?.isRunning}
                  fullWidth
                >
                  {actionLoading === 'start' ? 'Starting...' : 'Start Worker'}
                </Button>

                <Button
                  variant="contained"
                  color="error"
                  startIcon={actionLoading === 'stop' ? <CircularProgress size={20} /> : <StopIcon />}
                  onClick={handleStop}
                  disabled={!!actionLoading || !workerStatus?.isRunning}
                  fullWidth
                >
                  {actionLoading === 'stop' ? 'Stopping...' : 'Stop Worker'}
                </Button>

                <Button
                  variant="outlined"
                  startIcon={actionLoading === 'restart' ? <CircularProgress size={20} /> : <RefreshIcon />}
                  onClick={handleRestart}
                  disabled={!!actionLoading}
                  fullWidth
                >
                  {actionLoading === 'restart' ? 'Restarting...' : 'Restart Worker'}
                </Button>

                <Divider />

                <Button
                  variant="text"
                  startIcon={<RefreshIcon />}
                  onClick={fetchWorkerStatus}
                  disabled={loading}
                  fullWidth
                >
                  Refresh Status
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Real-time Processing Monitor */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Real-time Processing Activity
              </Typography>

              {isConnected ? (
                <Box>
                  <Box display="flex" alignItems="center" mb={2}>
                    <StatusIcon sx={{ color: 'success.main', fontSize: 16, mr: 1 }} />
                    <Typography variant="body2" color="success.main">
                      Live monitoring active
                    </Typography>
                  </Box>

                  {runningIndicators && runningIndicators.length > 0 ? (
                    <Stack spacing={1}>
                      <Typography variant="subtitle2">
                        Currently Running ({runningIndicators.length}):
                      </Typography>
                      {runningIndicators.map((indicator, index) => (
                        <Box
                          key={index}
                          sx={{
                            p: 2,
                            bgcolor: 'action.hover',
                            borderRadius: 1,
                            border: '1px solid',
                            borderColor: 'divider'
                          }}
                        >
                          <Typography variant="body2" fontWeight="medium">
                            {indicator.indicator || `Indicator ${indicator.indicatorID}`}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Started: {new Date(indicator.startTime).toLocaleTimeString()} |
                            Duration: {Math.round((Date.now() - new Date(indicator.startTime).getTime()) / 1000)}s
                          </Typography>
                          {indicator.progress !== undefined && (
                            <Box sx={{ mt: 1 }}>
                              <Typography variant="caption">
                                Progress: {indicator.progress}%
                              </Typography>
                            </Box>
                          )}
                          {indicator.currentStep && (
                            <Box sx={{ mt: 0.5 }}>
                              <Typography variant="caption" color="primary">
                                {indicator.currentStep}
                              </Typography>
                            </Box>
                          )}
                        </Box>
                      ))}
                    </Stack>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      No indicators currently running
                    </Typography>
                  )}
                </Box>
              ) : (
                <Box>
                  <Box display="flex" alignItems="center" mb={2}>
                    <StatusIcon sx={{ color: 'warning.main', fontSize: 16, mr: 1 }} />
                    <Typography variant="body2" color="warning.main">
                      Real-time monitoring disconnected
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Connect to see live processing activity
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default WorkerControl;

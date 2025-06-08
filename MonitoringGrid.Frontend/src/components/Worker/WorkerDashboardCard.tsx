import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Box,
  IconButton,
  CircularProgress,
  Grid,
  Tooltip,
  Alert,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Refresh,
  Build,
  CheckCircle,
  Error as ErrorIcon,
  Schedule,
  Memory,
  AutorenewOutlined,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';
import { WorkerStatusUpdate } from '../../services/signalRService';

interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  errorMessage?: string;
}

interface WorkerStatus {
  isRunning: boolean;
  mode: string;
  processId?: number;
  startTime?: string;
  services: WorkerService[];
  timestamp: string;
}

interface WorkerActionResult {
  success: boolean;
  message: string;
  processId?: number;
  timestamp: string;
}

interface WorkerDashboardCardProps {
  workerStatus?: WorkerStatusUpdate | null;
  realtimeEnabled?: boolean;
}

const WorkerDashboardCard: React.FC<WorkerDashboardCardProps> = ({
  workerStatus: realtimeWorkerStatus,
  realtimeEnabled = false
}) => {
  const navigate = useNavigate();
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const fetchStatus = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/worker/status');
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      } else {
        console.error('Failed to fetch worker status');
      }
    } catch (error) {
      console.error('Error fetching worker status:', error);
    } finally {
      setLoading(false);
    }
  };

  const performAction = async (action: string) => {
    setActionLoading(action);
    try {
      const response = await fetch(`/api/worker/${action}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result: WorkerActionResult = await response.json();

      if (result.success) {
        toast.success(result.message);
        await fetchStatus(); // Refresh status
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error(`Failed to ${action} worker`);
      console.error(`Error ${action}ing worker:`, error);
    } finally {
      setActionLoading(null);
    }
  };

  const formatUptime = (startTime?: string) => {
    if (!startTime) return 'N/A';

    try {
      const start = new Date(startTime);
      const now = new Date();

      // Check if the date is valid
      if (isNaN(start.getTime())) {
        return 'Invalid Date';
      }

      const diff = now.getTime() - start.getTime();

      // If negative or very small, show as just started
      if (diff < 0 || diff < 60000) {
        // Less than 1 minute
        return 'Just started';
      }

      const hours = Math.floor(diff / (1000 * 60 * 60));
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

      if (hours === 0) {
        return `${minutes}m`;
      }

      return `${hours}h ${minutes}m`;
    } catch (error) {
      console.error('Error formatting uptime:', error);
      return 'Error';
    }
  };

  // Merge real-time status with fetched status
  const currentStatus = React.useMemo(() => {
    if (realtimeWorkerStatus) {
      // Convert real-time status to WorkerStatus format
      return {
        isRunning: realtimeWorkerStatus.isRunning,
        mode: realtimeWorkerStatus.mode,
        processId: realtimeWorkerStatus.processId,
        startTime: realtimeWorkerStatus.lastHeartbeat, // Use lastHeartbeat as startTime approximation
        services: realtimeWorkerStatus.services,
        timestamp: realtimeWorkerStatus.lastHeartbeat,
      };
    }
    return status;
  }, [realtimeWorkerStatus, status]);

  useEffect(() => {
    fetchStatus();

    // Reduce auto-refresh interval since we have real-time updates
    const interval = setInterval(fetchStatus, 60000); // Reduced to 60 seconds
    return () => clearInterval(interval);
  }, []);

  if (loading && !currentStatus) {
    return (
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Build sx={{ color: 'primary.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Worker Service
              </Typography>
            </Box>
          </Box>
          <Box
            sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1 }}
          >
            <CircularProgress />
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box display="flex" alignItems="center" gap={1}>
            <Build sx={{ color: (theme) => theme.palette.primary.main }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Worker Service
            </Typography>
          </Box>
          <Box display="flex" alignItems="center" gap={1}>
            <IconButton
              size="small"
              onClick={fetchStatus}
              disabled={loading}
              sx={{
                backgroundColor: (theme) => theme.palette.primary.main,
                color: 'white',
                '&:hover': {
                  backgroundColor: (theme) => theme.palette.primary.dark,
                },
              }}
            >
              {loading ? <CircularProgress size={16} /> : <Refresh />}
            </IconButton>
            <IconButton
              size="small"
              onClick={() => navigate('/worker')}
              sx={{
                backgroundColor: 'secondary.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'secondary.dark',
                },
              }}
            >
              <Build />
            </IconButton>
          </Box>
        </Box>

        <Box sx={{ flexGrow: 1 }}>
          {currentStatus ? (
            <>
              {/* Status Overview */}
              <Box sx={{ mb: 3 }}>
                <Grid container spacing={2} alignItems="center">
                  <Grid item xs={12} sm={6}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {currentStatus.isRunning ? (
                        <CheckCircle sx={{
                          color: 'success.main',
                          fontSize: 24,
                          animation: realtimeWorkerStatus ? 'pulse 2s infinite' : 'none',
                          '@keyframes pulse': {
                            '0%': { opacity: 1 },
                            '50%': { opacity: 0.7 },
                            '100%': { opacity: 1 },
                          },
                        }} />
                      ) : (
                        <ErrorIcon sx={{ color: 'error.main', fontSize: 24 }} />
                      )}
                      <Box>
                        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                          {currentStatus.isRunning ? 'Running' : 'Stopped'}
                          {realtimeWorkerStatus && (
                            <Chip
                              label="Live"
                              size="small"
                              color="success"
                              sx={{
                                ml: 1,
                                fontSize: '0.6rem',
                                height: 16,
                                animation: 'pulse 2s infinite',
                                '@keyframes pulse': {
                                  '0%': { opacity: 1 },
                                  '50%': { opacity: 0.8 },
                                  '100%': { opacity: 1 },
                                },
                              }}
                            />
                          )}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {currentStatus.mode} Mode
                          {currentStatus.processId && ` (PID: ${currentStatus.processId})`}
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>

                  <Grid item xs={12} sm={6}>
                    <Box sx={{ textAlign: { xs: 'left', sm: 'right' } }}>
                      <Box
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 0.5,
                          justifyContent: { xs: 'flex-start', sm: 'flex-end' },
                        }}
                      >
                        <Schedule fontSize="small" />
                        <Typography variant="body2">{formatUptime(currentStatus.startTime)}</Typography>
                      </Box>
                      <Typography variant="caption" color="text.secondary">
                        Uptime
                      </Typography>
                    </Box>
                  </Grid>
                </Grid>
              </Box>

              {/* Action Buttons */}
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                <Button
                  variant="contained"
                  color="success"
                  size="small"
                  onClick={() => performAction('start')}
                  disabled={
                    currentStatus.isRunning || actionLoading === 'start' || currentStatus.mode === 'Integrated'
                  }
                  startIcon={
                    actionLoading === 'start' ? <CircularProgress size={14} /> : <PlayArrow />
                  }
                >
                  Start
                </Button>

                <Button
                  variant="contained"
                  color="error"
                  size="small"
                  onClick={() => performAction('stop')}
                  disabled={
                    !currentStatus.isRunning || actionLoading === 'stop' || currentStatus.mode === 'Integrated'
                  }
                  startIcon={actionLoading === 'stop' ? <CircularProgress size={14} /> : <Stop />}
                >
                  Stop
                </Button>

                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => performAction('restart')}
                  disabled={
                    !currentStatus.isRunning || actionLoading === 'restart' || currentStatus.mode === 'Integrated'
                  }
                  startIcon={
                    actionLoading === 'restart' ? (
                      <CircularProgress size={14} />
                    ) : (
                      <AutorenewOutlined />
                    )
                  }
                >
                  Restart
                </Button>
              </Box>

              {/* Integration Mode Alert */}
              {currentStatus.mode === 'Integrated' && (
                <Alert severity="info" sx={{ mb: 2, fontSize: '0.8rem' }}>
                  Running in integrated mode. Use API restart to control.
                </Alert>
              )}

              {/* Real-time Status Alert */}
              {currentStatus.isRunning && currentStatus.mode !== 'Integrated' && (
                <Alert
                  severity={realtimeEnabled ? "success" : "info"}
                  sx={{ mb: 2, fontSize: '0.8rem' }}
                >
                  Real-time features: {realtimeEnabled ? 'Connected' : 'Disconnected'}
                  {!realtimeEnabled && ' - Go to Worker Management to enable'}
                </Alert>
              )}

              {/* Services Summary */}
              <Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                  <Memory fontSize="small" />
                  <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                    Services ({currentStatus.services.length})
                  </Typography>
                </Box>

                {currentStatus.services.length > 0 ? (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {currentStatus.services.slice(0, 4).map((service, index) => (
                      <Tooltip key={index} title={service.name}>
                        <Chip
                          label={service.name.replace('MonitoringWorker', '').replace('Worker', '')}
                          color={service.status === 'Running' ? 'success' : 'error'}
                          size="small"
                          sx={{ fontSize: '0.7rem', height: 20 }}
                        />
                      </Tooltip>
                    ))}
                    {currentStatus.services.length > 4 && (
                      <Chip
                        label={`+${currentStatus.services.length - 4} more`}
                        size="small"
                        variant="outlined"
                        sx={{ fontSize: '0.7rem', height: 20 }}
                      />
                    )}
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No services running
                  </Typography>
                )}
              </Box>
            </>
          ) : (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                py: 4,
                textAlign: 'center',
              }}
            >
              <ErrorIcon sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
              <Typography color="text.secondary" variant="body2">
                Unable to connect to Worker service
              </Typography>
              <Button size="small" onClick={fetchStatus} sx={{ mt: 1 }} startIcon={<Refresh />}>
                Retry
              </Button>
            </Box>
          )}
        </Box>
      </CardContent>
    </Card>
  );
};

export default WorkerDashboardCard;

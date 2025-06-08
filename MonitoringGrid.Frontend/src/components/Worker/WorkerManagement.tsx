import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  Chip,
  Alert,
  Box,
  Divider,
  IconButton,
  Switch,
  FormControlLabel,
  CircularProgress,
  Grid,
  Paper,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Refresh,
  Activity,
  Schedule,
  Memory,
  Error as ErrorIcon,
  CheckCircle,
  Build,
  AutorenewOutlined,
} from '@mui/icons-material';
import { toast } from 'react-hot-toast';

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
  uptime?: string;
  services: WorkerService[];
  timestamp: string;
}

interface WorkerActionResult {
  success: boolean;
  message: string;
  processId?: number;
  timestamp: string;
}

const WorkerManagement: React.FC = () => {
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchStatus = async () => {
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
    
    const start = new Date(startTime);
    const now = new Date();
    const diff = now.getTime() - start.getTime();
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    
    return `${hours}h ${minutes}m`;
  };

  const formatDateTime = (dateTime?: string) => {
    if (!dateTime) return 'N/A';
    return new Date(dateTime).toLocaleString();
  };

  useEffect(() => {
    fetchStatus();
    
    if (autoRefresh) {
      const interval = setInterval(fetchStatus, 5000); // Refresh every 5 seconds
      return () => clearInterval(interval);
    }
  }, [autoRefresh]);

  if (!status) {
    return (
      <Box sx={{ p: 3 }}>
        <Card>
          <CardHeader>
            <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <Build />
              Worker Management
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Loading worker status...
            </Typography>
          </CardHeader>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
              <CircularProgress />
            </Box>
          </CardContent>
        </Card>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Card sx={{ mb: 3 }}>
        <CardHeader>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <Box>
              <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <Build />
                Worker Management
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Control and monitor the MonitoringGrid Worker service
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={autoRefresh}
                    onChange={(e) => setAutoRefresh(e.target.checked)}
                    color="primary"
                  />
                }
                label="Auto Refresh"
              />
              <IconButton
                onClick={fetchStatus}
                disabled={loading}
                color="primary"
              >
                {loading ? <CircularProgress size={20} /> : <Refresh />}
              </IconButton>
            </Box>
          </Box>
        </CardHeader>
        <CardContent>
          {/* Status Overview */}
          <Paper sx={{ p: 3, mb: 3, bgcolor: 'grey.50' }}>
            <Grid container spacing={3} alignItems="center">
              <Grid item xs={12} md={6}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  {status.isRunning ? (
                    <CheckCircle sx={{ color: 'success.main', fontSize: 32 }} />
                  ) : (
                    <ErrorIcon sx={{ color: 'error.main', fontSize: 32 }} />
                  )}
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      <Typography variant="h6">Status:</Typography>
                      <Chip
                        label={status.isRunning ? 'Running' : 'Stopped'}
                        color={status.isRunning ? 'success' : 'error'}
                        size="small"
                      />
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      Mode: {status.mode}
                      {status.processId && ` (PID: ${status.processId})`}
                    </Typography>
                  </Box>
                </Box>
              </Grid>

              <Grid item xs={12} md={6}>
                <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, justifyContent: { xs: 'flex-start', md: 'flex-end' }, mb: 0.5 }}>
                    <Schedule fontSize="small" />
                    <Typography variant="body2">
                      Uptime: {formatUptime(status.startTime)}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Started: {formatDateTime(status.startTime)}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Paper>

          {/* Action Buttons */}
          <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
            <Button
              variant="contained"
              color="success"
              onClick={() => performAction('start')}
              disabled={status.isRunning || actionLoading === 'start' || status.mode === 'Integrated'}
              startIcon={actionLoading === 'start' ? <CircularProgress size={16} /> : <PlayArrow />}
            >
              {actionLoading === 'start' ? 'Starting...' : 'Start Worker'}
            </Button>

            <Button
              variant="contained"
              color="error"
              onClick={() => performAction('stop')}
              disabled={!status.isRunning || actionLoading === 'stop' || status.mode === 'Integrated'}
              startIcon={actionLoading === 'stop' ? <CircularProgress size={16} /> : <Stop />}
            >
              {actionLoading === 'stop' ? 'Stopping...' : 'Stop Worker'}
            </Button>

            <Button
              variant="outlined"
              onClick={() => performAction('restart')}
              disabled={!status.isRunning || actionLoading === 'restart' || status.mode === 'Integrated'}
              startIcon={actionLoading === 'restart' ? <CircularProgress size={16} /> : <AutorenewOutlined />}
            >
              {actionLoading === 'restart' ? 'Restarting...' : 'Restart Worker'}
            </Button>
          </Box>

          {/* Integration Mode Alert */}
          {status.mode === 'Integrated' && (
            <Alert severity="info" sx={{ mb: 3 }}>
              Worker services are running in integrated mode. To control them separately,
              set <code>EnableWorkerServices</code> to <code>false</code> in configuration and restart the API.
            </Alert>
          )}

          <Divider sx={{ mb: 3 }} />

          {/* Services Status */}
          <Box>
            <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <Memory />
              Worker Services ({status.services.length})
            </Typography>

            {status.services.length > 0 ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {status.services.map((service, index) => (
                  <Paper
                    key={index}
                    sx={{ p: 2, border: 1, borderColor: 'divider' }}
                  >
                    <Grid container spacing={2} alignItems="center">
                      <Grid item xs={12} md={8}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                          <Box
                            sx={{
                              width: 8,
                              height: 8,
                              borderRadius: '50%',
                              bgcolor: service.status === 'Running' ? 'success.main' : 'error.main'
                            }}
                          />
                          <Box>
                            <Typography variant="subtitle1" fontWeight="medium">
                              {service.name}
                            </Typography>
                            {service.errorMessage && (
                              <Typography variant="body2" color="error">
                                {service.errorMessage}
                              </Typography>
                            )}
                          </Box>
                        </Box>
                      </Grid>

                      <Grid item xs={12} md={4}>
                        <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                          <Chip
                            label={service.status}
                            color={service.status === 'Running' ? 'success' : 'error'}
                            size="small"
                            sx={{ mb: service.lastActivity ? 1 : 0 }}
                          />
                          {service.lastActivity && (
                            <Typography variant="body2" color="text.secondary">
                              Last: {formatDateTime(service.lastActivity)}
                            </Typography>
                          )}
                        </Box>
                      </Grid>
                    </Grid>
                  </Paper>
                ))}
              </Box>
            ) : (
              <Paper sx={{ p: 4, textAlign: 'center' }}>
                <Typography variant="body1" color="text.secondary">
                  No worker services running
                </Typography>
              </Paper>
            )}
          </Box>

          {/* Last Updated */}
          <Box sx={{ textAlign: 'center', mt: 3 }}>
            <Typography variant="caption" color="text.secondary">
              Last updated: {formatDateTime(status.timestamp)}
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default WorkerManagement;

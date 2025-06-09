import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Paper,
  LinearProgress,
  Badge,
  Button,
  Alert,
} from '@mui/material';
import {
  Wifi,
  WifiOff,
  PlayCircle,
  Timer,
  Schedule,
  Build,
  CheckCircle,
  Error as ErrorIcon,
  Send,
  Refresh,
} from '@mui/icons-material';
import { useRealtimeDashboard } from '../hooks/useRealtimeDashboard';
import { format } from 'date-fns';
import { formatCountdown } from '../utils/countdown';
import { signalRService } from '../services/signalRService';

const RealtimeTest: React.FC = () => {
  const realtimeDashboard = useRealtimeDashboard();
  const [events, setEvents] = useState<Array<{ timestamp: Date; type: string; data: any }>>([]);
  const [connectionState, setConnectionState] = useState<string>('Unknown');
  const [testResults, setTestResults] = useState<Array<{ test: string; success: boolean; message: string }>>([]);

  // Track connection state
  useEffect(() => {
    const updateConnectionState = () => {
      const state = signalRService.getConnectionState();
      setConnectionState(state);
    };

    // Update immediately
    updateConnectionState();

    // Update every second
    const interval = setInterval(updateConnectionState, 1000);

    return () => clearInterval(interval);
  }, []);

  // Track events for debugging
  useEffect(() => {
    const newEvent = {
      timestamp: new Date(),
      type: 'dashboard_update',
      data: {
        isConnected: realtimeDashboard.isConnected,
        runningKpis: realtimeDashboard.runningKpis.length,
        countdown: realtimeDashboard.countdown,
        workerStatus: realtimeDashboard.workerStatus?.isRunning,
        connectionState,
      },
    };

    setEvents(prev => [newEvent, ...prev.slice(0, 19)]); // Keep last 20 events
  }, [
    realtimeDashboard.isConnected,
    realtimeDashboard.runningKpis,
    realtimeDashboard.countdown,
    realtimeDashboard.workerStatus,
    realtimeDashboard.lastUpdate,
    connectionState,
  ]);

  // Test functions
  const testSignalRConnection = async () => {
    try {
      const response = await fetch('/api/v1/signalrtest/test-connection', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result = await response.json();
      setTestResults(prev => [...prev, {
        test: 'Connection Test',
        success: result.success,
        message: result.message
      }]);
    } catch (error) {
      setTestResults(prev => [...prev, {
        test: 'Connection Test',
        success: false,
        message: `Error: ${error}`
      }]);
    }
  };

  const testWorkerStatus = async () => {
    try {
      const response = await fetch('/api/v1/signalrtest/test-worker-status', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result = await response.json();
      setTestResults(prev => [...prev, {
        test: 'Worker Status Test',
        success: result.success,
        message: result.message
      }]);
    } catch (error) {
      setTestResults(prev => [...prev, {
        test: 'Worker Status Test',
        success: false,
        message: `Error: ${error}`
      }]);
    }
  };

  const testCountdown = async () => {
    try {
      const response = await fetch('/api/v1/signalrtest/test-countdown', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result = await response.json();
      setTestResults(prev => [...prev, {
        test: 'Countdown Test',
        success: result.success,
        message: result.message
      }]);
    } catch (error) {
      setTestResults(prev => [...prev, {
        test: 'Countdown Test',
        success: false,
        message: `Error: ${error}`
      }]);
    }
  };

  const clearTestResults = () => {
    setTestResults([]);
    setEvents([]);
  };



  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Real-time Dashboard Test
      </Typography>

      {/* Test Controls */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            SignalR Connection Tests
          </Typography>

          <Box display="flex" gap={2} mb={2}>
            <Button
              variant="contained"
              startIcon={<Send />}
              onClick={testSignalRConnection}
            >
              Test Connection
            </Button>
            <Button
              variant="contained"
              startIcon={<Build />}
              onClick={testWorkerStatus}
            >
              Test Worker Status
            </Button>
            <Button
              variant="contained"
              startIcon={<Timer />}
              onClick={testCountdown}
            >
              Test Countdown
            </Button>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={clearTestResults}
            >
              Clear Results
            </Button>
          </Box>

          {testResults.length > 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Test Results:
              </Typography>
              {testResults.map((result, index) => (
                <Alert
                  key={index}
                  severity={result.success ? 'success' : 'error'}
                  sx={{ mb: 1 }}
                >
                  <strong>{result.test}:</strong> {result.message}
                </Alert>
              ))}
            </Box>
          )}
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        {/* Connection Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                <Badge
                  badgeContent={realtimeDashboard.isConnected ? <Wifi /> : <WifiOff />}
                  color={realtimeDashboard.isConnected ? 'success' : 'error'}
                >
                  <Build />
                </Badge>
                <Typography variant="h6">
                  Connection Status
                </Typography>
              </Box>
              
              <Chip
                label={realtimeDashboard.isConnected ? 'Connected' : 'Disconnected'}
                color={realtimeDashboard.isConnected ? 'success' : 'error'}
                sx={{ mb: 2 }}
              />
              
              <Typography variant="body2" color="text.secondary">
                Last Update: {format(realtimeDashboard.lastUpdate, 'HH:mm:ss')}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Worker Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                {realtimeDashboard.workerStatus?.isRunning ? (
                  <CheckCircle sx={{ color: 'success.main' }} />
                ) : (
                  <ErrorIcon sx={{ color: 'error.main' }} />
                )}
                <Typography variant="h6">
                  Worker Status
                </Typography>
              </Box>
              
              {realtimeDashboard.workerStatus ? (
                <Box>
                  <Chip
                    label={realtimeDashboard.workerStatus.isRunning ? 'Running' : 'Stopped'}
                    color={realtimeDashboard.workerStatus.isRunning ? 'success' : 'error'}
                    sx={{ mb: 1 }}
                  />
                  <Typography variant="body2" color="text.secondary">
                    Mode: {realtimeDashboard.workerStatus.mode}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    PID: {realtimeDashboard.workerStatus.processId}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Services: {realtimeDashboard.workerStatus.services.length}
                  </Typography>
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No worker status data
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Countdown */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                <Timer />
                <Typography variant="h6">
                  Next KPI Countdown
                </Typography>
              </Box>
              
              {realtimeDashboard.countdown !== null ? (
                <Box>
                  <Typography variant="h4" color="primary" gutterBottom>
                    {formatCountdown(realtimeDashboard.countdown)}
                  </Typography>
                  {realtimeDashboard.nextKpiDue && (
                    <Box>
                      <Typography variant="body1" sx={{ fontWeight: 600 }}>
                        {realtimeDashboard.nextKpiDue.indicator}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Owner: {realtimeDashboard.nextKpiDue.owner}
                      </Typography>
                    </Box>
                  )}
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No upcoming KPIs
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Running KPIs */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                <Badge badgeContent={realtimeDashboard.runningKpis.length} color="primary">
                  <PlayCircle />
                </Badge>
                <Typography variant="h6">
                  Running KPIs
                </Typography>
              </Box>
              
              {realtimeDashboard.runningKpis.length > 0 ? (
                <List dense>
                  {realtimeDashboard.runningKpis.map(kpi => (
                    <ListItem key={kpi.kpiId}>
                      <ListItemIcon>
                        <PlayCircle color="primary" />
                      </ListItemIcon>
                      <ListItemText
                        primary={kpi.indicator}
                        secondary={
                          <Box>
                            <Typography variant="caption" display="block">
                              Owner: {kpi.owner}
                            </Typography>
                            {kpi.progress !== undefined && (
                              <Box sx={{ mt: 1 }}>
                                <LinearProgress 
                                  variant="determinate" 
                                  value={kpi.progress} 
                                  sx={{ height: 4, borderRadius: 2 }}
                                />
                                <Typography variant="caption" color="text.secondary">
                                  {kpi.progress}% complete
                                </Typography>
                              </Box>
                            )}
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No KPIs currently running
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Event Log */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Real-time Events Log
              </Typography>
              
              <Paper sx={{ maxHeight: 300, overflow: 'auto', p: 2 }}>
                {events.length > 0 ? (
                  <List dense>
                    {events.map((event, index) => (
                      <ListItem key={index}>
                        <ListItemText
                          primary={`${event.type} - ${format(event.timestamp, 'HH:mm:ss.SSS')}`}
                          secondary={JSON.stringify(event.data, null, 2)}
                          sx={{ 
                            '& .MuiListItemText-secondary': { 
                              fontFamily: 'monospace',
                              fontSize: '0.75rem',
                              whiteSpace: 'pre-wrap'
                            }
                          }}
                        />
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No events yet...
                  </Typography>
                )}
              </Paper>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default RealtimeTest;

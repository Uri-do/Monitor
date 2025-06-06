import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Alert,
  AlertTitle,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
  Badge,
  Paper
} from '@mui/material';
import {
  Timeline as Activity,
  Warning as AlertTriangle,
  CheckCircle,
  PlayArrow,
  Pause,
  Refresh,
  Wifi,
  WifiOff,
  Speed,
  Timeline,
  Notifications
} from '@mui/icons-material';
import { 
  useRealtimeStatus, 
  useLiveDashboard, 
  useSystemHealth 
} from '@/hooks/useEnhancedApi';
import { useSignalR } from '@/services/signalRService';
import { RealtimeStatusDto, LiveDashboardDto, SystemHealthDto } from '@/types/api';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer } from 'recharts';

interface RealtimeEvent {
  id: number;
  type: string;
  timestamp: Date;
  message: string;
  data?: any;
}

interface RealtimeMonitoringProps {
  className?: string;
}

export const RealtimeMonitoring: React.FC<RealtimeMonitoringProps> = ({ className }) => {
  const [isMonitoring, setIsMonitoring] = useState(true);
  const [realtimeEvents, setRealtimeEvents] = useState<RealtimeEvent[]>([]);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const { 
    data: realtimeStatus, 
    loading: statusLoading, 
    refetch: refetchStatus 
  } = useRealtimeStatus(isMonitoring && autoRefresh, 5000);

  const { 
    data: liveDashboard, 
    loading: dashboardLoading, 
    refetch: refetchDashboard 
  } = useLiveDashboard(isMonitoring && autoRefresh, 10000);

  const { 
    data: systemHealth, 
    loading: healthLoading 
  } = useSystemHealth(isMonitoring && autoRefresh, 30000);

  const { 
    isConnected, 
    connectionState, 
    joinGroup, 
    leaveGroup, 
    on, 
    off 
  } = useSignalR();

  // Real-time event handlers
  const handleStatusUpdate = useCallback((status: RealtimeStatusDto) => {
    setRealtimeEvents(prev => [{
      id: Date.now(),
      type: 'status_update',
      timestamp: new Date(),
      message: `System status updated - ${status.activeKpis} active KPIs, ${status.dueKpis} due`,
      data: status
    }, ...prev.slice(0, 49)]);
  }, []);

  const handleDashboardUpdate = useCallback((dashboard: LiveDashboardDto) => {
    setRealtimeEvents(prev => [{
      id: Date.now(),
      type: 'dashboard_update',
      timestamp: new Date(),
      message: `Dashboard updated - ${dashboard.executionsLastHour} executions in last hour`,
      data: dashboard
    }, ...prev.slice(0, 49)]);
  }, []);

  const handleKpiExecuted = useCallback((data: any) => {
    setRealtimeEvents(prev => [{
      id: Date.now(),
      type: 'kpi_executed',
      timestamp: new Date(),
      message: `KPI executed: ${data.Indicator} - ${data.Result?.isSuccessful ? 'Success' : 'Failed'}`,
      data: data
    }, ...prev.slice(0, 49)]);
  }, []);

  const handleAlertTriggered = useCallback((data: any) => {
    setRealtimeEvents(prev => [{
      id: Date.now(),
      type: 'alert_triggered',
      timestamp: new Date(),
      message: `Alert triggered: ${data.indicator} - ${data.severity} severity`,
      data: data
    }, ...prev.slice(0, 49)]);
  }, []);

  const handleSystemHealthUpdate = useCallback((health: any) => {
    setRealtimeEvents(prev => [{
      id: Date.now(),
      type: 'health_update',
      timestamp: new Date(),
      message: `System health updated - Score: ${health.overallHealthScore?.toFixed(1)}%`,
      data: health
    }, ...prev.slice(0, 49)]);
  }, []);

  // Setup SignalR event listeners
  useEffect(() => {
    if (isConnected && isMonitoring) {
      on('onStatusUpdate', handleStatusUpdate);
      on('onDashboardUpdate', handleDashboardUpdate);
      on('onKpiExecuted', handleKpiExecuted);
      on('onAlertTriggered', handleAlertTriggered);
      on('onSystemHealthUpdate', handleSystemHealthUpdate);

      joinGroup('monitoring');

      return () => {
        off('onStatusUpdate');
        off('onDashboardUpdate');
        off('onKpiExecuted');
        off('onAlertTriggered');
        off('onSystemHealthUpdate');
        leaveGroup('monitoring');
      };
    }
  }, [isConnected, isMonitoring, on, off, joinGroup, leaveGroup, 
      handleStatusUpdate, handleDashboardUpdate, handleKpiExecuted, 
      handleAlertTriggered, handleSystemHealthUpdate]);

  const toggleMonitoring = () => {
    setIsMonitoring(!isMonitoring);
  };

  const handleManualRefresh = () => {
    refetchStatus();
    refetchDashboard();
  };

  const getEventIcon = (type: string) => {
    switch (type) {
      case 'status_update': return <Activity color="primary" />;
      case 'dashboard_update': return <Timeline color="success" />;
      case 'kpi_executed': return <PlayArrow color="secondary" />;
      case 'alert_triggered': return <AlertTriangle color="error" />;
      case 'health_update': return <CheckCircle color="success" />;
      default: return <Activity color="disabled" />;
    }
  };

  const getEventColor = (type: string) => {
    switch (type) {
      case 'status_update': return 'primary.light';
      case 'dashboard_update': return 'success.light';
      case 'kpi_executed': return 'secondary.light';
      case 'alert_triggered': return 'error.light';
      case 'health_update': return 'success.light';
      default: return 'grey.100';
    }
  };

  const formatRecentExecutions = () => {
    return liveDashboard?.recentExecutions?.slice(0, 10).map((execution, index) => ({
      time: new Date(execution.timestamp).toLocaleTimeString(),
      value: execution.value,
      deviation: Math.abs(execution.deviationPercent),
      success: execution.isSuccessful ? 1 : 0
    })) || [];
  };

  return (
    <Box className={className} sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Real-time Monitoring
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
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
          <Chip
            icon={isConnected ? <Wifi /> : <WifiOff />}
            label={isConnected ? "Connected" : "Disconnected"}
            color={isConnected ? "success" : "error"}
            variant="outlined"
          />
          <Typography variant="body2" color="text.secondary">
            {connectionState}
          </Typography>
          <Tooltip title="Manual Refresh">
            <IconButton onClick={handleManualRefresh} size="small">
              <Refresh />
            </IconButton>
          </Tooltip>
          <Button
            onClick={toggleMonitoring}
            variant={isMonitoring ? "contained" : "outlined"}
            startIcon={isMonitoring ? <Pause /> : <PlayArrow />}
            color={isMonitoring ? "secondary" : "primary"}
          >
            {isMonitoring ? 'Pause' : 'Start'} Monitoring
          </Button>
        </Box>
      </Box>

      {/* Connection Status Alert */}
      {!isConnected && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          <AlertTitle>Real-time Connection Lost</AlertTitle>
          Some features may not work properly. Check your network connection.
        </Alert>
      )}

      {/* Real-time Status Cards */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    System Health
                  </Typography>
                  <Typography 
                    variant="h4" 
                    component="div" 
                    color={
                      (systemHealth?.overallHealthScore || 0) >= 90 ? 'success.main' :
                      (systemHealth?.overallHealthScore || 0) >= 70 ? 'warning.main' :
                      'error.main'
                    }
                  >
                    {systemHealth?.overallHealthScore?.toFixed(1) || 0}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {systemHealth?.systemStatus || 'Unknown'}
                  </Typography>
                </Box>
                <CheckCircle 
                  color={
                    (systemHealth?.overallHealthScore || 0) >= 90 ? 'success' :
                    (systemHealth?.overallHealthScore || 0) >= 70 ? 'warning' :
                    'error'
                  }
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Active KPIs
                  </Typography>
                  <Typography variant="h4" component="div">
                    {realtimeStatus?.activeKpis || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {realtimeStatus?.dueKpis || 0} due for execution
                  </Typography>
                </Box>
                <Activity color="primary" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    System Load
                  </Typography>
                  <Typography variant="h4" component="div">
                    {realtimeStatus?.systemLoad?.toFixed(1) || 0}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Current load
                  </Typography>
                </Box>
                <Speed color="primary" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Recent Alerts
                  </Typography>
                  <Typography variant="h4" component="div">
                    {realtimeStatus?.recentAlerts || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {realtimeStatus?.unresolvedAlerts || 0} unresolved
                  </Typography>
                </Box>
                <Badge badgeContent={realtimeStatus?.unresolvedAlerts || 0} color="error">
                  <Notifications color="warning" />
                </Badge>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        {/* Real-time Execution Chart */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Real-time Executions
              </Typography>
              <Box height={300}>
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={formatRecentExecutions()}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="time" />
                    <YAxis />
                    <RechartsTooltip />
                    <Line 
                      type="monotone" 
                      dataKey="value" 
                      stroke="#4caf50" 
                      strokeWidth={2}
                      name="Value"
                    />
                    <Line 
                      type="monotone" 
                      dataKey="deviation" 
                      stroke="#f44336" 
                      strokeWidth={2}
                      name="Deviation %"
                    />
                  </LineChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Live Event Stream */}
        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">
                  Live Event Stream
                </Typography>
                <Chip 
                  label={`${realtimeEvents.length} events`} 
                  size="small" 
                  variant="outlined"
                />
              </Box>
              <Paper 
                sx={{ 
                  maxHeight: 300, 
                  overflow: 'auto',
                  backgroundColor: 'grey.50'
                }}
              >
                <List dense>
                  {realtimeEvents.map((event) => (
                    <ListItem 
                      key={event.id}
                      sx={{
                        backgroundColor: getEventColor(event.type),
                        mb: 0.5,
                        borderRadius: 1,
                        border: 1,
                        borderColor: 'grey.300'
                      }}
                    >
                      <ListItemIcon>
                        {getEventIcon(event.type)}
                      </ListItemIcon>
                      <ListItemText
                        primary={event.message}
                        secondary={event.timestamp.toLocaleTimeString()}
                        primaryTypographyProps={{ variant: 'body2' }}
                        secondaryTypographyProps={{ variant: 'caption' }}
                      />
                    </ListItem>
                  ))}
                  
                  {realtimeEvents.length === 0 && (
                    <ListItem>
                      <ListItemText
                        primary="Waiting for real-time events..."
                        secondary={!isConnected ? "Connect to see live updates" : "No events yet"}
                        sx={{ textAlign: 'center', color: 'text.secondary' }}
                      />
                    </ListItem>
                  )}
                </List>
              </Paper>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Executions */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Executions
              </Typography>
              <List>
                {liveDashboard?.recentExecutions?.slice(0, 5).map((execution, index) => (
                  <ListItem 
                    key={index}
                    sx={{
                      border: 1,
                      borderColor: 'grey.300',
                      borderRadius: 1,
                      mb: 1,
                      backgroundColor: execution.isSuccessful ? 'success.light' : 'error.light',
                      opacity: 0.8
                    }}
                  >
                    <ListItemIcon>
                      {execution.isSuccessful ? 
                        <CheckCircle color="success" /> : 
                        <AlertTriangle color="error" />
                      }
                    </ListItemIcon>
                    <ListItemText
                      primary={execution.indicator}
                      secondary={`${new Date(execution.timestamp).toLocaleTimeString()} - ${execution.executionTimeMs}ms`}
                    />
                    <Typography variant="body2" color="text.secondary">
                      {execution.value}
                    </Typography>
                  </ListItem>
                ))}
                
                {(!liveDashboard?.recentExecutions || liveDashboard.recentExecutions.length === 0) && (
                  <ListItem>
                    <ListItemText
                      primary="No recent executions"
                      sx={{ textAlign: 'center', color: 'text.secondary' }}
                    />
                  </ListItem>
                )}
              </List>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Alerts */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Alerts
              </Typography>
              <List>
                {liveDashboard?.recentAlerts?.slice(0, 5).map((alert, index) => (
                  <ListItem 
                    key={index}
                    sx={{
                      border: 1,
                      borderColor: 'grey.300',
                      borderRadius: 1,
                      mb: 1,
                      backgroundColor: 
                        alert.severity === 'Critical' ? 'error.light' :
                        alert.severity === 'High' ? 'warning.light' :
                        'info.light',
                      opacity: 0.8
                    }}
                  >
                    <ListItemIcon>
                      <AlertTriangle 
                        color={
                          alert.severity === 'Critical' ? 'error' :
                          alert.severity === 'High' ? 'warning' :
                          'info'
                        }
                      />
                    </ListItemIcon>
                    <ListItemText
                      primary={alert.indicator}
                      secondary={`${alert.owner} - ${new Date(alert.triggerTime).toLocaleTimeString()}`}
                    />
                    <Chip
                      label={alert.severity}
                      size="small"
                      color={
                        alert.severity === 'Critical' ? 'error' :
                        alert.severity === 'High' ? 'warning' :
                        'default'
                      }
                    />
                  </ListItem>
                ))}
                
                {(!liveDashboard?.recentAlerts || liveDashboard.recentAlerts.length === 0) && (
                  <ListItem>
                    <ListItemText
                      primary="No recent alerts"
                      sx={{ textAlign: 'center', color: 'text.secondary' }}
                    />
                  </ListItem>
                )}
              </List>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* System Issues */}
      {systemHealth?.issues && systemHealth.issues.length > 0 && (
        <Alert severity="error" sx={{ mt: 3 }}>
          <AlertTitle>System Issues Detected</AlertTitle>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {systemHealth.issues.map((issue, index) => (
              <li key={index}>{issue}</li>
            ))}
          </ul>
        </Alert>
      )}

      {/* System Recommendations */}
      {systemHealth?.recommendations && systemHealth.recommendations.length > 0 && (
        <Alert severity="info" sx={{ mt: 3 }}>
          <AlertTitle>System Recommendations</AlertTitle>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {systemHealth.recommendations.map((recommendation, index) => (
              <li key={index}>{recommendation}</li>
            ))}
          </ul>
        </Alert>
      )}
    </Box>
  );
};

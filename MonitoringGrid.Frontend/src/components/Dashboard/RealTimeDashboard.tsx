import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Alert,
  Chip,
  Badge,
  IconButton,
  Tooltip,
  Paper,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Avatar,
  Stack,
} from '@mui/material';
import {
  Dashboard as DashboardIcon,
  TrendingUp,
  Warning,
  CheckCircle,
  Error,
  Refresh,
  Notifications,
  Speed,
  Timeline,
  People,
  Circle,
  SignalWifi4Bar,
  SignalWifiOff,
} from '@mui/icons-material';
import { useSignalR } from '../../services/signalRService';
import { AlertNotification, KpiExecutionResult, SystemStatus } from '../../types/monitoring';

interface DashboardStats {
  totalKpis: number;
  activeAlerts: number;
  healthyKpis: number;
  criticalKpis: number;
  systemStatus: 'Healthy' | 'Warning' | 'Critical';
  lastUpdate: Date;
}

interface RealtimeMetric {
  id: string;
  name: string;
  value: number;
  trend: 'up' | 'down' | 'stable';
  status: 'healthy' | 'warning' | 'critical';
  lastUpdate: Date;
}

export const RealTimeDashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    totalKpis: 0,
    activeAlerts: 0,
    healthyKpis: 0,
    criticalKpis: 0,
    systemStatus: 'Healthy',
    lastUpdate: new Date(),
  });

  const [realtimeAlerts, setRealtimeAlerts] = useState<AlertNotification[]>([]);
  const [realtimeMetrics, setRealtimeMetrics] = useState<RealtimeMetric[]>([]);
  const [connectedUsers, setConnectedUsers] = useState<string[]>([]);
  const [systemHealth, setSystemHealth] = useState<SystemStatus | null>(null);

  const { isConnected, connectionState, on, off, joinGroup } = useSignalR();

  useEffect(() => {
    // Join dashboard group for real-time updates
    if (isConnected) {
      joinGroup('Dashboard');
    }
  }, [isConnected, joinGroup]);

  useEffect(() => {
    // Setup real-time event handlers
    const handleAlertTriggered = (alert: AlertNotification) => {
      setRealtimeAlerts(prev => [alert, ...prev.slice(0, 9)]); // Keep last 10
      setStats(prev => ({
        ...prev,
        activeAlerts: prev.activeAlerts + 1,
        systemStatus: alert.severity === 'Critical' ? 'Critical' : prev.systemStatus,
        lastUpdate: new Date(),
      }));
    };

    const handleKpiExecuted = (result: KpiExecutionResult) => {
      const metric: RealtimeMetric = {
        id: result.kpiId.toString(),
        name: `KPI ${result.kpiId}`,
        value: result.currentValue,
        trend:
          result.currentValue > result.historicalValue
            ? 'up'
            : result.currentValue < result.historicalValue
              ? 'down'
              : 'stable',
        status: 'healthy',
        lastUpdate: new Date(result.executionTime),
      };

      setRealtimeMetrics(prev => {
        const existing = prev.findIndex(m => m.id === metric.id);
        if (existing >= 0) {
          const updated = [...prev];
          updated[existing] = metric;
          return updated;
        }
        return [metric, ...prev.slice(0, 7)]; // Keep last 8
      });

      setStats(prev => ({ ...prev, lastUpdate: new Date() }));
    };

    const handleSystemStatusChanged = (status: SystemStatus) => {
      setSystemHealth(status);
      setStats(prev => ({
        ...prev,
        systemStatus: status.status as any,
        lastUpdate: new Date(),
      }));
    };

    const handleUserConnected = (userId: string) => {
      setConnectedUsers(prev => [...prev.filter(id => id !== userId), userId]);
    };

    const handleUserDisconnected = (userId: string) => {
      setConnectedUsers(prev => prev.filter(id => id !== userId));
    };

    on('onAlertTriggered', handleAlertTriggered);
    on('onKpiExecuted', handleKpiExecuted);
    on('onSystemStatusChanged', handleSystemStatusChanged);
    on('onUserConnected', handleUserConnected);
    on('onUserDisconnected', handleUserDisconnected);

    return () => {
      off('onAlertTriggered');
      off('onKpiExecuted');
      off('onSystemStatusChanged');
      off('onUserConnected');
      off('onUserDisconnected');
    };
  }, [on, off]);

  const getStatusColor = (status: string | undefined) => {
    if (!status) return 'secondary';
    switch (status.toLowerCase()) {
      case 'healthy':
        return 'success';
      case 'warning':
        return 'warning';
      case 'critical':
        return 'error';
      default:
        return 'secondary';
    }
  };

  const getStatusIcon = (status: string | undefined) => {
    if (!status) return <CheckCircle color="secondary" />;
    switch (status.toLowerCase()) {
      case 'healthy':
        return <CheckCircle color="success" />;
      case 'warning':
        return <Warning color="warning" />;
      case 'critical':
        return <Error color="error" />;
      default:
        return <TrendingUp />;
    }
  };

  const getTrendIcon = (trend: string) => {
    switch (trend) {
      case 'up':
        return <TrendingUp color="success" />;
      case 'down':
        return <TrendingUp sx={{ transform: 'rotate(180deg)', color: 'error.main' }} />;
      default:
        return <Timeline color="info" />;
    }
  };

  return (
    <Box>
      {/* Connection Status */}
      <Paper sx={{ p: 2, mb: 3, bgcolor: isConnected ? 'success.light' : 'error.light' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {isConnected ? <SignalWifi4Bar /> : <SignalWifiOff />}
            <Typography variant="h6">Real-time Dashboard - {connectionState}</Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Tooltip title="Connected Users">
              <Badge badgeContent={connectedUsers.length} color="primary">
                <People />
              </Badge>
            </Tooltip>
            <Typography variant="body2">
              Last Update: {stats.lastUpdate.toLocaleTimeString()}
            </Typography>
          </Box>
        </Box>
      </Paper>

      <Grid container spacing={3}>
        {/* System Overview */}
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Speed sx={{ mr: 1 }} />
                <Typography variant="h6">System Health</Typography>
              </Box>
              <Box sx={{ textAlign: 'center' }}>
                {getStatusIcon(stats.systemStatus)}
                <Typography variant="h4" sx={{ mt: 1 }}>
                  {stats.systemStatus}
                </Typography>
                <Chip
                  label={`${stats.healthyKpis}/${stats.totalKpis} KPIs Healthy`}
                  color={getStatusColor(stats.systemStatus)}
                  size="small"
                  sx={{ mt: 1 }}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Real-time Metrics */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Real-time Metrics
              </Typography>
              <Grid container spacing={2}>
                {realtimeMetrics.map(metric => (
                  <Grid item xs={6} sm={4} md={3} key={metric.id}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                      <Box
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          mb: 1,
                        }}
                      >
                        {getTrendIcon(metric.trend)}
                        <Circle
                          sx={{
                            fontSize: 8,
                            ml: 1,
                            color: `${getStatusColor(metric.status)}.main`,
                          }}
                        />
                      </Box>
                      <Typography variant="h6">{metric.value.toFixed(2)}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {metric.name}
                      </Typography>
                    </Paper>
                  </Grid>
                ))}
              </Grid>
              {realtimeMetrics.length === 0 && (
                <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
                  Waiting for real-time data...
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Live Alerts */}
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Badge badgeContent={realtimeAlerts.length} color="error">
                  <Notifications />
                </Badge>
                <Typography variant="h6" sx={{ ml: 1 }}>
                  Live Alerts
                </Typography>
              </Box>
              <List dense>
                {realtimeAlerts.map((alert, index) => (
                  <React.Fragment key={`${alert.id}-${index}`}>
                    <ListItem>
                      <ListItemIcon>
                        <Circle
                          sx={{
                            fontSize: 12,
                            color: alert.severity === 'Critical' ? 'error.main' : 'warning.main',
                          }}
                        />
                      </ListItemIcon>
                      <ListItemText
                        primary={alert.message}
                        secondary={new Date(alert.timestamp).toLocaleTimeString()}
                        primaryTypographyProps={{ variant: 'body2' }}
                        secondaryTypographyProps={{ variant: 'caption' }}
                      />
                    </ListItem>
                    {index < realtimeAlerts.length - 1 && <Divider />}
                  </React.Fragment>
                ))}
              </List>
              {realtimeAlerts.length === 0 && (
                <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                  No recent alerts
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Connected Users */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Connected Users ({connectedUsers.length})
              </Typography>
              <Stack direction="row" spacing={1} flexWrap="wrap">
                {connectedUsers.map(userId => (
                  <Tooltip key={userId} title={`User: ${userId}`}>
                    <Avatar sx={{ width: 32, height: 32 }}>{userId.charAt(0).toUpperCase()}</Avatar>
                  </Tooltip>
                ))}
              </Stack>
              {connectedUsers.length === 0 && (
                <Typography color="text.secondary">No other users connected</Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default RealTimeDashboard;

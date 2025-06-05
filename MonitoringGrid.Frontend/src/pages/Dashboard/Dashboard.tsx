import React from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  LinearProgress,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Warning,
  CheckCircle,
  Error,
  PlayArrow,
  Refresh,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { kpiApi, alertApi } from '@/services/api';
import { format } from 'date-fns';

const Dashboard: React.FC = () => {
  const navigate = useNavigate();

  // Fetch dashboard data
  const { data: kpiDashboard, isLoading: kpiLoading, refetch: refetchKpi } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const { data: alertDashboard, isLoading: alertLoading, refetch: refetchAlert } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const handleRefresh = () => {
    refetchKpi();
    refetchAlert();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return 'success';
      case 'due':
      case 'due soon':
        return 'warning';
      case 'error':
      case 'never run':
        return 'error';
      default:
        return 'default';
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'error';
      case 'high':
        return 'warning';
      case 'medium':
        return 'info';
      case 'low':
        return 'success';
      default:
        return 'default';
    }
  };

  if (kpiLoading || alertLoading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <LinearProgress />
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <IconButton onClick={handleRefresh} color="primary">
          <Refresh />
        </IconButton>
      </Box>

      <Grid container spacing={3}>
        {/* KPI Overview Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Total KPIs
              </Typography>
              <Typography variant="h4">
                {kpiDashboard?.totalKpis || 0}
              </Typography>
              <Box display="flex" alignItems="center" mt={1}>
                <Chip
                  label={`${kpiDashboard?.activeKpis || 0} Active`}
                  color="success"
                  size="small"
                />
                <Chip
                  label={`${kpiDashboard?.inactiveKpis || 0} Inactive`}
                  color="default"
                  size="small"
                  sx={{ ml: 1 }}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                KPIs Due
              </Typography>
              <Typography variant="h4" color={kpiDashboard?.kpisDue ? 'warning.main' : 'text.primary'}>
                {kpiDashboard?.kpisDue || 0}
              </Typography>
              <Box display="flex" alignItems="center" mt={1}>
                {kpiDashboard?.kpisDue ? (
                  <Chip
                    label="Needs Attention"
                    color="warning"
                    size="small"
                    icon={<Warning />}
                  />
                ) : (
                  <Chip
                    label="All Up to Date"
                    color="success"
                    size="small"
                    icon={<CheckCircle />}
                  />
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Alerts Today
              </Typography>
              <Typography variant="h4">
                {alertDashboard?.totalAlertsToday || 0}
              </Typography>
              <Box display="flex" alignItems="center" mt={1}>
                {alertDashboard?.alertTrendPercentage !== undefined && (
                  <Chip
                    label={`${alertDashboard.alertTrendPercentage > 0 ? '+' : ''}${alertDashboard.alertTrendPercentage.toFixed(1)}%`}
                    color={alertDashboard.alertTrendPercentage > 0 ? 'error' : 'success'}
                    size="small"
                    icon={alertDashboard.alertTrendPercentage > 0 ? <TrendingUp /> : <TrendingDown />}
                  />
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Unresolved Alerts
              </Typography>
              <Typography variant="h4" color={alertDashboard?.unresolvedAlerts ? 'error.main' : 'text.primary'}>
                {alertDashboard?.unresolvedAlerts || 0}
              </Typography>
              <Box display="flex" alignItems="center" mt={1}>
                <Chip
                  label={`${alertDashboard?.criticalAlerts || 0} Critical`}
                  color="error"
                  size="small"
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Alerts */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">
                  Recent Alerts
                </Typography>
                <IconButton
                  size="small"
                  onClick={() => navigate('/alerts')}
                  color="primary"
                >
                  <PlayArrow />
                </IconButton>
              </Box>
              
              {alertDashboard?.recentAlerts && alertDashboard.recentAlerts.length > 0 ? (
                <List dense>
                  {alertDashboard.recentAlerts.slice(0, 5).map((alert) => (
                    <ListItem
                      key={alert.alertId}
                      button
                      onClick={() => navigate(`/alerts/${alert.alertId}`)}
                    >
                      <ListItemIcon>
                        <Chip
                          label={alert.severity}
                          color={getSeverityColor(alert.severity)}
                          size="small"
                        />
                      </ListItemIcon>
                      <ListItemText
                        primary={alert.kpiIndicator}
                        secondary={
                          <Box>
                            <Typography variant="body2" color="textSecondary">
                              {alert.message}
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                              {format(new Date(alert.triggerTime), 'MMM dd, HH:mm')}
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="textSecondary" align="center" py={2}>
                  No recent alerts
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* KPIs Due for Execution */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">
                  KPIs Due for Execution
                </Typography>
                <IconButton
                  size="small"
                  onClick={() => navigate('/kpis')}
                  color="primary"
                >
                  <PlayArrow />
                </IconButton>
              </Box>
              
              {kpiDashboard?.dueKpis && kpiDashboard.dueKpis.length > 0 ? (
                <List dense>
                  {kpiDashboard.dueKpis.slice(0, 5).map((kpi) => (
                    <ListItem
                      key={kpi.kpiId}
                      button
                      onClick={() => navigate(`/kpis/${kpi.kpiId}`)}
                    >
                      <ListItemIcon>
                        <Chip
                          label={kpi.status}
                          color={getStatusColor(kpi.status)}
                          size="small"
                        />
                      </ListItemIcon>
                      <ListItemText
                        primary={kpi.indicator}
                        secondary={
                          <Box>
                            <Typography variant="body2" color="textSecondary">
                              Owner: {kpi.owner}
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                              Frequency: {kpi.frequency} minutes
                              {kpi.lastRun && ` • Last run: ${format(new Date(kpi.lastRun), 'MMM dd, HH:mm')}`}
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="textSecondary" align="center" py={2}>
                  All KPIs are up to date
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Top Alerting KPIs */}
        {alertDashboard?.topAlertingKpis && alertDashboard.topAlertingKpis.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Top Alerting KPIs (Last 7 Days)
                </Typography>
                <List dense>
                  {alertDashboard.topAlertingKpis.slice(0, 5).map((kpi) => (
                    <ListItem
                      key={kpi.kpiId}
                      button
                      onClick={() => navigate(`/kpis/${kpi.kpiId}`)}
                    >
                      <ListItemText
                        primary={kpi.indicator}
                        secondary={
                          <Box display="flex" alignItems="center" gap={1}>
                            <Typography variant="body2" color="textSecondary">
                              Owner: {kpi.owner}
                            </Typography>
                            <Chip
                              label={`${kpi.alertCount} alerts`}
                              color="error"
                              size="small"
                            />
                            {kpi.unresolvedCount > 0 && (
                              <Chip
                                label={`${kpi.unresolvedCount} unresolved`}
                                color="warning"
                                size="small"
                              />
                            )}
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default Dashboard;

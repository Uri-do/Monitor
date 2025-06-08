import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  Button,
  Alert,
  AlertTitle,
  Tabs,
  Tab,
  Paper,
  CircularProgress,
  IconButton,
  Tooltip,
  LinearProgress,
} from '@mui/material';
import {
  Timeline as Activity,
  Warning as AlertTriangle,
  CheckCircle,
  TrendingUp,
  TrendingDown,
  Refresh,
  Wifi,
  WifiOff,
} from '@mui/icons-material';
import {
  useSystemAnalytics,
  useSystemHealth,
  useLiveDashboard,
  useRealtimeStatus,
  useCriticalAlerts,
} from '@/hooks/useEnhancedApi';
import { useSignalR } from '@/services/signalRService';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';

const COLORS = ['#4caf50', '#ff9800', '#f44336', '#9e9e9e'];

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`enhanced-tabpanel-${index}`}
      aria-labelledby={`enhanced-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

interface EnhancedDashboardProps {
  className?: string;
}

export const EnhancedDashboard: React.FC<EnhancedDashboardProps> = ({ className }) => {
  const [selectedPeriod, setSelectedPeriod] = useState(30);
  const [tabValue, setTabValue] = useState(0);

  const {
    data: systemAnalytics,
    loading: analyticsLoading,
    refetch: refetchAnalytics,
  } = useSystemAnalytics(selectedPeriod);
  const {
    data: systemHealth,
    loading: healthLoading,
    refetch: refetchHealth,
  } = useSystemHealth(true, 30000);
  const { data: liveDashboard, loading: dashboardLoading } = useLiveDashboard(true, 10000);
  const { data: realtimeStatus, loading: statusLoading } = useRealtimeStatus(true, 5000);
  const { data: criticalAlerts, loading: alertsLoading } = useCriticalAlerts(true, 15000);

  const { isConnected, connectionState } = useSignalR();

  const getHealthColor = (score: number) => {
    if (score >= 90) return 'success';
    if (score >= 70) return 'warning';
    return 'error';
  };

  const getHealthIcon = (score: number) => {
    if (score >= 90) return <CheckCircle color="success" />;
    if (score >= 70) return <AlertTriangle color="warning" />;
    return <AlertTriangle color="error" />;
  };

  const formatTrendData = (trends: any[]) => {
    return (
      trends?.map(trend => ({
        date: new Date(trend.date).toLocaleDateString(),
        value: trend.value,
      })) || []
    );
  };

  const formatHealthDistribution = (distribution: any) => {
    if (!distribution) return [];
    return [
      { name: 'Healthy', value: distribution.healthy, color: COLORS[0] },
      { name: 'Warning', value: distribution.warning, color: COLORS[1] },
      { name: 'Critical', value: distribution.critical, color: COLORS[2] },
      { name: 'Inactive', value: distribution.inactive, color: COLORS[3] },
    ];
  };

  const handleRefresh = () => {
    refetchAnalytics();
    refetchHealth();
  };

  if (analyticsLoading || healthLoading || dashboardLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress size={40} />
      </Box>
    );
  }

  return (
    <Box className={className} sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Enhanced Monitoring Dashboard
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
          <Chip
            icon={isConnected ? <Wifi /> : <WifiOff />}
            label={isConnected ? 'Live' : 'Offline'}
            color={isConnected ? 'success' : 'error'}
            variant="outlined"
          />
          <Typography variant="body2" color="text.secondary">
            {connectionState}
          </Typography>
          <Tooltip title="Refresh Data">
            <IconButton onClick={handleRefresh} size="small">
              <Refresh />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Period Selector */}
      <Box mb={3}>
        {[7, 30, 90].map(days => (
          <Button
            key={days}
            variant={selectedPeriod === days ? 'contained' : 'outlined'}
            size="small"
            onClick={() => setSelectedPeriod(days)}
            sx={{ mr: 1 }}
          >
            {days} days
          </Button>
        ))}
      </Box>

      {/* Critical Alerts Banner */}
      {criticalAlerts && criticalAlerts.length > 0 && (
        <Alert severity="error" sx={{ mb: 3 }}>
          <AlertTitle>Critical Alerts</AlertTitle>
          <strong>{criticalAlerts.length} critical alerts</strong> require immediate attention.
          <Button variant="text" size="small" sx={{ ml: 2 }}>
            View Details
          </Button>
        </Alert>
      )}

      {/* System Issues */}
      {systemHealth?.issues && systemHealth.issues.length > 0 && (
        <Alert severity="error" sx={{ mb: 3 }}>
          <AlertTitle>System Issues Detected</AlertTitle>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {systemHealth.issues.map((issue, index) => (
              <li key={index}>{issue}</li>
            ))}
          </ul>
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
                    color={`${getHealthColor(systemHealth?.overallHealthScore || 0)}.main`}
                  >
                    {systemHealth?.overallHealthScore?.toFixed(1) || 0}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {systemHealth?.systemStatus || 'Unknown'}
                  </Typography>
                </Box>
                {getHealthIcon(systemHealth?.overallHealthScore || 0)}
              </Box>
              <LinearProgress
                variant="determinate"
                value={systemHealth?.overallHealthScore || 0}
                color={getHealthColor(systemHealth?.overallHealthScore || 0)}
                sx={{ mt: 1 }}
              />
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
                    Recent Alerts
                  </Typography>
                  <Typography variant="h4" component="div">
                    {realtimeStatus?.recentAlerts || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {realtimeStatus?.unresolvedAlerts || 0} unresolved
                  </Typography>
                </Box>
                <AlertTriangle color="warning" />
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
                <Activity color="primary" />
              </Box>
              <LinearProgress
                variant="determinate"
                value={realtimeStatus?.systemLoad || 0}
                color={
                  realtimeStatus?.systemLoad && realtimeStatus.systemLoad > 80 ? 'error' : 'primary'
                }
                sx={{ mt: 1 }}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Paper sx={{ width: '100%' }}>
        <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
          <Tab label="Overview" />
          <Tab label="Analytics" />
          <Tab label="Trends" />
          <Tab label="Health" />
        </Tabs>

        <TabPanel value={tabValue} index={0}>
          <Grid container spacing={3}>
            {/* KPI Health Distribution */}
            <Grid item xs={12} lg={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    KPI Health Distribution
                  </Typography>
                  <Box height={300}>
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={formatHealthDistribution(systemAnalytics?.kpiHealthDistribution)}
                          cx="50%"
                          cy="50%"
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                          label={({ name, value }) => `${name}: ${value}`}
                        >
                          {formatHealthDistribution(systemAnalytics?.kpiHealthDistribution).map(
                            (entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.color} />
                            )
                          )}
                        </Pie>
                        <RechartsTooltip />
                      </PieChart>
                    </ResponsiveContainer>
                  </Box>
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
                  <Box>
                    {liveDashboard?.recentExecutions?.slice(0, 5).map((execution, index) => (
                      <Box
                        key={index}
                        display="flex"
                        justifyContent="space-between"
                        alignItems="center"
                        p={2}
                        border={1}
                        borderColor="grey.300"
                        borderRadius={1}
                        mb={1}
                        sx={{
                          backgroundColor: execution.isSuccessful ? 'success.light' : 'error.light',
                          opacity: 0.8,
                        }}
                      >
                        <Box>
                          <Typography variant="body1" fontWeight="medium">
                            {execution.indicator}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {new Date(execution.timestamp).toLocaleTimeString()}
                          </Typography>
                        </Box>
                        <Box textAlign="right">
                          <Chip
                            label={execution.isSuccessful ? 'Success' : 'Failed'}
                            color={execution.isSuccessful ? 'success' : 'error'}
                            size="small"
                            icon={execution.isSuccessful ? <CheckCircle /> : <AlertTriangle />}
                          />
                          <Typography variant="body2" color="text.secondary">
                            {execution.executionTimeMs}ms
                          </Typography>
                        </Box>
                      </Box>
                    ))}
                    {(!liveDashboard?.recentExecutions ||
                      liveDashboard.recentExecutions.length === 0) && (
                      <Box textAlign="center" py={4}>
                        <Typography color="text.secondary">No recent executions</Typography>
                      </Box>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography variant="h6" color="text.secondary">
                    Total Executions
                  </Typography>
                  <Typography variant="h4" component="div">
                    {systemAnalytics?.totalExecutions || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg: {systemAnalytics?.averageExecutionsPerDay?.toFixed(1) || 0}/day
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography variant="h6" color="text.secondary">
                    Total Alerts
                  </Typography>
                  <Typography variant="h4" component="div">
                    {systemAnalytics?.totalAlerts || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg: {systemAnalytics?.averageAlertsPerDay?.toFixed(1) || 0}/day
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography variant="h6" color="text.secondary">
                    Resolved Alerts
                  </Typography>
                  <Typography variant="h4" component="div" color="success.main">
                    {systemAnalytics?.resolvedAlerts || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {systemAnalytics?.totalAlerts
                      ? (
                          (systemAnalytics.resolvedAlerts / systemAnalytics.totalAlerts) *
                          100
                        ).toFixed(1)
                      : 0}
                    % resolution rate
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography variant="h6" color="text.secondary">
                    Critical Alerts
                  </Typography>
                  <Typography variant="h4" component="div" color="error.main">
                    {systemAnalytics?.criticalAlerts || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Require immediate attention
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Grid container spacing={3}>
            {/* Alert Trends */}
            <Grid item xs={12} lg={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Alert Trends
                  </Typography>
                  <Box height={300}>
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={formatTrendData(systemAnalytics?.alertTrends || [])}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis />
                        <RechartsTooltip />
                        <Line type="monotone" dataKey="value" stroke="#f44336" strokeWidth={2} />
                      </LineChart>
                    </ResponsiveContainer>
                  </Box>
                </CardContent>
              </Card>
            </Grid>

            {/* Execution Trends */}
            <Grid item xs={12} lg={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Execution Trends
                  </Typography>
                  <Box height={300}>
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={formatTrendData(systemAnalytics?.executionTrends || [])}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis />
                        <RechartsTooltip />
                        <Line type="monotone" dataKey="value" stroke="#4caf50" strokeWidth={2} />
                      </LineChart>
                    </ResponsiveContainer>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Grid container spacing={3}>
            {/* System Recommendations */}
            <Grid item xs={12} lg={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    System Recommendations
                  </Typography>
                  <Box>
                    {systemHealth?.recommendations?.map((recommendation, index) => (
                      <Alert key={index} severity="info" sx={{ mb: 2 }}>
                        {recommendation}
                      </Alert>
                    ))}
                    {(!systemHealth?.recommendations ||
                      systemHealth.recommendations.length === 0) && (
                      <Box textAlign="center" py={4}>
                        <CheckCircle sx={{ fontSize: 48, color: 'success.main', mb: 2 }} />
                        <Typography color="text.secondary">
                          No recommendations at this time
                        </Typography>
                      </Box>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>

            {/* Top Performing KPIs */}
            <Grid item xs={12} lg={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Top Performing KPIs
                  </Typography>
                  <Box>
                    {systemAnalytics?.topPerformingKpis?.map((kpi, index) => (
                      <Box
                        key={index}
                        display="flex"
                        justifyContent="space-between"
                        alignItems="center"
                        p={2}
                        border={1}
                        borderColor="grey.300"
                        borderRadius={1}
                        mb={1}
                        sx={{ backgroundColor: 'success.light', opacity: 0.8 }}
                      >
                        <Box>
                          <Typography variant="body1" fontWeight="medium">
                            {kpi.indicator}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {kpi.owner}
                          </Typography>
                        </Box>
                        <Box textAlign="right">
                          <Typography variant="h6" color="success.main">
                            {kpi.performanceScore.toFixed(1)}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Score
                          </Typography>
                        </Box>
                      </Box>
                    ))}
                    {(!systemAnalytics?.topPerformingKpis ||
                      systemAnalytics.topPerformingKpis.length === 0) && (
                      <Box textAlign="center" py={4}>
                        <Typography color="text.secondary">
                          No performance data available
                        </Typography>
                      </Box>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>
    </Box>
  );
};

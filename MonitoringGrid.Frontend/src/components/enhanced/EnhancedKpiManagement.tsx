import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  Tooltip,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
} from '@mui/material';
import {
  PlayArrow,
  Edit,
  Delete,
  BarChart,
  Add,
  Refresh,
  Speed,
  CheckCircle,
  Warning,
  Error,
  Pause,
} from '@mui/icons-material';
import {
  useKpiPerformanceAnalytics,
  useRealtimeKpiExecution,
  useManualAlert,
} from '@/hooks/useEnhancedApi';
import { useKpis } from '@/hooks/useKpis';
import { KpiDto } from '@/types/api';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
} from 'recharts';

interface EnhancedKpiManagementProps {
  className?: string;
}

export const EnhancedKpiManagement: React.FC<EnhancedKpiManagementProps> = ({ className }) => {
  const [selectedKpi, setSelectedKpi] = useState<KpiDto | null>(null);
  const [isAnalyticsDialogOpen, setIsAnalyticsDialogOpen] = useState(false);
  const [filterOwner, setFilterOwner] = useState('');
  const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [analyticsPeriod, setAnalyticsPeriod] = useState(30);

  const {
    data: kpis,
    loading: kpisLoading,
    error: kpisError,
    refetch: refetchKpis,
    executeKpi,
    deleteKpi,
  } = useKpis();

  const { data: kpiAnalytics, loading: analyticsLoading } = useKpiPerformanceAnalytics(
    selectedKpi?.kpiId || 0,
    analyticsPeriod
  );

  const { executeKpi: executeKpiRealtime, loading: executingRealtime } = useRealtimeKpiExecution();
  const { sendAlert: sendManualAlert, loading: sendingAlert } = useManualAlert();

  // Filter KPIs based on current filters
  const filteredKpis =
    kpis?.filter(kpi => {
      const matchesOwner =
        !filterOwner || kpi.owner.toLowerCase().includes(filterOwner.toLowerCase());
      const matchesStatus =
        filterStatus === 'all' ||
        (filterStatus === 'active' && kpi.isActive) ||
        (filterStatus === 'inactive' && !kpi.isActive);
      return matchesOwner && matchesStatus;
    }) || [];

  const getKpiStatusColor = (kpi: KpiDto) => {
    if (!kpi.isActive) return 'default';
    if (kpi.lastRun) {
      const nextDue = new Date(kpi.lastRun);
      nextDue.setMinutes(nextDue.getMinutes() + kpi.frequency);
      const now = new Date();
      if (now > nextDue) return 'error';
      if (now > new Date(nextDue.getTime() - kpi.frequency * 0.5 * 60000)) return 'warning';
    }
    return 'success';
  };

  const getKpiStatusText = (kpi: KpiDto) => {
    if (!kpi.isActive) return 'Inactive';
    if (kpi.lastRun) {
      const nextDue = new Date(kpi.lastRun);
      nextDue.setMinutes(nextDue.getMinutes() + kpi.frequency);
      const now = new Date();
      if (now > nextDue) return 'Overdue';
      if (now > new Date(nextDue.getTime() - kpi.frequency * 0.5 * 60000)) return 'Due Soon';
    }
    return 'Healthy';
  };

  const getKpiStatusIcon = (kpi: KpiDto) => {
    const status = getKpiStatusColor(kpi);
    switch (status) {
      case 'success':
        return <CheckCircle color="success" />;
      case 'warning':
        return <Warning color="warning" />;
      case 'error':
        return <Error color="error" />;
      default:
        return <Pause color="disabled" />;
    }
  };

  const handleExecuteKpi = async (kpi: KpiDto, realtime: boolean = false) => {
    try {
      if (realtime) {
        await executeKpiRealtime(kpi.kpiId);
      } else {
        await executeKpi(kpi.kpiId);
      }
      refetchKpis();
    } catch (error) {
      console.error('Failed to execute KPI:', error);
    }
  };

  const handleSendManualAlert = async (kpi: KpiDto) => {
    try {
      await sendManualAlert(
        kpi.kpiId,
        `Manual alert for ${kpi.indicator}`,
        'Alert sent manually from KPI management interface',
        kpi.priority
      );
    } catch (error) {
      console.error('Failed to send manual alert:', error);
    }
  };

  const handleDeleteKpi = async (kpiId: number) => {
    if (window.confirm('Are you sure you want to delete this KPI?')) {
      try {
        await deleteKpi(kpiId);
        refetchKpis();
      } catch (error) {
        console.error('Failed to delete KPI:', error);
      }
    }
  };

  const formatTrendData = (trends: any[]) => {
    return (
      trends?.map(trend => ({
        timestamp: new Date(trend.timestamp).toLocaleDateString(),
        value: trend.value,
        deviation: Math.abs(trend.deviationPercent),
        successful: trend.isSuccessful,
      })) || []
    );
  };

  if (kpisLoading) {
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
          Enhanced KPI Management
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
          <Button variant="contained" startIcon={<Add />}>
            Create KPI
          </Button>
          <Tooltip title="Refresh KPIs">
            <IconButton onClick={refetchKpis}>
              <Refresh />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Filters */}
      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} md={6}>
          <TextField
            fullWidth
            label="Filter by Owner"
            placeholder="Enter owner email..."
            value={filterOwner}
            onChange={e => setFilterOwner(e.target.value)}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <FormControl fullWidth>
            <InputLabel>Status</InputLabel>
            <Select
              value={filterStatus}
              label="Status"
              onChange={e => setFilterStatus(e.target.value as any)}
            >
              <MenuItem value="all">All</MenuItem>
              <MenuItem value="active">Active</MenuItem>
              <MenuItem value="inactive">Inactive</MenuItem>
            </Select>
          </FormControl>
        </Grid>
        <Grid item xs={12} md={3}>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            Showing {filteredKpis.length} of {kpis?.length || 0} KPIs
          </Typography>
        </Grid>
      </Grid>

      {/* KPI Grid */}
      <Grid container spacing={3}>
        {filteredKpis.map(kpi => (
          <Grid item xs={12} md={6} lg={4} key={kpi.kpiId}>
            <Card sx={{ height: '100%' }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
                  <Box flex={1}>
                    <Typography variant="h6" gutterBottom>
                      {kpi.indicator}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      {kpi.owner}
                    </Typography>
                  </Box>
                  <Box display="flex" alignItems="center" gap={1}>
                    {getKpiStatusIcon(kpi)}
                    <Chip
                      label={getKpiStatusText(kpi)}
                      color={getKpiStatusColor(kpi)}
                      size="small"
                    />
                  </Box>
                </Box>

                <Grid container spacing={2} sx={{ mb: 2 }}>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">
                      Frequency
                    </Typography>
                    <Typography variant="body2" fontWeight="medium">
                      {kpi.frequency} min
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">
                      Priority
                    </Typography>
                    <Typography variant="body2" fontWeight="medium">
                      {kpi.priority === 1 ? 'SMS + Email' : 'Email Only'}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">
                      Deviation
                    </Typography>
                    <Typography variant="body2" fontWeight="medium">
                      {kpi.deviation}%
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">
                      Last Run
                    </Typography>
                    <Typography variant="body2" fontWeight="medium">
                      {kpi.lastRun ? new Date(kpi.lastRun).toLocaleDateString() : 'Never'}
                    </Typography>
                  </Grid>
                </Grid>

                <Box display="flex" flexWrap="wrap" gap={1} mb={2}>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => handleExecuteKpi(kpi)}
                    disabled={executingRealtime}
                    startIcon={<PlayArrow />}
                  >
                    Execute
                  </Button>
                  <Button
                    size="small"
                    variant="contained"
                    color="primary"
                    onClick={() => handleExecuteKpi(kpi, true)}
                    disabled={executingRealtime}
                    startIcon={<Speed />}
                  >
                    Real-time
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => {
                      setSelectedKpi(kpi);
                      setIsAnalyticsDialogOpen(true);
                    }}
                    startIcon={<BarChart />}
                  >
                    Analytics
                  </Button>
                </Box>

                <Box display="flex" justifyContent="space-between">
                  <Button size="small" variant="outlined" startIcon={<Edit />}>
                    Edit
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    color="error"
                    onClick={() => handleDeleteKpi(kpi.kpiId)}
                    startIcon={<Delete />}
                  >
                    Delete
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      {/* No KPIs */}
      {filteredKpis.length === 0 && (
        <Card>
          <CardContent>
            <Box textAlign="center" py={8}>
              <BarChart sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h5" color="text.secondary" gutterBottom>
                No KPIs found
              </Typography>
              <Typography color="text.secondary" mb={3}>
                {filterOwner || filterStatus !== 'all'
                  ? 'No KPIs match your current filters.'
                  : 'Get started by creating your first KPI.'}
              </Typography>
              {!filterOwner && filterStatus === 'all' && (
                <Button variant="contained" startIcon={<Add />}>
                  Create Your First KPI
                </Button>
              )}
            </Box>
          </CardContent>
        </Card>
      )}

      {/* Analytics Dialog */}
      <Dialog
        open={isAnalyticsDialogOpen}
        onClose={() => setIsAnalyticsDialogOpen(false)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>KPI Performance Analytics - {selectedKpi?.indicator}</DialogTitle>
        <DialogContent>
          {selectedKpi && (
            <Box sx={{ pt: 2 }}>
              {/* Period Selector */}
              <Box mb={3}>
                {[7, 30, 90].map(days => (
                  <Button
                    key={days}
                    variant={analyticsPeriod === days ? 'contained' : 'outlined'}
                    size="small"
                    onClick={() => setAnalyticsPeriod(days)}
                    sx={{ mr: 1 }}
                  >
                    {days} days
                  </Button>
                ))}
              </Box>

              {analyticsLoading ? (
                <Box display="flex" justifyContent="center" py={4}>
                  <CircularProgress />
                </Box>
              ) : kpiAnalytics ? (
                <Box>
                  {/* Overview Stats */}
                  <Grid container spacing={3} mb={3}>
                    <Grid item xs={6} md={3}>
                      <Card>
                        <CardContent sx={{ textAlign: 'center' }}>
                          <Typography variant="h4" color="primary.main">
                            {kpiAnalytics.totalExecutions}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Total Executions
                          </Typography>
                        </CardContent>
                      </Card>
                    </Grid>
                    <Grid item xs={6} md={3}>
                      <Card>
                        <CardContent sx={{ textAlign: 'center' }}>
                          <Typography variant="h4" color="success.main">
                            {kpiAnalytics.successRate.toFixed(1)}%
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Success Rate
                          </Typography>
                        </CardContent>
                      </Card>
                    </Grid>
                    <Grid item xs={6} md={3}>
                      <Card>
                        <CardContent sx={{ textAlign: 'center' }}>
                          <Typography variant="h4" color="error.main">
                            {kpiAnalytics.totalAlerts}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Total Alerts
                          </Typography>
                        </CardContent>
                      </Card>
                    </Grid>
                    <Grid item xs={6} md={3}>
                      <Card>
                        <CardContent sx={{ textAlign: 'center' }}>
                          <Typography variant="h4" color="info.main">
                            {kpiAnalytics.performanceScore.toFixed(1)}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Performance Score
                          </Typography>
                        </CardContent>
                      </Card>
                    </Grid>
                  </Grid>

                  {/* Performance Trends Chart */}
                  <Card sx={{ mb: 3 }}>
                    <CardContent>
                      <Typography variant="h6" gutterBottom>
                        Performance Trends
                      </Typography>
                      <Box height={300}>
                        <ResponsiveContainer width="100%" height="100%">
                          <LineChart data={formatTrendData(kpiAnalytics.detailedTrends)}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey="timestamp" />
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

                  {/* Recommendations */}
                  {kpiAnalytics.recommendations.length > 0 && (
                    <Card>
                      <CardContent>
                        <Typography variant="h6" gutterBottom>
                          Performance Recommendations
                        </Typography>
                        <List>
                          {kpiAnalytics.recommendations.map((recommendation, index) => (
                            <ListItem key={index}>
                              <ListItemIcon>
                                <CheckCircle color="info" />
                              </ListItemIcon>
                              <ListItemText primary={recommendation} />
                            </ListItem>
                          ))}
                        </List>
                      </CardContent>
                    </Card>
                  )}
                </Box>
              ) : (
                <Box textAlign="center" py={4}>
                  <Typography color="text.secondary">
                    No analytics data available for this KPI.
                  </Typography>
                </Box>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsAnalyticsDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

import React, { useState } from 'react';
import { Box, Grid, CardContent, Typography, Chip, Stack, Paper } from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Assessment,
  Timeline,
  PieChart,
  BarChart,
  ShowChart,
  Warning,
  CheckCircle,
  Error,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart as RechartsBarChart,
  Bar,
  PieChart as RechartsPieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { useAlertStatistics } from '@/hooks/useAlerts';
import { useIndicatorDashboard } from '@/hooks/useIndicators';
import { PageHeader, LoadingSpinner, StatusChip, Card, Select, MetricCard } from '@/components/UI';

// Analytics data will be loaded from real API endpoints
// No mock data - show empty state when no real data is available

const Analytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState('7d');

  // Use enhanced hooks for data fetching
  const timeRangeDays = timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : 90;
  const { data: alertStats, isLoading: alertStatsLoading } = useAlertStatistics(timeRangeDays);
  const { data: indicatorDashboard, isLoading: indicatorLoading } = useIndicatorDashboard();

  const isLoading = alertStatsLoading || indicatorLoading;

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title="Analytics & Insights"
        subtitle="Comprehensive monitoring system analytics and performance metrics"
        secondaryActions={[
          {
            label: 'Export Report',
            icon: <Assessment />,
            onClick: () => {
              // TODO: Implement export functionality
              console.log('Export report');
            },
            gradient: 'info',
          },
        ]}
      />

      {/* Time Range Selector */}
      <Box sx={{ mb: 3 }}>
        <Select
          label="Time Range"
          value={timeRange}
          onChange={e => setTimeRange(e.target.value as string)}
          options={[
            { value: '7d', label: 'Last 7 Days' },
            { value: '30d', label: 'Last 30 Days' },
            { value: '90d', label: 'Last 90 Days' },
          ]}
          sx={{ minWidth: 200 }}
        />
      </Box>

      <Grid container spacing={3}>
        {/* Key Metrics Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Total KPIs"
            value={(indicatorDashboard?.totalIndicators || 0).toString()}
            icon={<Assessment />}
            gradient="primary"
            chip={{
              label: `${indicatorDashboard?.activeIndicators || 0} Active`,
              color: 'success',
            }}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Alerts Today"
            value={(alertStats?.alertsToday || 0).toString()}
            icon={<Warning />}
            gradient="warning"
            chip={{
              label: `${alertStats?.unresolvedAlerts || 0} Unresolved`,
              color: 'error',
            }}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Success Rate"
            value={alertStats?.successRate ? `${alertStats.successRate.toFixed(1)}%` : 'N/A'}
            icon={<CheckCircle />}
            gradient="success"
            subtitle="Based on recent executions"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Avg Response"
            value={alertStats?.avgResponseTime ? `${alertStats.avgResponseTime}ms` : 'N/A'}
            icon={<Timeline />}
            gradient="info"
            subtitle="Average execution time"
          />
        </Grid>

        {/* Alert Trends Chart */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Alert Trends Over Time
              </Typography>
              <ResponsiveContainer width="100%" height={300}>
                {alertStats?.trendData && alertStats.trendData.length > 0 ? (
                  <AreaChart data={alertStats.trendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Area
                      type="monotone"
                      dataKey="alerts"
                      stackId="1"
                      stroke="#f44336"
                      fill="#f44336"
                      fillOpacity={0.6}
                      name="Alerts"
                    />
                    <Area
                      type="monotone"
                      dataKey="kpiExecutions"
                      stackId="2"
                      stroke="#2196f3"
                      fill="#2196f3"
                      fillOpacity={0.6}
                      name="KPI Executions"
                    />
                  </AreaChart>
                ) : (
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      height: '100%',
                    }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      No trend data available
                    </Typography>
                  </Box>
                )}
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Grid>

        {/* Alert Severity Distribution */}
        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Alert Severity Distribution
              </Typography>
              <ResponsiveContainer width="100%" height={300}>
                {alertStats?.severityDistribution && alertStats.severityDistribution.length > 0 ? (
                  <RechartsPieChart>
                    <Pie
                      data={alertStats.severityDistribution}
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      dataKey="value"
                      label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                    >
                      {alertStats.severityDistribution.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </RechartsPieChart>
                ) : (
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      height: '100%',
                    }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      No severity data available
                    </Typography>
                  </Box>
                )}
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Grid>

        {/* KPI Performance Table */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                KPI Performance Summary
              </Typography>
              <Box sx={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid #e0e0e0' }}>
                      <th style={{ textAlign: 'left', padding: '12px' }}>KPI Name</th>
                      <th style={{ textAlign: 'center', padding: '12px' }}>Executions</th>
                      <th style={{ textAlign: 'center', padding: '12px' }}>Alerts</th>
                      <th style={{ textAlign: 'center', padding: '12px' }}>Success Rate</th>
                      <th style={{ textAlign: 'center', padding: '12px' }}>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {/* Recent executions not available in IndicatorDashboardDto - showing placeholder */}
                    {false ? (
                      [].map((execution: any, index: number) => (
                        <tr key={index} style={{ borderBottom: '1px solid #f0f0f0' }}>
                          <td style={{ padding: '12px', fontWeight: 'medium' }}>
                            {execution.indicator}
                          </td>
                          <td style={{ textAlign: 'center', padding: '12px' }}>1</td>
                          <td style={{ textAlign: 'center', padding: '12px' }}>0</td>
                          <td style={{ textAlign: 'center', padding: '12px' }}>
                            {execution.isSuccessful ? '100' : '0'}%
                          </td>
                          <td style={{ textAlign: 'center', padding: '12px' }}>
                            <StatusChip status={execution.isSuccessful ? 'success' : 'error'} />
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} style={{ textAlign: 'center', padding: '24px' }}>
                          <Typography variant="body2" color="text.secondary">
                            No KPI execution data available
                          </Typography>
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Analytics;

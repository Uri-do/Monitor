import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Stack,
  Paper,
} from '@mui/material';
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
import { useQuery } from '@tanstack/react-query';
import { alertApi, kpiApi } from '@/services/api';
import { PageHeader, LoadingSpinner, StatusChip } from '@/components/Common';

// Mock analytics data
const mockTrendData = [
  { date: '2024-01-01', alerts: 12, kpiExecutions: 45, successRate: 95 },
  { date: '2024-01-02', alerts: 8, kpiExecutions: 48, successRate: 98 },
  { date: '2024-01-03', alerts: 15, kpiExecutions: 42, successRate: 92 },
  { date: '2024-01-04', alerts: 6, kpiExecutions: 50, successRate: 96 },
  { date: '2024-01-05', alerts: 10, kpiExecutions: 47, successRate: 94 },
  { date: '2024-01-06', alerts: 4, kpiExecutions: 52, successRate: 100 },
  { date: '2024-01-07', alerts: 9, kpiExecutions: 49, successRate: 97 },
];

const mockSeverityData = [
  { name: 'Critical', value: 15, color: '#f44336' },
  { name: 'High', value: 25, color: '#ff9800' },
  { name: 'Medium', value: 35, color: '#2196f3' },
  { name: 'Low', value: 25, color: '#4caf50' },
];

const mockKpiPerformance = [
  { name: 'Daily Sales Revenue', executions: 168, alerts: 12, successRate: 92.8 },
  { name: 'Customer Satisfaction', executions: 84, alerts: 3, successRate: 96.4 },
  { name: 'System Response Time', executions: 504, alerts: 25, successRate: 95.0 },
  { name: 'Order Processing', executions: 336, alerts: 8, successRate: 97.6 },
  { name: 'User Engagement', executions: 168, alerts: 5, successRate: 97.0 },
];

const Analytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState('7d');

  // Fetch analytics data
  const { data: alertStats, isLoading: alertStatsLoading } = useQuery({
    queryKey: ['alert-statistics', timeRange],
    queryFn: () => alertApi.getStatistics(timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : 90),
  });

  const { data: kpiDashboard, isLoading: kpiLoading } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
  });

  const isLoading = alertStatsLoading || kpiLoading;

  if (isLoading) {
    return <LoadingSpinner message="Loading analytics..." />;
  }

  return (
    <Box>
      <PageHeader
        title="Analytics & Insights"
        subtitle="Comprehensive monitoring system analytics and performance metrics"
        actions={[
          {
            label: 'Export Report',
            icon: <Assessment />,
            onClick: () => {
              // TODO: Implement export functionality
              console.log('Export report');
            },
            variant: 'outlined',
          },
        ]}
      />

      {/* Time Range Selector */}
      <Box sx={{ mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Time Range</InputLabel>
          <Select
            value={timeRange}
            label="Time Range"
            onChange={(e) => setTimeRange(e.target.value)}
          >
            <MenuItem value="7d">Last 7 Days</MenuItem>
            <MenuItem value="30d">Last 30 Days</MenuItem>
            <MenuItem value="90d">Last 90 Days</MenuItem>
          </Select>
        </FormControl>
      </Box>

      <Grid container spacing={3}>
        {/* Key Metrics Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Assessment color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Total KPIs</Typography>
              </Box>
              <Typography variant="h3" color="primary">
                {kpiDashboard?.totalKpis || 0}
              </Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                <Chip
                  label={`${kpiDashboard?.activeKpis || 0} Active`}
                  color="success"
                  size="small"
                />
                <Chip
                  label={`${kpiDashboard?.inactiveKpis || 0} Inactive`}
                  color="default"
                  size="small"
                />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Warning color="warning" sx={{ mr: 1 }} />
                <Typography variant="h6">Alerts Today</Typography>
              </Box>
              <Typography variant="h3" color="warning.main">
                {alertStats?.alertsToday || 2}
              </Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                <Chip
                  label={`${alertStats?.unresolvedAlerts || 1} Unresolved`}
                  color="error"
                  size="small"
                />
                <Chip
                  label={`${alertStats?.resolvedAlerts || 1} Resolved`}
                  color="success"
                  size="small"
                />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <CheckCircle color="success" sx={{ mr: 1 }} />
                <Typography variant="h6">Success Rate</Typography>
              </Box>
              <Typography variant="h3" color="success.main">
                96.2%
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
                <TrendingUp color="success" sx={{ mr: 0.5 }} />
                <Typography variant="body2" color="success.main">
                  +2.1% from last week
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Timeline color="info" sx={{ mr: 1 }} />
                <Typography variant="h6">Avg Response</Typography>
              </Box>
              <Typography variant="h3" color="info.main">
                2.4m
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
                <TrendingDown color="success" sx={{ mr: 0.5 }} />
                <Typography variant="body2" color="success.main">
                  -15s from last week
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Alert Trends Chart */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Alert Trends Over Time
              </Typography>
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={mockTrendData}>
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
                <RechartsPieChart>
                  <Pie
                    data={mockSeverityData}
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    dataKey="value"
                    label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                  >
                    {mockSeverityData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </RechartsPieChart>
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
                    {mockKpiPerformance.map((kpi, index) => (
                      <tr key={index} style={{ borderBottom: '1px solid #f0f0f0' }}>
                        <td style={{ padding: '12px', fontWeight: 'medium' }}>{kpi.name}</td>
                        <td style={{ textAlign: 'center', padding: '12px' }}>{kpi.executions}</td>
                        <td style={{ textAlign: 'center', padding: '12px' }}>{kpi.alerts}</td>
                        <td style={{ textAlign: 'center', padding: '12px' }}>{kpi.successRate}%</td>
                        <td style={{ textAlign: 'center', padding: '12px' }}>
                          <StatusChip
                            status={kpi.successRate >= 95 ? 'success' : kpi.successRate >= 90 ? 'warning' : 'error'}
                          />
                        </td>
                      </tr>
                    ))}
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

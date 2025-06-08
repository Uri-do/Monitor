import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Grid,
  Typography,
  Paper,
  Switch,
  FormControlLabel,
  Chip,
  useTheme,
  alpha,
  styled,
  keyframes,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Speed,
  Timeline,
  ShowChart,
  PieChart,
} from '@mui/icons-material';
import EnhancedChart, { ChartDataPoint } from './EnhancedCharts';

// Animations
const pulse = keyframes`
  0% { opacity: 1; }
  50% { opacity: 0.7; }
  100% { opacity: 1; }
`;

const slideIn = keyframes`
  from { transform: translateX(-20px); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
`;

const LiveIndicator = styled(Box)(({ theme }) => ({
  display: 'flex',
  alignItems: 'center',
  gap: theme.spacing(1),
  '& .live-dot': {
    width: 8,
    height: 8,
    borderRadius: '50%',
    backgroundColor: theme.palette.success.main,
    animation: `${pulse} 2s ease-in-out infinite`,
  },
}));

const MetricCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(3),
  borderRadius: '16px',
  transition: 'all 0.3s ease',
  animation: `${slideIn} 0.6s ease-out`,
  '&:hover': {
    transform: 'translateY(-4px)',
    boxShadow: theme.shadows[8],
  },
}));

interface RealtimeMetric {
  id: string;
  name: string;
  value: number;
  previousValue: number;
  unit: string;
  trend: 'up' | 'down' | 'neutral';
  change: number;
  status: 'healthy' | 'warning' | 'critical';
}

interface RealtimeDashboardProps {
  refreshInterval?: number;
  autoRefresh?: boolean;
  onMetricClick?: (metric: RealtimeMetric) => void;
}

export const RealtimeDashboard: React.FC<RealtimeDashboardProps> = ({
  refreshInterval = 5000,
  autoRefresh = true,
  onMetricClick,
}) => {
  const theme = useTheme();
  const [isLive, setIsLive] = useState(autoRefresh);
  const [metrics, setMetrics] = useState<RealtimeMetric[]>([]);
  const [chartData, setChartData] = useState<{
    performance: ChartDataPoint[];
    transactions: ChartDataPoint[];
    errors: ChartDataPoint[];
    distribution: ChartDataPoint[];
  }>({
    performance: [],
    transactions: [],
    errors: [],
    distribution: [],
  });

  // Simulate real-time data generation
  const generateMetrics = useCallback((): RealtimeMetric[] => {
    const baseMetrics = [
      {
        id: 'response-time',
        name: 'Avg Response Time',
        baseValue: 150,
        unit: 'ms',
        status: 'healthy' as const,
      },
      {
        id: 'throughput',
        name: 'Requests/sec',
        baseValue: 1250,
        unit: 'req/s',
        status: 'healthy' as const,
      },
      {
        id: 'error-rate',
        name: 'Error Rate',
        baseValue: 0.5,
        unit: '%',
        status: 'healthy' as const,
      },
      { id: 'cpu-usage', name: 'CPU Usage', baseValue: 65, unit: '%', status: 'warning' as const },
      {
        id: 'memory-usage',
        name: 'Memory Usage',
        baseValue: 78,
        unit: '%',
        status: 'warning' as const,
      },
      {
        id: 'active-users',
        name: 'Active Users',
        baseValue: 2340,
        unit: 'users',
        status: 'healthy' as const,
      },
    ];

    return baseMetrics.map(metric => {
      const variation = (Math.random() - 0.5) * 0.2; // ±10% variation
      const newValue = Math.max(0, metric.baseValue * (1 + variation));
      const previousValue = metric.baseValue;
      const change = ((newValue - previousValue) / previousValue) * 100;

      let status: 'healthy' | 'warning' | 'critical' = 'healthy';
      if (metric.id === 'error-rate' && newValue > 2) status = 'critical';
      else if (metric.id === 'error-rate' && newValue > 1) status = 'warning';
      else if ((metric.id === 'cpu-usage' || metric.id === 'memory-usage') && newValue > 85)
        status = 'critical';
      else if ((metric.id === 'cpu-usage' || metric.id === 'memory-usage') && newValue > 70)
        status = 'warning';

      return {
        ...metric,
        value: Math.round(newValue * 100) / 100,
        previousValue,
        change: Math.round(change * 100) / 100,
        trend: change > 0 ? 'up' : change < 0 ? 'down' : ('neutral' as const),
        status,
      };
    });
  }, []);

  // Generate chart data
  const generateChartData = useCallback(() => {
    const now = new Date();
    const timePoints = Array.from({ length: 20 }, (_, i) => {
      const time = new Date(now.getTime() - (19 - i) * 30000); // 30-second intervals
      return time.toLocaleTimeString('en-US', { hour12: false, timeStyle: 'short' });
    });

    return {
      performance: timePoints.map(time => ({
        name: time,
        value: Math.round(120 + Math.random() * 60), // 120-180ms
      })),
      transactions: timePoints.map(time => ({
        name: time,
        value: Math.round(1000 + Math.random() * 500), // 1000-1500 req/s
      })),
      errors: timePoints.map(time => ({
        name: time,
        value: Math.round(Math.random() * 5 * 100) / 100, // 0-5%
      })),
      distribution: [
        { name: 'API Calls', value: 45 },
        { name: 'Database', value: 25 },
        { name: 'Cache', value: 20 },
        { name: 'External', value: 10 },
      ],
    };
  }, []);

  // Initialize data
  useEffect(() => {
    setMetrics(generateMetrics());
    setChartData(generateChartData());
  }, [generateMetrics, generateChartData]);

  // Real-time updates
  useEffect(() => {
    if (!isLive) return;

    const interval = setInterval(() => {
      setMetrics(generateMetrics());
      setChartData(generateChartData());
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [isLive, refreshInterval, generateMetrics, generateChartData]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
        return theme.palette.success.main;
      case 'warning':
        return theme.palette.warning.main;
      case 'critical':
        return theme.palette.error.main;
      default:
        return theme.palette.text.secondary;
    }
  };

  const getTrendIcon = (trend: string) => {
    switch (trend) {
      case 'up':
        return <TrendingUp fontSize="small" color="success" />;
      case 'down':
        return <TrendingDown fontSize="small" color="error" />;
      default:
        return null;
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 4,
        }}
      >
        <Box>
          <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
            Real-time Dashboard
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Live monitoring with interactive visualizations
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <LiveIndicator>
            <Box className="live-dot" />
            <Typography variant="body2" color="text.secondary">
              {isLive ? 'LIVE' : 'PAUSED'}
            </Typography>
          </LiveIndicator>

          <FormControlLabel
            control={
              <Switch
                checked={isLive}
                onChange={e => setIsLive(e.target.checked)}
                color="primary"
              />
            }
            label="Auto Refresh"
          />
        </Box>
      </Box>

      {/* Metrics Grid */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {metrics.map(metric => (
          <Grid item xs={12} sm={6} md={4} lg={2} key={metric.id}>
            <MetricCard
              onClick={() => onMetricClick?.(metric)}
              sx={{ cursor: onMetricClick ? 'pointer' : 'default' }}
            >
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  {metric.name}
                </Typography>
                <Chip
                  size="small"
                  label={metric.status}
                  sx={{
                    backgroundColor: alpha(getStatusColor(metric.status), 0.1),
                    color: getStatusColor(metric.status),
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    fontSize: '0.7rem',
                  }}
                />
              </Box>

              <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
                {metric.value.toLocaleString()} {metric.unit}
              </Typography>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                {getTrendIcon(metric.trend)}
                <Typography
                  variant="body2"
                  sx={{
                    color:
                      metric.trend === 'up'
                        ? theme.palette.success.main
                        : metric.trend === 'down'
                          ? theme.palette.error.main
                          : theme.palette.text.secondary,
                    fontWeight: 600,
                  }}
                >
                  {metric.change > 0 ? '+' : ''}
                  {metric.change}%
                </Typography>
              </Box>
            </MetricCard>
          </Grid>
        ))}
      </Grid>

      {/* Charts Grid */}
      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <EnhancedChart
            data={chartData.performance}
            title="Response Time"
            subtitle="Average API response time over time"
            type="area"
            height={300}
            animated
            showGrid
            showTooltip
            color={theme.palette.primary.main}
            threshold={200}
            realTime={isLive}
            refreshInterval={refreshInterval}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <EnhancedChart
            data={chartData.transactions}
            title="Transaction Volume"
            subtitle="Requests per second"
            type="line"
            height={300}
            animated
            showGrid
            showTooltip
            showBrush
            color={theme.palette.success.main}
            realTime={isLive}
            refreshInterval={refreshInterval}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <EnhancedChart
            data={chartData.errors}
            title="Error Rate"
            subtitle="Percentage of failed requests"
            type="bar"
            height={300}
            animated
            showGrid
            showTooltip
            color={theme.palette.error.main}
            threshold={2}
            realTime={isLive}
            refreshInterval={refreshInterval}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <EnhancedChart
            data={chartData.distribution}
            title="Request Distribution"
            subtitle="Traffic breakdown by source"
            type="donut"
            height={300}
            animated
            showTooltip
            showLegend
            onDataPointClick={data => console.log('Clicked:', data)}
          />
        </Grid>
      </Grid>
    </Box>
  );
};

export default RealtimeDashboard;

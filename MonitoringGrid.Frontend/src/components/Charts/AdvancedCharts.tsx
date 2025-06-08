import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  ToggleButton,
  ToggleButtonGroup,
  useTheme
} from '@mui/material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  ScatterChart,
  Scatter,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ReferenceLine,
  Brush,
  ComposedChart
} from 'recharts';
import { format, subDays } from 'date-fns';

interface ChartData {
  timestamp: string;
  value: number;
  target?: number;
  deviation?: number;
  status?: 'healthy' | 'warning' | 'critical';
}

interface TrendData {
  period: string;
  current: number;
  previous: number;
  change: number;
}

interface DistributionData {
  name: string;
  value: number;
  color: string;
}

interface AdvancedChartsProps {
  data: ChartData[];
  trendData?: TrendData[];
  distributionData?: DistributionData[];
  title?: string;
  showControls?: boolean;
}

const COLORS = {
  primary: '#1976d2',
  secondary: '#dc004e',
  success: '#2e7d32',
  warning: '#ed6c02',
  error: '#d32f2f',
  info: '#0288d1'
};

const STATUS_COLORS = {
  healthy: COLORS.success,
  warning: COLORS.warning,
  critical: COLORS.error
};

export const AdvancedCharts: React.FC<AdvancedChartsProps> = ({
  data,
  trendData = [],
  distributionData = [],
  title = 'KPI Analytics',
  showControls = true
}) => {
  const [chartType, setChartType] = React.useState<'line' | 'area' | 'bar' | 'scatter'>('line');
  const [timeRange, setTimeRange] = React.useState<'1d' | '7d' | '30d' | '90d'>('7d');
  const [showTarget, setShowTarget] = React.useState(true);

  // Filter data based on time range
  const filteredData = React.useMemo(() => {
    const days = timeRange === '1d' ? 1 : timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : 90;
    const cutoff = subDays(new Date(), days);
    return data.filter(item => new Date(item.timestamp) >= cutoff);
  }, [data, timeRange]);

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <Box
          sx={{
            bgcolor: 'background.paper',
            p: 2,
            border: 1,
            borderColor: 'divider',
            borderRadius: 1,
            boxShadow: 2
          }}
        >
          <Typography variant="subtitle2">
            {format(new Date(label), 'MMM dd, yyyy HH:mm')}
          </Typography>
          {payload.map((entry: any, index: number) => (
            <Typography
              key={index}
              variant="body2"
              sx={{ color: entry.color }}
            >
              {entry.name}: {entry.value?.toFixed(2)}
            </Typography>
          ))}
        </Box>
      );
    }
    return null;
  };

  const renderMainChart = () => {
    const commonProps = {
      data: filteredData,
      margin: { top: 5, right: 30, left: 20, bottom: 5 }
    };

    switch (chartType) {
      case 'area':
        return (
          <AreaChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis />
            <Tooltip content={<CustomTooltip />} />
            <Legend />
            <Area
              type="monotone"
              dataKey="value"
              stroke={COLORS.primary}
              fill={COLORS.primary}
              fillOpacity={0.3}
              name="Value"
            />
            {showTarget && (
              <Area
                type="monotone"
                dataKey="target"
                stroke={COLORS.secondary}
                fill="none"
                strokeDasharray="5 5"
                name="Target"
              />
            )}
            <Brush dataKey="timestamp" height={30} />
          </AreaChart>
        );

      case 'bar':
        return (
          <BarChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis />
            <Tooltip content={<CustomTooltip />} />
            <Legend />
            <Bar dataKey="value" fill={COLORS.primary} name="Value" />
            {showTarget && (
              <ReferenceLine y={filteredData[0]?.target} stroke={COLORS.secondary} strokeDasharray="5 5" />
            )}
          </BarChart>
        );

      case 'scatter':
        return (
          <ScatterChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis />
            <Tooltip content={<CustomTooltip />} />
            <Legend />
            <Scatter
              dataKey="value"
              fill={COLORS.primary}
              name="Value"
            />
          </ScatterChart>
        );

      default: // line
        return (
          <LineChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis />
            <Tooltip content={<CustomTooltip />} />
            <Legend />
            <Line
              type="monotone"
              dataKey="value"
              stroke={COLORS.primary}
              strokeWidth={2}
              dot={{ r: 4 }}
              name="Value"
            />
            {showTarget && (
              <Line
                type="monotone"
                dataKey="target"
                stroke={COLORS.secondary}
                strokeDasharray="5 5"
                dot={false}
                name="Target"
              />
            )}
            <Brush dataKey="timestamp" height={30} />
          </LineChart>
        );
    }
  };

  return (
    <Box>
      {/* Controls */}
      {showControls && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Grid container spacing={2} alignItems="center">
              <Grid item>
                <Typography variant="h6">{title}</Typography>
              </Grid>
              <Grid item>
                <ToggleButtonGroup
                  value={chartType}
                  exclusive
                  onChange={(_, value) => value && setChartType(value)}
                  size="small"
                >
                  <ToggleButton value="line">Line</ToggleButton>
                  <ToggleButton value="area">Area</ToggleButton>
                  <ToggleButton value="bar">Bar</ToggleButton>
                  <ToggleButton value="scatter">Scatter</ToggleButton>
                </ToggleButtonGroup>
              </Grid>
              <Grid item>
                <FormControl size="small" sx={{ minWidth: 120 }}>
                  <InputLabel>Time Range</InputLabel>
                  <Select
                    value={timeRange}
                    label="Time Range"
                    onChange={(e) => setTimeRange(e.target.value as any)}
                  >
                    <MenuItem value="1d">Last Day</MenuItem>
                    <MenuItem value="7d">Last 7 Days</MenuItem>
                    <MenuItem value="30d">Last 30 Days</MenuItem>
                    <MenuItem value="90d">Last 90 Days</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item>
                <ToggleButtonGroup
                  value={showTarget}
                  exclusive
                  onChange={(_, value) => setShowTarget(value)}
                  size="small"
                >
                  <ToggleButton value={true}>Show Target</ToggleButton>
                </ToggleButtonGroup>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      <Grid container spacing={3}>
        {/* Main Chart */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Trend Analysis
              </Typography>
              <Box sx={{ height: 400 }}>
                <ResponsiveContainer width="100%" height="100%">
                  {renderMainChart()}
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Distribution Chart */}
        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Status Distribution
              </Typography>
              <Box sx={{ height: 300 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={distributionData}
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      dataKey="value"
                      label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                    >
                      {distributionData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Trend Comparison */}
        {trendData.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Period Comparison
                </Typography>
                <Box sx={{ height: 300 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <ComposedChart data={trendData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="period" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="current" fill={COLORS.primary} name="Current Period" />
                      <Bar dataKey="previous" fill={COLORS.info} name="Previous Period" />
                      <Line
                        type="monotone"
                        dataKey="change"
                        stroke={COLORS.secondary}
                        strokeWidth={3}
                        name="Change %"
                        yAxisId="right"
                      />
                    </ComposedChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default AdvancedCharts;

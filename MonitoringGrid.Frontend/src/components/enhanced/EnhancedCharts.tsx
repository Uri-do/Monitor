import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  useTheme,
  alpha,
  Skeleton,
  IconButton,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
  Legend,
  ReferenceLine,
  Brush,
  Tooltip,
} from 'recharts';
import { MoreVert, TrendingUp, TrendingDown } from '@mui/icons-material';

export interface ChartDataPoint {
  name: string;
  value: number;
  timestamp?: string;
  category?: string;
  target?: number;
  previous?: number;
}

export interface EnhancedChartProps {
  data: ChartDataPoint[];
  title?: string;
  subtitle?: string;
  type: 'line' | 'area' | 'bar' | 'pie' | 'donut';
  height?: number;
  loading?: boolean;
  animated?: boolean;
  showBrush?: boolean;
  showGrid?: boolean;
  showLegend?: boolean;
  showTooltip?: boolean;
  color?: string;
  gradientColors?: [string, string];
  threshold?: number;
  realTime?: boolean;
  refreshInterval?: number;
  onDataPointClick?: (data: ChartDataPoint) => void;
}

const CustomTooltip = ({ active, payload, label }: any) => {
  const theme = useTheme();

  if (active && payload && payload.length) {
    return (
      <Paper
        sx={{
          p: 2,
          backgroundColor: alpha(theme.palette.background.paper, 0.95),
          backdropFilter: 'blur(8px)',
          border: `1px solid ${alpha(theme.palette.primary.main, 0.2)}`,
          borderRadius: '12px',
          boxShadow: theme.shadows[8],
        }}
      >
        <Typography variant="subtitle2" fontWeight={600} sx={{ mb: 1 }}>
          {label}
        </Typography>
        {payload.map((entry: any, index: number) => (
          <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box
              sx={{
                width: 12,
                height: 12,
                borderRadius: '50%',
                backgroundColor: entry.color,
              }}
            />
            <Typography variant="body2" color="text.secondary">
              {entry.name}:
            </Typography>
            <Typography variant="body2" fontWeight={600}>
              {typeof entry.value === 'number' ? entry.value.toLocaleString() : entry.value}
            </Typography>
          </Box>
        ))}
      </Paper>
    );
  }
  return null;
};

const AnimatedNumber: React.FC<{ value: number; duration?: number }> = ({
  value,
  duration = 1000,
}) => {
  const [displayValue, setDisplayValue] = useState(0);

  useEffect(() => {
    let startTime: number;
    let animationFrame: number;

    const animate = (timestamp: number) => {
      if (!startTime) startTime = timestamp;
      const progress = Math.min((timestamp - startTime) / duration, 1);

      setDisplayValue(Math.floor(value * progress));

      if (progress < 1) {
        animationFrame = requestAnimationFrame(animate);
      }
    };

    animationFrame = requestAnimationFrame(animate);

    return () => {
      if (animationFrame) {
        cancelAnimationFrame(animationFrame);
      }
    };
  }, [value, duration]);

  return <span>{displayValue.toLocaleString()}</span>;
};

export const EnhancedChart: React.FC<EnhancedChartProps> = ({
  data,
  title,
  subtitle,
  type,
  height = 300,
  loading = false,
  animated = true,
  showBrush = false,
  showGrid = true,
  showLegend = false,
  showTooltip = true,
  color,
  gradientColors,
  threshold,
  realTime = false,
  refreshInterval = 5000,
  onDataPointClick,
}) => {
  const theme = useTheme();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [animationKey, setAnimationKey] = useState(0);

  const chartColor = color || theme.palette.primary.main;
  const gradientId = `gradient-${Math.random().toString(36).substr(2, 9)}`;

  // Real-time data simulation
  useEffect(() => {
    if (realTime && refreshInterval > 0) {
      const interval = setInterval(() => {
        setAnimationKey(prev => prev + 1);
      }, refreshInterval);

      return () => clearInterval(interval);
    }
  }, [realTime, refreshInterval]);

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const renderChart = () => {
    const commonProps = {
      data,
      margin: { top: 20, right: 30, left: 20, bottom: 20 },
    };

    const animationProps = animated
      ? {
          animationBegin: 0,
          animationDuration: 1000,
        }
      : {};

    switch (type) {
      case 'line':
        return (
          <LineChart {...commonProps}>
            <defs>
              <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={chartColor} stopOpacity={0.3} />
                <stop offset="95%" stopColor={chartColor} stopOpacity={0} />
              </linearGradient>
            </defs>
            {showGrid && (
              <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
            )}
            <XAxis
              dataKey="name"
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <YAxis
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            {showTooltip && <Tooltip content={CustomTooltip as any} />}
            {threshold && (
              <ReferenceLine
                y={threshold}
                stroke={theme.palette.warning.main}
                strokeDasharray="5 5"
                label="Threshold"
              />
            )}
            <Line
              type="monotone"
              dataKey="value"
              stroke={chartColor}
              strokeWidth={3}
              dot={{ fill: chartColor, strokeWidth: 2, r: 4 }}
              activeDot={{ r: 6, stroke: chartColor, strokeWidth: 2 }}
              {...animationProps}
            />
            {showBrush && <Brush dataKey="name" height={30} stroke={chartColor} />}
          </LineChart>
        );

      case 'area':
        return (
          <AreaChart {...commonProps}>
            <defs>
              <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={chartColor} stopOpacity={0.8} />
                <stop offset="95%" stopColor={chartColor} stopOpacity={0.1} />
              </linearGradient>
            </defs>
            {showGrid && (
              <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
            )}
            <XAxis
              dataKey="name"
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <YAxis
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            {showTooltip && <Tooltip content={CustomTooltip as any} />}
            <Area
              type="monotone"
              dataKey="value"
              stroke={chartColor}
              strokeWidth={2}
              fill={`url(#${gradientId})`}
              {...animationProps}
            />
          </AreaChart>
        );

      case 'bar':
        return (
          <BarChart {...commonProps}>
            {showGrid && (
              <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
            )}
            <XAxis
              dataKey="name"
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <YAxis
              axisLine={false}
              tickLine={false}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            {showTooltip && <Tooltip content={CustomTooltip as any} />}
            <Bar dataKey="value" fill={chartColor} radius={[4, 4, 0, 0]} {...animationProps} />
          </BarChart>
        );

      case 'pie':
      case 'donut':
        const COLORS = [
          theme.palette.primary.main,
          theme.palette.secondary.main,
          theme.palette.success.main,
          theme.palette.warning.main,
          theme.palette.error.main,
          theme.palette.info.main,
        ];

        return (
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={type === 'donut' ? 60 : 0}
              outerRadius={100}
              paddingAngle={2}
              dataKey="value"
              {...animationProps}
            >
              {data.map((entry, index) => (
                <Cell
                  key={`cell-${index}`}
                  fill={COLORS[index % COLORS.length]}
                  onClick={() => onDataPointClick?.(entry)}
                  style={{ cursor: onDataPointClick ? 'pointer' : 'default' }}
                />
              ))}
            </Pie>
            {showTooltip && <Tooltip content={CustomTooltip as any} />}
            {showLegend && <Legend />}
          </PieChart>
        );

      default:
        return <div>Chart type not supported</div>;
    }
  };

  if (loading) {
    return (
      <Paper sx={{ p: 3, borderRadius: '16px', height }}>
        <Box sx={{ mb: 2 }}>
          <Skeleton variant="text" width="40%" height={32} />
          <Skeleton variant="text" width="60%" height={20} />
        </Box>
        <Skeleton variant="rectangular" width="100%" height={height - 100} />
      </Paper>
    );
  }

  return (
    <Paper
      sx={{
        p: 3,
        borderRadius: '16px',
        height,
        transition: 'all 0.3s ease',
        '&:hover': {
          boxShadow: theme.shadows[8],
        },
      }}
    >
      {(title || subtitle) && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            mb: 2,
          }}
        >
          <Box>
            {title && (
              <Typography variant="h6" fontWeight={600} sx={{ mb: 0.5 }}>
                {title}
              </Typography>
            )}
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Box>
          <IconButton size="small" onClick={handleMenuClick}>
            <MoreVert />
          </IconButton>
          <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
            <MenuItem onClick={handleMenuClose}>Export Chart</MenuItem>
            <MenuItem onClick={handleMenuClose}>View Details</MenuItem>
            <MenuItem onClick={handleMenuClose}>Configure</MenuItem>
          </Menu>
        </Box>
      )}

      <Box sx={{ height: height - (title || subtitle ? 100 : 50) }}>
        <ResponsiveContainer width="100%" height="100%" key={animationKey}>
          {renderChart()}
        </ResponsiveContainer>
      </Box>
    </Paper>
  );
};

export default EnhancedChart;

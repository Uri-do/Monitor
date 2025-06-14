import React, { useMemo, useRef, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  IconButton,
  Menu,
  MenuItem,
  Tooltip,
  useTheme,
  alpha,
} from '@mui/material';
import {
  MoreVert as MoreIcon,
  Download as DownloadIcon,
  Fullscreen as FullscreenIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  ScatterChart,
  Scatter,
  ReferenceLine,
  Brush,
  ComposedChart,
} from 'recharts';

export interface ChartDataPoint {
  [key: string]: any;
  timestamp?: string | Date;
  value?: number;
  category?: string;
}

export interface ChartSeries {
  key: string;
  name: string;
  color: string;
  type?: 'line' | 'area' | 'bar';
  yAxisId?: 'left' | 'right';
}

export interface ChartProps {
  title?: string;
  subtitle?: string;
  data: ChartDataPoint[];
  series: ChartSeries[];
  type: 'line' | 'area' | 'bar' | 'pie' | 'scatter' | 'composed';
  height?: number;
  loading?: boolean;
  error?: string | null;
  showLegend?: boolean;
  showGrid?: boolean;
  showBrush?: boolean;
  showTooltip?: boolean;
  enableZoom?: boolean;
  enableExport?: boolean;
  enableFullscreen?: boolean;
  enableRefresh?: boolean;
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'svg' | 'csv') => void;
  customTooltip?: React.ComponentType<any>;
  thresholds?: Array<{ value: number; label: string; color: string }>;
  animations?: boolean;
  responsive?: boolean;
}

export const Chart: React.FC<ChartProps> = ({
  title,
  subtitle,
  data,
  series,
  type,
  height = 400,
  loading = false,
  error,
  showLegend = true,
  showGrid = true,
  showBrush = false,
  showTooltip = true,
  enableZoom = false,
  enableExport = false,
  enableFullscreen = false,
  enableRefresh = false,
  onRefresh,
  onExport,
  customTooltip,
  thresholds = [],
  animations = true,
  responsive = true,
}) => {
  const theme = useTheme();
  const chartRef = useRef<HTMLDivElement>(null);
  const [menuAnchor, setMenuAnchor] = React.useState<null | HTMLElement>(null);
  const [isFullscreen, setIsFullscreen] = React.useState(false);

  // Generate colors based on theme
  const chartColors = useMemo(() => {
    const baseColors = [
      theme.palette.primary.main,
      theme.palette.secondary.main,
      theme.palette.error.main,
      theme.palette.warning.main,
      theme.palette.info.main,
      theme.palette.success.main,
    ];

    return series.map((s, index) => s.color || baseColors[index % baseColors.length]);
  }, [series, theme]);

  // Custom tooltip component
  const CustomTooltip =
    customTooltip ||
    (({ active, payload, label }: any) => {
      if (!active || !payload || !payload.length) return null;

      return (
        <Card sx={{ p: 1, maxWidth: 300 }}>
          <Typography variant="body2" fontWeight="bold">
            {label}
          </Typography>
          {payload.map((entry: any, index: number) => (
            <Typography key={index} variant="body2" sx={{ color: entry.color }}>
              {entry.name}: {typeof entry.value === 'number' ? entry.value.toFixed(2) : entry.value}
            </Typography>
          ))}
        </Card>
      );
    });

  // Export functionality
  const handleExport = (format: 'png' | 'svg' | 'csv') => {
    if (format === 'csv') {
      const csvContent = [
        Object.keys(data[0] || {}).join(','),
        ...data.map(row => Object.values(row).join(',')),
      ].join('\n');

      const blob = new Blob([csvContent], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${title || 'chart'}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } else {
      // For PNG/SVG export, you would typically use a library like html2canvas
      console.log(`Exporting as ${format}`);
    }

    onExport?.(format);
    setMenuAnchor(null);
  };

  // Fullscreen functionality
  const toggleFullscreen = () => {
    if (!isFullscreen && chartRef.current) {
      chartRef.current.requestFullscreen?.();
      setIsFullscreen(true);
    } else if (document.fullscreenElement) {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  // Render different chart types
  const renderChart = () => {
    const commonProps = {
      data,
      margin: { top: 20, right: 30, left: 20, bottom: 20 },
    };

    switch (type) {
      case 'line':
        return (
          <LineChart {...commonProps}>
            {showGrid && (
              <CartesianGrid
                strokeDasharray="3 3"
                stroke={alpha(theme.palette.text.primary, 0.1)}
              />
            )}
            <XAxis dataKey="timestamp" stroke={theme.palette.text.secondary} />
            <YAxis stroke={theme.palette.text.secondary} />
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
            {thresholds.map((threshold, index) => (
              <ReferenceLine
                key={index}
                y={threshold.value}
                stroke={threshold.color}
                strokeDasharray="5 5"
                label={threshold.label}
              />
            ))}
            {series.map((s, index) => (
              <Line
                key={s.key}
                type="monotone"
                dataKey={s.key}
                stroke={chartColors[index]}
                strokeWidth={2}
                dot={{ fill: chartColors[index], strokeWidth: 2, r: 4 }}
                activeDot={{ r: 6 }}
                animationDuration={animations ? 1000 : 0}
              />
            ))}
            {showBrush && (
              <Brush dataKey="timestamp" height={30} stroke={theme.palette.primary.main} />
            )}
          </LineChart>
        );

      case 'area':
        return (
          <AreaChart {...commonProps}>
            {showGrid && (
              <CartesianGrid
                strokeDasharray="3 3"
                stroke={alpha(theme.palette.text.primary, 0.1)}
              />
            )}
            <XAxis dataKey="timestamp" stroke={theme.palette.text.secondary} />
            <YAxis stroke={theme.palette.text.secondary} />
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
            {series.map((s, index) => (
              <Area
                key={s.key}
                type="monotone"
                dataKey={s.key}
                stackId="1"
                stroke={chartColors[index]}
                fill={alpha(chartColors[index], 0.6)}
                animationDuration={animations ? 1000 : 0}
              />
            ))}
          </AreaChart>
        );

      case 'bar':
        return (
          <BarChart {...commonProps}>
            {showGrid && (
              <CartesianGrid
                strokeDasharray="3 3"
                stroke={alpha(theme.palette.text.primary, 0.1)}
              />
            )}
            <XAxis dataKey="timestamp" stroke={theme.palette.text.secondary} />
            <YAxis stroke={theme.palette.text.secondary} />
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
            {series.map((s, index) => (
              <Bar
                key={s.key}
                dataKey={s.key}
                fill={chartColors[index]}
                animationDuration={animations ? 1000 : 0}
              />
            ))}
          </BarChart>
        );

      case 'pie':
        return (
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              outerRadius={Math.min(height * 0.3, 120)}
              fill={theme.palette.primary.main}
              dataKey="value"
              label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
              animationDuration={animations ? 1000 : 0}
            >
              {data.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={chartColors[index % chartColors.length]} />
              ))}
            </Pie>
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
          </PieChart>
        );

      case 'scatter':
        return (
          <ScatterChart {...commonProps}>
            {showGrid && (
              <CartesianGrid
                strokeDasharray="3 3"
                stroke={alpha(theme.palette.text.primary, 0.1)}
              />
            )}
            <XAxis dataKey="x" stroke={theme.palette.text.secondary} />
            <YAxis dataKey="y" stroke={theme.palette.text.secondary} />
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
            {series.map((s, index) => (
              <Scatter
                key={s.key}
                name={s.name}
                data={data}
                fill={chartColors[index]}
                animationDuration={animations ? 1000 : 0}
              />
            ))}
          </ScatterChart>
        );

      case 'composed':
        return (
          <ComposedChart {...commonProps}>
            {showGrid && (
              <CartesianGrid
                strokeDasharray="3 3"
                stroke={alpha(theme.palette.text.primary, 0.1)}
              />
            )}
            <XAxis dataKey="timestamp" stroke={theme.palette.text.secondary} />
            <YAxis stroke={theme.palette.text.secondary} />
            {showTooltip && <RechartsTooltip content={<CustomTooltip />} />}
            {showLegend && <Legend />}
            {series.map((s, index) => {
              const color = chartColors[index];
              switch (s.type) {
                case 'area':
                  return (
                    <Area
                      key={s.key}
                      type="monotone"
                      dataKey={s.key}
                      fill={alpha(color, 0.6)}
                      stroke={color}
                      animationDuration={animations ? 1000 : 0}
                    />
                  );
                case 'bar':
                  return (
                    <Bar
                      key={s.key}
                      dataKey={s.key}
                      fill={color}
                      animationDuration={animations ? 1000 : 0}
                    />
                  );
                default:
                  return (
                    <Line
                      key={s.key}
                      type="monotone"
                      dataKey={s.key}
                      stroke={color}
                      strokeWidth={2}
                      dot={{ fill: color, strokeWidth: 2, r: 4 }}
                      animationDuration={animations ? 1000 : 0}
                    />
                  );
              }
            })}
          </ComposedChart>
        );

      default:
        return <div>Unsupported chart type</div>;
    }
  };

  if (loading) {
    return (
      <Card sx={{ height }}>
        <CardContent
          sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}
        >
          <Typography>Loading chart...</Typography>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card sx={{ height }}>
        <CardContent
          sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}
        >
          <Typography color="error">{error}</Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card ref={chartRef} sx={{ height: isFullscreen ? '100vh' : height }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <Box
          sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}
        >
          <Box>
            {title && (
              <Typography variant="h6" component="h2">
                {title}
              </Typography>
            )}
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Box>

          {/* Actions */}
          <Box sx={{ display: 'flex', gap: 1 }}>
            {enableRefresh && (
              <Tooltip title="Refresh">
                <IconButton size="small" onClick={onRefresh}>
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            )}
            {enableFullscreen && (
              <Tooltip title={isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}>
                <IconButton size="small" onClick={toggleFullscreen}>
                  <FullscreenIcon />
                </IconButton>
              </Tooltip>
            )}
            {enableExport && (
              <Tooltip title="Export">
                <IconButton size="small" onClick={e => setMenuAnchor(e.currentTarget)}>
                  <MoreIcon />
                </IconButton>
              </Tooltip>
            )}
          </Box>
        </Box>

        {/* Chart */}
        <Box sx={{ flex: 1, minHeight: 0 }}>
          {responsive ? (
            <ResponsiveContainer width="100%" height="100%">
              {renderChart()}
            </ResponsiveContainer>
          ) : (
            renderChart()
          )}
        </Box>

        {/* Export Menu */}
        <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={() => setMenuAnchor(null)}>
          <MenuItem onClick={() => handleExport('png')}>
            <DownloadIcon sx={{ mr: 1 }} />
            Export as PNG
          </MenuItem>
          <MenuItem onClick={() => handleExport('svg')}>
            <DownloadIcon sx={{ mr: 1 }} />
            Export as SVG
          </MenuItem>
          <MenuItem onClick={() => handleExport('csv')}>
            <DownloadIcon sx={{ mr: 1 }} />
            Export as CSV
          </MenuItem>
        </Menu>
      </CardContent>
    </Card>
  );
};

export default Chart;

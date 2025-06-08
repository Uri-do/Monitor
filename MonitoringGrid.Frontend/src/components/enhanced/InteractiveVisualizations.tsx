import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Slider,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  ButtonGroup,
  Chip,
  useTheme,
  alpha,
  styled,
} from '@mui/material';
import {
  ComposedChart,
  Line,
  Bar,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  ZAxis,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  Treemap,
  Cell,
} from 'recharts';
import { PlayArrow, Pause, Refresh, FilterList } from '@mui/icons-material';

const ControlPanel = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2),
  marginBottom: theme.spacing(3),
  borderRadius: '16px',
  backgroundColor: alpha(theme.palette.background.paper, 0.8),
  backdropFilter: 'blur(8px)',
}));

interface DataPoint {
  timestamp: string;
  value: number;
  category: string;
  size?: number;
  performance?: number;
  errors?: number;
  users?: number;
}

interface InteractiveVisualizationProps {
  title: string;
  type: 'heatmap' | 'scatter' | 'radar' | 'treemap' | 'composed';
  data: DataPoint[];
  height?: number;
  interactive?: boolean;
  filters?: string[];
  onFilterChange?: (filters: string[]) => void;
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
          maxWidth: 300,
        }}
      >
        <Typography variant="subtitle2" fontWeight={600} sx={{ mb: 1 }}>
          {label}
        </Typography>
        {payload.map((entry: any, index: number) => (
          <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
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

export const InteractiveVisualization: React.FC<InteractiveVisualizationProps> = ({
  title,
  type,
  data,
  height = 400,
  interactive = true,
  filters = [],
  onFilterChange,
}) => {
  const theme = useTheme();
  const [timeRange, setTimeRange] = useState<[number, number]>([0, 100]);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [isPlaying, setIsPlaying] = useState(false);
  const [animationSpeed, setAnimationSpeed] = useState(1000);
  const [activeFilters, setActiveFilters] = useState<string[]>([]);

  // Animation for time-based data
  useEffect(() => {
    if (!isPlaying) return;

    const interval = setInterval(() => {
      setTimeRange(([start, end]) => {
        const newStart = start + 1;
        const newEnd = end + 1;
        if (newEnd > 100) {
          return [0, end - start];
        }
        return [newStart, newEnd];
      });
    }, animationSpeed);

    return () => clearInterval(interval);
  }, [isPlaying, animationSpeed]);

  const filteredData = data.filter(item => {
    if (selectedCategory !== 'all' && item.category !== selectedCategory) return false;
    if (activeFilters.length > 0 && !activeFilters.includes(item.category)) return false;
    return true;
  });

  const handleFilterToggle = (filter: string) => {
    const newFilters = activeFilters.includes(filter)
      ? activeFilters.filter(f => f !== filter)
      : [...activeFilters, filter];

    setActiveFilters(newFilters);
    onFilterChange?.(newFilters);
  };

  const renderVisualization = () => {
    const commonProps = {
      data: filteredData,
      margin: { top: 20, right: 30, left: 20, bottom: 20 },
    };

    switch (type) {
      case 'scatter':
        return (
          <ScatterChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
            <XAxis
              dataKey="value"
              type="number"
              domain={['dataMin', 'dataMax']}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <YAxis
              dataKey="performance"
              type="number"
              domain={['dataMin', 'dataMax']}
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <ZAxis dataKey="size" range={[50, 400]} />
            <Tooltip content={<CustomTooltip />} />
            <Scatter dataKey="performance" fill={theme.palette.primary.main} fillOpacity={0.7} />
          </ScatterChart>
        );

      case 'radar':
        const radarData = [
          { subject: 'Performance', A: 120, B: 110, fullMark: 150 },
          { subject: 'Reliability', A: 98, B: 130, fullMark: 150 },
          { subject: 'Scalability', A: 86, B: 130, fullMark: 150 },
          { subject: 'Security', A: 99, B: 100, fullMark: 150 },
          { subject: 'Usability', A: 85, B: 90, fullMark: 150 },
          { subject: 'Maintainability', A: 65, B: 85, fullMark: 150 },
        ];

        return (
          <RadarChart data={radarData} margin={{ top: 20, right: 80, bottom: 20, left: 80 }}>
            <PolarGrid stroke={alpha(theme.palette.divider, 0.3)} />
            <PolarAngleAxis
              dataKey="subject"
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <PolarRadiusAxis
              angle={90}
              domain={[0, 150]}
              tick={{ fontSize: 10, fill: theme.palette.text.secondary }}
            />
            <Radar
              name="Current"
              dataKey="A"
              stroke={theme.palette.primary.main}
              fill={theme.palette.primary.main}
              fillOpacity={0.3}
              strokeWidth={2}
            />
            <Radar
              name="Target"
              dataKey="B"
              stroke={theme.palette.secondary.main}
              fill={theme.palette.secondary.main}
              fillOpacity={0.3}
              strokeWidth={2}
            />
            <Legend />
            <Tooltip content={<CustomTooltip />} />
          </RadarChart>
        );

      case 'treemap':
        const treemapData = [
          { name: 'API Endpoints', size: 2400, fill: theme.palette.primary.main },
          { name: 'Database Queries', size: 1800, fill: theme.palette.secondary.main },
          { name: 'Cache Operations', size: 1200, fill: theme.palette.success.main },
          { name: 'External Services', size: 800, fill: theme.palette.warning.main },
          { name: 'File Operations', size: 400, fill: theme.palette.error.main },
          { name: 'Authentication', size: 600, fill: theme.palette.info.main },
        ];

        return (
          <Treemap
            data={treemapData}
            dataKey="size"
            aspectRatio={4 / 3}
            stroke={theme.palette.background.paper}
          >
            {treemapData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.fill} />
            ))}
          </Treemap>
        );

      case 'composed':
        return (
          <ComposedChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
            <XAxis
              dataKey="timestamp"
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <YAxis yAxisId="left" tick={{ fontSize: 12, fill: theme.palette.text.secondary }} />
            <YAxis
              yAxisId="right"
              orientation="right"
              tick={{ fontSize: 12, fill: theme.palette.text.secondary }}
            />
            <Tooltip content={<CustomTooltip />} />
            <Legend />
            <Area
              yAxisId="left"
              type="monotone"
              dataKey="users"
              fill={alpha(theme.palette.info.main, 0.3)}
              stroke={theme.palette.info.main}
              strokeWidth={2}
            />
            <Bar
              yAxisId="left"
              dataKey="value"
              fill={theme.palette.primary.main}
              fillOpacity={0.8}
            />
            <Line
              yAxisId="right"
              type="monotone"
              dataKey="performance"
              stroke={theme.palette.success.main}
              strokeWidth={3}
              dot={{ fill: theme.palette.success.main, strokeWidth: 2, r: 4 }}
            />
          </ComposedChart>
        );

      default:
        return <div>Visualization type not supported</div>;
    }
  };

  return (
    <Box>
      <Typography variant="h5" fontWeight={600} sx={{ mb: 3 }}>
        {title}
      </Typography>

      {interactive && (
        <ControlPanel>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
            {/* Time Range Slider */}
            <Box sx={{ minWidth: 200 }}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                Time Range
              </Typography>
              <Slider
                value={timeRange}
                onChange={(_, newValue) => setTimeRange(newValue as [number, number])}
                valueLabelDisplay="auto"
                min={0}
                max={100}
                size="small"
              />
            </Box>

            {/* Category Filter */}
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Category</InputLabel>
              <Select
                value={selectedCategory}
                onChange={e => setSelectedCategory(e.target.value)}
                label="Category"
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="api">API</MenuItem>
                <MenuItem value="database">Database</MenuItem>
                <MenuItem value="cache">Cache</MenuItem>
              </Select>
            </FormControl>

            {/* Animation Controls */}
            <ButtonGroup size="small">
              <Button
                onClick={() => setIsPlaying(!isPlaying)}
                startIcon={isPlaying ? <Pause /> : <PlayArrow />}
              >
                {isPlaying ? 'Pause' : 'Play'}
              </Button>
              <Button onClick={() => setTimeRange([0, 20])}>
                <Refresh />
              </Button>
            </ButtonGroup>

            {/* Speed Control */}
            <Box sx={{ minWidth: 100 }}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                Speed
              </Typography>
              <Slider
                value={2000 - animationSpeed}
                onChange={(_, value) => setAnimationSpeed(2000 - (value as number))}
                min={0}
                max={1500}
                step={100}
                size="small"
                valueLabelFormat={value => `${((2000 - value) / 1000).toFixed(1)}x`}
                valueLabelDisplay="auto"
              />
            </Box>
          </Box>

          {/* Filter Chips */}
          {filters.length > 0 && (
            <Box sx={{ mt: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              <FilterList fontSize="small" color="action" />
              {filters.map(filter => (
                <Chip
                  key={filter}
                  label={filter}
                  size="small"
                  onClick={() => handleFilterToggle(filter)}
                  color={activeFilters.includes(filter) ? 'primary' : 'default'}
                  variant={activeFilters.includes(filter) ? 'filled' : 'outlined'}
                />
              ))}
            </Box>
          )}
        </ControlPanel>
      )}

      <Paper sx={{ p: 2, borderRadius: '16px', height }}>
        <ResponsiveContainer width="100%" height="100%">
          {renderVisualization()}
        </ResponsiveContainer>
      </Paper>
    </Box>
  );
};

export default InteractiveVisualization;

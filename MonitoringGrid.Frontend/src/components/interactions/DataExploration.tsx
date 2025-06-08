import React, { useState, useRef, useCallback, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Slider,
  Button,
  ButtonGroup,
  Chip,
  IconButton,
  Tooltip,
  Popover,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Checkbox,
  TextField,
  useTheme,
  alpha,
  styled,
  keyframes,
} from '@mui/material';
import {
  ZoomIn,
  ZoomOut,
  FilterList,
  Visibility,
  VisibilityOff,
  Timeline,
  Brush,
  CropFree,
  Fullscreen,
  PlayArrow,
  Pause,
  SkipNext,
  SkipPrevious,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  Brush as RechartsBrush,
  ReferenceLine,
  ReferenceArea,
} from 'recharts';

// Animations for interactive elements
const zoomIn = keyframes`
  from { transform: scale(1); }
  to { transform: scale(1.1); }
`;

const highlight = keyframes`
  0%, 100% { background-color: transparent; }
  50% { background-color: rgba(25, 118, 210, 0.1); }
`;

const slidePanel = keyframes`
  from { transform: translateX(-100%); }
  to { transform: translateX(0); }
`;

// Interactive chart with zoom and brush
interface InteractiveChartProps {
  data: Array<{ name: string; value: number; timestamp: number }>;
  onDataPointSelect?: (dataPoint: any) => void;
  onRangeSelect?: (range: { start: number; end: number }) => void;
  height?: number;
}

export const InteractiveChart: React.FC<InteractiveChartProps> = ({
  data,
  onDataPointSelect,
  onRangeSelect,
  height = 400,
}) => {
  const theme = useTheme();
  const [zoomDomain, setZoomDomain] = useState<{ left: number; right: number } | null>(null);
  const [brushDomain, setBrushDomain] = useState<{ startIndex: number; endIndex: number } | null>(
    null
  );
  const [selectedArea, setSelectedArea] = useState<{
    x1: number;
    x2: number;
    y1: number;
    y2: number;
  } | null>(null);
  const [isSelecting, setIsSelecting] = useState(false);
  const [hoveredPoint, setHoveredPoint] = useState<any>(null);

  const handleMouseDown = (e: any) => {
    if (e && e.activeLabel) {
      setIsSelecting(true);
      setSelectedArea({
        x1: e.activeLabel,
        x2: e.activeLabel,
        y1: e.activePayload?.[0]?.value || 0,
        y2: e.activePayload?.[0]?.value || 0,
      });
    }
  };

  const handleMouseMove = (e: any) => {
    if (isSelecting && selectedArea && e && e.activeLabel) {
      setSelectedArea(prev =>
        prev
          ? {
              ...prev,
              x2: e.activeLabel,
              y2: e.activePayload?.[0]?.value || prev.y2,
            }
          : null
      );
    }
  };

  const handleMouseUp = () => {
    if (isSelecting && selectedArea) {
      const startIndex = Math.min(selectedArea.x1, selectedArea.x2);
      const endIndex = Math.max(selectedArea.x1, selectedArea.x2);
      onRangeSelect?.({ start: startIndex, end: endIndex });
    }
    setIsSelecting(false);
    setSelectedArea(null);
  };

  const resetZoom = () => {
    setZoomDomain(null);
    setBrushDomain(null);
  };

  const zoomIn = () => {
    if (brushDomain) {
      const start = Math.max(0, brushDomain.startIndex - 5);
      const end = Math.min(data.length - 1, brushDomain.endIndex + 5);
      setBrushDomain({ startIndex: start, endIndex: end });
    }
  };

  const zoomOut = () => {
    if (brushDomain) {
      const range = brushDomain.endIndex - brushDomain.startIndex;
      const expansion = Math.floor(range * 0.2);
      const start = Math.max(0, brushDomain.startIndex - expansion);
      const end = Math.min(data.length - 1, brushDomain.endIndex + expansion);
      setBrushDomain({ startIndex: start, endIndex: end });
    } else {
      setBrushDomain({ startIndex: 0, endIndex: data.length - 1 });
    }
  };

  const filteredData = brushDomain
    ? data.slice(brushDomain.startIndex, brushDomain.endIndex + 1)
    : data;

  return (
    <Box sx={{ position: 'relative' }}>
      {/* Chart Controls */}
      <Box
        sx={{
          position: 'absolute',
          top: 8,
          right: 8,
          zIndex: 10,
          display: 'flex',
          gap: 1,
        }}
      >
        <ButtonGroup size="small" variant="outlined">
          <Tooltip title="Zoom In">
            <IconButton onClick={zoomIn} size="small">
              <ZoomIn />
            </IconButton>
          </Tooltip>
          <Tooltip title="Zoom Out">
            <IconButton onClick={zoomOut} size="small">
              <ZoomOut />
            </IconButton>
          </Tooltip>
          <Tooltip title="Reset Zoom">
            <IconButton onClick={resetZoom} size="small">
              <CropFree />
            </IconButton>
          </Tooltip>
        </ButtonGroup>
      </Box>

      {/* Interactive Chart */}
      <ResponsiveContainer width="100%" height={height}>
        <LineChart
          data={filteredData}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          margin={{ top: 20, right: 80, left: 20, bottom: 20 }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke={alpha(theme.palette.divider, 0.3)} />
          <XAxis dataKey="name" tick={{ fontSize: 12, fill: theme.palette.text.secondary }} />
          <YAxis tick={{ fontSize: 12, fill: theme.palette.text.secondary }} />
          <RechartsTooltip
            content={({ active, payload, label }) => {
              if (active && payload && payload.length) {
                return (
                  <Paper
                    sx={{ p: 2, backgroundColor: alpha(theme.palette.background.paper, 0.95) }}
                  >
                    <Typography variant="subtitle2">{label}</Typography>
                    <Typography variant="body2" color="primary">
                      Value: {payload[0].value}
                    </Typography>
                  </Paper>
                );
              }
              return null;
            }}
          />

          {/* Selection Area */}
          {selectedArea && (
            <ReferenceArea
              x1={selectedArea.x1}
              x2={selectedArea.x2}
              fill={alpha(theme.palette.primary.main, 0.2)}
              stroke={theme.palette.primary.main}
            />
          )}

          <Line
            type="monotone"
            dataKey="value"
            stroke={theme.palette.primary.main}
            strokeWidth={2}
            dot={{ fill: theme.palette.primary.main, strokeWidth: 2, r: 4 }}
            activeDot={{
              r: 6,
              stroke: theme.palette.primary.main,
              strokeWidth: 2,
              onClick: onDataPointSelect,
            }}
          />

          {/* Brush for range selection */}
          <RechartsBrush
            dataKey="name"
            height={30}
            stroke={theme.palette.primary.main}
            onChange={(brushData: any) => {
              if (brushData) {
                setBrushDomain({
                  startIndex: brushData.startIndex || 0,
                  endIndex: brushData.endIndex || data.length - 1,
                });
              }
            }}
          />
        </LineChart>
      </ResponsiveContainer>
    </Box>
  );
};

// Data filter panel
interface DataFilterPanelProps {
  filters: Array<{
    id: string;
    label: string;
    type: 'range' | 'select' | 'multiselect' | 'date';
    options?: string[];
    value: any;
    min?: number;
    max?: number;
  }>;
  onFilterChange: (filterId: string, value: any) => void;
  onResetFilters: () => void;
}

export const DataFilterPanel: React.FC<DataFilterPanelProps> = ({
  filters,
  onFilterChange,
  onResetFilters,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const theme = useTheme();

  const FilterPanel = styled(Paper)(({ theme }) => ({
    position: 'absolute',
    top: '100%',
    left: 0,
    width: 320,
    maxHeight: 400,
    overflowY: 'auto',
    zIndex: 1200,
    animation: `${slidePanel} 0.3s ease-out`,
  }));

  const renderFilter = (filter: any) => {
    switch (filter.type) {
      case 'range':
        return (
          <Box sx={{ px: 2, py: 1 }}>
            <Typography variant="body2" gutterBottom>
              {filter.label}
            </Typography>
            <Slider
              value={filter.value}
              onChange={(_, value) => onFilterChange(filter.id, value)}
              min={filter.min}
              max={filter.max}
              valueLabelDisplay="auto"
              size="small"
            />
          </Box>
        );

      case 'multiselect':
        return (
          <Box sx={{ px: 2, py: 1 }}>
            <Typography variant="body2" gutterBottom>
              {filter.label}
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {filter.options?.map((option: string) => (
                <Chip
                  key={option}
                  label={option}
                  size="small"
                  clickable
                  color={filter.value.includes(option) ? 'primary' : 'default'}
                  onClick={() => {
                    const newValue = filter.value.includes(option)
                      ? filter.value.filter((v: string) => v !== option)
                      : [...filter.value, option];
                    onFilterChange(filter.id, newValue);
                  }}
                />
              ))}
            </Box>
          </Box>
        );

      default:
        return null;
    }
  };

  return (
    <Box sx={{ position: 'relative' }}>
      <Button
        startIcon={<FilterList />}
        onClick={e => {
          setAnchorEl(e.currentTarget);
          setIsOpen(!isOpen);
        }}
        variant="outlined"
        size="small"
      >
        Filters ({filters.filter(f => f.value !== null && f.value !== '').length})
      </Button>

      <Popover
        open={isOpen}
        anchorEl={anchorEl}
        onClose={() => setIsOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Box sx={{ p: 2, minWidth: 300 }}>
          <Box
            sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}
          >
            <Typography variant="h6">Filters</Typography>
            <Button size="small" onClick={onResetFilters}>
              Reset All
            </Button>
          </Box>

          {filters.map(filter => (
            <Box key={filter.id} sx={{ mb: 2 }}>
              {renderFilter(filter)}
            </Box>
          ))}
        </Box>
      </Popover>
    </Box>
  );
};

// Time series player
interface TimeSeriesPlayerProps {
  data: Array<{ timestamp: number; [key: string]: any }>;
  onTimeChange: (timestamp: number) => void;
  autoPlay?: boolean;
  playbackSpeed?: number;
}

export const TimeSeriesPlayer: React.FC<TimeSeriesPlayerProps> = ({
  data,
  onTimeChange,
  autoPlay = false,
  playbackSpeed = 1000,
}) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(autoPlay);
  const [speed, setSpeed] = useState(playbackSpeed);
  const intervalRef = useRef<NodeJS.Timeout>();

  useEffect(() => {
    if (isPlaying && data.length > 0) {
      intervalRef.current = setInterval(() => {
        setCurrentIndex(prev => {
          const nextIndex = (prev + 1) % data.length;
          onTimeChange(data[nextIndex].timestamp);
          return nextIndex;
        });
      }, speed);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [isPlaying, speed, data, onTimeChange]);

  const handleSliderChange = (value: number) => {
    setCurrentIndex(value);
    onTimeChange(data[value].timestamp);
  };

  const PlayerContainer = styled(Paper)(({ theme }) => ({
    padding: theme.spacing(2),
    display: 'flex',
    alignItems: 'center',
    gap: theme.spacing(2),
    borderRadius: theme.shape.borderRadius * 2,
  }));

  return (
    <PlayerContainer>
      <IconButton
        onClick={() => setCurrentIndex(Math.max(0, currentIndex - 1))}
        disabled={currentIndex === 0}
      >
        <SkipPrevious />
      </IconButton>

      <IconButton onClick={() => setIsPlaying(!isPlaying)} color="primary">
        {isPlaying ? <Pause /> : <PlayArrow />}
      </IconButton>

      <IconButton
        onClick={() => setCurrentIndex(Math.min(data.length - 1, currentIndex + 1))}
        disabled={currentIndex === data.length - 1}
      >
        <SkipNext />
      </IconButton>

      <Box sx={{ flex: 1, mx: 2 }}>
        <Slider
          value={currentIndex}
          onChange={(_, value) => handleSliderChange(value as number)}
          min={0}
          max={data.length - 1}
          step={1}
          valueLabelDisplay="auto"
          valueLabelFormat={value => new Date(data[value]?.timestamp || 0).toLocaleTimeString()}
        />
      </Box>

      <Typography variant="body2" sx={{ minWidth: 100 }}>
        {currentIndex + 1} / {data.length}
      </Typography>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Typography variant="caption">Speed:</Typography>
        <Slider
          value={2000 - speed}
          onChange={(_, value) => setSpeed(2000 - (value as number))}
          min={100}
          max={1900}
          step={100}
          sx={{ width: 80 }}
          valueLabelFormat={value => `${((2000 - value) / 1000).toFixed(1)}x`}
          valueLabelDisplay="auto"
        />
      </Box>
    </PlayerContainer>
  );
};

export default {
  InteractiveChart,
  DataFilterPanel,
  TimeSeriesPlayer,
};

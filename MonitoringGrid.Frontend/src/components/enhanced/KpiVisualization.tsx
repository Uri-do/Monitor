import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  LinearProgress,
  CircularProgress,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  useTheme,
  alpha,
  styled,
  keyframes,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Remove,
  MoreVert,
  CheckCircle,
  Warning,
  Error,
  Schedule,
  Speed,
  Timeline,
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
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';

// Animations
const progressAnimation = keyframes`
  0% { transform: scaleX(0); }
  100% { transform: scaleX(1); }
`;

const countUp = keyframes`
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
`;

const StyledCard = styled(Card)(({ theme }) => ({
  borderRadius: '16px',
  transition: 'all 0.3s ease',
  cursor: 'pointer',
  '&:hover': {
    transform: 'translateY(-4px)',
    boxShadow: theme.shadows[8],
  },
}));

const AnimatedProgress = styled(LinearProgress)(({ theme }) => ({
  height: 8,
  borderRadius: 4,
  '& .MuiLinearProgress-bar': {
    borderRadius: 4,
    animation: `${progressAnimation} 1.5s ease-out`,
  },
}));

const MetricValue = styled(Typography)(({ theme }) => ({
  animation: `${countUp} 0.8s ease-out`,
}));

interface KpiData {
  id: string;
  name: string;
  value: number;
  target: number;
  unit: string;
  trend: 'up' | 'down' | 'neutral';
  change: number;
  status: 'healthy' | 'warning' | 'critical';
  history: Array<{ timestamp: string; value: number; target?: number }>;
  threshold?: {
    warning: number;
    critical: number;
  };
}

interface KpiVisualizationProps {
  kpis: KpiData[];
  layout?: 'grid' | 'list';
  showTrends?: boolean;
  showTargets?: boolean;
  onKpiClick?: (kpi: KpiData) => void;
  refreshInterval?: number;
}

const KpiCard: React.FC<{
  kpi: KpiData;
  showTrends: boolean;
  showTargets: boolean;
  onClick?: () => void;
}> = ({ kpi, showTrends, showTargets, onClick }) => {
  const theme = useTheme();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy': return theme.palette.success.main;
      case 'warning': return theme.palette.warning.main;
      case 'critical': return theme.palette.error.main;
      default: return theme.palette.text.secondary;
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'healthy': return <CheckCircle fontSize="small" />;
      case 'warning': return <Warning fontSize="small" />;
      case 'critical': return <Error fontSize="small" />;
      default: return <Schedule fontSize="small" />;
    }
  };

  const getTrendIcon = (trend: string) => {
    switch (trend) {
      case 'up': return <TrendingUp fontSize="small" color="success" />;
      case 'down': return <TrendingDown fontSize="small" color="error" />;
      default: return <Remove fontSize="small" color="disabled" />;
    }
  };

  const progressPercentage = showTargets ? (kpi.value / kpi.target) * 100 : 0;
  const statusColor = getStatusColor(kpi.status);

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  return (
    <StyledCard onClick={onClick}>
      <CardContent sx={{ p: 3 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Box sx={{ flex: 1 }}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
              {kpi.name}
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Chip
                icon={getStatusIcon(kpi.status)}
                label={kpi.status}
                size="small"
                sx={{
                  backgroundColor: alpha(statusColor, 0.1),
                  color: statusColor,
                  fontWeight: 600,
                  textTransform: 'capitalize',
                }}
              />
            </Box>
          </Box>
          
          <IconButton size="small" onClick={handleMenuClick}>
            <MoreVert />
          </IconButton>
          
          <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
            <MenuItem onClick={handleMenuClose}>View Details</MenuItem>
            <MenuItem onClick={handleMenuClose}>Edit KPI</MenuItem>
            <MenuItem onClick={handleMenuClose}>Export Data</MenuItem>
          </Menu>
        </Box>

        {/* Value and Trend */}
        <Box sx={{ mb: 2 }}>
          <MetricValue variant="h4" fontWeight={700} sx={{ mb: 1 }}>
            {kpi.value.toLocaleString()} {kpi.unit}
          </MetricValue>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {getTrendIcon(kpi.trend)}
            <Typography
              variant="body2"
              sx={{
                color: kpi.trend === 'up' 
                  ? theme.palette.success.main 
                  : kpi.trend === 'down' 
                    ? theme.palette.error.main 
                    : theme.palette.text.secondary,
                fontWeight: 600,
              }}
            >
              {kpi.change > 0 ? '+' : ''}{kpi.change}%
            </Typography>
            <Typography variant="body2" color="text.secondary">
              vs last period
            </Typography>
          </Box>
        </Box>

        {/* Target Progress */}
        {showTargets && (
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Progress to Target
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {Math.round(progressPercentage)}%
              </Typography>
            </Box>
            <AnimatedProgress
              variant="determinate"
              value={Math.min(progressPercentage, 100)}
              sx={{
                backgroundColor: alpha(statusColor, 0.1),
                '& .MuiLinearProgress-bar': {
                  backgroundColor: statusColor,
                },
              }}
            />
            <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
              Target: {kpi.target.toLocaleString()} {kpi.unit}
            </Typography>
          </Box>
        )}

        {/* Mini Chart */}
        {showTrends && kpi.history.length > 0 && (
          <Box sx={{ height: 60, mt: 2 }}>
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={kpi.history.slice(-10)}>
                <Area
                  type="monotone"
                  dataKey="value"
                  stroke={statusColor}
                  fill={alpha(statusColor, 0.2)}
                  strokeWidth={2}
                />
                {showTargets && (
                  <ReferenceLine 
                    y={kpi.target} 
                    stroke={theme.palette.text.secondary}
                    strokeDasharray="3 3"
                  />
                )}
              </AreaChart>
            </ResponsiveContainer>
          </Box>
        )}
      </CardContent>
    </StyledCard>
  );
};

export const KpiVisualization: React.FC<KpiVisualizationProps> = ({
  kpis,
  layout = 'grid',
  showTrends = true,
  showTargets = true,
  onKpiClick,
  refreshInterval = 30000,
}) => {
  const theme = useTheme();
  const [animationKey, setAnimationKey] = useState(0);

  // Trigger re-animation on data updates
  useEffect(() => {
    const interval = setInterval(() => {
      setAnimationKey(prev => prev + 1);
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [refreshInterval]);

  const getGridSize = () => {
    const count = kpis.length;
    if (count <= 2) return { xs: 12, sm: 6 };
    if (count <= 4) return { xs: 12, sm: 6, md: 3 };
    if (count <= 6) return { xs: 12, sm: 6, md: 4 };
    return { xs: 12, sm: 6, md: 4, lg: 3 };
  };

  const gridSize = getGridSize();

  if (layout === 'list') {
    return (
      <Box>
        {kpis.map((kpi, index) => (
          <Box key={`${kpi.id}-${animationKey}`} sx={{ mb: 2 }}>
            <KpiCard
              kpi={kpi}
              showTrends={showTrends}
              showTargets={showTargets}
              onClick={() => onKpiClick?.(kpi)}
            />
          </Box>
        ))}
      </Box>
    );
  }

  return (
    <Grid container spacing={3}>
      {kpis.map((kpi, index) => (
        <Grid item {...gridSize} key={`${kpi.id}-${animationKey}`}>
          <KpiCard
            kpi={kpi}
            showTrends={showTrends}
            showTargets={showTargets}
            onClick={() => onKpiClick?.(kpi)}
          />
        </Grid>
      ))}
    </Grid>
  );
};

export default KpiVisualization;

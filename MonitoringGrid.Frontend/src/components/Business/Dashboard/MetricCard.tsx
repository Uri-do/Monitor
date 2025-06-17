import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  IconButton,
  Chip,
  LinearProgress,
  Tooltip,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  TrendingFlat,
  Info as InfoIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  CheckCircle as SuccessIcon,
} from '@mui/icons-material';

/**
 * Advanced Metric Card Component
 * Displays key metrics with trends, progress, and status indicators
 */

export type MetricTrend = 'up' | 'down' | 'flat';
export type MetricStatus = 'success' | 'warning' | 'error' | 'info';

export interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  trend?: MetricTrend;
  trendValue?: string;
  status?: MetricStatus;
  progress?: number;
  icon?: React.ReactNode;
  color?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  onClick?: () => void;
  loading?: boolean;
  tooltip?: string;
  badge?: string;
  compact?: boolean;
}

const getTrendIcon = (trend: MetricTrend) => {
  switch (trend) {
    case 'up': return <TrendingUp />;
    case 'down': return <TrendingDown />;
    case 'flat': return <TrendingFlat />;
    default: return null;
  }
};

const getTrendColor = (trend: MetricTrend) => {
  switch (trend) {
    case 'up': return 'success.main';
    case 'down': return 'error.main';
    case 'flat': return 'text.secondary';
    default: return 'text.secondary';
  }
};

const getStatusIcon = (status: MetricStatus) => {
  switch (status) {
    case 'success': return <SuccessIcon color="success" />;
    case 'warning': return <WarningIcon color="warning" />;
    case 'error': return <ErrorIcon color="error" />;
    case 'info': return <InfoIcon color="info" />;
    default: return null;
  }
};

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  subtitle,
  trend,
  trendValue,
  status,
  progress,
  icon,
  color = 'primary',
  onClick,
  loading = false,
  tooltip,
  badge,
  compact = false,
}) => {
  const cardContent = (
    <Card
      sx={{
        cursor: onClick ? 'pointer' : 'default',
        transition: 'all 0.2s ease-in-out',
        '&:hover': onClick ? {
          transform: 'translateY(-2px)',
          boxShadow: 4,
        } : {},
        height: compact ? 'auto' : 140,
        position: 'relative',
        overflow: 'visible',
      }}
      onClick={onClick}
    >
      <CardContent sx={{ 
        height: '100%', 
        display: 'flex', 
        flexDirection: 'column',
        p: compact ? 2 : 3,
      }}>
        {/* Header */}
        <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={1}>
          <Box display="flex" alignItems="center" gap={1}>
            {icon && (
              <Box sx={{ color: `${color}.main` }}>
                {icon}
              </Box>
            )}
            <Typography 
              variant={compact ? "body2" : "subtitle2"} 
              color="text.secondary"
              sx={{ fontWeight: 500 }}
            >
              {title}
            </Typography>
          </Box>
          
          <Box display="flex" alignItems="center" gap={0.5}>
            {status && getStatusIcon(status)}
            {badge && (
              <Chip 
                label={badge} 
                size="small" 
                color={color}
                sx={{ height: 20, fontSize: '0.7rem' }}
              />
            )}
          </Box>
        </Box>

        {/* Value */}
        <Box sx={{ flexGrow: 1, display: 'flex', alignItems: 'center' }}>
          <Typography 
            variant={compact ? "h6" : "h4"} 
            sx={{ 
              fontWeight: 700,
              color: `${color}.main`,
              lineHeight: 1,
            }}
          >
            {loading ? '...' : value}
          </Typography>
        </Box>

        {/* Footer */}
        <Box>
          {/* Trend */}
          {trend && trendValue && (
            <Box display="flex" alignItems="center" gap={0.5} mb={1}>
              <Box sx={{ color: getTrendColor(trend), display: 'flex', alignItems: 'center' }}>
                {getTrendIcon(trend)}
              </Box>
              <Typography 
                variant="caption" 
                sx={{ color: getTrendColor(trend), fontWeight: 600 }}
              >
                {trendValue}
              </Typography>
            </Box>
          )}

          {/* Subtitle */}
          {subtitle && (
            <Typography variant="caption" color="text.secondary">
              {subtitle}
            </Typography>
          )}

          {/* Progress */}
          {progress !== undefined && (
            <Box sx={{ mt: 1 }}>
              <LinearProgress
                variant="determinate"
                value={progress}
                color={color}
                sx={{
                  height: 4,
                  borderRadius: 2,
                  backgroundColor: 'grey.200',
                }}
              />
              <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                {progress}% complete
              </Typography>
            </Box>
          )}
        </Box>

        {/* Loading overlay */}
        {loading && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundColor: 'rgba(255, 255, 255, 0.8)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              borderRadius: 1,
            }}
          >
            <LinearProgress sx={{ width: '60%' }} />
          </Box>
        )}
      </CardContent>
    </Card>
  );

  if (tooltip) {
    return (
      <Tooltip title={tooltip} arrow>
        {cardContent}
      </Tooltip>
    );
  }

  return cardContent;
};

// Predefined metric card variants
export const CounterCard: React.FC<Omit<MetricCardProps, 'icon'> & { iconType?: 'success' | 'warning' | 'error' | 'info' }> = ({
  iconType = 'info',
  ...props
}) => (
  <MetricCard
    {...props}
    icon={getStatusIcon(iconType)}
    color={iconType}
  />
);

export const ProgressCard: React.FC<Omit<MetricCardProps, 'progress'> & { current: number; total: number }> = ({
  current,
  total,
  value,
  ...props
}) => {
  const progress = total > 0 ? Math.round((current / total) * 100) : 0;
  
  return (
    <MetricCard
      {...props}
      value={value || `${current}/${total}`}
      progress={progress}
    />
  );
};

export const TrendCard: React.FC<MetricCardProps & { previousValue?: number; currentValue?: number }> = ({
  previousValue,
  currentValue,
  trend: propTrend,
  trendValue: propTrendValue,
  ...props
}) => {
  let calculatedTrend: MetricTrend = 'flat';
  let calculatedTrendValue = '';

  if (previousValue !== undefined && currentValue !== undefined) {
    const change = currentValue - previousValue;
    const percentChange = previousValue > 0 ? Math.round((change / previousValue) * 100) : 0;
    
    if (change > 0) {
      calculatedTrend = 'up';
      calculatedTrendValue = `+${percentChange}%`;
    } else if (change < 0) {
      calculatedTrend = 'down';
      calculatedTrendValue = `${percentChange}%`;
    } else {
      calculatedTrend = 'flat';
      calculatedTrendValue = '0%';
    }
  }

  return (
    <MetricCard
      {...props}
      trend={propTrend || calculatedTrend}
      trendValue={propTrendValue || calculatedTrendValue}
    />
  );
};

export default MetricCard;

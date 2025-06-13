import React from 'react';
import {
  Box,
  Typography,
  Grid,
  LinearProgress,
  Chip,
  Paper,
  CardContent,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Badge,
  useTheme,
} from '@mui/material';
import { PlayCircle, PlayArrow, TrendingUp } from '@mui/icons-material';
import { formatDistanceToNow } from 'date-fns';
import { Card } from '@/components';

export interface RunningIndicator {
  indicatorID: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
}

interface RunningIndicatorsDisplayProps {
  runningIndicators: RunningIndicator[];
  variant?: 'card' | 'section';
  title?: string;
  showNavigateButton?: boolean;
  onNavigate?: () => void;
  maxDisplay?: number;
  showProgress?: boolean;
  compact?: boolean;
}

const RunningIndicatorsDisplay: React.FC<RunningIndicatorsDisplayProps> = ({
  runningIndicators,
  variant = 'card',
  title = 'Currently Executing Indicators',
  showNavigateButton = false,
  onNavigate,
  maxDisplay,
  showProgress = true,
  compact = false,
}) => {
  const theme = useTheme();

  const formatElapsedTime = (elapsedTime?: number) => {
    if (!elapsedTime) return '';
    const minutes = Math.floor(elapsedTime / 60);
    const seconds = elapsedTime % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString();
  };

  const displayIndicators = maxDisplay ? runningIndicators.slice(0, maxDisplay) : runningIndicators;

  const renderIndicatorItem = (indicator: RunningIndicator, _index: number) => {
    if (compact) {
      // Compact version for WorkerDashboardCard
      return (
        <Box
          key={indicator.indicatorID}
          sx={{
            mb: 2,
            p: 1.5,
            border: 1,
            borderColor: 'success.main',
            borderRadius: 1,
            backgroundColor: 'success.light',
            color: 'success.contrastText',
          }}
        >
          <Box
            sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}
          >
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {indicator.indicator}
            </Typography>
            <Chip
              label="Running"
              size="small"
              color="success"
              sx={{ fontSize: '0.6rem', height: 16 }}
            />
          </Box>

          <Typography variant="caption" sx={{ opacity: 0.8 }}>
            Started: {formatDateTime(indicator.startTime)}
          </Typography>

          {showProgress && indicator.progress !== undefined && (
            <Box sx={{ mt: 1 }}>
              <LinearProgress
                variant="determinate"
                value={indicator.progress}
                sx={{
                  height: 4,
                  borderRadius: 2,
                }}
              />
              <Typography variant="caption" sx={{ opacity: 0.8 }}>
                {indicator.progress}% - {indicator.currentStep}
              </Typography>
            </Box>
          )}
        </Box>
      );
    }

    if (variant === 'section') {
      // Section version for WorkerManagement
      return (
        <Box
          key={indicator.indicatorID}
          sx={{
            p: 2,
            bgcolor: 'background.paper',
            borderRadius: 1,
            border: 1,
            borderColor: 'divider',
          }}
        >
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={8}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Box
                  sx={{
                    width: 8,
                    height: 8,
                    borderRadius: '50%',
                    bgcolor: 'success.main',
                    animation: 'pulse 1s infinite',
                    '@keyframes pulse': {
                      '0%': { opacity: 1, transform: 'scale(1)' },
                      '50%': { opacity: 0.6, transform: 'scale(1.3)' },
                      '100%': { opacity: 1, transform: 'scale(1)' },
                    },
                  }}
                />
                <Box>
                  <Typography variant="subtitle1" fontWeight="medium">
                    {indicator.indicator}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Owner: {indicator.owner}
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid item xs={12} md={4}>
              <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                <Typography variant="body2" color="text.secondary">
                  Started: {formatDateTime(indicator.startTime)}
                </Typography>
                {showProgress && indicator.progress !== undefined && (
                  <Box sx={{ mt: 1 }}>
                    <LinearProgress
                      variant="determinate"
                      value={indicator.progress}
                      sx={{ height: 4, borderRadius: 2 }}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {indicator.progress}% - {indicator.currentStep || 'Processing...'}
                    </Typography>
                  </Box>
                )}
              </Box>
            </Grid>
          </Grid>
        </Box>
      );
    }

    // Card version for Dashboard
    return (
      <ListItem
        key={indicator.indicatorID}
        sx={{
          borderRadius: 2,
          mb: 1,
          border: '1px solid',
          borderColor: 'primary.light',
          backgroundColor: 'primary.50',
          '&:hover': {
            backgroundColor: 'primary.100',
            transform: 'translateX(4px)',
            transition: 'all 0.2s ease-in-out',
          },
        }}
      >
        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
          <Chip
            label="Running"
            color="primary"
            size="small"
            icon={<PlayCircle sx={{ fontSize: '14px !important' }} />}
            sx={{
              fontWeight: 600,
              animation: 'pulse 2s infinite',
              '@keyframes pulse': {
                '0%': { opacity: 1 },
                '50%': { opacity: 0.7 },
                '100%': { opacity: 1 },
              },
            }}
          />
        </ListItemIcon>
        <ListItemText
          primary={indicator.indicator}
          secondary={
            <Box component="span">
              <Box component="span" sx={{ display: 'block', mb: 0.5 }}>
                Owner: {indicator.owner}
              </Box>
              <Box component="span" sx={{ display: 'block', mb: 1 }}>
                Started: {formatDistanceToNow(new Date(indicator.startTime), { addSuffix: true })}
                {indicator.elapsedTime && ` • Elapsed: ${formatElapsedTime(indicator.elapsedTime)}`}
              </Box>

              {/* Progress Bar */}
              {showProgress && indicator.progress !== undefined && (
                <Box sx={{ mt: 1 }}>
                  <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                    <Box component="span" sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>
                      {indicator.currentStep || 'Processing...'}
                    </Box>
                    <Box component="span" sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>
                      {indicator.progress}%
                    </Box>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={indicator.progress}
                    sx={{
                      height: 4,
                      borderRadius: 2,
                    }}
                  />
                </Box>
              )}
            </Box>
          }
          primaryTypographyProps={{
            variant: 'subtitle2',
            sx: { fontWeight: 600, mb: 0.5 },
          }}
          secondaryTypographyProps={{
            component: 'div',
            sx: { fontSize: '0.875rem', color: 'text.secondary' },
          }}
        />
      </ListItem>
    );
  };

  const renderEmptyState = () => (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      py={4}
      sx={{
        backgroundColor: theme.palette.mode === 'light' ? 'grey.50' : 'grey.900',
        borderRadius: 2,
        border: '2px dashed',
        borderColor: theme.palette.mode === 'light' ? 'grey.300' : 'grey.700',
      }}
    >
      <PlayCircle sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
      <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
        No Indicators currently running
      </Typography>
      <Typography color="text.secondary" variant="caption">
        All Indicators are idle
      </Typography>
    </Box>
  );

  if (variant === 'section') {
    return (
      <Paper sx={{ p: 3, mb: 3, bgcolor: 'success.50', border: 1, borderColor: 'success.200' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <TrendingUp sx={{ color: 'success.main' }} />
          <Typography variant="h6" color="success.main">
            {title} ({runningIndicators.length})
          </Typography>
          <Chip
            label="LIVE"
            size="small"
            color="success"
            sx={{
              animation: 'pulse 2s infinite',
              '@keyframes pulse': {
                '0%': { opacity: 1 },
                '50%': { opacity: 0.7 },
                '100%': { opacity: 1 },
              },
            }}
          />
        </Box>

        {runningIndicators.length > 0 ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {displayIndicators.map(renderIndicatorItem)}
          </Box>
        ) : (
          renderEmptyState()
        )}
      </Paper>
    );
  }

  // Card variant (default)
  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Badge
                badgeContent={runningIndicators.length}
                color="primary"
                sx={{ '& .MuiBadge-badge': { fontSize: '0.7rem' } }}
              >
                <PlayCircle sx={{ color: 'primary.main' }} />
              </Badge>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {title}
              </Typography>
            </Box>
            {showNavigateButton && onNavigate && (
              <IconButton
                size="small"
                onClick={onNavigate}
                sx={{
                  backgroundColor: 'primary.main',
                  color: 'white',
                  '&:hover': {
                    backgroundColor: 'primary.dark',
                  },
                }}
              >
                <PlayArrow />
              </IconButton>
            )}
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {runningIndicators.length > 0 ? (
              <List sx={{ p: 0 }}>{displayIndicators.map(renderIndicatorItem)}</List>
            ) : (
              renderEmptyState()
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default RunningIndicatorsDisplay;

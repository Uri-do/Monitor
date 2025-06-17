import React from 'react';
import {
  Box,
  Typography,
  Grid,
  LinearProgress,
  Chip,
  ListItem,
  ListItemText,
  ListItemIcon,
} from '@mui/material';
import { PlayCircle } from '@mui/icons-material';
import { formatDistanceToNow } from 'date-fns';
import { RunningIndicator } from '../types';

interface IndicatorItemProps {
  indicator: RunningIndicator;
  variant: 'card' | 'section' | 'compact';
  showProgress: boolean;
}

export const IndicatorItem: React.FC<IndicatorItemProps> = ({
  indicator,
  variant,
  showProgress,
}) => {
  const formatElapsedTime = (elapsedTime?: number) => {
    if (!elapsedTime) return '';
    const minutes = Math.floor(elapsedTime / 60);
    const seconds = elapsedTime % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString();
  };

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'completed': return 'success';
      case 'failed': return 'error';
      case 'running':
      default: return 'primary';
    }
  };

  const getStatusLabel = (status?: string) => {
    switch (status) {
      case 'completed': return 'Completed';
      case 'failed': return 'Failed';
      case 'running':
      default: return 'Running';
    }
  };

  const statusColor = getStatusColor(indicator.status);
  const statusLabel = getStatusLabel(indicator.status);
  const isCompleted = indicator.status === 'completed' || indicator.status === 'failed';

  if (variant === 'compact') {
    return (
      <Box
        sx={{
          mb: 2,
          p: 1.5,
          border: 1,
          borderColor: `${statusColor}.main`,
          borderRadius: 1,
          backgroundColor: `${statusColor}.light`,
          color: `${statusColor}.contrastText`,
          opacity: isCompleted ? 0.8 : 1,
        }}
      >
        <Box
          sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}
        >
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {indicator.indicator}
          </Typography>
          <Chip
            label={statusLabel}
            size="small"
            color={statusColor}
            sx={{ fontSize: '0.6rem', height: 16 }}
          />
        </Box>

        <Typography variant="caption" sx={{ opacity: 0.8 }}>
          Started: {formatDateTime(indicator.startTime)}
        </Typography>

        {isCompleted && indicator.completedAt && (
          <Typography variant="caption" sx={{ opacity: 0.8, display: 'block' }}>
            Completed: {formatDateTime(indicator.completedAt)}
          </Typography>
        )}

        {isCompleted && indicator.duration && (
          <Typography variant="caption" sx={{ opacity: 0.8, display: 'block' }}>
            Duration: {Math.round(indicator.duration / 1000)}s
          </Typography>
        )}

        {showProgress && indicator.progress !== undefined && (
          <Box sx={{ mt: 1 }}>
            <LinearProgress
              variant="determinate"
              value={isCompleted ? 100 : indicator.progress}
              sx={{
                height: 4,
                borderRadius: 2,
              }}
            />
            <Typography variant="caption" sx={{ opacity: 0.8 }}>
              {isCompleted ? '100%' : `${indicator.progress}%`} - {
                isCompleted
                  ? (indicator.status === 'completed' ? 'Completed Successfully' : 'Failed')
                  : (indicator.currentStep || 'Processing...')
              }
            </Typography>
          </Box>
        )}
      </Box>
    );
  }

  if (variant === 'section') {
    return (
      <Box
        sx={{
          p: 2,
          bgcolor: 'background.paper',
          borderRadius: 1,
          border: 1,
          borderColor: isCompleted ? `${statusColor}.light` : 'divider',
          opacity: isCompleted ? 0.9 : 1,
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
                  bgcolor: `${statusColor}.main`,
                  animation: isCompleted ? 'none' : 'pulse 1s infinite',
                  '@keyframes pulse': {
                    '0%': { opacity: 1, transform: 'scale(1)' },
                    '50%': { opacity: 0.6, transform: 'scale(1.3)' },
                    '100%': { opacity: 1, transform: 'scale(1)' },
                  },
                }}
              />
              <Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography variant="subtitle1" fontWeight="medium">
                    {indicator.indicator}
                  </Typography>
                  <Chip
                    label={statusLabel}
                    size="small"
                    color={statusColor}
                    sx={{ fontSize: '0.7rem', height: 18 }}
                  />
                </Box>
                <Typography variant="body2" color="text.secondary">
                  Owner: {indicator.owner}
                </Typography>
                {isCompleted && indicator.value !== undefined && (
                  <Typography variant="body2" color="text.secondary">
                    Value: {indicator.value}
                  </Typography>
                )}
                {indicator.errorMessage && (
                  <Typography variant="body2" color="error.main">
                    Error: {indicator.errorMessage}
                  </Typography>
                )}
              </Box>
            </Box>
          </Grid>
          <Grid item xs={12} md={4}>
            <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
              <Typography variant="body2" color="text.secondary">
                Started: {formatDateTime(indicator.startTime)}
              </Typography>
              {isCompleted && indicator.completedAt && (
                <Typography variant="body2" color="text.secondary">
                  Completed: {formatDateTime(indicator.completedAt)}
                </Typography>
              )}
              {isCompleted && indicator.duration && (
                <Typography variant="body2" color="text.secondary">
                  Duration: {Math.round(indicator.duration / 1000)}s
                </Typography>
              )}
              {showProgress && indicator.progress !== undefined && (
                <Box sx={{ mt: 1 }}>
                  <LinearProgress
                    variant="determinate"
                    value={isCompleted ? 100 : indicator.progress}
                    color={statusColor}
                    sx={{ height: 4, borderRadius: 2 }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {isCompleted ? '100%' : `${indicator.progress}%`} - {
                      isCompleted
                        ? (indicator.status === 'completed' ? 'Completed Successfully' : 'Failed')
                        : (indicator.currentStep || 'Processing...')
                    }
                  </Typography>
                </Box>
              )}
            </Box>
          </Grid>
        </Grid>
      </Box>
    );
  }

  // Card variant (default)
  return (
    <ListItem
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

export default IndicatorItem;

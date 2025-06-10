import React from 'react';
import {
  Box,
  Typography,
  Grid,
  LinearProgress,
  Chip,
  Paper,
  Card,
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

export interface RunningKpi {
  kpiId: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
}

interface RunningKpisDisplayProps {
  runningKpis: RunningKpi[];
  variant?: 'card' | 'section';
  title?: string;
  showNavigateButton?: boolean;
  onNavigate?: () => void;
  maxDisplay?: number;
  showProgress?: boolean;
  compact?: boolean;
}

const RunningKpisDisplay: React.FC<RunningKpisDisplayProps> = ({
  runningKpis,
  variant = 'card',
  title = 'Currently Executing KPIs',
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

  const displayKpis = maxDisplay ? runningKpis.slice(0, maxDisplay) : runningKpis;

  const renderKpiItem = (kpi: RunningKpi, index: number) => {
    if (compact) {
      // Compact version for WorkerDashboardCard
      return (
        <Box
          key={kpi.kpiId}
          sx={{
            mb: 2,
            p: 1.5,
            border: 1,
            borderColor: 'success.main',
            borderRadius: 1,
            backgroundColor: 'success.light',
            color: 'success.contrastText'
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {kpi.indicator}
            </Typography>
            <Chip
              label="Running"
              size="small"
              color="success"
              sx={{ fontSize: '0.6rem', height: 16 }}
            />
          </Box>

          <Typography variant="caption" sx={{ opacity: 0.8 }}>
            Started: {formatDateTime(kpi.startTime)}
          </Typography>

          {showProgress && kpi.progress !== undefined && (
            <Box sx={{ mt: 1 }}>
              <LinearProgress
                variant="determinate"
                value={kpi.progress}
                sx={{
                  height: 4,
                  borderRadius: 2,
                  backgroundColor: 'rgba(255,255,255,0.3)',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: 'white'
                  }
                }}
              />
              <Typography variant="caption" sx={{ opacity: 0.8 }}>
                {kpi.progress}% - {kpi.currentStep}
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
          key={kpi.kpiId}
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
                    {kpi.indicator}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Owner: {kpi.owner}
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid item xs={12} md={4}>
              <Box sx={{ textAlign: { xs: 'left', md: 'right' } }}>
                <Typography variant="body2" color="text.secondary">
                  Started: {formatDateTime(kpi.startTime)}
                </Typography>
                {showProgress && kpi.progress !== undefined && (
                  <Box sx={{ mt: 1 }}>
                    <LinearProgress
                      variant="determinate"
                      value={kpi.progress}
                      sx={{ height: 4, borderRadius: 2 }}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {kpi.progress}% - {kpi.currentStep || 'Processing...'}
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
        key={kpi.kpiId}
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
          primary={kpi.indicator}
          secondary={
            <Box component="span">
              <Box component="span" sx={{ display: 'block', mb: 0.5 }}>
                Owner: {kpi.owner}
              </Box>
              <Box component="span" sx={{ display: 'block', mb: 1 }}>
                Started: {formatDistanceToNow(new Date(kpi.startTime), { addSuffix: true })}
                {kpi.elapsedTime && ` • Elapsed: ${formatElapsedTime(kpi.elapsedTime)}`}
              </Box>

              {/* Progress Bar */}
              {showProgress && kpi.progress !== undefined && (
                <Box sx={{ mt: 1 }}>
                  <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                    <Box component="span" sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>
                      {kpi.currentStep || 'Processing...'}
                    </Box>
                    <Box component="span" sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>
                      {kpi.progress}%
                    </Box>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={kpi.progress}
                    sx={{
                      height: 4,
                      borderRadius: 2,
                      backgroundColor: 'rgba(0, 0, 0, 0.1)',
                      '& .MuiLinearProgress-bar': {
                        borderRadius: 2,
                        backgroundColor: 'primary.main'
                      }
                    }}
                  />
                </Box>
              )}
            </Box>
          }
          primaryTypographyProps={{
            variant: 'subtitle2',
            sx: { fontWeight: 600, mb: 0.5 }
          }}
          secondaryTypographyProps={{
            component: 'div',
            sx: { fontSize: '0.875rem', color: 'text.secondary' }
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
        No KPIs currently running
      </Typography>
      <Typography color="text.secondary" variant="caption">
        All KPIs are idle
      </Typography>
    </Box>
  );

  if (variant === 'section') {
    return (
      <Paper sx={{ p: 3, mb: 3, bgcolor: 'success.50', border: 1, borderColor: 'success.200' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <TrendingUp sx={{ color: 'success.main' }} />
          <Typography variant="h6" color="success.main">
            {title} ({runningKpis.length})
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

        {runningKpis.length > 0 ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {displayKpis.map(renderKpiItem)}
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
                badgeContent={runningKpis.length}
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
            {runningKpis.length > 0 ? (
              <List sx={{ p: 0 }}>
                {displayKpis.map(renderKpiItem)}
              </List>
            ) : (
              renderEmptyState()
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default RunningKpisDisplay;

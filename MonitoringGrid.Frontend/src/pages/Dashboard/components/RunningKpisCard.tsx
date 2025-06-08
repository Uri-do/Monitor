import React from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Tooltip,
  useTheme,
  LinearProgress,
  Badge,
} from '@mui/material';
import { PlayCircle, PlayArrow, Timer, TrendingUp } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { formatDistanceToNow } from 'date-fns';
import { KpiDashboardDto } from '../../../types/api';

interface RealtimeRunningKpi {
  kpiId: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
}

interface RunningKpisCardProps {
  kpiDashboard?: KpiDashboardDto;
  realtimeRunningKpis?: RealtimeRunningKpi[];
}

const RunningKpisCard: React.FC<RunningKpisCardProps> = ({
  kpiDashboard,
  realtimeRunningKpis = []
}) => {
  const navigate = useNavigate();
  const theme = useTheme();

  // Use real-time data if available, otherwise fall back to dashboard data
  const runningKpis = realtimeRunningKpis.length > 0
    ? realtimeRunningKpis
    : kpiDashboard?.runningKpis || [];

  const formatElapsedTime = (elapsedTime?: number) => {
    if (!elapsedTime) return '';
    const minutes = Math.floor(elapsedTime / 60);
    const seconds = elapsedTime % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

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
                Currently Running KPIs
              </Typography>
            </Box>
            <IconButton
              size="small"
              onClick={() => navigate('/kpis')}
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
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {runningKpis.length > 0 ? (
              <List sx={{ p: 0 }}>
                {runningKpis.map(kpi => (
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
                      <Box display="flex" alignItems="center" gap={1}>
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
                        {kpi.executionDurationSeconds && (
                          <Tooltip title="Execution Duration">
                            <Chip
                              icon={<Timer sx={{ fontSize: '14px !important' }} />}
                              label={`${Math.floor(kpi.executionDurationSeconds / 60)}:${(kpi.executionDurationSeconds % 60).toString().padStart(2, '0')}`}
                              size="small"
                              variant="outlined"
                              sx={{
                                fontSize: '0.7rem',
                                height: 20,
                                color:
                                  kpi.executionDurationSeconds > 300
                                    ? 'warning.main'
                                    : 'text.secondary',
                              }}
                            />
                          </Tooltip>
                        )}
                      </Box>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                          {kpi.indicator}
                        </Typography>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                            Owner: {kpi.owner}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Context: {kpi.executionContext || 'Manual'}
                            {kpi.executionStartTime &&
                              ` • Started: ${formatDistanceToNow(new Date(kpi.executionStartTime), { addSuffix: true })}`}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
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
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default RunningKpisCard;

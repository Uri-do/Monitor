import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Tooltip,
  Alert,
  Button,
  Stack,
  LinearProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Grid,
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
  PlayArrow as PlayIcon,
  AccessTime as TimeIcon,
  TrendingUp as TrendIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  Timer as TimerIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { format, formatDistanceToNow, isAfter, isBefore, addMinutes } from 'date-fns';
import { schedulerApi, workerApi } from '@/services/api';
import { LoadingSpinner } from '@/components/UI';

interface UpcomingExecution {
  indicatorId: number;
  indicatorName: string;
  scheduledTime: string;
  schedulerName: string;
  lastExecuted?: string;
  estimatedDuration?: number;
  priority?: 'high' | 'medium' | 'low';
  status?: 'pending' | 'due' | 'overdue';
  owner?: string;
  collectorName?: string;
}

interface DueIndicator {
  indicatorId: number;
  indicatorName: string;
  schedulerName: string;
  lastExecuted?: string;
  isOverdue: boolean;
  minutesOverdue?: number;
}

interface UpcomingIndicatorsQueueProps {
  refreshInterval?: number;
  maxDisplay?: number;
  showDueIndicators?: boolean;
  showUpcomingHours?: number;
  onExecuteIndicator?: (indicatorId: number) => void;
}

const UpcomingIndicatorsQueue: React.FC<UpcomingIndicatorsQueueProps> = ({
  refreshInterval = 10000,
  maxDisplay = 20,
  showDueIndicators = true,
  showUpcomingHours = 24,
  onExecuteIndicator,
}) => {
  const [upcomingExecutions, setUpcomingExecutions] = useState<UpcomingExecution[]>([]);
  const [dueIndicators, setDueIndicators] = useState<DueIndicator[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [executing, setExecuting] = useState<Set<number>>(new Set());

  const fetchQueueData = async () => {
    try {
      setError(null);
      
      // Fetch upcoming executions - using mock data for now
      const mockUpcomingResponse = [
        {
          indicatorId: 1,
          indicatorName: 'Daily Sales Analysis',
          scheduledTime: new Date(Date.now() + 300000).toISOString(), // 5 minutes from now
          schedulerName: 'Daily Reports',
          lastExecuted: new Date(Date.now() - 86400000).toISOString(), // 1 day ago
          estimatedDuration: 120,
          priority: 'high',
          owner: 'System',
          collectorName: 'Sales Database',
        },
        {
          indicatorId: 2,
          indicatorName: 'Website Traffic Analysis',
          scheduledTime: new Date(Date.now() + 900000).toISOString(), // 15 minutes from now
          schedulerName: 'Hourly Analytics',
          lastExecuted: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
          estimatedDuration: 60,
          priority: 'medium',
          owner: 'marketing.team',
          collectorName: 'Google Analytics',
        },
        {
          indicatorId: 3,
          indicatorName: 'Customer Satisfaction Score',
          scheduledTime: new Date(Date.now() + 1800000).toISOString(), // 30 minutes from now
          schedulerName: 'Customer Metrics',
          lastExecuted: new Date(Date.now() - 7200000).toISOString(), // 2 hours ago
          estimatedDuration: 45,
          priority: 'medium',
          owner: 'support.team',
          collectorName: 'Survey API',
        },
        {
          indicatorId: 4,
          indicatorName: 'Inventory Levels Check',
          scheduledTime: new Date(Date.now() + 3600000).toISOString(), // 1 hour from now
          schedulerName: 'Inventory Monitor',
          lastExecuted: null,
          estimatedDuration: 30,
          priority: 'high',
          owner: 'System',
          collectorName: 'Inventory Database',
        },
        {
          indicatorId: 5,
          indicatorName: 'Revenue Forecast',
          scheduledTime: new Date(Date.now() + 7200000).toISOString(), // 2 hours from now
          schedulerName: 'Financial Reports',
          lastExecuted: new Date(Date.now() - 172800000).toISOString(), // 2 days ago
          estimatedDuration: 180,
          priority: 'low',
          owner: 'finance.team',
          collectorName: 'Financial Database',
        },
      ];

      const processedUpcoming = mockUpcomingResponse.map((item: any) => ({
        indicatorId: item.indicatorId || item.id,
        indicatorName: item.indicatorName || item.name,
        scheduledTime: item.scheduledTime || item.nextExecution,
        schedulerName: item.schedulerName || item.scheduler,
        lastExecuted: item.lastExecuted,
        estimatedDuration: item.estimatedDuration,
        priority: item.priority || 'medium',
        status: getExecutionStatus(item.scheduledTime || item.nextExecution),
        owner: item.owner,
        collectorName: item.collectorName,
      }));
      setUpcomingExecutions(processedUpcoming.slice(0, maxDisplay));

      // Fetch due indicators if enabled - using mock data for now
      if (showDueIndicators) {
        const mockDueResponse = [
          {
            indicatorId: 6,
            indicatorName: 'System Health Check',
            schedulerName: 'System Monitor',
            lastExecuted: new Date(Date.now() - 1800000).toISOString(), // 30 minutes ago
            isOverdue: true,
            minutesOverdue: 15,
          },
          {
            indicatorId: 7,
            indicatorName: 'Database Performance',
            schedulerName: 'DB Monitor',
            lastExecuted: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
            isOverdue: true,
            minutesOverdue: 45,
          },
          {
            indicatorId: 8,
            indicatorName: 'API Response Times',
            schedulerName: 'API Monitor',
            lastExecuted: null,
            isOverdue: false,
            minutesOverdue: 0,
          },
        ];

        const processedDue = mockDueResponse.map((item: any) => ({
          indicatorId: item.indicatorId || item.id,
          indicatorName: item.indicatorName || item.name,
          schedulerName: item.schedulerName || item.scheduler,
          lastExecuted: item.lastExecuted,
          isOverdue: item.isOverdue || false,
          minutesOverdue: item.minutesOverdue,
        }));
        setDueIndicators(processedDue);
      }

    } catch (err: any) {
      console.error('Error fetching queue data:', err);
      setError(err.message || 'Failed to fetch queue data');
    } finally {
      setLoading(false);
    }
  };

  const getExecutionStatus = (scheduledTime: string): 'pending' | 'due' | 'overdue' => {
    if (!scheduledTime) return 'pending';
    
    const scheduled = new Date(scheduledTime);
    const now = new Date();
    const dueThreshold = addMinutes(scheduled, -5); // 5 minutes before scheduled time
    
    if (isAfter(now, scheduled)) {
      return 'overdue';
    } else if (isAfter(now, dueThreshold)) {
      return 'due';
    } else {
      return 'pending';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'due': return 'warning';
      case 'overdue': return 'error';
      case 'pending': return 'info';
      default: return 'default';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high': return 'error';
      case 'medium': return 'warning';
      case 'low': return 'success';
      default: return 'default';
    }
  };

  const handleExecuteIndicator = async (indicatorId: number) => {
    if (executing.has(indicatorId)) return;
    
    setExecuting(prev => new Set(prev).add(indicatorId));
    
    try {
      if (onExecuteIndicator) {
        onExecuteIndicator(indicatorId);
      } else {
        await workerApi.executeIndicator(indicatorId);
      }
      
      // Refresh the queue after execution
      setTimeout(fetchQueueData, 2000);
    } catch (err: any) {
      console.error('Error executing indicator:', err);
    } finally {
      setExecuting(prev => {
        const newSet = new Set(prev);
        newSet.delete(indicatorId);
        return newSet;
      });
    }
  };

  useEffect(() => {
    fetchQueueData();
    
    const interval = setInterval(fetchQueueData, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshInterval, maxDisplay, showDueIndicators, showUpcomingHours]);

  if (loading) {
    return <LoadingSpinner message="Loading indicator queue..." />;
  }

  if (error) {
    return (
      <Alert severity="error" action={
        <Button onClick={fetchQueueData} startIcon={<RefreshIcon />}>
          Retry
        </Button>
      }>
        {error}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Next Up - Immediate Upcoming Indicators */}
      {upcomingExecutions.length > 0 && (
        <Card sx={{ mb: 3, border: 2, borderColor: 'primary.main' }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
              <TimerIcon color="primary" />
              <Typography variant="h6" color="primary">
                Next Up - Immediate Executions
              </Typography>
              <Chip
                label={`${upcomingExecutions.slice(0, 3).length} indicators`}
                size="small"
                color="primary"
              />
            </Stack>

            <Grid container spacing={2}>
              {upcomingExecutions.slice(0, 3).map((execution, index) => (
                <Grid item xs={12} md={4} key={execution.indicatorId}>
                  <Paper
                    sx={{
                      p: 2,
                      border: 1,
                      borderColor: index === 0 ? 'success.main' : 'grey.300',
                      backgroundColor: index === 0 ? 'success.50' : 'background.paper'
                    }}
                  >
                    <Stack spacing={1}>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        <Typography variant="subtitle2" fontWeight="bold">
                          #{index + 1}
                        </Typography>
                        <Chip
                          label={execution.priority}
                          color={getPriorityColor(execution.priority || 'medium') as any}
                          size="small"
                        />
                        {index === 0 && (
                          <Chip
                            label="NEXT"
                            color="success"
                            size="small"
                            sx={{ fontSize: '0.7rem', height: 18 }}
                          />
                        )}
                      </Stack>

                      <Typography variant="body2" fontWeight="medium">
                        {execution.indicatorName}
                      </Typography>

                      <Typography variant="caption" color="text.secondary">
                        {execution.collectorName} • {execution.schedulerName}
                      </Typography>

                      <Stack direction="row" alignItems="center" spacing={1}>
                        <TimeIcon fontSize="small" />
                        <Typography variant="body2" fontWeight="medium">
                          {format(new Date(execution.scheduledTime), 'HH:mm')}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          ({formatDistanceToNow(new Date(execution.scheduledTime), { addSuffix: true })})
                        </Typography>
                      </Stack>

                      {execution.estimatedDuration && (
                        <Stack direction="row" alignItems="center" spacing={1}>
                          <TimerIcon fontSize="small" />
                          <Typography variant="caption" color="text.secondary">
                            Est. {execution.estimatedDuration}s duration
                          </Typography>
                        </Stack>
                      )}

                      <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                        <Button
                          size="small"
                          variant={index === 0 ? "contained" : "outlined"}
                          startIcon={<PlayIcon />}
                          onClick={() => handleExecuteIndicator(execution.indicatorId)}
                          disabled={executing.has(execution.indicatorId)}
                          color={index === 0 ? "success" : "primary"}
                        >
                          {executing.has(execution.indicatorId) ? 'Running...' : 'Execute Now'}
                        </Button>
                      </Stack>
                    </Stack>
                  </Paper>
                </Grid>
              ))}
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Due Indicators (Critical) */}
      {showDueIndicators && dueIndicators.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
              <WarningIcon color="error" />
              <Typography variant="h6" color="error">
                Due Indicators ({dueIndicators.length})
              </Typography>
            </Stack>
            
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Indicator</TableCell>
                    <TableCell>Scheduler</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Last Executed</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {dueIndicators.map((indicator) => (
                    <TableRow key={indicator.indicatorId}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {indicator.indicatorName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {indicator.schedulerName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={indicator.isOverdue ? 'Overdue' : 'Due'}
                          color={indicator.isOverdue ? 'error' : 'warning'}
                          size="small"
                          icon={indicator.isOverdue ? <ErrorIcon /> : <WarningIcon />}
                        />
                        {indicator.minutesOverdue && (
                          <Typography variant="caption" color="error" display="block">
                            {indicator.minutesOverdue}m overdue
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {indicator.lastExecuted 
                            ? formatDistanceToNow(new Date(indicator.lastExecuted), { addSuffix: true })
                            : 'Never'
                          }
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Tooltip title="Execute Now">
                          <IconButton
                            size="small"
                            onClick={() => handleExecuteIndicator(indicator.indicatorId)}
                            disabled={executing.has(indicator.indicatorId)}
                            color="primary"
                          >
                            {executing.has(indicator.indicatorId) ? (
                              <LinearProgress size={20} />
                            ) : (
                              <PlayIcon />
                            )}
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {/* Upcoming Executions */}
      <Accordion defaultExpanded>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <ScheduleIcon />
            <Typography variant="h6">
              Upcoming Executions ({upcomingExecutions.length})
            </Typography>
          </Stack>
        </AccordionSummary>
        <AccordionDetails>
          {upcomingExecutions.length === 0 ? (
            <Alert severity="info">
              No upcoming executions scheduled for the next {showUpcomingHours} hours
            </Alert>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Indicator</TableCell>
                    <TableCell>Scheduled Time</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Priority</TableCell>
                    <TableCell>Owner</TableCell>
                    <TableCell>Last Executed</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {upcomingExecutions.map((execution) => (
                    <TableRow key={execution.indicatorId}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {execution.indicatorName}
                        </Typography>
                        {execution.collectorName && (
                          <Typography variant="caption" color="text.secondary" display="block">
                            {execution.collectorName}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {format(new Date(execution.scheduledTime), 'MMM dd, HH:mm')}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" display="block">
                          {formatDistanceToNow(new Date(execution.scheduledTime), { addSuffix: true })}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={execution.status}
                          color={getStatusColor(execution.status || 'pending') as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={execution.priority}
                          color={getPriorityColor(execution.priority || 'medium') as any}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {execution.owner || 'N/A'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {execution.lastExecuted 
                            ? formatDistanceToNow(new Date(execution.lastExecuted), { addSuffix: true })
                            : 'Never'
                          }
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Tooltip title="Execute Now">
                          <IconButton
                            size="small"
                            onClick={() => handleExecuteIndicator(execution.indicatorId)}
                            disabled={executing.has(execution.indicatorId)}
                            color="primary"
                          >
                            {executing.has(execution.indicatorId) ? (
                              <LinearProgress size={20} />
                            ) : (
                              <PlayIcon />
                            )}
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </AccordionDetails>
      </Accordion>
    </Box>
  );
};

export default UpcomingIndicatorsQueue;

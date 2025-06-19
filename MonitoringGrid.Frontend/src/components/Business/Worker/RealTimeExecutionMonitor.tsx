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
  LinearProgress,
  Alert,
  Stack,
  IconButton,
  Tooltip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Grid,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Schedule as ScheduleIcon,
  Timer as TimerIcon,
  TrendingUp as TrendIcon,
  ExpandMore as ExpandMoreIcon,
  Visibility as ViewIcon,
  Stop as StopIcon,
  Speed as SpeedIcon,
  DataUsage as DataIcon,
  Timeline as TimelineIcon,
  Refresh as RefreshIcon,
  Launch as LaunchIcon,
  Assessment as AssessmentIcon,
  Memory as MemoryIcon,
  Storage as StorageIcon,
} from '@mui/icons-material';
import { format, formatDistanceToNow } from 'date-fns';
import { useRealtime } from '@/contexts/RealtimeContext';
import { RunningIndicator } from '../Indicator/RunningIndicatorsDisplay';
import { signalRService } from '@/services/signalRService';
import { workerApi } from '@/services/api';

interface ExecutionResult {
  indicatorId: number;
  indicatorName: string;
  startTime: string;
  endTime?: string;
  duration?: number;
  status: 'running' | 'completed' | 'failed' | 'cancelled';
  result?: any;
  errorMessage?: string;
  executedBy?: string;
  currentValue?: number;
  previousValue?: number;
  deviationPercent?: number;
  executionContext?: string;
  recordCount?: number;
  memoryUsage?: number;
  cpuUsage?: number;
  thresholdBreached?: boolean;
  alertTriggered?: boolean;
  rawData?: any[];
  metadata?: any;
}

interface RealTimeExecutionMonitorProps {
  maxResults?: number;
  showRunningOnly?: boolean;
  showCompletedResults?: boolean;
  autoRefresh?: boolean;
  onViewDetails?: (indicatorId: number) => void;
}

const RealTimeExecutionMonitor: React.FC<RealTimeExecutionMonitorProps> = ({
  maxResults = 50,
  showRunningOnly = false,
  showCompletedResults = true,
  autoRefresh = true,
  onViewDetails,
}) => {
  const [runningIndicators, setRunningIndicators] = useState<RunningIndicator[]>([]);
  const [recentResults, setRecentResults] = useState<ExecutionResult[]>([]);
  const [executionStats, setExecutionStats] = useState({
    totalToday: 0,
    successfulToday: 0,
    failedToday: 0,
    averageDuration: 0,
    currentlyRunning: 0,
  });

  const { isConnected } = useRealtime();

  // Fetch initial execution history from API
  useEffect(() => {
    const fetchInitialData = async () => {
      try {
        const status = await workerApi.getStatus();
        if (status.executionHistory && status.executionHistory.length > 0) {
          const apiResults: ExecutionResult[] = status.executionHistory.map((item: any) => ({
            indicatorId: item.indicatorId,
            indicatorName: item.indicatorName,
            startTime: item.executedAt,
            endTime: item.executedAt,
            duration: Math.round(item.durationMs / 1000),
            status: item.success ? 'completed' : 'failed',
            errorMessage: item.errorMessage,
            executedBy: 'System',
            executionContext: item.context || 'Scheduled',
          }));

          setRecentResults(apiResults.slice(0, maxResults));

          // Calculate stats from API data
          const successful = apiResults.filter(r => r.status === 'completed').length;
          const failed = apiResults.filter(r => r.status === 'failed').length;
          const avgDuration = apiResults.length > 0
            ? Math.round(apiResults.reduce((sum, r) => sum + (r.duration || 0), 0) / apiResults.length)
            : 0;

          setExecutionStats(prev => ({
            ...prev,
            totalToday: apiResults.length,
            successfulToday: successful,
            failedToday: failed,
            averageDuration: avgDuration,
          }));
        }
      } catch (error) {
        console.error('Failed to fetch initial execution history:', error);
      }
    };

    fetchInitialData();
  }, [maxResults]);

  // Mock data for demonstration - in real implementation, this would come from SignalR
  useEffect(() => {
    if (!autoRefresh || isConnected) return; // Only use mock data when SignalR is not connected

    const mockRunningIndicators: RunningIndicator[] = [
      {
        indicatorID: 1,
        indicator: 'Daily Sales Report',
        collectorID: 1,
        collectorItemName: 'Sales Database',
        startTime: new Date(Date.now() - 30000).toISOString(),
        lastMinutes: 1440,
        status: 'running',
        progress: 65,
        estimatedTimeRemaining: 45,
        executionContext: 'Scheduled execution',
      },
      {
        indicatorID: 2,
        indicator: 'Customer Satisfaction Score',
        collectorID: 2,
        collectorItemName: 'Survey API',
        startTime: new Date(Date.now() - 15000).toISOString(),
        lastMinutes: 60,
        status: 'running',
        progress: 30,
        estimatedTimeRemaining: 90,
        executionContext: 'Manual execution',
      },
    ];

    const mockRecentResults: ExecutionResult[] = [
      {
        indicatorId: 3,
        indicatorName: 'Website Traffic Analysis',
        startTime: new Date(Date.now() - 300000).toISOString(),
        endTime: new Date(Date.now() - 240000).toISOString(),
        duration: 60,
        status: 'completed',
        executedBy: 'System',
        currentValue: 15420,
        previousValue: 14800,
        deviationPercent: 4.2,
        executionContext: 'Scheduled',
        recordCount: 1247,
        memoryUsage: 45.2,
        cpuUsage: 12.8,
        thresholdBreached: false,
        alertTriggered: false,
        rawData: [{ total: 15420, marked: 1247 }],
      },
      {
        indicatorId: 4,
        indicatorName: 'Inventory Levels Check',
        startTime: new Date(Date.now() - 600000).toISOString(),
        endTime: new Date(Date.now() - 580000).toISOString(),
        duration: 20,
        status: 'failed',
        executedBy: 'System',
        errorMessage: 'Database connection timeout after 30 seconds',
        executionContext: 'Scheduled',
        memoryUsage: 23.1,
        cpuUsage: 8.4,
        thresholdBreached: false,
        alertTriggered: true,
      },
      {
        indicatorId: 5,
        indicatorName: 'Revenue Forecast',
        startTime: new Date(Date.now() - 900000).toISOString(),
        endTime: new Date(Date.now() - 870000).toISOString(),
        duration: 30,
        status: 'completed',
        executedBy: 'john.doe',
        currentValue: 125000,
        previousValue: 120000,
        deviationPercent: 4.2,
        executionContext: 'Manual',
        recordCount: 892,
        memoryUsage: 67.8,
        cpuUsage: 15.3,
        thresholdBreached: true,
        alertTriggered: true,
        rawData: [{ total: 125000, marked: 892 }],
      },
      {
        indicatorId: 6,
        indicatorName: 'Customer Satisfaction Score',
        startTime: new Date(Date.now() - 1200000).toISOString(),
        endTime: new Date(Date.now() - 1170000).toISOString(),
        duration: 30,
        status: 'completed',
        executedBy: 'System',
        currentValue: 4.7,
        previousValue: 4.5,
        deviationPercent: 4.4,
        executionContext: 'Scheduled',
        recordCount: 2156,
        memoryUsage: 34.5,
        cpuUsage: 9.2,
        thresholdBreached: false,
        alertTriggered: false,
        rawData: [{ total: 4.7, marked: 2156 }],
      },
    ];

    setRunningIndicators(mockRunningIndicators);
    setRecentResults(mockRecentResults);
    setExecutionStats({
      totalToday: 47,
      successfulToday: 42,
      failedToday: 5,
      averageDuration: 45,
      currentlyRunning: mockRunningIndicators.length,
    });

    // Simulate real-time updates - disabled to prevent flickering
    // const interval = setInterval(() => {
    //   setRunningIndicators(prev =>
    //     prev.map(indicator => ({
    //       ...indicator,
    //       progress: Math.min(indicator.progress + Math.random() * 10, 100),
    //       estimatedTimeRemaining: Math.max(indicator.estimatedTimeRemaining - 5, 0),
    //     }))
    //   );
    // }, 5000);

    // return () => clearInterval(interval);
  }, [autoRefresh, isConnected]);

  // SignalR event handlers for real-time updates
  useEffect(() => {
    if (!isConnected) return;

    const handleIndicatorExecutionStarted = (data: any) => {
      console.log('🚀 RealTimeExecutionMonitor: Indicator execution started:', data);

      // Add to running indicators
      const newRunningIndicator: RunningIndicator = {
        indicatorID: data.indicatorID,
        indicator: data.indicatorName || data.indicator,
        collectorID: data.collectorID,
        collectorItemName: data.collectorItemName,
        startTime: data.startTime,
        lastMinutes: data.lastMinutes,
        status: 'running',
        progress: 0,
        estimatedTimeRemaining: 60, // Default estimate
        executionContext: data.executionContext || 'Scheduled',
      };

      setRunningIndicators(prev => {
        // Remove any existing execution for this indicator
        const filtered = prev.filter(ind => ind.indicatorID !== data.indicatorID);
        return [...filtered, newRunningIndicator];
      });

      // Update stats
      setExecutionStats(prev => ({
        ...prev,
        currentlyRunning: prev.currentlyRunning + 1,
      }));
    };

    const handleIndicatorExecutionProgress = (data: any) => {
      console.log('📊 RealTimeExecutionMonitor: Indicator execution progress:', data);

      setRunningIndicators(prev =>
        prev.map(indicator =>
          indicator.indicatorID === data.indicatorId
            ? {
                ...indicator,
                progress: data.progressPercentage || 0,
                estimatedTimeRemaining: Math.max(60 - (data.progressPercentage || 0), 0),
              }
            : indicator
        )
      );
    };

    const handleIndicatorExecutionCompleted = (data: any) => {
      console.log('✅ RealTimeExecutionMonitor: Indicator execution completed:', data);

      // Remove from running indicators
      setRunningIndicators(prev => prev.filter(ind => ind.indicatorID !== data.indicatorId));

      // Add to recent results
      const newResult: ExecutionResult = {
        indicatorId: data.indicatorId,
        indicatorName: data.indicatorName,
        startTime: data.startTime,
        endTime: new Date().toISOString(),
        duration: Math.round(data.duration / 1000), // Convert ms to seconds
        status: data.success ? 'completed' : 'failed',
        errorMessage: data.errorMessage,
        executedBy: 'System',
        currentValue: data.value,
        executionContext: data.executionContext || 'Scheduled',
        recordCount: data.recordCount,
        thresholdBreached: data.thresholdBreached,
        alertTriggered: data.alertTriggered,
      };

      setRecentResults(prev => {
        const updated = [newResult, ...prev];
        return updated.slice(0, maxResults); // Keep only the most recent results
      });

      // Update stats
      setExecutionStats(prev => ({
        ...prev,
        currentlyRunning: Math.max(prev.currentlyRunning - 1, 0),
        totalToday: prev.totalToday + 1,
        successfulToday: data.success ? prev.successfulToday + 1 : prev.successfulToday,
        failedToday: data.success ? prev.failedToday : prev.failedToday + 1,
        averageDuration: Math.round((prev.averageDuration * (prev.totalToday - 1) + Math.round(data.duration / 1000)) / prev.totalToday),
      }));
    };

    // Register event handlers - use the "on" prefix to match the event handler names
    signalRService.on('onIndicatorExecutionStarted', handleIndicatorExecutionStarted);
    signalRService.on('onIndicatorExecutionProgress', handleIndicatorExecutionProgress);
    signalRService.on('onIndicatorExecutionCompleted', handleIndicatorExecutionCompleted);

    return () => {
      // Cleanup event handlers - use the "on" prefix to match registration
      signalRService.off('onIndicatorExecutionStarted');
      signalRService.off('onIndicatorExecutionProgress');
      signalRService.off('onIndicatorExecutionCompleted');
    };
  }, [isConnected, maxResults]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'running': return 'info';
      case 'completed': return 'success';
      case 'failed': return 'error';
      case 'cancelled': return 'warning';
      default: return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running': return <PlayIcon />;
      case 'completed': return <SuccessIcon />;
      case 'failed': return <ErrorIcon />;
      case 'cancelled': return <StopIcon />;
      default: return <ScheduleIcon />;
    }
  };

  const formatDuration = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}m ${remainingSeconds}s`;
  };

  return (
    <Box>
      {/* Connection Status */}
      {!isConnected && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Real-time connection is not available. Data may not be up to date.
        </Alert>
      )}

      {/* Execution Statistics */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="primary">
                {executionStats.currentlyRunning}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Currently Running
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="success.main">
                {executionStats.successfulToday}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Successful Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="error.main">
                {executionStats.failedToday}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Failed Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="info.main">
                {executionStats.averageDuration}s
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Avg Duration
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Currently Running Indicators */}
      {runningIndicators.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
              <PlayIcon color="info" />
              <Typography variant="h6">
                Currently Executing ({runningIndicators.length})
              </Typography>
            </Stack>
            
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Indicator</TableCell>
                    <TableCell>Started</TableCell>
                    <TableCell>Progress</TableCell>
                    <TableCell>ETA</TableCell>
                    <TableCell>Context</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {runningIndicators.map((indicator) => (
                    <TableRow key={indicator.indicatorID}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {indicator.indicator}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" display="block">
                          {indicator.collectorItemName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {formatDistanceToNow(new Date(indicator.startTime), { addSuffix: true })}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Box sx={{ width: 100 }}>
                          <LinearProgress 
                            variant="determinate" 
                            value={indicator.progress} 
                            sx={{ mb: 0.5 }}
                          />
                          <Typography variant="caption" color="text.secondary">
                            {indicator.progress.toFixed(0)}%
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {indicator.estimatedTimeRemaining > 0 
                            ? `${indicator.estimatedTimeRemaining}s`
                            : 'Finishing...'
                          }
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {indicator.executionContext}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Tooltip title="View Details">
                          <IconButton
                            size="small"
                            onClick={() => onViewDetails?.(indicator.indicatorID)}
                          >
                            <ViewIcon />
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

      {/* Recent Execution Results */}
      {showCompletedResults && (
        <Accordion defaultExpanded={!showRunningOnly}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Stack direction="row" alignItems="center" spacing={1}>
              <TrendIcon />
              <Typography variant="h6">
                Recent Execution Results ({recentResults.length})
              </Typography>
            </Stack>
          </AccordionSummary>
          <AccordionDetails>
            {recentResults.length === 0 ? (
              <Alert severity="info">
                No recent execution results available
              </Alert>
            ) : (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Indicator</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Duration</TableCell>
                      <TableCell>Result & Performance</TableCell>
                      <TableCell>Context</TableCell>
                      <TableCell>Completed</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {recentResults.map((result) => (
                      <TableRow key={result.indicatorId}>
                        <TableCell>
                          <Typography variant="body2" fontWeight="medium">
                            {result.indicatorName}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip 
                            label={result.status}
                            color={getStatusColor(result.status) as any}
                            size="small"
                            icon={getStatusIcon(result.status)}
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">
                            {result.duration ? formatDuration(result.duration) : 'N/A'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          {result.status === 'completed' && result.currentValue !== undefined ? (
                            <Box>
                              <Typography variant="body2" fontWeight="medium">
                                {result.currentValue.toLocaleString()}
                              </Typography>
                              {result.deviationPercent !== undefined && (
                                <Typography
                                  variant="caption"
                                  color={result.deviationPercent > 0 ? 'success.main' : 'error.main'}
                                  display="block"
                                >
                                  {result.deviationPercent > 0 ? '+' : ''}{result.deviationPercent.toFixed(1)}%
                                </Typography>
                              )}
                              <Stack direction="row" spacing={1} sx={{ mt: 0.5 }}>
                                {result.recordCount && (
                                  <Chip
                                    label={`${result.recordCount} records`}
                                    size="small"
                                    variant="outlined"
                                    icon={<DataIcon />}
                                  />
                                )}
                                {result.memoryUsage && (
                                  <Chip
                                    label={`${result.memoryUsage.toFixed(1)}MB`}
                                    size="small"
                                    variant="outlined"
                                    icon={<MemoryIcon />}
                                  />
                                )}
                                {result.thresholdBreached && (
                                  <Chip
                                    label="Threshold Breached"
                                    size="small"
                                    color="warning"
                                  />
                                )}
                                {result.alertTriggered && (
                                  <Chip
                                    label="Alert Sent"
                                    size="small"
                                    color="error"
                                  />
                                )}
                              </Stack>
                            </Box>
                          ) : result.status === 'failed' ? (
                            <Box>
                              <Typography variant="body2" color="error" fontWeight="medium">
                                Execution Failed
                              </Typography>
                              <Typography variant="caption" color="error" display="block">
                                {result.errorMessage || 'Unknown error'}
                              </Typography>
                              <Stack direction="row" spacing={1} sx={{ mt: 0.5 }}>
                                {result.memoryUsage && (
                                  <Chip
                                    label={`${result.memoryUsage.toFixed(1)}MB`}
                                    size="small"
                                    variant="outlined"
                                    icon={<MemoryIcon />}
                                  />
                                )}
                                {result.alertTriggered && (
                                  <Chip
                                    label="Alert Sent"
                                    size="small"
                                    color="error"
                                  />
                                )}
                              </Stack>
                            </Box>
                          ) : (
                            <Typography variant="body2" color="text.secondary">
                              N/A
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontWeight="medium">
                            {result.executionContext || 'Unknown'}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" display="block">
                            by {result.executedBy || 'System'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">
                            {result.endTime 
                              ? formatDistanceToNow(new Date(result.endTime), { addSuffix: true })
                              : 'N/A'
                            }
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Tooltip title="View Details">
                            <IconButton
                              size="small"
                              onClick={() => onViewDetails?.(result.indicatorId)}
                            >
                              <ViewIcon />
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
      )}
    </Box>
  );
};

export default RealTimeExecutionMonitor;

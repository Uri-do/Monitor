import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Stack,
  Chip,
  LinearProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  IconButton,
  Tooltip,
  Badge,
  Divider,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Timer as TimerIcon,
  Speed as SpeedIcon,
  Memory as MemoryIcon,
  Storage as StorageIcon,
  Timeline as TimelineIcon,
  TrendingUp as TrendIcon,
  ExpandMore as ExpandMoreIcon,
  Visibility as ViewIcon,
  DataUsage as DataIcon,
  Assessment as AssessmentIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { format, formatDistanceToNow } from 'date-fns';
import { useRealtime } from '@/contexts/RealtimeContext';
import { signalRService } from '@/services/signalRService';
import { workerApi } from '@/services/api';

interface ProcessingIndicator {
  indicatorId: number;
  indicatorName: string;
  startTime: string;
  progress: number;
  currentStep: string;
  estimatedTimeRemaining: number;
  memoryUsage: number;
  cpuUsage: number;
  recordsProcessed: number;
  totalRecords: number;
  executionContext: string;
  owner: string;
  collectorName: string;
}

interface CompletedExecution {
  indicatorId: number;
  indicatorName: string;
  startTime: string;
  endTime: string;
  duration: number;
  status: 'success' | 'failed';
  result?: number;
  errorMessage?: string;
  recordCount: number;
  memoryUsage: number;
  cpuUsage: number;
  thresholdBreached: boolean;
  alertTriggered: boolean;
  executionContext: string;
}

interface ProcessingStats {
  totalProcessing: number;
  averageProgress: number;
  totalMemoryUsage: number;
  totalCpuUsage: number;
  estimatedCompletionTime: string;
  queueLength: number;
}

interface RealTimeProcessingMonitorProps {
  refreshInterval?: number;
  maxCompletedResults?: number;
  showDetailedMetrics?: boolean;
}

const RealTimeProcessingMonitor: React.FC<RealTimeProcessingMonitorProps> = ({
  refreshInterval = 2000,
  maxCompletedResults = 10,
  showDetailedMetrics = true,
}) => {
  const [processingIndicators, setProcessingIndicators] = useState<ProcessingIndicator[]>([]);
  const [completedExecutions, setCompletedExecutions] = useState<CompletedExecution[]>([]);
  const [processingStats, setProcessingStats] = useState<ProcessingStats>({
    totalProcessing: 0,
    averageProgress: 0,
    totalMemoryUsage: 0,
    totalCpuUsage: 0,
    estimatedCompletionTime: '',
    queueLength: 0,
  });

  const { isConnected } = useRealtime();

  // Fetch real execution history from worker status
  const fetchExecutionHistory = async () => {
    try {
      const workerStatus = await workerApi.getStatus(true, true, true); // Include history
      if (workerStatus?.history && Array.isArray(workerStatus.history)) {
        const historyItems: CompletedExecution[] = workerStatus.history.map((item: any) => ({
          indicatorId: item.indicatorId,
          indicatorName: item.indicatorName,
          startTime: new Date(item.executedAt).toISOString(),
          endTime: new Date(item.executedAt).toISOString(),
          duration: Math.round(item.durationMs / 1000), // Convert to seconds
          status: item.success ? 'success' : 'failed',
          result: item.success ? 'Completed' : undefined,
          recordCount: 0, // Not available in history
          memoryUsage: 0, // Not available in history
          cpuUsage: 0, // Not available in history
          thresholdBreached: false, // Not available in history
          alertTriggered: false, // Not available in history
          executionContext: item.context || 'Unknown',
          errorMessage: item.errorMessage,
        }));

        setCompletedExecutions(historyItems.slice(0, maxCompletedResults));
      }
    } catch (error) {
      console.error('Failed to fetch execution history:', error);
    }
  };

  // Initialize state and fetch real execution history
  useEffect(() => {
    // Initialize with empty arrays
    setProcessingIndicators([]);
    setCompletedExecutions([]);

    // Initialize empty processing stats
    setProcessingStats({
      totalProcessing: 0,
      averageProgress: 0,
      totalMemoryUsage: 0,
      totalCpuUsage: 0,
      estimatedCompletionTime: '',
      queueLength: 0,
    });

    // Fetch real execution history
    fetchExecutionHistory();
  }, [maxCompletedResults]);

  // SignalR event handlers for real-time processing updates
  useEffect(() => {
    if (!isConnected) return;

    // Handle indicator execution started
    const handleIndicatorExecutionStarted = (data: any) => {
      console.log('🚀 Processing Monitor - Indicator execution started:', data);
      const newIndicator: ProcessingIndicator = {
        indicatorId: data.indicatorID || data.indicatorId,
        indicatorName: data.indicatorName,
        startTime: data.startTime,
        progress: 0,
        currentStep: 'Starting...',
        estimatedTimeRemaining: data.estimatedDuration || 0,
        memoryUsage: 0,
        cpuUsage: 0,
        recordsProcessed: 0,
        totalRecords: 0,
        executionContext: data.executionContext || 'Unknown',
        owner: data.owner || 'System',
        collectorName: 'Processing...',
      };

      setProcessingIndicators(prev => {
        const filtered = prev.filter(ind => ind.indicatorId !== newIndicator.indicatorId);
        return [newIndicator, ...filtered];
      });
    };

    // Handle indicator execution progress
    const handleIndicatorExecutionProgress = (data: any) => {
      console.log('📊 Processing Monitor - Indicator execution progress:', data);
      setProcessingIndicators(prev =>
        prev.map(indicator =>
          indicator.indicatorId === (data.indicatorID || data.indicatorId)
            ? {
                ...indicator,
                progress: data.progress || 0,
                currentStep: data.currentStep || indicator.currentStep,
                estimatedTimeRemaining: data.estimatedTimeRemaining || indicator.estimatedTimeRemaining,
              }
            : indicator
        )
      );
    };

    // Handle indicator execution completed
    const handleIndicatorExecutionCompleted = (data: any) => {
      console.log('✅ Processing Monitor - Indicator execution completed:', data);

      // Remove from processing indicators
      setProcessingIndicators(prev =>
        prev.filter(indicator => indicator.indicatorId !== (data.indicatorId || data.indicatorID))
      );

      // Add to completed executions
      const completedExecution: CompletedExecution = {
        indicatorId: data.indicatorId || data.indicatorID,
        indicatorName: data.indicatorName,
        startTime: data.startTime || new Date().toISOString(),
        endTime: data.completedAt || new Date().toISOString(),
        duration: data.duration || 0,
        status: data.success ? 'success' : 'failed',
        result: data.value,
        recordCount: 0,
        memoryUsage: 0,
        cpuUsage: 0,
        thresholdBreached: data.thresholdBreached || false,
        alertTriggered: data.thresholdBreached || false,
        executionContext: data.executionContext || 'Unknown',
        errorMessage: data.errorMessage,
      };

      setCompletedExecutions(prev => {
        const updated = [completedExecution, ...prev];
        return updated.slice(0, maxCompletedResults); // Keep only the most recent results
      });

      // Don't refresh execution history from API when using SignalR - we already have the data
    };

    // Register event handlers
    signalRService.on('onIndicatorExecutionStarted', handleIndicatorExecutionStarted);
    signalRService.on('onIndicatorExecutionProgress', handleIndicatorExecutionProgress);
    signalRService.on('onIndicatorExecutionCompleted', handleIndicatorExecutionCompleted);

    return () => {
      // Cleanup event handlers
      signalRService.off('onIndicatorExecutionStarted');
      signalRService.off('onIndicatorExecutionProgress');
      signalRService.off('onIndicatorExecutionCompleted');
    };
  }, [isConnected, maxCompletedResults]);

  // Update processing stats when processing indicators change
  useEffect(() => {
    const totalMemory = processingIndicators.reduce((sum, ind) => sum + ind.memoryUsage, 0);
    const totalCpu = processingIndicators.reduce((sum, ind) => sum + ind.cpuUsage, 0);
    const avgProgress = processingIndicators.length > 0
      ? processingIndicators.reduce((sum, ind) => sum + ind.progress, 0) / processingIndicators.length
      : 0;

    setProcessingStats({
      totalProcessing: processingIndicators.length,
      averageProgress: avgProgress,
      totalMemoryUsage: totalMemory,
      totalCpuUsage: totalCpu,
      estimatedCompletionTime: processingIndicators.length > 0 ? 'Calculating...' : '',
      queueLength: 0, // This would come from worker status if available
    });
  }, [processingIndicators]);

  // Periodic refresh of execution history - only when SignalR is not connected
  useEffect(() => {
    if (isConnected) return; // Don't poll when SignalR is connected

    // Only poll if refreshInterval is reasonable (> 60 seconds for execution history)
    if (!refreshInterval || refreshInterval < 60000) return;

    const interval = setInterval(() => {
      fetchExecutionHistory();
    }, Math.max(refreshInterval * 5, 120000)); // Refresh history much less frequently, minimum 2 minutes

    return () => clearInterval(interval);
  }, [refreshInterval, isConnected]);

  const formatBytes = (bytes: number) => {
    return `${bytes.toFixed(1)} MB`;
  };

  const formatPercentage = (value: number) => {
    return `${value.toFixed(1)}%`;
  };

  return (
    <Box>
      {/* Connection Status */}
      {!isConnected && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Real-time connection is not available. Processing data may not be up to date.
        </Alert>
      )}

      {/* Processing Overview Stats */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="primary">
                {processingStats.totalProcessing}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Currently Processing
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="info.main">
                {formatPercentage(processingStats.averageProgress)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Average Progress
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="warning.main">
                {formatBytes(processingStats.totalMemoryUsage)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total Memory Usage
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" color="success.main">
                {processingStats.queueLength}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Queue Length
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Currently Processing Indicators */}
      {processingIndicators.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
              <PlayIcon color="primary" />
              <Typography variant="h6">
                Currently Processing ({processingIndicators.length})
              </Typography>
              <Chip 
                label="LIVE" 
                size="small" 
                color="success" 
                sx={{ fontSize: '0.7rem', height: 18 }}
              />
            </Stack>
            
            <Stack spacing={2}>
              {processingIndicators.map((indicator) => (
                <Paper key={indicator.indicatorId} sx={{ p: 2, border: 1, borderColor: 'primary.light' }}>
                  <Grid container spacing={2} alignItems="center">
                    <Grid item xs={12} md={4}>
                      <Typography variant="subtitle1" fontWeight="medium">
                        {indicator.indicatorName}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {indicator.collectorName} • {indicator.executionContext}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Started {formatDistanceToNow(new Date(indicator.startTime), { addSuffix: true })}
                      </Typography>
                    </Grid>
                    
                    <Grid item xs={12} md={3}>
                      <Typography variant="body2" fontWeight="medium" sx={{ mb: 0.5 }}>
                        {indicator.currentStep}
                      </Typography>
                      <LinearProgress 
                        variant="determinate" 
                        value={indicator.progress} 
                        sx={{ mb: 0.5 }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        {indicator.progress.toFixed(0)}% • ETA: {indicator.estimatedTimeRemaining}s
                      </Typography>
                    </Grid>
                    
                    <Grid item xs={12} md={3}>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        <Chip 
                          label={`${indicator.recordsProcessed.toLocaleString()}/${indicator.totalRecords.toLocaleString()}`}
                          size="small" 
                          icon={<DataIcon />}
                          variant="outlined"
                        />
                        <Chip 
                          label={formatBytes(indicator.memoryUsage)}
                          size="small" 
                          icon={<MemoryIcon />}
                          variant="outlined"
                        />
                        <Chip 
                          label={formatPercentage(indicator.cpuUsage)}
                          size="small" 
                          icon={<SpeedIcon />}
                          variant="outlined"
                        />
                      </Stack>
                    </Grid>
                    
                    <Grid item xs={12} md={2}>
                      <Stack direction="row" spacing={1}>
                        <Tooltip title="View Details">
                          <IconButton size="small">
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </Grid>
                  </Grid>
                </Paper>
              ))}
            </Stack>
          </CardContent>
        </Card>
      )}

      {/* Recent Completed Executions */}
      <Accordion defaultExpanded>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <AssessmentIcon />
            <Typography variant="h6">
              Recent Completed Executions ({completedExecutions.length})
            </Typography>
          </Stack>
        </AccordionSummary>
        <AccordionDetails>
          {completedExecutions.length === 0 ? (
            <Alert severity="info">
              No recent completed executions
            </Alert>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Indicator</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Duration</TableCell>
                    <TableCell>Result</TableCell>
                    <TableCell>Performance</TableCell>
                    <TableCell>Completed</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {completedExecutions.map((execution) => (
                    <TableRow key={execution.indicatorId}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {execution.indicatorName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {execution.executionContext}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={execution.status}
                          color={execution.status === 'success' ? 'success' : 'error'}
                          size="small"
                          icon={execution.status === 'success' ? <SuccessIcon /> : <ErrorIcon />}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {execution.duration}s
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {execution.status === 'success' && execution.result ? (
                          <Typography variant="body2" fontWeight="medium">
                            {execution.result.toLocaleString()}
                          </Typography>
                        ) : execution.errorMessage ? (
                          <Typography variant="body2" color="error">
                            {execution.errorMessage}
                          </Typography>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            N/A
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.5}>
                          <Chip 
                            label={`${execution.recordCount} records`}
                            size="small" 
                            variant="outlined"
                          />
                          <Chip 
                            label={formatBytes(execution.memoryUsage)}
                            size="small" 
                            variant="outlined"
                          />
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {formatDistanceToNow(new Date(execution.endTime), { addSuffix: true })}
                        </Typography>
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

export default RealTimeProcessingMonitor;

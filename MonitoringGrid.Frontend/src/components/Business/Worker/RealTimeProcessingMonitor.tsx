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

  // Mock real-time data - in production this would come from SignalR
  useEffect(() => {
    const mockProcessingIndicators: ProcessingIndicator[] = [
      {
        indicatorId: 1,
        indicatorName: 'Daily Sales Analysis',
        startTime: new Date(Date.now() - 45000).toISOString(),
        progress: 75,
        currentStep: 'Aggregating sales data by region',
        estimatedTimeRemaining: 30,
        memoryUsage: 128.5,
        cpuUsage: 45.2,
        recordsProcessed: 7500,
        totalRecords: 10000,
        executionContext: 'Scheduled',
        owner: 'System',
        collectorName: 'Sales Database',
      },
      {
        indicatorId: 2,
        indicatorName: 'Customer Satisfaction Score',
        startTime: new Date(Date.now() - 20000).toISOString(),
        progress: 35,
        currentStep: 'Processing survey responses',
        estimatedTimeRemaining: 85,
        memoryUsage: 89.3,
        cpuUsage: 32.1,
        recordsProcessed: 1750,
        totalRecords: 5000,
        executionContext: 'Manual',
        owner: 'john.doe',
        collectorName: 'Survey API',
      },
    ];

    const mockCompletedExecutions: CompletedExecution[] = [
      {
        indicatorId: 3,
        indicatorName: 'Website Traffic Analysis',
        startTime: new Date(Date.now() - 300000).toISOString(),
        endTime: new Date(Date.now() - 240000).toISOString(),
        duration: 60,
        status: 'success',
        result: 15420,
        recordCount: 1247,
        memoryUsage: 67.8,
        cpuUsage: 23.4,
        thresholdBreached: false,
        alertTriggered: false,
        executionContext: 'Scheduled',
      },
      {
        indicatorId: 4,
        indicatorName: 'Inventory Levels Check',
        startTime: new Date(Date.now() - 600000).toISOString(),
        endTime: new Date(Date.now() - 580000).toISOString(),
        duration: 20,
        status: 'failed',
        errorMessage: 'Database connection timeout',
        recordCount: 0,
        memoryUsage: 34.2,
        cpuUsage: 12.1,
        thresholdBreached: false,
        alertTriggered: true,
        executionContext: 'Scheduled',
      },
    ];

    setProcessingIndicators(mockProcessingIndicators);
    setCompletedExecutions(mockCompletedExecutions);
    
    // Calculate processing stats
    const totalMemory = mockProcessingIndicators.reduce((sum, ind) => sum + ind.memoryUsage, 0);
    const totalCpu = mockProcessingIndicators.reduce((sum, ind) => sum + ind.cpuUsage, 0);
    const avgProgress = mockProcessingIndicators.length > 0 
      ? mockProcessingIndicators.reduce((sum, ind) => sum + ind.progress, 0) / mockProcessingIndicators.length 
      : 0;

    setProcessingStats({
      totalProcessing: mockProcessingIndicators.length,
      averageProgress: avgProgress,
      totalMemoryUsage: totalMemory,
      totalCpuUsage: totalCpu,
      estimatedCompletionTime: '2 minutes',
      queueLength: 5,
    });

    // Simulate real-time updates
    const interval = setInterval(() => {
      setProcessingIndicators(prev => 
        prev.map(indicator => ({
          ...indicator,
          progress: Math.min(indicator.progress + Math.random() * 5, 100),
          estimatedTimeRemaining: Math.max(indicator.estimatedTimeRemaining - 2, 0),
          recordsProcessed: Math.min(
            indicator.recordsProcessed + Math.floor(Math.random() * 100),
            indicator.totalRecords
          ),
          memoryUsage: indicator.memoryUsage + (Math.random() - 0.5) * 5,
          cpuUsage: Math.max(0, Math.min(100, indicator.cpuUsage + (Math.random() - 0.5) * 10)),
        }))
      );
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [refreshInterval]);

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

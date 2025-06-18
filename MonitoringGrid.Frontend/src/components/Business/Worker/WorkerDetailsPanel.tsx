import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  LinearProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Alert,
  Divider,
  Stack,
  IconButton,
  Tooltip,
  Button,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Memory as MemoryIcon,
  Speed as SpeedIcon,
  Schedule as ScheduleIcon,
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Refresh as RefreshIcon,
  Timeline as TimelineIcon,
  Storage as StorageIcon,
  Computer as ProcessIcon,
  AccessTime as TimeIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { workerApi, schedulerApi } from '@/services/api';
import { LoadingSpinner } from '@/components/UI';

interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  details?: Record<string, any>;
}

interface WorkerMetrics {
  cpuUsage?: number;
  memoryUsageMB?: number;
  threadCount?: number;
  totalIndicatorsProcessed?: number;
  successfulExecutions?: number;
  failedExecutions?: number;
  averageExecutionTimeMs?: number;
}

interface WorkerStatus {
  isRunning: boolean;
  mode: string;
  processId?: number;
  startTime?: string;
  uptime?: string;
  services: WorkerService[];
  metrics?: WorkerMetrics;
  timestamp: string;
}

interface UpcomingExecution {
  indicatorId: number;
  indicatorName: string;
  scheduledTime: string;
  schedulerName: string;
  lastExecuted?: string;
  estimatedDuration?: number;
}

interface WorkerDetailsPanelProps {
  refreshInterval?: number;
  showMetrics?: boolean;
  showUpcomingQueue?: boolean;
  showServices?: boolean;
}

const WorkerDetailsPanel: React.FC<WorkerDetailsPanelProps> = ({
  refreshInterval = 5000,
  showMetrics = true,
  showUpcomingQueue = true,
  showServices = true,
}) => {
  const [workerStatus, setWorkerStatus] = useState<WorkerStatus | null>(null);
  const [upcomingExecutions, setUpcomingExecutions] = useState<UpcomingExecution[]>([]);
  const [debugInfo, setDebugInfo] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchWorkerDetails = async () => {
    try {
      setError(null);
      
      // Fetch worker status with detailed metrics
      const statusResponse = await workerApi.getStatus(true, true);
      if (statusResponse?.data) {
        setWorkerStatus(statusResponse.data);
      }

      // Fetch upcoming executions if worker is running
      if (statusResponse?.data?.isRunning && showUpcomingQueue) {
        const upcomingResponse = await schedulerApi.getUpcomingExecutions(24);
        setUpcomingExecutions(upcomingResponse || []);
      }

      // Fetch debug information
      const debugResponse = await workerApi.getDebugIndicators();
      if (debugResponse?.data) {
        setDebugInfo(debugResponse.data);
      }

    } catch (err: any) {
      console.error('Error fetching worker details:', err);
      setError(err.message || 'Failed to fetch worker details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWorkerDetails();
    
    const interval = setInterval(fetchWorkerDetails, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshInterval, showUpcomingQueue]);

  const formatUptime = (uptime: string) => {
    if (!uptime) return 'N/A';
    
    // Parse uptime string (format: "days.hours:minutes:seconds.milliseconds")
    const parts = uptime.split('.');
    if (parts.length >= 2) {
      const days = parseInt(parts[0]);
      const timePart = parts[1].split(':');
      const hours = parseInt(timePart[0]);
      const minutes = parseInt(timePart[1]);
      
      if (days > 0) {
        return `${days}d ${hours}h ${minutes}m`;
      } else if (hours > 0) {
        return `${hours}h ${minutes}m`;
      } else {
        return `${minutes}m`;
      }
    }
    
    return uptime;
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running': return 'success';
      case 'stopped': return 'error';
      case 'starting': return 'warning';
      case 'stopping': return 'warning';
      default: return 'default';
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading worker details..." />;
  }

  if (error) {
    return (
      <Alert severity="error" action={
        <Button onClick={fetchWorkerDetails} startIcon={<RefreshIcon />}>
          Retry
        </Button>
      }>
        {error}
      </Alert>
    );
  }

  if (!workerStatus) {
    return (
      <Alert severity="warning">
        No worker status information available
      </Alert>
    );
  }

  return (
    <Box>
      {/* Worker Status Overview */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <ProcessIcon color={workerStatus.isRunning ? 'success' : 'error'} />
                <Box>
                  <Typography variant="h6">
                    {workerStatus.isRunning ? 'Running' : 'Stopped'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Mode: {workerStatus.mode}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <TimeIcon color="primary" />
                <Box>
                  <Typography variant="h6">
                    {formatUptime(workerStatus.uptime || '')}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Uptime
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <StorageIcon color="info" />
                <Box>
                  <Typography variant="h6">
                    PID: {workerStatus.processId || 'N/A'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Process ID
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <ScheduleIcon color="warning" />
                <Box>
                  <Typography variant="h6">
                    {workerStatus.services?.length || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Active Services
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Performance Metrics */}
      {showMetrics && workerStatus.metrics && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <SpeedIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
              Performance Metrics
            </Typography>
            
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Memory Usage: {workerStatus.metrics.memoryUsageMB?.toFixed(1) || 'N/A'} MB
                  </Typography>
                  {workerStatus.metrics.memoryUsageMB && (
                    <LinearProgress 
                      variant="determinate" 
                      value={Math.min((workerStatus.metrics.memoryUsageMB / 1024) * 100, 100)} 
                      sx={{ mt: 1 }}
                    />
                  )}
                </Box>
                
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Thread Count: {workerStatus.metrics.threadCount || 'N/A'}
                  </Typography>
                </Box>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Total Processed: {workerStatus.metrics.totalIndicatorsProcessed || 'N/A'}
                  </Typography>
                </Box>
                
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Success Rate: {
                      workerStatus.metrics.successfulExecutions && workerStatus.metrics.totalIndicatorsProcessed
                        ? `${((workerStatus.metrics.successfulExecutions / workerStatus.metrics.totalIndicatorsProcessed) * 100).toFixed(1)}%`
                        : 'N/A'
                    }
                  </Typography>
                </Box>
                
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Avg Execution Time: {workerStatus.metrics.averageExecutionTimeMs || 'N/A'}ms
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Worker Services */}
      {showServices && workerStatus.services && workerStatus.services.length > 0 && (
        <Accordion defaultExpanded>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6">
              Worker Services ({workerStatus.services.length})
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Service Name</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Last Activity</TableCell>
                    <TableCell>Details</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {workerStatus.services.map((service, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {service.name}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={service.status} 
                          color={getStatusColor(service.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {service.lastActivity 
                            ? format(new Date(service.lastActivity), 'MMM dd, HH:mm:ss')
                            : 'N/A'
                          }
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {service.details && (
                          <Typography variant="body2" color="text.secondary">
                            {Object.entries(service.details).map(([key, value]) => (
                              <div key={key}>
                                <strong>{key}:</strong> {String(value)}
                              </div>
                            ))}
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </AccordionDetails>
        </Accordion>
      )}
    </Box>
  );
};

export default WorkerDetailsPanel;

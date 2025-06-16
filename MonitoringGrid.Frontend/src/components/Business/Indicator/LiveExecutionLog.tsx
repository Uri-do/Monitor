import React, { useState, useEffect, useRef } from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
  Badge,
  Paper,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Clear as ClearIcon,
  Pause as PauseIcon,
  ExpandMore as ExpandIcon,
  ExpandLess as CollapseIcon,
  Timeline as TimelineIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { useRealtime } from '@/contexts/RealtimeContext';
import {
  useSignalR,
  IndicatorExecutionStarted,
  IndicatorExecutionCompleted
} from '@/services/signalRService';

export interface ExecutionLogEntry {
  id: string;
  timestamp: Date;
  indicatorID: number;
  indicator: string;
  type: 'started' | 'completed' | 'error' | 'info';
  message: string;
  duration?: number;
  success?: boolean;
  errorMessage?: string;
  details?: {
    collectorID?: number;
    collectorItemName?: string;
    lastMinutes?: number;
    availableItems?: string[];
    value?: number;
    executionContext?: string;
    alertsGenerated?: number;
  };
}

interface LiveExecutionLogProps {
  maxEntries?: number;
  autoScroll?: boolean;
  showOnlyErrors?: boolean;
}

const LiveExecutionLog: React.FC<LiveExecutionLogProps> = ({
  maxEntries = 50,
  autoScroll = true,
  showOnlyErrors = false,
}) => {
  const [logEntries, setLogEntries] = useState<ExecutionLogEntry[]>([]);
  const [isPaused, setIsPaused] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);
  const [filterErrors, setFilterErrors] = useState(showOnlyErrors);
  const logContainerRef = useRef<HTMLDivElement>(null);

  const dashboardState = useRealtimeDashboard();
  const realtimeContext = useRealtime();
  const signalR = useSignalR();

  // Add new log entries from real-time events
  useEffect(() => {
    if (isPaused) return;

    // Handle execution started events
    dashboardState.runningIndicators.forEach(indicator => {
      const existingEntry = logEntries.find(
        entry => entry.indicatorID === indicator.indicatorID && entry.type === 'started'
      );

      if (!existingEntry) {
        const newEntry: ExecutionLogEntry = {
          id: `${indicator.indicatorID}-started-${Date.now()}`,
          timestamp: new Date(indicator.startTime),
          indicatorID: indicator.indicatorID,
          indicator: indicator.indicator,
          type: 'started',
          message: `Started execution`,
          details: {
            collectorID: undefined, // Will be populated from SignalR events
          }
        };

        setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
      }
    });
  }, [dashboardState.runningIndicators, isPaused, maxEntries, logEntries]);

  // Auto-scroll to top when new entries are added
  useEffect(() => {
    if (autoScroll && logContainerRef.current) {
      logContainerRef.current.scrollTop = 0;
    }
  }, [logEntries, autoScroll]);

  // Listen for SignalR execution events
  useEffect(() => {
    if (!realtimeContext.isConnected || isPaused) return;

    // Handle IndicatorExecutionStarted events
    const handleExecutionStarted = (data: IndicatorExecutionStarted) => {
      console.log('📊 Execution started event received:', data);

      const newEntry: ExecutionLogEntry = {
        id: `${data.indicatorID}-started-${Date.now()}`,
        timestamp: new Date(data.startTime),
        indicatorID: data.indicatorID,
        indicator: data.indicator,
        type: 'started',
        message: `Started execution`,
        details: {
          collectorID: data.collectorID,
          collectorItemName: data.collectorItemName,
          lastMinutes: data.lastMinutes,
          executionContext: data.executionContext,
        }
      };

      setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
    };

    // Handle IndicatorExecutionCompleted events
    const handleExecutionCompleted = (data: IndicatorExecutionCompleted) => {
      console.log('✅ Execution completed event received:', data);

      const newEntry: ExecutionLogEntry = {
        id: `${data.indicatorID}-completed-${Date.now()}`,
        timestamp: new Date(data.completedAt),
        indicatorID: data.indicatorID,
        indicator: data.indicator,
        type: data.success ? 'completed' : 'error',
        message: data.success ? 'Execution completed successfully' : (data.errorMessage || 'Execution failed'),
        duration: data.duration,
        success: data.success,
        errorMessage: data.errorMessage,
        details: {
          collectorID: data.collectorID,
          collectorItemName: data.collectorItemName,
          lastMinutes: data.lastMinutes,
          value: data.value,
          executionContext: data.executionContext,
          alertsGenerated: data.alertsGenerated,
        }
      };

      setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
    };

    // Register event handlers
    signalR.on('onIndicatorExecutionStarted', handleExecutionStarted);
    signalR.on('onIndicatorExecutionCompleted', handleExecutionCompleted);

    // Cleanup function
    return () => {
      signalR.off('onIndicatorExecutionStarted', handleExecutionStarted);
      signalR.off('onIndicatorExecutionCompleted', handleExecutionCompleted);
    };
  }, [realtimeContext.isConnected, isPaused, maxEntries, signalR]);

  const getLogIcon = (type: ExecutionLogEntry['type']) => {
    switch (type) {
      case 'started':
        return <PlayIcon color="primary" />;
      case 'completed':
        return <SuccessIcon color="success" />;
      case 'error':
        return <ErrorIcon color="error" />;
      case 'info':
        return <InfoIcon color="info" />;
      default:
        return <InfoIcon />;
    }
  };

  const getLogColor = (type: ExecutionLogEntry['type']) => {
    switch (type) {
      case 'started':
        return 'primary';
      case 'completed':
        return 'success';
      case 'error':
        return 'error';
      case 'info':
        return 'info';
      default:
        return 'default';
    }
  };

  const filteredEntries = filterErrors 
    ? logEntries.filter(entry => entry.type === 'error')
    : logEntries;

  const errorCount = logEntries.filter(entry => entry.type === 'error').length;

  const clearLog = () => {
    setLogEntries([]);
  };

  return (
    <Card>
      <CardHeader
        title={
          <Box display="flex" alignItems="center" gap={1}>
            <TimelineIcon />
            <Typography variant="h6">Live Execution Log</Typography>
            {errorCount > 0 && (
              <Badge badgeContent={errorCount} color="error">
                <ErrorIcon color="error" />
              </Badge>
            )}
          </Box>
        }
        action={
          <Box display="flex" alignItems="center" gap={1}>
            <FormControlLabel
              control={
                <Switch
                  checked={filterErrors}
                  onChange={(e) => setFilterErrors(e.target.checked)}
                  size="small"
                />
              }
              label="Errors Only"
            />
            <Tooltip title={isPaused ? "Resume logging" : "Pause logging"}>
              <IconButton onClick={() => setIsPaused(!isPaused)} size="small">
                {isPaused ? <PlayIcon /> : <PauseIcon />}
              </IconButton>
            </Tooltip>
            <Tooltip title="Clear log">
              <IconButton onClick={clearLog} size="small">
                <ClearIcon />
              </IconButton>
            </Tooltip>
            <Tooltip title={isExpanded ? "Collapse" : "Expand"}>
              <IconButton onClick={() => setIsExpanded(!isExpanded)} size="small">
                {isExpanded ? <CollapseIcon /> : <ExpandIcon />}
              </IconButton>
            </Tooltip>
          </Box>
        }
      />
      
      {isExpanded && (
        <CardContent sx={{ pt: 0 }}>
          <Paper 
            variant="outlined" 
            sx={{ 
              maxHeight: 400, 
              overflow: 'auto',
              backgroundColor: 'grey.50'
            }}
            ref={logContainerRef}
          >
            {filteredEntries.length === 0 ? (
              <Box p={3} textAlign="center">
                <Typography variant="body2" color="text.secondary">
                  {filterErrors ? 'No errors logged' : 'No execution logs yet'}
                </Typography>
              </Box>
            ) : (
              <List dense>
                {filteredEntries.map((entry, index) => (
                  <React.Fragment key={entry.id}>
                    <ListItem alignItems="flex-start">
                      <ListItemIcon sx={{ minWidth: 40 }}>
                        {getLogIcon(entry.type)}
                      </ListItemIcon>
                      <ListItemText
                        primary={
                          <Box display="flex" alignItems="center" gap={1} mb={0.5}>
                            <Typography variant="body2" fontWeight="medium">
                              {entry.indicator}
                            </Typography>
                            <Chip 
                              label={entry.type.toUpperCase()} 
                              size="small" 
                              color={getLogColor(entry.type) as any}
                              variant="outlined"
                            />
                            {entry.duration && (
                              <Chip 
                                label={`${entry.duration}ms`} 
                                size="small" 
                                variant="outlined"
                              />
                            )}
                          </Box>
                        }
                        secondary={
                          <Box>
                            <Typography variant="body2" color="text.primary" sx={{ mb: 0.5 }}>
                              {entry.message}
                            </Typography>
                            {entry.details && (
                              <Box display="flex" flexWrap="wrap" gap={0.5} mb={0.5}>
                                {entry.details.collectorID && (
                                  <Chip 
                                    label={`Collector: ${entry.details.collectorID}`} 
                                    size="small" 
                                    variant="outlined"
                                  />
                                )}
                                {entry.details.collectorItemName && (
                                  <Chip 
                                    label={`Item: ${entry.details.collectorItemName}`} 
                                    size="small" 
                                    variant="outlined"
                                  />
                                )}
                                {entry.details.lastMinutes && (
                                  <Chip
                                    label={`${entry.details.lastMinutes}min`}
                                    size="small"
                                    variant="outlined"
                                  />
                                )}
                                {entry.details.executionContext && (
                                  <Chip
                                    label={entry.details.executionContext}
                                    size="small"
                                    variant="outlined"
                                    color="info"
                                  />
                                )}
                                {entry.details.alertsGenerated !== undefined && entry.details.alertsGenerated > 0 && (
                                  <Chip
                                    label={`${entry.details.alertsGenerated} alerts`}
                                    size="small"
                                    variant="outlined"
                                    color="warning"
                                  />
                                )}
                                {entry.details.value !== undefined && (
                                  <Chip
                                    label={`Value: ${entry.details.value}`}
                                    size="small"
                                    variant="outlined"
                                    color="success"
                                  />
                                )}
                              </Box>
                            )}
                            <Typography variant="caption" color="text.secondary">
                              {format(entry.timestamp, 'HH:mm:ss')}
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                    {index < filteredEntries.length - 1 && <Divider />}
                  </React.Fragment>
                ))}
              </List>
            )}
          </Paper>
          
          {isPaused && (
            <Box mt={1} textAlign="center">
              <Chip 
                label="Logging Paused" 
                color="warning" 
                size="small" 
                icon={<PauseIcon />}
              />
            </Box>
          )}
        </CardContent>
      )}
    </Card>
  );
};

export default LiveExecutionLog;

import React, { useState, useEffect } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Chip,
  IconButton,
  Badge,
  Alert,
  Collapse,
  Button,
  Tooltip,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Clear as ClearIcon,
  Visibility as ViewIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { format, formatDistanceToNow } from 'date-fns';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { IndicatorExecutionCompleted } from '@/services/signalRService';
import ExecutionErrorDialog, { ExecutionErrorDetails } from '@/components/Business/Indicator/ExecutionErrorDialog';

interface ExecutionError {
  id: string;
  indicatorID: number;
  indicator: string;
  owner: string;
  errorMessage: string;
  duration: number;
  completedAt: string;
  collectorID?: number;
  collectorItemName?: string;
  lastMinutes?: number;
  executionContext?: string;
  timestamp: Date;
}

interface ExecutionErrorsCardProps {
  maxErrors?: number;
}

const ExecutionErrorsCard: React.FC<ExecutionErrorsCardProps> = ({
  maxErrors = 5,
}) => {
  const [executionErrors, setExecutionErrors] = useState<ExecutionError[]>([]);
  const [isExpanded, setIsExpanded] = useState(true);
  const [selectedError, setSelectedError] = useState<ExecutionErrorDetails | null>(null);
  const [showErrorDialog, setShowErrorDialog] = useState(false);
  
  const dashboardState = useRealtimeDashboard();

  // Listen for execution completed events and capture errors
  useEffect(() => {
    // This would be replaced with actual SignalR event handling
    // For now, we'll simulate error events
    const handleExecutionCompleted = (data: IndicatorExecutionCompleted) => {
      if (!data.success && data.errorMessage) {
        const newError: ExecutionError = {
          id: `${data.indicatorID}-${Date.now()}`,
          indicatorID: data.indicatorID,
          indicator: data.indicator,
          owner: data.owner || 'Unknown',
          errorMessage: data.errorMessage,
          duration: data.duration,
          completedAt: data.completedAt,
          collectorID: data.collectorID,
          collectorItemName: data.collectorItemName,
          lastMinutes: data.lastMinutes,
          executionContext: data.executionContext,
          timestamp: new Date(),
        };

        setExecutionErrors(prev => [newError, ...prev].slice(0, maxErrors));
      }
    };

    // Mock error generation for demonstration
    const interval = setInterval(() => {
      if (Math.random() > 0.8) { // 20% chance of error
        const mockError: ExecutionError = {
          id: `mock-${Date.now()}`,
          indicatorID: Math.floor(Math.random() * 3) + 1,
          indicator: `Test Indicator ${Math.floor(Math.random() * 3) + 1}`,
          owner: 'System',
          errorMessage: `Item 'TestItem${Math.floor(Math.random() * 3) + 1}' not found in collector results. Available items: ['item1', 'item2']`,
          duration: Math.floor(Math.random() * 5000) + 500,
          completedAt: new Date().toISOString(),
          collectorID: Math.floor(Math.random() * 3) + 1,
          collectorItemName: `TestItem${Math.floor(Math.random() * 3) + 1}`,
          lastMinutes: 10,
          executionContext: 'Scheduled',
          timestamp: new Date(),
        };

        setExecutionErrors(prev => [mockError, ...prev].slice(0, maxErrors));
      }
    }, 5000);

    return () => clearInterval(interval);
  }, [maxErrors]);

  const handleViewError = (error: ExecutionError) => {
    const errorDetails: ExecutionErrorDetails = {
      indicatorID: error.indicatorID,
      indicator: error.indicator,
      owner: error.owner,
      errorMessage: error.errorMessage,
      duration: error.duration,
      completedAt: error.completedAt,
      collectorID: error.collectorID,
      collectorItemName: error.collectorItemName,
      lastMinutes: error.lastMinutes,
      executionContext: error.executionContext,
    };

    setSelectedError(errorDetails);
    setShowErrorDialog(true);
  };

  const handleClearErrors = () => {
    setExecutionErrors([]);
  };

  const getErrorSeverity = (errorMessage: string) => {
    if (errorMessage.includes('not found in collector results')) {
      return 'warning'; // Configuration issue
    }
    if (errorMessage.includes('timeout') || errorMessage.includes('cancelled')) {
      return 'info'; // Operational issue
    }
    return 'error'; // Critical error
  };

  const getErrorIcon = (errorMessage: string) => {
    const severity = getErrorSeverity(errorMessage);
    switch (severity) {
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'info':
        return <ErrorIcon color="info" />;
      default:
        return <ErrorIcon color="error" />;
    }
  };

  return (
    <>
      <Grid item xs={12} md={6}>
        <Card sx={{ height: '100%' }}>
          <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Box display="flex" alignItems="center" gap={1}>
                <Badge
                  badgeContent={executionErrors.length}
                  color="error"
                  sx={{ '& .MuiBadge-badge': { fontSize: '0.7rem' } }}
                >
                  <ErrorIcon sx={{ color: 'error.main' }} />
                </Badge>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                  Recent Execution Errors
                </Typography>
              </Box>
              <Box display="flex" alignItems="center" gap={1}>
                <Tooltip title="Clear all errors">
                  <IconButton 
                    size="small" 
                    onClick={handleClearErrors}
                    disabled={executionErrors.length === 0}
                  >
                    <ClearIcon />
                  </IconButton>
                </Tooltip>
                <Tooltip title={isExpanded ? "Collapse" : "Expand"}>
                  <IconButton 
                    size="small" 
                    onClick={() => setIsExpanded(!isExpanded)}
                  >
                    {isExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                  </IconButton>
                </Tooltip>
              </Box>
            </Box>

            <Collapse in={isExpanded}>
              <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
                {executionErrors.length > 0 ? (
                  <List sx={{ p: 0, maxHeight: 300, overflow: 'auto' }}>
                    {executionErrors.map((error) => (
                      <ListItem
                        key={error.id}
                        sx={{
                          borderRadius: 2,
                          mb: 1,
                          border: '1px solid',
                          borderColor: `${getErrorSeverity(error.errorMessage)}.light`,
                          backgroundColor: `${getErrorSeverity(error.errorMessage)}.50`,
                          '&:hover': {
                            backgroundColor: `${getErrorSeverity(error.errorMessage)}.100`,
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                          {getErrorIcon(error.errorMessage)}
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Box display="flex" alignItems="center" gap={1} mb={0.5}>
                              <Typography variant="subtitle2" fontWeight="medium">
                                {error.indicator}
                              </Typography>
                              <Chip 
                                label={`${error.duration}ms`} 
                                size="small" 
                                variant="outlined"
                              />
                            </Box>
                          }
                          secondary={
                            <Box>
                              <Typography 
                                variant="body2" 
                                color="text.primary" 
                                sx={{ mb: 0.5 }}
                                noWrap
                              >
                                {error.errorMessage.length > 60 
                                  ? `${error.errorMessage.substring(0, 60)}...`
                                  : error.errorMessage
                                }
                              </Typography>
                              <Box display="flex" justifyContent="space-between" alignItems="center">
                                <Typography variant="caption" color="text.secondary">
                                  {formatDistanceToNow(error.timestamp, { addSuffix: true })}
                                </Typography>
                                <Button
                                  size="small"
                                  startIcon={<ViewIcon />}
                                  onClick={() => handleViewError(error)}
                                  sx={{ minWidth: 'auto', px: 1 }}
                                >
                                  Details
                                </Button>
                              </Box>
                            </Box>
                          }
                          primaryTypographyProps={{
                            component: 'div',
                          }}
                          secondaryTypographyProps={{
                            component: 'div',
                          }}
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
                      backgroundColor: 'success.50',
                      borderRadius: 2,
                      border: '2px dashed',
                      borderColor: 'success.200',
                    }}
                  >
                    <ErrorIcon sx={{ fontSize: 48, color: 'success.400', mb: 2 }} />
                    <Typography color="success.main" variant="body2" sx={{ fontWeight: 500 }}>
                      No execution errors
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      All indicators are running successfully
                    </Typography>
                  </Box>
                )}
              </Box>
            </Collapse>

            {executionErrors.length > 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
                <Typography variant="body2">
                  Showing {executionErrors.length} most recent execution errors. 
                  Click "Details" to view full error analysis and troubleshooting steps.
                </Typography>
              </Alert>
            )}
          </CardContent>
        </Card>
      </Grid>

      <ExecutionErrorDialog
        open={showErrorDialog}
        onClose={() => setShowErrorDialog(false)}
        errorDetails={selectedError}
      />
    </>
  );
};

export default ExecutionErrorsCard;

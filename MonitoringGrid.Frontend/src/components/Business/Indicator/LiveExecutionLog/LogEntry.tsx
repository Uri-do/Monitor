import React, { useState } from 'react';
import {
  Box,
  Typography,
  Chip,
  ListItem,
  ListItemIcon,
  ListItemText,
  Collapse,
  IconButton,
  Alert,
  Tooltip,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Storage as DatabaseIcon,
  Code as QueryIcon,
  Assessment as ThresholdIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ExecutionLogEntry, DetailedExecutionInfo } from '../types';
import { useQuery } from '@tanstack/react-query';
import { executionHistoryApi } from '../../../../services/api';

interface LogEntryProps {
  entry: ExecutionLogEntry;
}

export const LogEntry: React.FC<LogEntryProps> = ({ entry }) => {
  const [expanded, setExpanded] = useState(false);

  // Show expand arrows for completed/error entries or entries with execution history ID
  const hasExpandableContent = entry.errorMessage ||
    (entry.details && Object.keys(entry.details).length > 2) || // Reduced threshold from 3 to 2
    entry.type === 'error' ||
    entry.type === 'completed' ||
    !!entry.executionHistoryId; // Always show expand for entries with execution history

  // Fetch detailed execution information when expanded and available
  const { data: detailedInfo, isLoading: isLoadingDetails } = useQuery({
    queryKey: ['executionDetail', entry.executionHistoryId],
    queryFn: () => executionHistoryApi.getExecutionDetail(entry.executionHistoryId!),
    enabled: expanded && !!entry.executionHistoryId && (entry.type === 'completed' || entry.type === 'error'),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
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

  return (
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
            {hasExpandableContent && (
              <Tooltip title={expanded ? "Show less" : "Show more details"}>
                <IconButton
                  size="small"
                  onClick={() => setExpanded(!expanded)}
                  sx={{ ml: 'auto' }}
                >
                  {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                </IconButton>
              </Tooltip>
            )}
          </Box>
        }
        secondary={
          <Box>
            <Typography variant="body2" color="text.primary" sx={{ mb: 0.5 }}>
              {entry.message}
            </Typography>

            {/* Show error message prominently for error entries */}
            {entry.type === 'error' && entry.errorMessage && !expanded && (
              <Alert severity="error" sx={{ mb: 1, fontSize: '0.875rem' }}>
                <Typography variant="body2" component="div">
                  {entry.errorMessage.length > 100
                    ? `${entry.errorMessage.substring(0, 100)}...`
                    : entry.errorMessage
                  }
                </Typography>
              </Alert>
            )}

            {/* Basic details - always shown */}
            {entry.details && (
              <Box display="flex" flexWrap="wrap" gap={0.5} mb={0.5}>
                {entry.details.executionContext && (
                  <Chip
                    label={entry.details.executionContext}
                    size="small"
                    variant="outlined"
                    color="info"
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
                {entry.details.alertsGenerated !== undefined && entry.details.alertsGenerated > 0 && (
                  <Chip
                    label={`${entry.details.alertsGenerated} alerts`}
                    size="small"
                    variant="outlined"
                    color="warning"
                  />
                )}
              </Box>
            )}

            {/* Expandable detailed content */}
            <Collapse in={expanded}>
              <Box sx={{ mt: 1, p: 2, backgroundColor: 'grey.50', borderRadius: 1 }}>
                {isLoadingDetails && (
                  <Box display="flex" alignItems="center" gap={1} mb={2}>
                    <CircularProgress size={16} />
                    <Typography variant="body2">Loading detailed information...</Typography>
                  </Box>
                )}

                {/* Full error message */}
                {entry.errorMessage && (
                  <Alert severity="error" sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Error Details:
                    </Typography>
                    <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
                      {entry.errorMessage}
                    </Typography>
                  </Alert>
                )}

                {/* Basic execution details */}
                {entry.details && (
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Execution Summary:
                    </Typography>
                    <Box display="flex" flexDirection="column" gap={1}>
                      {entry.details.collectorID && (
                        <Box>
                          <strong>Collector ID:</strong> {entry.details.collectorID}
                        </Box>
                      )}
                      {entry.details.collectorItemName && (
                        <Box>
                          <strong>Collector Item:</strong> {entry.details.collectorItemName}
                        </Box>
                      )}
                      {entry.details.lastMinutes && (
                        <Box>
                          <strong>Time Range:</strong> {entry.details.lastMinutes} minutes
                        </Box>
                      )}
                      {entry.details.executionContext && (
                        <Box>
                          <strong>Context:</strong> {entry.details.executionContext}
                        </Box>
                      )}
                      {entry.details.value !== undefined && (
                        <Box>
                          <strong>Result Value:</strong> {entry.details.value}
                        </Box>
                      )}
                      {entry.details.alertsGenerated !== undefined && (
                        <Box>
                          <strong>Alerts Generated:</strong> {entry.details.alertsGenerated}
                        </Box>
                      )}
                    </Box>
                  </Box>
                )}

                {/* Detailed execution information from API */}
                {detailedInfo && (
                  <Box>
                    {/* Threshold Analysis */}
                    {(detailedInfo.alertThreshold || entry.details?.thresholdBreached) && (
                      <Accordion sx={{ mb: 1 }}>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <ThresholdIcon />
                            <Typography variant="subtitle2">Threshold Analysis</Typography>
                            {entry.details?.thresholdBreached && (
                              <Chip label="BREACHED" color="error" size="small" />
                            )}
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box display="flex" flexDirection="column" gap={1}>
                            {detailedInfo.alertThreshold && (
                              <Box>
                                <strong>Threshold:</strong> {detailedInfo.alertThreshold} ({detailedInfo.alertOperator || 'N/A'})
                              </Box>
                            )}
                            {entry.details?.value !== undefined && (
                              <Box>
                                <strong>Current Value:</strong> {entry.details.value}
                              </Box>
                            )}
                            {detailedInfo.historicalValue && (
                              <Box>
                                <strong>Historical Value:</strong> {detailedInfo.historicalValue}
                              </Box>
                            )}
                            {detailedInfo.deviationPercent && (
                              <Box>
                                <strong>Deviation:</strong> {detailedInfo.deviationPercent}%
                              </Box>
                            )}
                          </Box>
                        </AccordionDetails>
                      </Accordion>
                    )}

                    {/* SQL Query */}
                    {detailedInfo.sqlCommand && (
                      <Accordion sx={{ mb: 1 }}>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <QueryIcon />
                            <Typography variant="subtitle2">SQL Query</Typography>
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box
                            component="pre"
                            sx={{
                              backgroundColor: 'grey.100',
                              p: 2,
                              borderRadius: 1,
                              overflow: 'auto',
                              fontSize: '0.875rem',
                              fontFamily: 'monospace',
                              maxHeight: 300,
                            }}
                          >
                            {detailedInfo.sqlCommand}
                          </Box>
                          {detailedInfo.sqlParameters && (
                            <Box sx={{ mt: 2 }}>
                              <Typography variant="subtitle2" gutterBottom>
                                Parameters:
                              </Typography>
                              <Box
                                component="pre"
                                sx={{
                                  backgroundColor: 'grey.50',
                                  p: 1,
                                  borderRadius: 1,
                                  fontSize: '0.8rem',
                                  fontFamily: 'monospace',
                                }}
                              >
                                {detailedInfo.sqlParameters}
                              </Box>
                            </Box>
                          )}
                        </AccordionDetails>
                      </Accordion>
                    )}

                    {/* Raw Response */}
                    {detailedInfo.rawResponse && (
                      <Accordion sx={{ mb: 1 }}>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <DatabaseIcon />
                            <Typography variant="subtitle2">Raw Response</Typography>
                            {detailedInfo.resultCount && (
                              <Chip label={`${detailedInfo.resultCount} rows`} size="small" variant="outlined" />
                            )}
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box
                            component="pre"
                            sx={{
                              backgroundColor: 'grey.100',
                              p: 2,
                              borderRadius: 1,
                              overflow: 'auto',
                              fontSize: '0.875rem',
                              fontFamily: 'monospace',
                              maxHeight: 400,
                            }}
                          >
                            {detailedInfo.rawResponse}
                          </Box>
                        </AccordionDetails>
                      </Accordion>
                    )}

                    {/* Database Information */}
                    {(detailedInfo.databaseName || detailedInfo.serverName) && (
                      <Accordion>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box display="flex" alignItems="center" gap={1}>
                            <DatabaseIcon />
                            <Typography variant="subtitle2">Database Information</Typography>
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box display="flex" flexDirection="column" gap={1}>
                            {detailedInfo.databaseName && (
                              <Box>
                                <strong>Database:</strong> {detailedInfo.databaseName}
                              </Box>
                            )}
                            {detailedInfo.serverName && (
                              <Box>
                                <strong>Server:</strong> {detailedInfo.serverName}
                              </Box>
                            )}
                            {detailedInfo.sessionId && (
                              <Box>
                                <strong>Session ID:</strong> {detailedInfo.sessionId}
                              </Box>
                            )}
                          </Box>
                        </AccordionDetails>
                      </Accordion>
                    )}
                  </Box>
                )}
              </Box>
            </Collapse>

            <Typography variant="caption" color="text.secondary">
              {format(entry.timestamp, 'HH:mm:ss')}
            </Typography>
          </Box>
        }
      />
    </ListItem>
  );
};

export default LogEntry;

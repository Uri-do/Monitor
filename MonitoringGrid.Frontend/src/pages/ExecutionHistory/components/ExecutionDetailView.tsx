import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  Person as PersonIcon,
  Timer as TimerIcon,
  Storage as DatabaseIcon,
  ExpandMore as ExpandMoreIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ExecutionHistoryDetailDto } from '@/types/api';

interface ExecutionDetailViewProps {
  execution: ExecutionHistoryDetailDto;
}

export const ExecutionDetailView: React.FC<ExecutionDetailViewProps> = ({ execution }) => {
  return (
    <Box sx={{ mt: 2 }}>
      <Grid container spacing={3}>
        {/* Basic Information */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <PersonIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Basic Information
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box>
                  <strong>KPI:</strong> {execution.indicator}
                </Box>
                <Box>
                  <strong>Owner:</strong> {execution.kpiOwner}
                </Box>
                <Box>
                  <strong>Stored Procedure:</strong> {execution.spName}
                </Box>
                <Box>
                  <strong>Executed By:</strong> {execution.executedBy || 'System'}
                </Box>
                <Box>
                  <strong>Method:</strong> {execution.executionMethod || 'Unknown'}
                </Box>
                <Box>
                  <strong>Timestamp:</strong> {format(new Date(execution.timestamp), 'PPpp')}
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Performance Metrics */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <TimerIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Performance Metrics
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box>
                  <strong>Current Value:</strong> {execution.currentValue?.toFixed(2) || 'N/A'}
                </Box>
                <Box>
                  <strong>Historical Value:</strong>{' '}
                  {execution.historicalValue?.toFixed(2) || 'N/A'}
                </Box>
                <Box>
                  <strong>Deviation:</strong> {execution.deviationPercent?.toFixed(2) || 'N/A'}%
                </Box>
                <Box>
                  <strong>Execution Time:</strong>{' '}
                  {execution.executionTimeMs ? `${execution.executionTimeMs}ms` : 'N/A'}
                </Box>
                <Box>
                  <strong>Performance:</strong>
                  <Chip
                    label={execution.performanceCategory}
                    size="small"
                    color={execution.performanceCategory === 'Fast' ? 'success' : 'warning'}
                    sx={{ ml: 1 }}
                  />
                </Box>
                <Box>
                  <strong>Status:</strong>
                  <Chip
                    label={execution.isSuccessful ? 'Success' : 'Failed'}
                    size="small"
                    color={execution.isSuccessful ? 'success' : 'error'}
                    sx={{ ml: 1 }}
                  />
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Technical Details */}
        <Grid item xs={12}>
          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="h6">
                <DatabaseIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Technical Details
              </Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>
                    Database Information
                  </Typography>
                  <Box
                    sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}
                  >
                    <Box>
                      <strong>Database:</strong> {execution.databaseName || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Server:</strong> {execution.serverName || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Session ID:</strong> {execution.sessionId || 'N/A'}
                    </Box>
                    <Box>
                      <strong>IP Address:</strong> {execution.ipAddress || 'N/A'}
                    </Box>
                    <Box>
                      <strong>User Agent:</strong> {execution.userAgent || 'N/A'}
                    </Box>
                  </Box>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>
                    Execution Context
                  </Typography>
                  <Box
                    sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}
                  >
                    <Box>
                      <strong>Request ID:</strong> {execution.requestId || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Correlation ID:</strong> {execution.correlationId || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Environment:</strong> {execution.environment || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Version:</strong> {execution.applicationVersion || 'N/A'}
                    </Box>
                  </Box>
                </Grid>
              </Grid>

              {/* Error Details */}
              {!execution.isSuccessful && execution.errorMessage && (
                <Box sx={{ mt: 3 }}>
                  <Typography variant="subtitle2" gutterBottom color="error">
                    Error Information
                  </Typography>
                  <Box
                    sx={{
                      p: 2,
                      backgroundColor: 'error.50',
                      borderRadius: 1,
                      border: '1px solid',
                      borderColor: 'error.200',
                    }}
                  >
                    <Typography variant="body2" color="error.main">
                      {execution.errorMessage}
                    </Typography>
                    {execution.stackTrace && (
                      <Box sx={{ mt: 2 }}>
                        <Typography variant="caption" color="error.main">
                          Stack Trace:
                        </Typography>
                        <Box
                          component="pre"
                          sx={{
                            fontSize: '0.75rem',
                            fontFamily: 'monospace',
                            whiteSpace: 'pre-wrap',
                            maxHeight: 200,
                            overflow: 'auto',
                            mt: 1,
                            p: 1,
                            backgroundColor: 'error.100',
                            borderRadius: 0.5,
                          }}
                        >
                          {execution.stackTrace}
                        </Box>
                      </Box>
                    )}
                  </Box>
                </Box>
              )}

              {/* Raw Data */}
              {execution.rawData && (
                <Box sx={{ mt: 3 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Raw Execution Data
                  </Typography>
                  <Box
                    component="pre"
                    sx={{
                      fontSize: '0.75rem',
                      fontFamily: 'monospace',
                      whiteSpace: 'pre-wrap',
                      maxHeight: 300,
                      overflow: 'auto',
                      p: 2,
                      backgroundColor: 'grey.50',
                      borderRadius: 1,
                      border: '1px solid',
                      borderColor: 'grey.300',
                    }}
                  >
                    {JSON.stringify(execution.rawData, null, 2)}
                  </Box>
                </Box>
              )}
            </AccordionDetails>
          </Accordion>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ExecutionDetailView;

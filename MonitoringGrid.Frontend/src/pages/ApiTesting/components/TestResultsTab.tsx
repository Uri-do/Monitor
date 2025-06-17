import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Alert,
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Paper,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails
} from '@mui/material';
import {
  Refresh,
  ExpandMore,
  CheckCircle,
  Error,
  Warning,
  Info
} from '@mui/icons-material';
import { TestExecutionResult } from '../../../types/testing';

interface TestResultsTabProps {
  testResults: TestExecutionResult[];
  onClearResults: () => void;
}

export const TestResultsTab: React.FC<TestResultsTabProps> = ({
  testResults,
  onClearResults
}) => {
  const getStatusIcon = (isSuccess: boolean, statusCode: number) => {
    if (isSuccess) {
      return <CheckCircle color="success" />;
    } else if (statusCode >= 500) {
      return <Error color="error" />;
    } else if (statusCode >= 400) {
      return <Warning color="warning" />;
    } else {
      return <Info color="info" />;
    }
  };

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="h6">
                Test Results ({testResults.length})
              </Typography>
              <Button
                variant="outlined"
                onClick={onClearResults}
                startIcon={<Refresh />}
              >
                Clear Results
              </Button>
            </Box>

            {testResults.length === 0 ? (
              <Alert severity="info">
                No test results yet. Run some tests to see results here.
              </Alert>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Status</TableCell>
                      <TableCell>Test Name</TableCell>
                      <TableCell>Method</TableCell>
                      <TableCell>Path</TableCell>
                      <TableCell>Status Code</TableCell>
                      <TableCell>Duration</TableCell>
                      <TableCell>Executed At</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {testResults.map((result, index) => (
                      <TableRow key={index} hover>
                        <TableCell>
                          {getStatusIcon(result.isSuccess, result.statusCode)}
                        </TableCell>
                        <TableCell>{result.testName}</TableCell>
                        <TableCell>
                          <Chip 
                            label={result.method} 
                            size="small" 
                            color={result.method === 'GET' ? 'primary' : result.method === 'POST' ? 'success' : 'default'}
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace">
                            {result.path}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip 
                            label={result.statusCode} 
                            size="small" 
                            color={result.isSuccess ? 'success' : 'error'}
                          />
                        </TableCell>
                        <TableCell>{result.testDuration}ms</TableCell>
                        <TableCell>
                          {new Date(result.executedAt).toLocaleString()}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}

            {testResults.length > 0 && (
              <Box sx={{ mt: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Detailed Results
                </Typography>
                {testResults.map((result, index) => (
                  <Accordion key={index}>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                      <Box display="flex" alignItems="center" gap={1}>
                        {getStatusIcon(result.isSuccess, result.statusCode)}
                        <Typography>
                          {result.testName} - {result.method} {result.path}
                        </Typography>
                      </Box>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Grid container spacing={2}>
                        <Grid item xs={12} md={6}>
                          <Typography variant="subtitle2" gutterBottom>
                            Request Details
                          </Typography>
                          <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
                            Method: {result.method}{'\n'}
                            Path: {result.path}{'\n'}
                            Status Code: {result.statusCode}{'\n'}
                            Duration: {result.testDuration}ms{'\n'}
                            Success: {result.isSuccess ? 'Yes' : 'No'}
                          </Typography>
                        </Grid>
                        <Grid item xs={12} md={6}>
                          <Typography variant="subtitle2" gutterBottom>
                            Response Body
                          </Typography>
                          <Typography 
                            variant="body2" 
                            component="pre" 
                            sx={{ 
                              whiteSpace: 'pre-wrap',
                              backgroundColor: 'grey.100',
                              p: 1,
                              borderRadius: 1,
                              maxHeight: 200,
                              overflow: 'auto'
                            }}
                          >
                            {result.responseBody || 'No response body'}
                          </Typography>
                        </Grid>
                        {result.errorMessage && (
                          <Grid item xs={12}>
                            <Typography variant="subtitle2" gutterBottom color="error">
                              Error Message
                            </Typography>
                            <Typography 
                              variant="body2" 
                              color="error"
                              sx={{ 
                                backgroundColor: 'error.light',
                                p: 1,
                                borderRadius: 1,
                                color: 'error.contrastText'
                              }}
                            >
                              {result.errorMessage}
                            </Typography>
                          </Grid>
                        )}
                        {result.performanceMetrics && (
                          <Grid item xs={12}>
                            <Typography variant="subtitle2" gutterBottom>
                              Performance Metrics
                            </Typography>
                            <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
                              Response Time: {result.performanceMetrics.responseTime}ms{'\n'}
                              Response Size: {result.performanceMetrics.responseSizeBytes} bytes{'\n'}
                              {result.performanceMetrics.requestsPerSecond && `Requests/sec: ${result.performanceMetrics.requestsPerSecond}`}
                            </Typography>
                          </Grid>
                        )}
                      </Grid>
                    </AccordionDetails>
                  </Accordion>
                ))}
              </Box>
            )}
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
};

import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Alert,
  CircularProgress,
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Paper,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  CheckCircle,
  Error,
  Warning,
  Info,
  Visibility
} from '@mui/icons-material';
import { TestHistoryResponse } from '../../../types/testing';

interface HistoryTabProps {
  history: TestHistoryResponse | undefined;
  isLoading: boolean;
  onViewDetails?: (executionId: string) => void;
}

export const HistoryTab: React.FC<HistoryTabProps> = ({
  history,
  isLoading,
  onViewDetails
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

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="300px">
        <CircularProgress />
        <Typography variant="h6" sx={{ ml: 2 }}>Loading test history...</Typography>
      </Box>
    );
  }

  if (!history || history.executions.length === 0) {
    return (
      <Alert severity="info">
        No test history available yet. Run some tests to see history here.
      </Alert>
    );
  }

  return (
    <Grid container spacing={3}>
      {/* Summary Statistics */}
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Test History Summary
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="primary">
                    {history.totalExecutions}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Executions
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="success.main">
                    {history.executions.filter(e => e.isSuccess).length}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Successful
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="error.main">
                    {history.executions.filter(e => !e.isSuccess).length}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Failed
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="info.main">
                    {((history.executions.filter(e => e.isSuccess).length / history.executions.length) * 100).toFixed(1)}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Success Rate
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      </Grid>

      {/* Test History Table */}
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Recent Test Executions
            </Typography>
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
                    <TableCell>Executed By</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {history.executions.map((execution, index) => (
                    <TableRow key={index} hover>
                      <TableCell>
                        {getStatusIcon(execution.isSuccess, execution.statusCode)}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {execution.testName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={execution.method} 
                          size="small" 
                          color={execution.method === 'GET' ? 'primary' : execution.method === 'POST' ? 'success' : 'default'}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" fontFamily="monospace">
                          {execution.path}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={execution.statusCode} 
                          size="small" 
                          color={execution.isSuccess ? 'success' : 'error'}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {execution.testDuration}ms
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {new Date(execution.executedAt).toLocaleString()}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {execution.executedBy || 'System'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {onViewDetails && (
                          <Tooltip title="View Details">
                            <IconButton
                              size="small"
                              onClick={() => onViewDetails(execution.testName)}
                            >
                              <Visibility />
                            </IconButton>
                          </Tooltip>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>

            {/* Pagination Info */}
            <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                Showing {history.executions.length} of {history.totalExecutions} executions
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Page {history.page} of {Math.ceil(history.totalExecutions / history.pageSize)}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
};

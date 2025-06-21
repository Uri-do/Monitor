import React, { useState } from 'react';
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
  IconButton,
  Collapse,
  Alert,
  Grid,
  Stack,
  Divider,
  Button,
  TextField,
  InputAdornment,
} from '@mui/material';
import {
  CheckCircle as PassIcon,
  Error as FailIcon,
  Schedule as PendingIcon,
  ExpandMore as ExpandIcon,
  ExpandLess as CollapseIcon,
  Search as SearchIcon,
  Download as ExportIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { TestSuiteStatus, TestResult } from '../TestSuitePage';

interface TestResultsProps {
  testResults: TestResult[];
  testStatus: TestSuiteStatus;
}

const TestResults: React.FC<TestResultsProps> = ({ testResults, testStatus }) => {
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const toggleRowExpansion = (testId: string) => {
    const newExpanded = new Set(expandedRows);
    if (newExpanded.has(testId)) {
      newExpanded.delete(testId);
    } else {
      newExpanded.add(testId);
    }
    setExpandedRows(newExpanded);
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'passed':
        return <PassIcon color="success" fontSize="small" />;
      case 'failed':
        return <FailIcon color="error" fontSize="small" />;
      case 'running':
        return <PendingIcon color="info" fontSize="small" />;
      default:
        return <PendingIcon color="disabled" fontSize="small" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'passed':
        return 'success';
      case 'failed':
        return 'error';
      case 'running':
        return 'info';
      default:
        return 'default';
    }
  };

  const filteredResults = testResults.filter(test => {
    const matchesSearch = test.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         test.category.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === 'all' || test.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const exportResults = () => {
    const csvContent = [
      ['Test Name', 'Category', 'Status', 'Duration (ms)', 'Message'].join(','),
      ...filteredResults.map(test => [
        test.name,
        test.category,
        test.status,
        test.duration.toString(),
        test.message || ''
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `test-results-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const getTestSummary = () => {
    const total = testResults.length;
    const passed = testResults.filter(t => t.status === 'passed').length;
    const failed = testResults.filter(t => t.status === 'failed').length;
    const running = testResults.filter(t => t.status === 'running').length;
    const pending = total - passed - failed - running;

    return { total, passed, failed, running, pending };
  };

  const summary = getTestSummary();

  return (
    <Box>
      {/* Summary Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="text.primary">
                {summary.total}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Total Tests
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="success.main">
                {summary.passed}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Passed
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="error.main">
                {summary.failed}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Failed
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="info.main">
                {summary.running}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Running
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters and Actions */}
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
            <TextField
              placeholder="Search tests..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              size="small"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
              sx={{ flexGrow: 1 }}
            />
            
            <TextField
              select
              label="Status"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              size="small"
              sx={{ minWidth: 120 }}
              SelectProps={{ native: true }}
            >
              <option value="all">All Status</option>
              <option value="passed">Passed</option>
              <option value="failed">Failed</option>
              <option value="running">Running</option>
            </TextField>

            <Button
              variant="outlined"
              startIcon={<ExportIcon />}
              onClick={exportResults}
              disabled={filteredResults.length === 0}
            >
              Export CSV
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {/* Results Table */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Test Results ({filteredResults.length})
          </Typography>

          {filteredResults.length === 0 ? (
            <Alert severity="info">
              {testResults.length === 0 
                ? "No tests have been run yet."
                : "No tests match the current filters."
              }
            </Alert>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell width="40px"></TableCell>
                    <TableCell>Test Name</TableCell>
                    <TableCell>Category</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell align="right">Duration</TableCell>
                    <TableCell width="40px"></TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredResults.map((test) => (
                    <React.Fragment key={test.id}>
                      <TableRow hover>
                        <TableCell>
                          {getStatusIcon(test.status)}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontWeight="medium">
                            {test.name}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip size="small" label={test.category} variant="outlined" />
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={test.status}
                            color={getStatusColor(test.status) as any}
                          />
                        </TableCell>
                        <TableCell align="right">
                          {test.duration > 0 ? `${test.duration}ms` : '-'}
                        </TableCell>
                        <TableCell>
                          {(test.message || test.stackTrace) && (
                            <IconButton
                              size="small"
                              onClick={() => toggleRowExpansion(test.id)}
                            >
                              {expandedRows.has(test.id) ? <CollapseIcon /> : <ExpandIcon />}
                            </IconButton>
                          )}
                        </TableCell>
                      </TableRow>
                      
                      {(test.message || test.stackTrace) && (
                        <TableRow>
                          <TableCell colSpan={6} sx={{ py: 0 }}>
                            <Collapse in={expandedRows.has(test.id)}>
                              <Box sx={{ p: 2, bgcolor: 'grey.50' }}>
                                {test.message && (
                                  <Box sx={{ mb: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary">
                                      Message:
                                    </Typography>
                                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                      {test.message}
                                    </Typography>
                                  </Box>
                                )}
                                {test.stackTrace && (
                                  <Box>
                                    <Typography variant="subtitle2" color="text.secondary">
                                      Stack Trace:
                                    </Typography>
                                    <Typography 
                                      variant="body2" 
                                      sx={{ 
                                        fontFamily: 'monospace', 
                                        fontSize: '0.75rem',
                                        whiteSpace: 'pre-wrap',
                                        bgcolor: 'grey.100',
                                        p: 1,
                                        borderRadius: 1,
                                        maxHeight: 200,
                                        overflow: 'auto'
                                      }}
                                    >
                                      {test.stackTrace}
                                    </Typography>
                                  </Box>
                                )}
                              </Box>
                            </Collapse>
                          </TableCell>
                        </TableRow>
                      )}
                    </React.Fragment>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default TestResults;

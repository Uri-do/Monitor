import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  LinearProgress,
  Stack,
  Chip,
  Divider,
  Alert,
} from '@mui/material';
import {
  Speed as PerformanceIcon,
  Timer as DurationIcon,
  TrendingUp as TrendIcon,
  Assessment as MetricsIcon,
} from '@mui/icons-material';
import { TestSuiteStatus, TestResult } from '../TestSuitePage';

interface TestMetricsProps {
  testResults: TestResult[];
  testStatus: TestSuiteStatus;
}

const TestMetrics: React.FC<TestMetricsProps> = ({ testResults, testStatus }) => {
  const calculateMetrics = () => {
    if (testResults.length === 0) {
      return {
        totalDuration: 0,
        averageDuration: 0,
        fastestTest: null,
        slowestTest: null,
        successRate: 0,
        categoryBreakdown: {},
        performanceTests: [],
      };
    }

    const completedTests = testResults.filter(t => t.status === 'passed' || t.status === 'failed');
    const totalDuration = completedTests.reduce((sum, test) => sum + test.duration, 0);
    const averageDuration = completedTests.length > 0 ? totalDuration / completedTests.length : 0;
    
    const sortedByDuration = [...completedTests].sort((a, b) => a.duration - b.duration);
    const fastestTest = sortedByDuration[0] || null;
    const slowestTest = sortedByDuration[sortedByDuration.length - 1] || null;
    
    const passedTests = testResults.filter(t => t.status === 'passed').length;
    const successRate = testResults.length > 0 ? (passedTests / testResults.length) * 100 : 0;
    
    const categoryBreakdown = testResults.reduce((acc, test) => {
      if (!acc[test.category]) {
        acc[test.category] = { total: 0, passed: 0, failed: 0 };
      }
      acc[test.category].total++;
      if (test.status === 'passed') acc[test.category].passed++;
      if (test.status === 'failed') acc[test.category].failed++;
      return acc;
    }, {} as Record<string, { total: number; passed: number; failed: number }>);

    const performanceTests = testResults.filter(t => t.category === 'Performance');

    return {
      totalDuration,
      averageDuration,
      fastestTest,
      slowestTest,
      successRate,
      categoryBreakdown,
      performanceTests,
    };
  };

  const metrics = calculateMetrics();

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  };

  const getSuccessRateColor = (rate: number) => {
    if (rate >= 90) return 'success';
    if (rate >= 70) return 'warning';
    return 'error';
  };

  return (
    <Box>
      {testResults.length === 0 ? (
        <Alert severity="info">
          No test metrics available. Run some tests to see performance metrics and analysis.
        </Alert>
      ) : (
        <Grid container spacing={3}>
          {/* Overall Metrics */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
                  <MetricsIcon color="primary" />
                  <Typography variant="h6">Overall Metrics</Typography>
                </Stack>

                <Stack spacing={2}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Success Rate
                    </Typography>
                    <Box display="flex" alignItems="center" gap={1}>
                      <LinearProgress
                        variant="determinate"
                        value={metrics.successRate}
                        color={getSuccessRateColor(metrics.successRate)}
                        sx={{ flexGrow: 1, height: 8, borderRadius: 4 }}
                      />
                      <Typography variant="body2" fontWeight="medium">
                        {metrics.successRate.toFixed(1)}%
                      </Typography>
                    </Box>
                  </Box>

                  <Divider />

                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Total Duration
                      </Typography>
                      <Typography variant="h6">
                        {formatDuration(metrics.totalDuration)}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Average Duration
                      </Typography>
                      <Typography variant="h6">
                        {formatDuration(metrics.averageDuration)}
                      </Typography>
                    </Grid>
                  </Grid>

                  <Divider />

                  {metrics.fastestTest && (
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Fastest Test
                      </Typography>
                      <Typography variant="body2" fontWeight="medium">
                        {metrics.fastestTest.name}
                      </Typography>
                      <Typography variant="caption" color="success.main">
                        {formatDuration(metrics.fastestTest.duration)}
                      </Typography>
                    </Box>
                  )}

                  {metrics.slowestTest && (
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Slowest Test
                      </Typography>
                      <Typography variant="body2" fontWeight="medium">
                        {metrics.slowestTest.name}
                      </Typography>
                      <Typography variant="caption" color="warning.main">
                        {formatDuration(metrics.slowestTest.duration)}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </CardContent>
            </Card>
          </Grid>

          {/* Category Breakdown */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
                  <TrendIcon color="primary" />
                  <Typography variant="h6">Category Breakdown</Typography>
                </Stack>

                <Stack spacing={2}>
                  {Object.entries(metrics.categoryBreakdown).map(([category, stats]) => (
                    <Box key={category}>
                      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
                        <Typography variant="body2" fontWeight="medium">
                          {category}
                        </Typography>
                        <Chip
                          size="small"
                          label={`${stats.passed}/${stats.total}`}
                          color={stats.passed === stats.total ? 'success' : stats.failed > 0 ? 'error' : 'default'}
                        />
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={stats.total > 0 ? (stats.passed / stats.total) * 100 : 0}
                        color={stats.passed === stats.total ? 'success' : stats.failed > 0 ? 'error' : 'primary'}
                        sx={{ height: 6, borderRadius: 3 }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        {stats.total > 0 ? ((stats.passed / stats.total) * 100).toFixed(1) : 0}% success rate
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          </Grid>

          {/* Performance Analysis */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
                  <PerformanceIcon color="primary" />
                  <Typography variant="h6">Performance Analysis</Typography>
                </Stack>

                {metrics.performanceTests.length === 0 ? (
                  <Alert severity="info">
                    No performance tests found. Performance tests help identify bottlenecks and ensure optimal response times.
                  </Alert>
                ) : (
                  <Grid container spacing={2}>
                    {metrics.performanceTests.map((test) => (
                      <Grid item xs={12} sm={6} md={4} key={test.id}>
                        <Card variant="outlined">
                          <CardContent sx={{ py: 2 }}>
                            <Typography variant="body2" fontWeight="medium" noWrap>
                              {test.name}
                            </Typography>
                            <Stack direction="row" alignItems="center" spacing={1} sx={{ mt: 1 }}>
                              <DurationIcon fontSize="small" color="action" />
                              <Typography variant="body2">
                                {formatDuration(test.duration)}
                              </Typography>
                              <Chip
                                size="small"
                                label={test.status}
                                color={test.status === 'passed' ? 'success' : test.status === 'failed' ? 'error' : 'default'}
                              />
                            </Stack>
                            {test.duration > 0 && (
                              <Box sx={{ mt: 1 }}>
                                <Typography variant="caption" color="text.secondary">
                                  Performance: {test.duration < 100 ? 'Excellent' : test.duration < 500 ? 'Good' : test.duration < 1000 ? 'Fair' : 'Needs Improvement'}
                                </Typography>
                              </Box>
                            )}
                          </CardContent>
                        </Card>
                      </Grid>
                    ))}
                  </Grid>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Test Execution Timeline */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Test Execution Summary
                </Typography>
                
                <Grid container spacing={3}>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="primary">
                        {testResults.length}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Total Tests
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="success.main">
                        {testResults.filter(t => t.status === 'passed').length}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Passed
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="error.main">
                        {testResults.filter(t => t.status === 'failed').length}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Failed
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="text.primary">
                        {formatDuration(metrics.totalDuration)}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Total Time
                      </Typography>
                    </Box>
                  </Grid>
                </Grid>

                {testStatus.lastRun && (
                  <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
                    <Typography variant="body2" color="text.secondary">
                      Last executed: {testStatus.lastRun.toLocaleString()}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
    </Box>
  );
};

export default TestMetrics;

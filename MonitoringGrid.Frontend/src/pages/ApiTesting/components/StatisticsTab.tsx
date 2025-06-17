import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Alert,
  CircularProgress,
  Chip,
  LinearProgress
} from '@mui/material';
import { TestStatistics } from '../../../types/testing';

interface StatisticsTabProps {
  statistics: TestStatistics | undefined;
  isLoading: boolean;
}

export const StatisticsTab: React.FC<StatisticsTabProps> = ({
  statistics,
  isLoading
}) => {
  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="300px">
        <CircularProgress />
        <Typography variant="h6" sx={{ ml: 2 }}>Loading statistics...</Typography>
      </Box>
    );
  }

  if (!statistics) {
    return (
      <Alert severity="info">
        No statistics available yet. Run some tests to see statistics here.
      </Alert>
    );
  }

  const { overview, performance, controllerStats, endpointStats } = statistics;

  return (
    <Grid container spacing={3}>
      {/* Overview Statistics */}
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Test Overview
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="primary">
                    {overview.totalTests}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Tests
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="success.main">
                    {overview.successfulTests}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Successful
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="error.main">
                    {overview.failedTests}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Failed
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="info.main">
                    {overview.successRate.toFixed(1)}%
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

      {/* Performance Statistics */}
      <Grid item xs={12} md={6}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Performance Metrics
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Average Response Time
              </Typography>
              <Typography variant="h5">
                {performance.averageResponseTime}ms
              </Typography>
            </Box>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Median Response Time
              </Typography>
              <Typography variant="h6">
                {performance.medianResponseTime}ms
              </Typography>
            </Box>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                95th Percentile
              </Typography>
              <Typography variant="h6">
                {performance.p95ResponseTime}ms
              </Typography>
            </Box>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Requests per Second
              </Typography>
              <Typography variant="h6">
                {performance.requestsPerSecond.toFixed(2)}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* Endpoint Coverage */}
      <Grid item xs={12} md={6}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Endpoint Coverage
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Total Endpoints: {overview.totalEndpoints}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Tested Endpoints: {overview.testedEndpoints}
              </Typography>
              <LinearProgress 
                variant="determinate" 
                value={overview.endpointCoverage} 
                sx={{ mt: 1, mb: 1 }}
              />
              <Typography variant="body2" color="text.secondary">
                Coverage: {overview.endpointCoverage.toFixed(1)}%
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* Controller Statistics */}
      {controllerStats.length > 0 && (
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Controller Statistics
              </Typography>
              <Grid container spacing={2}>
                {controllerStats.map((controller, index) => (
                  <Grid item xs={12} sm={6} md={4} key={index}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle1" gutterBottom>
                          {controller.controllerName}
                        </Typography>
                        <Box display="flex" justifyContent="space-between" mb={1}>
                          <Typography variant="body2">Total Tests:</Typography>
                          <Typography variant="body2">{controller.totalTests}</Typography>
                        </Box>
                        <Box display="flex" justifyContent="space-between" mb={1}>
                          <Typography variant="body2">Success Rate:</Typography>
                          <Chip 
                            label={`${controller.successRate.toFixed(1)}%`}
                            size="small"
                            color={controller.successRate >= 90 ? 'success' : controller.successRate >= 70 ? 'warning' : 'error'}
                          />
                        </Box>
                        <Box display="flex" justifyContent="space-between">
                          <Typography variant="body2">Avg Response:</Typography>
                          <Typography variant="body2">{controller.averageResponseTime}ms</Typography>
                        </Box>
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      )}

      {/* Top Performing Endpoints */}
      {endpointStats.length > 0 && (
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Endpoint Performance
            </Typography>
            <Grid container spacing={2}>
              {endpointStats.slice(0, 6).map((endpoint, index) => (
                <Grid item xs={12} sm={6} md={4} key={index}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography variant="subtitle2" gutterBottom>
                        <Chip 
                          label={endpoint.method} 
                          size="small" 
                          color={endpoint.method === 'GET' ? 'primary' : 'default'}
                          sx={{ mr: 1 }}
                        />
                        {endpoint.path}
                      </Typography>
                      <Box display="flex" justifyContent="space-between" mb={1}>
                        <Typography variant="body2">Tests:</Typography>
                        <Typography variant="body2">{endpoint.totalTests}</Typography>
                      </Box>
                      <Box display="flex" justifyContent="space-between" mb={1}>
                        <Typography variant="body2">Success:</Typography>
                        <Chip 
                          label={`${endpoint.successRate.toFixed(1)}%`}
                          size="small"
                          color={endpoint.successRate >= 90 ? 'success' : endpoint.successRate >= 70 ? 'warning' : 'error'}
                        />
                      </Box>
                      <Box display="flex" justifyContent="space-between">
                        <Typography variant="body2">Avg Time:</Typography>
                        <Typography variant="body2">{endpoint.averageResponseTime}ms</Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </CardContent>
        </Card>
      </Grid>
      )}
    </Grid>
  );
};

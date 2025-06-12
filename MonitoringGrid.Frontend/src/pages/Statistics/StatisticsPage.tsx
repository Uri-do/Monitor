import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Grid,
  Card,
  CardContent,
  CardActions,
  Alert,
  Chip,
  CircularProgress,
} from '@mui/material';
import {
  Analytics as AnalyticsIcon,
  BarChart as BarChartIcon,
  Timeline as TimelineIcon,
  Assessment as AssessmentIcon,
  TrendingUp,
  DataUsage,
} from '@mui/icons-material';
import { PageHeader } from '@/components/UI';
import { StatisticsBrowser, StatisticsBrowserButton } from '@/components';
import { useActiveCollectors } from '@/hooks/useMonitorStatistics';

const StatisticsPage: React.FC = () => {
  const [browserOpen, setBrowserOpen] = useState(false);

  const {
    data: collectors = [],
    isLoading: collectorsLoading,
    error: collectorsError,
  } = useActiveCollectors();

  // Calculate summary statistics
  const summary = React.useMemo(() => {
    if (!collectors.length) {
      return {
        totalCollectors: 0,
        activeCollectors: 0,
        collectorsWithData: 0,
        totalRecords: 0,
      };
    }

    return {
      totalCollectors: collectors.length,
      activeCollectors: collectors.filter(c => c.isActiveStatus).length,
      collectorsWithData: collectors.filter(c => c.statisticsCount > 0).length,
      totalRecords: collectors.reduce((sum, c) => sum + (c.statisticsCount || 0), 0),
    };
  }, [collectors]);

  const handleOpenBrowser = () => {
    setBrowserOpen(true);
  };

  const handleCloseBrowser = () => {
    setBrowserOpen(false);
  };

  return (
    <Box>
      <PageHeader
        title="Statistics & Analytics"
        subtitle="Browse and analyze collector statistics and performance data"
        actions={[
          {
            label: 'Open Statistics Browser',
            icon: <AnalyticsIcon />,
            onClick: handleOpenBrowser,
            variant: 'contained',
          },
        ]}
      />

      {/* Summary Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <AssessmentIcon color="primary" sx={{ fontSize: 48, mb: 1 }} />
              <Typography variant="h4" gutterBottom>
                {collectorsLoading ? '...' : summary.totalCollectors}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total Collectors
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <TrendingUp color="success" sx={{ fontSize: 48, mb: 1 }} />
              <Typography variant="h4" gutterBottom>
                {collectorsLoading ? '...' : summary.activeCollectors}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Active Collectors
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <DataUsage color="info" sx={{ fontSize: 48, mb: 1 }} />
              <Typography variant="h4" gutterBottom>
                {collectorsLoading ? '...' : summary.collectorsWithData}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                With Statistics
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <BarChartIcon color="warning" sx={{ fontSize: 48, mb: 1 }} />
              <Typography variant="h4" gutterBottom>
                {collectorsLoading ? '...' : summary.totalRecords.toLocaleString()}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total Records
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Error State */}
      {collectorsError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load collectors: {collectorsError.message}
        </Alert>
      )}

      {/* Main Content */}
      <Grid container spacing={3}>
        {/* Statistics Browser Card */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <AnalyticsIcon color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Statistics Browser</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" paragraph>
                Browse and analyze statistics for all collectors. View trends, compare performance,
                and drill down into specific items and time periods.
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                <Chip label="Interactive Charts" size="small" />
                <Chip label="Data Tables" size="small" />
                <Chip label="Item Breakdown" size="small" />
              </Box>
            </CardContent>
            <CardActions>
              <Button
                variant="contained"
                startIcon={<AnalyticsIcon />}
                onClick={handleOpenBrowser}
                fullWidth
              >
                Open Statistics Browser
              </Button>
            </CardActions>
          </Card>
        </Grid>

        {/* Quick Access Card */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <TimelineIcon color="secondary" sx={{ mr: 1 }} />
                <Typography variant="h6">Quick Access</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" paragraph>
                Access statistics directly from collector selectors throughout the application.
                Look for the analytics icon next to collector dropdowns.
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                <Chip label="Indicator Creation" size="small" />
                <Chip label="Monitor Test Page" size="small" />
                <Chip label="Embedded Views" size="small" />
              </Box>
            </CardContent>
            <CardActions>
              <StatisticsBrowserButton
                variant="button"
                size="medium"
              />
            </CardActions>
          </Card>
        </Grid>

        {/* Features Overview */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Available Features
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center', p: 2 }}>
                    <BarChartIcon color="primary" sx={{ fontSize: 32, mb: 1 }} />
                    <Typography variant="subtitle2" gutterBottom>
                      Multiple Chart Types
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Bar, Line, Pie charts and data tables
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center', p: 2 }}>
                    <TimelineIcon color="primary" sx={{ fontSize: 32, mb: 1 }} />
                    <Typography variant="subtitle2" gutterBottom>
                      Time-based Analysis
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Daily totals, averages, and hourly breakdowns
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center', p: 2 }}>
                    <AssessmentIcon color="primary" sx={{ fontSize: 32, mb: 1 }} />
                    <Typography variant="subtitle2" gutterBottom>
                      Item Filtering
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      View all items or filter by specific item
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center', p: 2 }}>
                    <DataUsage color="primary" sx={{ fontSize: 32, mb: 1 }} />
                    <Typography variant="subtitle2" gutterBottom>
                      Flexible Date Ranges
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      1-90 days back with custom date selection
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Statistics Browser Dialog */}
      <StatisticsBrowser
        open={browserOpen}
        onClose={handleCloseBrowser}
      />
    </Box>
  );
};

export default StatisticsPage;

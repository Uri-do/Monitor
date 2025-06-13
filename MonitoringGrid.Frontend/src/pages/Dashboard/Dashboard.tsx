import React, { useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useIndicatorDashboard } from '@/hooks/useIndicators';
import { useAlertDashboard } from '@/hooks/useAlerts';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { useRealtime } from '@/contexts/RealtimeContext';

// Dashboard Components
import DashboardHeader from './components/DashboardHeader';
import IndicatorOverviewCards from './components/IndicatorOverviewCards';
import IndicatorsDueCard from './components/IndicatorsDueCard';
import RunningIndicatorsCard from './components/RunningIndicatorsCard';
import NextIndicatorExecutionCard from './components/NextIndicatorExecutionCard';
import RecentAlertsCard from './components/RecentAlertsCard';
import TopAlertingIndicatorsCard from './components/TopAlertingKpisCard';

const Dashboard: React.FC = () => {
  const { isEnabled: realtimeEnabled, enableRealtime } = useRealtime();
  const queryClient = useQueryClient();

  // Real-time dashboard state - only enabled when real-time features are active
  const realtimeDashboard = useRealtimeDashboard();

  // Auto-enable real-time on component mount
  useEffect(() => {
    if (!realtimeEnabled) {
      enableRealtime().catch(console.error);
    }
  }, [realtimeEnabled, enableRealtime]);

  // Always use real-time data since SignalR is working
  // TODO: Remove this temporary fix once RealtimeContext connection detection is fixed
  const dashboardState = realtimeDashboard;

  // Use enhanced hooks for dashboard data with optimized real-time updates
  const { data: indicatorDashboard, isLoading: indicatorLoading, refetch: refetchIndicator } = useIndicatorDashboard();

  const {
    data: alertDashboard,
    isLoading: alertLoading,
    refetch: refetchAlert,
  } = useAlertDashboard();

  // Merge real-time data with dashboard data (temporarily simplified)
  const mergedIndicatorDashboard = React.useMemo(() => {
    if (!indicatorDashboard) return indicatorDashboard;

    return {
      ...indicatorDashboard,
      // TODO: Add real-time indicator support when RealtimeDashboardState is updated
      runningIndicators: indicatorDashboard.runningIndicators || [],
      nextIndicatorDue: indicatorDashboard.nextIndicatorDue,
    };
  }, [indicatorDashboard]);

  const handleRefresh = () => {
    // Use enhanced query keys for cache invalidation
    queryClient.invalidateQueries({ queryKey: ['indicators', 'dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['alerts', 'dashboard'] });
    refetchIndicator();
    refetchAlert();
    dashboardState.refreshDashboard();
  };

  if (indicatorLoading || alertLoading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <LinearProgress />
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <DashboardHeader
        lastUpdate={indicatorDashboard?.lastUpdate}
        countdown={dashboardState.countdown}
        onRefresh={handleRefresh}
      />

      <Grid container spacing={3}>
        {/* Indicator Overview Cards */}
        <IndicatorOverviewCards
          indicatorDashboard={mergedIndicatorDashboard}
          alertDashboard={alertDashboard}
          indicatorLoading={indicatorLoading}
          alertLoading={alertLoading}
        />

        {/* Indicators Due for Execution - Moved to top */}
        <IndicatorsDueCard indicatorDashboard={mergedIndicatorDashboard} />

        {/* Running Indicators */}
        <RunningIndicatorsCard
          indicatorDashboard={mergedIndicatorDashboard}
          realtimeRunningIndicators={(dashboardState.runningIndicators || []).map(indicator => ({
            indicatorId: indicator.indicatorID,
            indicator: indicator.indicator,
            owner: indicator.owner,
            startTime: indicator.startTime,
            progress: indicator.progress,
            estimatedCompletion: indicator.estimatedCompletion,
            currentStep: indicator.currentStep,
            elapsedTime: indicator.elapsedTime,
          }))}
        />

        {/* Next Indicator Due */}
        <NextIndicatorExecutionCard
          indicatorDashboard={mergedIndicatorDashboard}
          countdown={dashboardState.countdown}
          isConnected={dashboardState.isConnected}
        />

        {/* Recent Alerts */}
        <RecentAlertsCard alertDashboard={alertDashboard} />

        {/* Top Alerting Indicators */}
        <TopAlertingIndicatorsCard alertDashboard={alertDashboard} />
      </Grid>
    </Box>
  );
};

export default Dashboard;

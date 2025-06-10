import React, { useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography } from '@mui/material';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { kpiApi, alertApi } from '@/services/api';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { useRealtime } from '@/contexts/RealtimeContext';

// Dashboard Components
import DashboardHeader from './components/DashboardHeader';
import KpiOverviewCards from './components/KpiOverviewCards';
import KpisDueCard from './components/KpisDueCard';
import RunningKpisCard from './components/RunningKpisCard';
import NextKpiExecutionCard from './components/NextKpiExecutionCard';
import RecentAlertsCard from './components/RecentAlertsCard';
import TopAlertingKpisCard from './components/TopAlertingKpisCard';

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

  // Fetch dashboard data with aggressive refresh for real-time updates
  const {
    data: kpiDashboard,
    isLoading: kpiLoading,
    refetch: refetchKpi,
  } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
    refetchInterval: 5000, // Refresh every 5 seconds for real-time data
    staleTime: 0, // Always consider data stale to force fresh fetches
    cacheTime: 5000, // Cache for only 5 seconds
    retry: 2, // Retry failed requests twice
    retryDelay: 1000, // Wait 1 second between retries
  });

  const {
    data: alertDashboard,
    isLoading: alertLoading,
    refetch: refetchAlert,
  } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 10000, // Refresh every 10 seconds
    staleTime: 0, // Always consider data stale
    cacheTime: 5000, // Cache for only 5 seconds
  });

  // Merge real-time data with dashboard data
  const mergedKpiDashboard = React.useMemo(() => {
    if (!kpiDashboard) return kpiDashboard;

    return {
      ...kpiDashboard,
      runningKpis: dashboardState.runningKpis.length > 0
        ? dashboardState.runningKpis.map(kpi => ({
            kpiId: kpi.kpiId,
            indicator: kpi.indicator,
            owner: kpi.owner,
            startTime: kpi.startTime,
            progress: kpi.progress,
            estimatedCompletion: kpi.estimatedCompletion,
          }))
        : kpiDashboard.runningKpis,
      nextKpiDue: dashboardState.nextKpiDue || kpiDashboard.nextKpiDue,
    };
  }, [kpiDashboard, dashboardState.runningKpis, dashboardState.nextKpiDue]);

  const handleRefresh = () => {
    // Clear query cache and force fresh data
    queryClient.invalidateQueries({ queryKey: ['kpi-dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['alert-dashboard'] });
    refetchKpi();
    refetchAlert();
    dashboardState.refreshDashboard();
  };

  if (kpiLoading || alertLoading) {
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
    <Box sx={{ maxWidth: '1400px', margin: '0 auto' }}>

      {/* Header */}
      <DashboardHeader
        lastUpdate={kpiDashboard?.lastUpdate}
        countdown={dashboardState.countdown}
        onRefresh={handleRefresh}
      />

      <Grid container spacing={3}>
        {/* KPI Overview Cards */}
        <KpiOverviewCards
          kpiDashboard={mergedKpiDashboard as any}
          alertDashboard={alertDashboard}
          kpiLoading={kpiLoading}
          alertLoading={alertLoading}
        />

        {/* KPIs Due for Execution - Moved to top */}
        <KpisDueCard kpiDashboard={mergedKpiDashboard as any} />

        {/* Running KPIs */}
        <RunningKpisCard
          kpiDashboard={mergedKpiDashboard as any}
          realtimeRunningKpis={dashboardState.runningKpis}
        />

        {/* Next KPI Due */}
        <NextKpiExecutionCard
          kpiDashboard={mergedKpiDashboard as any}
          countdown={dashboardState.countdown}
          isConnected={dashboardState.isConnected}
        />

        {/* Recent Alerts */}
        <RecentAlertsCard alertDashboard={alertDashboard} />

        {/* Top Alerting KPIs */}
        <TopAlertingKpisCard alertDashboard={alertDashboard} />
      </Grid>
    </Box>
  );
};

export default Dashboard;

import React, { useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useKpiDashboard } from '@/hooks/useKpis';
import { useAlertDashboard } from '@/hooks/useAlerts';
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

  // Use enhanced hooks for dashboard data with optimized real-time updates
  const {
    data: kpiDashboard,
    isLoading: kpiLoading,
    refetch: refetchKpi,
  } = useKpiDashboard();

  const {
    data: alertDashboard,
    isLoading: alertLoading,
    refetch: refetchAlert,
  } = useAlertDashboard();

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
    // Use enhanced query keys for cache invalidation
    queryClient.invalidateQueries({ queryKey: ['kpis', 'dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['alerts', 'dashboard'] });
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

import React, { useState, useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography, useTheme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { kpiApi, alertApi } from '@/services/api';
import WorkerDashboardCard from '@/components/Worker/WorkerDashboardCard';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';

// Dashboard Components
import DashboardHeader from './components/DashboardHeader';
import KpiOverviewCards from './components/KpiOverviewCards';
import KpisDueCard from './components/KpisDueCard';
import RunningKpisCard from './components/RunningKpisCard';
import NextKpiExecutionCard from './components/NextKpiExecutionCard';
import RecentAlertsCard from './components/RecentAlertsCard';
import TopAlertingKpisCard from './components/TopAlertingKpisCard';

const Dashboard: React.FC = () => {
  // Real-time dashboard state
  const realtimeDashboard = useRealtimeDashboard();

  // Fetch dashboard data with less frequent refresh since we have real-time updates
  const {
    data: kpiDashboard,
    isLoading: kpiLoading,
    refetch: refetchKpi,
  } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
    refetchInterval: 60000, // Reduced to 60 seconds since we have real-time updates
  });

  const {
    data: alertDashboard,
    isLoading: alertLoading,
    refetch: refetchAlert,
  } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 60000, // Reduced to 60 seconds
  });

  // Merge real-time data with dashboard data
  const mergedKpiDashboard = React.useMemo(() => {
    if (!kpiDashboard) return kpiDashboard;

    return {
      ...kpiDashboard,
      runningKpis: realtimeDashboard.runningKpis.length > 0
        ? realtimeDashboard.runningKpis.map(kpi => ({
            kpiId: kpi.kpiId,
            indicator: kpi.indicator,
            owner: kpi.owner,
            startTime: kpi.startTime,
            progress: kpi.progress,
            estimatedCompletion: kpi.estimatedCompletion,
          }))
        : kpiDashboard.runningKpis,
      nextKpiDue: realtimeDashboard.nextKpiDue || kpiDashboard.nextKpiDue,
    };
  }, [kpiDashboard, realtimeDashboard.runningKpis, realtimeDashboard.nextKpiDue]);

  const handleRefresh = () => {
    refetchKpi();
    refetchAlert();
    realtimeDashboard.refreshDashboard();
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
        countdown={countdown}
        onRefresh={handleRefresh}
      />

      <Grid container spacing={3}>
        {/* KPI Overview Cards */}
        <KpiOverviewCards
          kpiDashboard={mergedKpiDashboard}
          alertDashboard={alertDashboard}
          kpiLoading={kpiLoading}
          alertLoading={alertLoading}
        />

        {/* KPIs Due for Execution - Moved to top */}
        <KpisDueCard kpiDashboard={mergedKpiDashboard} />

        {/* Worker Management - Moved to top */}
        <Grid item xs={12} md={6}>
          <WorkerDashboardCard workerStatus={realtimeDashboard.workerStatus} />
        </Grid>

        {/* Running KPIs */}
        <RunningKpisCard
          kpiDashboard={mergedKpiDashboard}
          realtimeRunningKpis={realtimeDashboard.runningKpis}
        />

        {/* Next KPI Due */}
        <NextKpiExecutionCard
          kpiDashboard={mergedKpiDashboard}
          countdown={realtimeDashboard.countdown}
          isConnected={realtimeDashboard.isConnected}
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

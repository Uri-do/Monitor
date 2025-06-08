import React, { useState, useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography, useTheme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { kpiApi, alertApi } from '@/services/api';
import WorkerDashboardCard from '@/components/Worker/WorkerDashboardCard';

// Dashboard Components
import DashboardHeader from './components/DashboardHeader';
import KpiOverviewCards from './components/KpiOverviewCards';
import KpisDueCard from './components/KpisDueCard';
import RunningKpisCard from './components/RunningKpisCard';
import NextKpiExecutionCard from './components/NextKpiExecutionCard';
import RecentAlertsCard from './components/RecentAlertsCard';
import TopAlertingKpisCard from './components/TopAlertingKpisCard';

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const [countdown, setCountdown] = useState<number | null>(null);

  // Fetch dashboard data with more frequent refresh for real-time updates
  const { data: kpiDashboard, isLoading: kpiLoading, refetch: refetchKpi } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: kpiApi.getDashboard,
    refetchInterval: 15000, // Refresh every 15 seconds for real-time updates
  });

  const { data: alertDashboard, isLoading: alertLoading, refetch: refetchAlert } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Countdown timer for next KPI due
  useEffect(() => {
    if (kpiDashboard?.nextKpiDue?.minutesUntilDue !== undefined) {
      setCountdown(kpiDashboard.nextKpiDue.minutesUntilDue * 60); // Convert to seconds
    }
  }, [kpiDashboard?.nextKpiDue?.minutesUntilDue]);

  // Update countdown every second
  useEffect(() => {
    if (countdown === null || countdown <= 0) return;

    const timer = setInterval(() => {
      setCountdown(prev => {
        if (prev === null || prev <= 1) {
          // Refresh data when countdown reaches 0
          refetchKpi();
          return null;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [countdown, refetchKpi]);

  const handleRefresh = () => {
    refetchKpi();
    refetchAlert();
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
          kpiDashboard={kpiDashboard}
          alertDashboard={alertDashboard}
          kpiLoading={kpiLoading}
          alertLoading={alertLoading}
        />

        {/* KPIs Due for Execution - Moved to top */}
        <KpisDueCard kpiDashboard={kpiDashboard} />

        {/* Worker Management - Moved to top */}
        <Grid item xs={12} md={6}>
          <WorkerDashboardCard />
        </Grid>

        {/* Running KPIs */}
        <RunningKpisCard kpiDashboard={kpiDashboard} />

        {/* Next KPI Due */}
        <NextKpiExecutionCard
          kpiDashboard={kpiDashboard}
          countdown={countdown}
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

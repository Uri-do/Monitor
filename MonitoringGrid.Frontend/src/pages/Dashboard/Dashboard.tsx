import React, { useState, useEffect } from 'react';
import { Box, Grid, LinearProgress, Typography, useTheme, Button, Paper, Chip } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { kpiApi, alertApi } from '@/services/api';
import WorkerDashboardCard from '@/components/Worker/WorkerDashboardCard';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { useRealtime } from '@/contexts/RealtimeContext';
import { signalRService } from '@/services/signalRService';

// Dashboard Components
import DashboardHeader from './components/DashboardHeader';
import KpiOverviewCards from './components/KpiOverviewCards';
import KpisDueCard from './components/KpisDueCard';
import RunningKpisCard from './components/RunningKpisCard';
import NextKpiExecutionCard from './components/NextKpiExecutionCard';
import RecentAlertsCard from './components/RecentAlertsCard';
import TopAlertingKpisCard from './components/TopAlertingKpisCard';

const Dashboard: React.FC = () => {
  const { isEnabled: realtimeEnabled, enableRealtime, disableRealtime, toggleRealtime } = useRealtime();
  const [debugInfo, setDebugInfo] = useState<any>({});

  // Real-time dashboard state - only enabled when real-time features are active
  const realtimeDashboard = useRealtimeDashboard();

  // Debug: Log SignalR connection state and countdown updates
  useEffect(() => {
    const updateDebugInfo = () => {
      setDebugInfo({
        signalRConnected: signalRService.isConnected(),
        connectionState: signalRService.getConnectionState(),
        connectionId: signalRService.getConnectionId(),
        realtimeEnabled,
        countdown: realtimeDashboard.countdown,
        nextKpiDue: realtimeDashboard.nextKpiDue,
        lastUpdate: realtimeDashboard.lastUpdate.toISOString(),
      });
    };

    updateDebugInfo();
    const interval = setInterval(updateDebugInfo, 1000);
    return () => clearInterval(interval);
  }, [realtimeEnabled, realtimeDashboard.countdown, realtimeDashboard.nextKpiDue, realtimeDashboard.lastUpdate]);

  // Auto-enable real-time on component mount
  useEffect(() => {
    if (!realtimeEnabled) {
      console.log('Auto-enabling real-time features...');
      enableRealtime().catch(console.error);
    }
  }, []);

  // Test functions
  const testSignalRConnection = async () => {
    try {
      console.log('Testing SignalR connection...');
      await signalRService.start();

      // Test basic connection
      const response = await fetch('/api/v1/signalrtest/connection-info');
      const info = await response.json();
      console.log('SignalR connection info:', info);

      // Test group join
      await signalRService.joinGroup('Dashboard');
      console.log('Joined Dashboard group');

      alert('SignalR connection test completed - check console for details');
    } catch (error) {
      console.error('SignalR connection test failed:', error);
      alert(`SignalR test failed: ${error}`);
    }
  };

  const testCountdownUpdate = async () => {
    try {
      console.log('Testing countdown update...');
      const response = await fetch('/api/v1/signalrtest/test-countdown', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      console.log('Response status:', response.status);
      console.log('Response headers:', response.headers);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const text = await response.text();
      console.log('Response text:', text);

      if (!text) {
        throw new Error('Empty response from server');
      }

      const result = JSON.parse(text);
      console.log('Test countdown result:', result);
      alert(`Countdown test sent: ${result.message}`);
    } catch (error) {
      console.error('Test countdown failed:', error);
      alert(`Countdown test failed: ${error}`);
    }
  };

  const testRunningKpis = async () => {
    try {
      console.log('Testing running KPIs update...');
      const response = await fetch('/api/v1/signalrtest/test-running-kpis', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });
      const result = await response.json();
      console.log('Test running KPIs result:', result);
      alert(`Running KPIs test sent: ${result.message}`);
    } catch (error) {
      console.error('Test running KPIs failed:', error);
      alert(`Running KPIs test failed: ${error}`);
    }
  };

  const checkServiceStatus = async () => {
    try {
      console.log('Checking service status...');

      // First, let's try different possible API paths
      const possiblePaths = [
        '/api/v1/signalrtest/service-status',
        '/api/signalrtest/service-status',
        '/api/v1/SignalRTest/service-status',
        '/api/SignalRTest/service-status'
      ];

      let response = null;
      let workingPath = null;

      for (const path of possiblePaths) {
        try {
          console.log(`Trying path: ${path}`);
          response = await fetch(path);
          if (response.ok) {
            workingPath = path;
            break;
          }
          console.log(`Path ${path} returned status: ${response.status}`);
        } catch (err) {
          console.log(`Path ${path} failed:`, err);
        }
      }

      if (!response || !response.ok) {
        // Try to check if API is running at all
        try {
          const healthCheck = await fetch('/api/health');
          console.log('Health check status:', healthCheck.status);
          alert(`API server seems to be running (health check: ${healthCheck.status}), but SignalR test endpoints are not found. Check API routing configuration.`);
        } catch {
          alert('API server appears to be down. Please start the MonitoringGrid.Api project.');
        }
        return;
      }

      const result = await response.json();
      console.log('Service status:', result);
      console.log('Working API path:', workingPath);

      const statusMessage = `
Service Status (via ${workingPath}):
- Total KPIs: ${result.totalKpis}
- Active KPIs: ${result.activeKpis}
- Next KPI: ${result.nextKpi?.indicator || 'None'}
- Seconds until due: ${result.nextKpi?.secondsUntilDue || 'N/A'}
- Server time: ${result.serverTime}
      `.trim();

      alert(statusMessage);
    } catch (error) {
      console.error('Service status check failed:', error);
      alert(`Service status check failed: ${error}`);
    }
  };

  // Use real-time data only if enabled, otherwise use mock data
  const dashboardState = realtimeEnabled ? realtimeDashboard : {
    workerStatus: null,
    runningKpis: [],
    countdown: null,
    nextKpiDue: null,
    dashboardData: null,
    isConnected: false,
    lastUpdate: new Date(),
    refreshDashboard: () => {},
  };

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
      {/* Debug Section - Remove in production */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#f5f5f5' }}>
        <Typography variant="h6" gutterBottom>
          🔧 Debug Info (SignalR & Countdown)
        </Typography>
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
          <Chip
            label={`SignalR: ${debugInfo.signalRConnected ? 'Connected' : 'Disconnected'}`}
            color={debugInfo.signalRConnected ? 'success' : 'error'}
            size="small"
          />
          <Chip
            label={`State: ${debugInfo.connectionState}`}
            color="info"
            size="small"
          />
          <Chip
            label={`Real-time: ${realtimeEnabled ? 'Enabled' : 'Disabled'}`}
            color={realtimeEnabled ? 'success' : 'warning'}
            size="small"
          />
          <Chip
            label={`Countdown: ${debugInfo.countdown ?? 'null'}`}
            color="primary"
            size="small"
          />
        </Box>
        <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
          <Button
            size="small"
            variant={realtimeEnabled ? "contained" : "outlined"}
            color={realtimeEnabled ? "success" : "primary"}
            onClick={toggleRealtime}
          >
            {realtimeEnabled ? 'Disable' : 'Enable'} Real-time
          </Button>
          <Button size="small" variant="outlined" onClick={testSignalRConnection}>
            Test SignalR Connection
          </Button>
          <Button size="small" variant="outlined" onClick={testCountdownUpdate}>
            Test Countdown Update
          </Button>
          <Button size="small" variant="outlined" onClick={testRunningKpis}>
            Test Running KPIs
          </Button>
          <Button size="small" variant="outlined" onClick={checkServiceStatus}>
            Check Service Status
          </Button>
        </Box>
        <Typography variant="caption" display="block">
          Connection ID: {debugInfo.connectionId || 'None'}
        </Typography>
        <Typography variant="caption" display="block">
          Next KPI: {debugInfo.nextKpiDue?.indicator || 'None'}
        </Typography>
        <Typography variant="caption" display="block">
          Worker Status: {dashboardState.workerStatus?.isRunning ? 'Running' : 'Stopped'}
        </Typography>
        <Typography variant="caption" display="block">
          Running KPIs: {dashboardState.runningKpis.length}
        </Typography>
        <Typography variant="caption" display="block">
          Last Update: {debugInfo.lastUpdate}
        </Typography>
      </Paper>

      {/* Header */}
      <DashboardHeader
        lastUpdate={kpiDashboard?.lastUpdate}
        countdown={dashboardState.countdown}
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
          <WorkerDashboardCard
            workerStatus={dashboardState.workerStatus}
            realtimeEnabled={realtimeEnabled}
          />
        </Grid>

        {/* Running KPIs */}
        <RunningKpisCard
          kpiDashboard={mergedKpiDashboard}
          realtimeRunningKpis={dashboardState.runningKpis}
        />

        {/* Next KPI Due */}
        <NextKpiExecutionCard
          kpiDashboard={mergedKpiDashboard}
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

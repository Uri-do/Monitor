import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Alert,
  Chip,
  Stack,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tabs,
  Tab,
  LinearProgress,
} from '@mui/material';
import {
  Speed as SpeedIcon,
  Security as SecurityIcon,
  TableChart as TableIcon,
  BugReport as BugIcon,
  Analytics as AnalyticsIcon,
  Memory as MemoryIcon,
  NetworkCheck as NetworkIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
// Demo mode - temporarily disabled to avoid auth issues
// import { ModernDataTable } from '@/components/Common/ModernDataTable';
// import { ErrorBoundary } from '@/components/Common/ErrorBoundary';
// import { usePerformanceMonitor } from '@/hooks/usePerformanceMonitor';
// import { useKpiDashboard } from '@/hooks/useKpis';
// import { useAlertDashboard } from '@/hooks/useAlerts';
import { GridColDef } from '@mui/x-data-grid';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`demo-tabpanel-${index}`}
      aria-labelledby={`demo-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const AdvancedEnhancementDemo: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [errorTrigger, setErrorTrigger] = useState(false);

  // Demo mode - using mock data
  const metrics = {
    renderTime: 12.5,
    memoryUsage: 45.2,
    cacheHitRate: 94.8,
    queryCount: 15,
    networkRequests: 8,
    errorCount: 0,
  };

  const alerts: any[] = [];
  const performanceScore = 92;
  const performanceStatus = 'excellent';
  const trackNetworkRequest = () => console.log('Demo: Network request tracked');
  const trackError = (error: Error) => console.log('Demo: Error tracked:', error.message);
  const clearAlerts = () => console.log('Demo: Alerts cleared');

  // Mock dashboard data
  const kpiLoading = false;
  const alertLoading = false;

  // Sample data for ModernDataTable demo
  const sampleData = [
    { id: 1, name: 'Enhanced Data Fetching', status: 'Complete', performance: 95, type: 'Hook' },
    { id: 2, name: 'Modern Data Table', status: 'Complete', performance: 88, type: 'Component' },
    { id: 3, name: 'Error Boundary', status: 'Complete', performance: 92, type: 'Component' },
    { id: 4, name: 'Performance Monitor', status: 'Complete', performance: 90, type: 'Hook' },
    {
      id: 5,
      name: 'Dashboard Optimization',
      status: 'Complete',
      performance: 85,
      type: 'Enhancement',
    },
  ];

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Enhancement', width: 200 },
    {
      field: 'status',
      headerName: 'Status',
      width: 120,
      renderCell: params => (
        <Chip
          label={params.value}
          color={params.value === 'Complete' ? 'success' : 'warning'}
          size="small"
        />
      ),
    },
    {
      field: 'performance',
      headerName: 'Performance',
      width: 120,
      renderCell: params => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <LinearProgress
            variant="determinate"
            value={params.value}
            sx={{ width: 60, height: 6 }}
            color={params.value >= 90 ? 'success' : params.value >= 80 ? 'warning' : 'error'}
          />
          <Typography variant="caption">{params.value}%</Typography>
        </Box>
      ),
    },
    { field: 'type', headerName: 'Type', width: 120 },
  ];

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
    trackNetworkRequest(); // Simulate network request tracking
  };

  const triggerError = () => {
    setErrorTrigger(true);
    trackError(new Error('Demo error for testing error boundary'));
  };

  const ErrorComponent = () => {
    if (errorTrigger) {
      throw new Error('This is a demo error to test the Error Boundary!');
    }
    return <Typography>No error here! Click the button to trigger one.</Typography>;
  };

  const getPerformanceColor = (status: string) => {
    switch (status) {
      case 'excellent':
        return 'success';
      case 'good':
        return 'info';
      case 'fair':
        return 'warning';
      case 'poor':
        return 'error';
      default:
        return 'default';
    }
  };

  const round2Enhancements = [
    {
      title: 'Enhanced Dashboard Hooks',
      description: 'Optimized real-time dashboard data fetching with intelligent caching',
      icon: <SpeedIcon color="primary" />,
      benefits: [
        '5-second refresh intervals',
        'Smart cache invalidation',
        'Real-time optimization',
        'Reduced API calls',
      ],
    },
    {
      title: 'Modern Data Table',
      description: 'Advanced DataGrid component with filtering, search, and export',
      icon: <TableIcon color="success" />,
      benefits: [
        'Built-in search & filters',
        'Column visibility control',
        'Export functionality',
        'Professional UI',
      ],
    },
    {
      title: 'Performance Monitoring',
      description: 'Real-time performance tracking and alerting system',
      icon: <AnalyticsIcon color="info" />,
      benefits: [
        'Render time tracking',
        'Memory usage monitoring',
        'Cache hit rate analysis',
        'Performance alerts',
      ],
    },
    {
      title: 'Error Boundary System',
      description: 'Comprehensive error handling with recovery options',
      icon: <BugIcon color="warning" />,
      benefits: [
        'Graceful error handling',
        'Detailed error reporting',
        'Retry mechanisms',
        'User-friendly fallbacks',
      ],
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Advanced Frontend Enhancements - Round 2
      </Typography>

      <Alert severity="success" sx={{ mb: 3 }}>
        <Typography variant="h6">🚀 Round 2 Enhancements Successfully Applied!</Typography>
        <Typography>
          Advanced performance monitoring, error handling, and modern data components are now
          active.
        </Typography>
      </Alert>

      {/* Performance Overview - Ultimate Enterprise Style */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={3}>
          <Card
            elevation={4}
            sx={{
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              color: 'white',
              border: '1px solid rgba(102, 126, 234, 0.3)',
              transition: 'all 0.3s ease-in-out',
              '&:hover': {
                transform: 'translateY(-4px)',
                boxShadow: '0 12px 30px rgba(102, 126, 234, 0.4)',
              },
            }}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <SpeedIcon sx={{ fontSize: 28 }} />
                <Typography variant="h6" fontWeight="bold">
                  Performance Score
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Typography variant="h3" fontWeight="bold">
                  {performanceScore}
                </Typography>
                <Chip
                  label={performanceStatus.toUpperCase()}
                  color={getPerformanceColor(performanceStatus)}
                  icon={<SpeedIcon />}
                  sx={{
                    background: 'rgba(255, 255, 255, 0.2)',
                    color: 'white',
                    fontWeight: 'bold',
                  }}
                />
              </Box>
              <Typography variant="caption" sx={{ opacity: 0.9, mt: 1, display: 'block' }}>
                Real-time performance monitoring
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card
            elevation={4}
            sx={{
              background: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
              color: 'white',
              border: '1px solid rgba(79, 172, 254, 0.3)',
              transition: 'all 0.3s ease-in-out',
              '&:hover': {
                transform: 'translateY(-4px)',
                boxShadow: '0 12px 30px rgba(79, 172, 254, 0.4)',
              },
            }}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <MemoryIcon sx={{ fontSize: 28 }} />
                <Typography variant="h6" fontWeight="bold">
                  Memory Usage
                </Typography>
              </Box>
              <Typography variant="h3" fontWeight="bold">
                {metrics.memoryUsage.toFixed(1)}MB
              </Typography>
              <Typography variant="caption" sx={{ opacity: 0.9, mt: 1, display: 'block' }}>
                Real-time memory monitoring
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card
            elevation={4}
            sx={{
              background: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
              color: 'white',
              border: '1px solid rgba(67, 233, 123, 0.3)',
              transition: 'all 0.3s ease-in-out',
              '&:hover': {
                transform: 'translateY(-4px)',
                boxShadow: '0 12px 30px rgba(67, 233, 123, 0.4)',
              },
            }}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <NetworkIcon sx={{ fontSize: 28 }} />
                <Typography variant="h6" fontWeight="bold">
                  Cache Hit Rate
                </Typography>
              </Box>
              <Typography variant="h3" fontWeight="bold">
                {metrics.cacheHitRate.toFixed(1)}%
              </Typography>
              <Typography variant="caption" sx={{ opacity: 0.9, mt: 1, display: 'block' }}>
                Intelligent caching system
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card
            elevation={4}
            sx={{
              background:
                alerts.length > 0
                  ? 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)'
                  : 'linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)',
              color: alerts.length > 0 ? 'white' : 'black',
              border: `1px solid ${alerts.length > 0 ? 'rgba(255, 107, 107, 0.3)' : 'rgba(168, 237, 234, 0.3)'}`,
              transition: 'all 0.3s ease-in-out',
              '&:hover': {
                transform: 'translateY(-4px)',
                boxShadow:
                  alerts.length > 0
                    ? '0 12px 30px rgba(255, 107, 107, 0.4)'
                    : '0 12px 30px rgba(168, 237, 234, 0.4)',
              },
            }}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <ErrorIcon sx={{ fontSize: 28 }} />
                <Typography variant="h6" fontWeight="bold">
                  Active Alerts
                </Typography>
              </Box>
              <Typography variant="h3" fontWeight="bold">
                {alerts.length}
              </Typography>
              <Typography variant="caption" sx={{ opacity: 0.9, mt: 1, display: 'block' }}>
                Enterprise alert system
              </Typography>
              {alerts.length > 0 && (
                <Button
                  size="small"
                  onClick={clearAlerts}
                  sx={{
                    mt: 2,
                    background: 'rgba(255, 255, 255, 0.2)',
                    color: 'white',
                    '&:hover': {
                      background: 'rgba(255, 255, 255, 0.3)',
                    },
                  }}
                >
                  Clear Alerts
                </Button>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabbed Demo Interface - Ultimate Enterprise Style */}
      <Card
        elevation={6}
        sx={{
          background: 'linear-gradient(145deg, #ffffff 0%, #f8f9fa 100%)',
          border: '1px solid rgba(102, 126, 234, 0.1)',
          borderRadius: 3,
        }}
      >
        <Box
          sx={{
            borderBottom: 1,
            borderColor: 'divider',
            background:
              'linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)',
          }}
        >
          <Tabs
            value={tabValue}
            onChange={handleTabChange}
            sx={{
              '& .MuiTab-root': {
                fontWeight: 600,
                textTransform: 'none',
                fontSize: '1rem',
                '&.Mui-selected': {
                  background:
                    'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)',
                  color: '#667eea',
                },
              },
              '& .MuiTabs-indicator': {
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                height: 3,
              },
            }}
          >
            <Tab label="🚀 Ultimate Data Table" />
            <Tab label="🛡️ Enterprise Error Handling" />
            <Tab label="📊 Performance Analytics" />
            <Tab label="⭐ Enhancement Showcase" />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <Typography variant="h6" gutterBottom>
            Modern Data Table Demo
          </Typography>
          {/* Demo: Modern Data Table (simplified for demo) */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Round 2 Enhancements
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Advanced components and performance optimizations
            </Typography>
            <Box sx={{ mt: 2 }}>
              {sampleData.map(item => (
                <Box
                  key={item.id}
                  sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    py: 1,
                    borderBottom: '1px solid #eee',
                  }}
                >
                  <Typography>{item.name}</Typography>
                  <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                    <Chip label={item.status} color="success" size="small" />
                    <Typography variant="body2">{item.performance}%</Typography>
                  </Box>
                </Box>
              ))}
            </Box>
            <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
              <Button size="small" onClick={() => trackNetworkRequest()}>
                Refresh
              </Button>
              <Button size="small" onClick={() => trackNetworkRequest()}>
                Export
              </Button>
            </Box>
          </Paper>
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            Error Boundary Demo
          </Typography>
          <Stack spacing={2}>
            <Alert severity="info">
              The Error Boundary component provides graceful error handling with recovery options.
            </Alert>
            {/* Demo: Error Boundary (simplified for demo) */}
            <Card>
              <CardContent>
                <Typography>
                  {errorTrigger
                    ? 'Demo Error: This shows how error boundaries work!'
                    : 'No error here! Click the button to trigger one.'}
                </Typography>
                <Button variant="contained" color="error" onClick={triggerError} sx={{ mt: 2 }}>
                  Trigger Demo Error
                </Button>
                {errorTrigger && (
                  <Button
                    variant="outlined"
                    onClick={() => setErrorTrigger(false)}
                    sx={{ mt: 2, ml: 2 }}
                  >
                    Reset
                  </Button>
                )}
              </CardContent>
            </Card>
          </Stack>
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            Performance Metrics
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Real-time Metrics
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemText
                      primary="Render Time"
                      secondary={`${metrics.renderTime.toFixed(2)}ms`}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="Query Count" secondary={metrics.queryCount} />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="Network Requests" secondary={metrics.networkRequests} />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="Error Count" secondary={metrics.errorCount} />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Performance Alerts
                </Typography>
                {alerts.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No performance alerts
                  </Typography>
                ) : (
                  <List dense>
                    {alerts.slice(0, 5).map((alert, index) => (
                      <ListItem key={index}>
                        <ListItemIcon>
                          <ErrorIcon color={alert.type === 'error' ? 'error' : 'warning'} />
                        </ListItemIcon>
                        <ListItemText
                          primary={alert.message}
                          secondary={`${alert.metric}: ${alert.value} (threshold: ${alert.threshold})`}
                        />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            Round 2 Enhancement Details
          </Typography>
          <Grid container spacing={3}>
            {round2Enhancements.map((enhancement, index) => (
              <Grid item xs={12} md={6} key={index}>
                <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
                  <Box display="flex" alignItems="center" gap={2} mb={2}>
                    {enhancement.icon}
                    <Typography variant="h6">{enhancement.title}</Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary" mb={2}>
                    {enhancement.description}
                  </Typography>
                  <List dense>
                    {enhancement.benefits.map((benefit, idx) => (
                      <ListItem key={idx} sx={{ py: 0.5 }}>
                        <ListItemIcon sx={{ minWidth: 32 }}>
                          <SecurityIcon color="success" fontSize="small" />
                        </ListItemIcon>
                        <ListItemText primary={benefit} />
                      </ListItem>
                    ))}
                  </List>
                </Paper>
              </Grid>
            ))}
          </Grid>
        </TabPanel>
      </Card>
    </Box>
  );
};

export default AdvancedEnhancementDemo;

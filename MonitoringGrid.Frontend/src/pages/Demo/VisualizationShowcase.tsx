import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Grid,
  Paper,
  Switch,
  FormControlLabel,
  Divider,
  Tabs,
  Tab,
  useTheme,
} from '@mui/material';
import {
  Dashboard,
  Timeline,
  BarChart,
  PieChart,
  ShowChart,
  Speed,
} from '@mui/icons-material';
import PageHeader from '../../components/Common/PageHeader';
import EnhancedChart from '../../components/enhanced/EnhancedCharts';
import RealtimeDashboard from '../../components/enhanced/RealtimeDashboard';
import InteractiveVisualization from '../../components/enhanced/InteractiveVisualizations';
import KpiVisualization from '../../components/enhanced/KpiVisualization';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index}>
    {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
  </div>
);

const VisualizationShowcase: React.FC = () => {
  const theme = useTheme();
  const [tabValue, setTabValue] = useState(0);
  const [realTimeEnabled, setRealTimeEnabled] = useState(true);

  // Sample data for charts
  const sampleLineData = [
    { name: '00:00', value: 120, target: 150 },
    { name: '04:00', value: 135, target: 150 },
    { name: '08:00', value: 180, target: 150 },
    { name: '12:00', value: 165, target: 150 },
    { name: '16:00', value: 190, target: 150 },
    { name: '20:00', value: 145, target: 150 },
    { name: '24:00', value: 125, target: 150 },
  ];

  const sampleBarData = [
    { name: 'API', value: 2400 },
    { name: 'Database', value: 1398 },
    { name: 'Cache', value: 9800 },
    { name: 'External', value: 3908 },
    { name: 'Auth', value: 4800 },
  ];

  const samplePieData = [
    { name: 'Success', value: 85 },
    { name: 'Warning', value: 10 },
    { name: 'Error', value: 5 },
  ];

  const sampleKpiData = [
    {
      id: 'response-time',
      name: 'Average Response Time',
      value: 145,
      target: 200,
      unit: 'ms',
      trend: 'down' as const,
      change: -12.5,
      status: 'healthy' as const,
      history: sampleLineData.map(d => ({ timestamp: d.name, value: d.value, target: d.target })),
      threshold: { warning: 180, critical: 250 },
    },
    {
      id: 'throughput',
      name: 'Requests per Second',
      value: 1250,
      target: 1000,
      unit: 'req/s',
      trend: 'up' as const,
      change: 25.0,
      status: 'healthy' as const,
      history: sampleLineData.map(d => ({ timestamp: d.name, value: d.value * 8, target: d.target * 8 })),
    },
    {
      id: 'error-rate',
      name: 'Error Rate',
      value: 2.1,
      target: 1.0,
      unit: '%',
      trend: 'up' as const,
      change: 15.5,
      status: 'warning' as const,
      history: sampleLineData.map(d => ({ timestamp: d.name, value: d.value * 0.02, target: d.target * 0.02 })),
      threshold: { warning: 1.5, critical: 3.0 },
    },
    {
      id: 'uptime',
      name: 'System Uptime',
      value: 99.95,
      target: 99.9,
      unit: '%',
      trend: 'neutral' as const,
      change: 0.05,
      status: 'healthy' as const,
      history: sampleLineData.map(d => ({ timestamp: d.name, value: 99.9 + Math.random() * 0.1, target: 99.9 })),
    },
  ];

  const interactiveData = [
    { timestamp: '2024-01-01', value: 100, category: 'api', performance: 95, errors: 2, users: 1200 },
    { timestamp: '2024-01-02', value: 120, category: 'database', performance: 88, errors: 5, users: 1350 },
    { timestamp: '2024-01-03', value: 95, category: 'cache', performance: 92, errors: 1, users: 1100 },
    { timestamp: '2024-01-04', value: 140, category: 'api', performance: 85, errors: 8, users: 1500 },
    { timestamp: '2024-01-05', value: 110, category: 'database', performance: 90, errors: 3, users: 1250 },
  ];

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <PageHeader
        title="Data Visualization Showcase"
        subtitle="Interactive charts, real-time dashboards, and advanced visualizations"
        icon={<Timeline />}
        actions={[
          {
            label: realTimeEnabled ? 'Disable Real-time' : 'Enable Real-time',
            onClick: () => setRealTimeEnabled(!realTimeEnabled),
            variant: 'outlined',
          },
        ]}
      />

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)}>
          <Tab icon={<Dashboard />} label="Real-time Dashboard" />
          <Tab icon={<ShowChart />} label="Enhanced Charts" />
          <Tab icon={<Speed />} label="KPI Visualizations" />
          <Tab icon={<BarChart />} label="Interactive Charts" />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <RealtimeDashboard
          refreshInterval={realTimeEnabled ? 3000 : 0}
          autoRefresh={realTimeEnabled}
          onMetricClick={(metric) => console.log('Metric clicked:', metric)}
        />
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Enhanced Chart Components
        </Typography>
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <EnhancedChart
              data={sampleLineData}
              title="Response Time Trend"
              subtitle="API response time over the last 24 hours"
              type="line"
              height={300}
              animated
              showGrid
              showTooltip
              threshold={150}
              color={theme.palette.primary.main}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <EnhancedChart
              data={sampleLineData}
              title="Performance Area Chart"
              subtitle="System performance metrics"
              type="area"
              height={300}
              animated
              showGrid
              showTooltip
              color={theme.palette.success.main}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <EnhancedChart
              data={sampleBarData}
              title="Request Distribution"
              subtitle="Requests by service type"
              type="bar"
              height={300}
              animated
              showGrid
              showTooltip
              color={theme.palette.secondary.main}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <EnhancedChart
              data={samplePieData}
              title="Status Distribution"
              subtitle="Request status breakdown"
              type="donut"
              height={300}
              animated
              showTooltip
              showLegend
              onDataPointClick={(data) => console.log('Clicked:', data)}
            />
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          KPI Visualization Dashboard
        </Typography>
        
        <KpiVisualization
          kpis={sampleKpiData}
          layout="grid"
          showTrends
          showTargets
          onKpiClick={(kpi) => console.log('KPI clicked:', kpi)}
          refreshInterval={realTimeEnabled ? 5000 : 0}
        />
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Interactive Visualizations
        </Typography>
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <InteractiveVisualization
              title="Performance Scatter Plot"
              type="scatter"
              data={interactiveData}
              height={400}
              interactive
              filters={['api', 'database', 'cache']}
              onFilterChange={(filters) => console.log('Filters:', filters)}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <InteractiveVisualization
              title="System Health Radar"
              type="radar"
              data={interactiveData}
              height={400}
              interactive
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <InteractiveVisualization
              title="Resource Usage Treemap"
              type="treemap"
              data={interactiveData}
              height={400}
              interactive
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <InteractiveVisualization
              title="Composed Metrics View"
              type="composed"
              data={interactiveData}
              height={400}
              interactive
              filters={['api', 'database', 'cache']}
            />
          </Grid>
        </Grid>
      </TabPanel>

      <Divider sx={{ my: 4 }} />

      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="h6" gutterBottom fontWeight={600}>
          📊 Advanced Data Visualizations
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
          Interactive charts with real-time updates and enhanced user experience
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Built with Recharts, Material-UI, and custom animations
        </Typography>
      </Box>
    </Container>
  );
};

export default VisualizationShowcase;

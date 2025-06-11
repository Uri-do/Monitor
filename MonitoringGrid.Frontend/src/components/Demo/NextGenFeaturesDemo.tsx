import React, { useState, useEffect } from 'react';
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
  Switch,
  FormControlLabel,
  LinearProgress,
  Badge,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  CloudDownload as PWAIcon,
  Accessibility as AccessibilityIcon,
  Speed as PerformanceIcon,
  People as CollaborationIcon,
  BugReport as TestingIcon,
  BarChart as ChartIcon,
  Visibility as VirtualizationIcon,
  Storage as StateIcon,
  GetApp as InstallIcon,
  Wifi as OnlineIcon,
  WifiOff as OfflineIcon,
  Update as UpdateIcon,
  Notifications as NotificationIcon,
} from '@mui/icons-material';
import { VirtualizedDataTable } from '@/components/Common/VirtualizedDataTable';
import { AdvancedChart } from '@/components/Charts/AdvancedChart';
import { useAppStore, useAppSelectors } from '@/stores/appStore';
import { useAccessibility } from '@/hooks/useAccessibility';
import { useCollaboration } from '@/hooks/useCollaboration';
import { usePWA } from '@/hooks/usePWA';
import { usePerformanceMonitor } from '@/hooks/usePerformanceMonitor';

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
      id={`nextgen-tabpanel-${index}`}
      aria-labelledby={`nextgen-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const NextGenFeaturesDemo: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);

  // Store hooks
  const preferences = useAppSelectors.preferences();
  const updatePreferences = useAppStore(state => state.updatePreferences);

  // Feature hooks
  const { announce, preferences: a11yPrefs } = useAccessibility();
  const { activeUsers, isCollaborationEnabled } = useCollaboration();
  const { isInstallable, isInstalled, isOnline, isUpdateAvailable, install, update } = usePWA();
  const { metrics, performanceScore, performanceStatus } =
    usePerformanceMonitor('NextGenFeaturesDemo');

  // Sample data for virtualized table
  const generateLargeDataset = (size: number) => {
    return Array.from({ length: size }, (_, index) => ({
      id: index + 1,
      name: `Item ${index + 1}`,
      value: Math.floor(Math.random() * 1000),
      category: ['A', 'B', 'C'][index % 3],
      timestamp: new Date(Date.now() - Math.random() * 86400000).toISOString(),
      status: Math.random() > 0.5 ? 'Active' : 'Inactive',
    }));
  };

  const [largeDataset] = useState(() => generateLargeDataset(10000));

  // Chart data
  const chartData = Array.from({ length: 30 }, (_, index) => ({
    timestamp: `Day ${index + 1}`,
    performance: 70 + Math.random() * 30,
    memory: 50 + Math.random() * 40,
    network: 60 + Math.random() * 35,
  }));

  const chartSeries = [
    { key: 'performance', name: 'Performance Score', color: '#1976d2' },
    { key: 'memory', name: 'Memory Usage', color: '#dc004e' },
    { key: 'network', name: 'Network Efficiency', color: '#00c853' },
  ];

  const virtualizedColumns = [
    { id: 'id', label: 'ID', field: 'id' as keyof (typeof largeDataset)[0], width: 80 },
    { id: 'name', label: 'Name', field: 'name' as keyof (typeof largeDataset)[0], width: 150 },
    { id: 'value', label: 'Value', field: 'value' as keyof (typeof largeDataset)[0], width: 100 },
    { id: 'category', label: 'Category', field: 'category' as keyof (typeof largeDataset)[0], width: 120 },
    { id: 'status', label: 'Status', field: 'status' as keyof (typeof largeDataset)[0], width: 100 },
  ];

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
    announce(`Switched to tab ${newValue + 1}`);
  };

  const handleAccessibilityToggle =
    (setting: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
      const newPrefs = {
        ...preferences,
        accessibility: {
          ...preferences.accessibility,
          [setting]: event.target.checked,
        },
      };
      updatePreferences(newPrefs);
      announce(`${setting} ${event.target.checked ? 'enabled' : 'disabled'}`);
    };

  const round3Features = [
    {
      title: 'Progressive Web App (PWA)',
      description: 'Full PWA capabilities with offline support and installability',
      icon: <PWAIcon color="primary" />,
      benefits: [
        'Offline functionality',
        'App-like experience',
        'Push notifications',
        'Background sync',
      ],
      status: isInstalled ? 'Installed' : isInstallable ? 'Installable' : 'Available',
    },
    {
      title: 'Advanced Virtualization',
      description: 'High-performance rendering for large datasets',
      icon: <VirtualizationIcon color="success" />,
      benefits: [
        '10,000+ rows support',
        'Smooth scrolling',
        'Memory efficient',
        'Real-time updates',
      ],
      status: 'Active',
    },
    {
      title: 'Accessibility (a11y)',
      description: 'Comprehensive accessibility features and WCAG compliance',
      icon: <AccessibilityIcon color="info" />,
      benefits: [
        'Screen reader support',
        'Keyboard navigation',
        'High contrast mode',
        'Focus management',
      ],
      status: 'Enhanced',
    },
    {
      title: 'Advanced State Management',
      description: 'Zustand-powered global state with persistence',
      icon: <StateIcon color="warning" />,
      benefits: [
        'Persistent preferences',
        'Performance tracking',
        'Offline state',
        'Real-time sync',
      ],
      status: 'Optimized',
    },
    {
      title: 'Real-time Collaboration',
      description: 'Multi-user collaboration with live cursors and presence',
      icon: <CollaborationIcon color="secondary" />,
      benefits: ['Live cursors', 'User presence', 'Conflict detection', 'Comments system'],
      status: isCollaborationEnabled ? 'Connected' : 'Offline',
    },
    {
      title: 'Advanced Testing',
      description: 'Comprehensive testing utilities and accessibility testing',
      icon: <TestingIcon color="error" />,
      benefits: [
        'Component testing',
        'Accessibility testing',
        'Performance testing',
        'Mock utilities',
      ],
      status: 'Ready',
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Next-Generation Frontend Features - Round 3
      </Typography>

      <Alert severity="success" sx={{ mb: 3 }}>
        <Typography variant="h6">🚀 Round 3: Next-Gen Features Successfully Applied!</Typography>
        <Typography>
          PWA capabilities, advanced virtualization, accessibility enhancements, and cutting-edge
          collaboration features are now active.
        </Typography>
      </Alert>

      {/* Status Overview */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                PWA Status
              </Typography>
              <Stack direction="row" spacing={1} alignItems="center">
                {isOnline ? <OnlineIcon color="success" /> : <OfflineIcon color="error" />}
                <Typography variant="body2">{isOnline ? 'Online' : 'Offline'}</Typography>
              </Stack>
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                {isInstalled && <Chip label="Installed" color="success" size="small" />}
                {isInstallable && (
                  <Button size="small" startIcon={<InstallIcon />} onClick={install}>
                    Install
                  </Button>
                )}
                {isUpdateAvailable && (
                  <Button size="small" startIcon={<UpdateIcon />} onClick={update}>
                    Update
                  </Button>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Performance
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Typography variant="h3" color="primary">
                  {performanceScore}
                </Typography>
                <Chip
                  label={performanceStatus.toUpperCase()}
                  color={
                    performanceScore >= 90 ? 'success' : performanceScore >= 75 ? 'info' : 'warning'
                  }
                  icon={<PerformanceIcon />}
                />
              </Box>
              <Typography variant="caption" color="text.secondary">
                Render: {metrics.renderTime.toFixed(1)}ms
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Collaboration
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Badge badgeContent={activeUsers.length} color="primary">
                  <CollaborationIcon color={isCollaborationEnabled ? 'success' : 'disabled'} />
                </Badge>
                <Typography variant="body2">{activeUsers.length} active users</Typography>
              </Box>
              <Typography variant="caption" color="text.secondary">
                {isCollaborationEnabled ? 'Connected' : 'Offline mode'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Accessibility
              </Typography>
              <Stack spacing={1}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={a11yPrefs.highContrast}
                      onChange={handleAccessibilityToggle('highContrast')}
                      size="small"
                    />
                  }
                  label="High Contrast"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={a11yPrefs.reducedMotion}
                      onChange={handleAccessibilityToggle('reducedMotion')}
                      size="small"
                    />
                  }
                  label="Reduced Motion"
                />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabbed Demo Interface */}
      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tabValue} onChange={handleTabChange}>
            <Tab label="Virtualized Table" />
            <Tab label="Advanced Charts" />
            <Tab label="PWA Features" />
            <Tab label="Accessibility" />
            <Tab label="Feature Overview" />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <Typography variant="h6" gutterBottom>
            Virtualized Data Table - 10,000 Rows
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            High-performance virtualization handles large datasets with smooth scrolling and minimal
            memory usage.
          </Typography>
          <VirtualizedDataTable
            data={largeDataset.slice(0, 1000)} // Show first 1000 for demo
            columns={virtualizedColumns}
            height={400}
            searchable
          />
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            Advanced Chart Components
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Interactive charts with export capabilities, real-time updates, and accessibility
            features.
          </Typography>
          <AdvancedChart
            title="System Performance Metrics"
            subtitle="Real-time monitoring dashboard"
            data={chartData}
            series={chartSeries}
            type="composed"
            height={400}
            showLegend
            showGrid
            showBrush
            enableExport
            enableFullscreen
            enableRefresh
            onRefresh={() => console.log('Refreshing chart...')}
            onExport={format => console.log(`Exporting as ${format}`)}
            thresholds={[
              { value: 80, label: 'Target', color: '#4caf50' },
              { value: 60, label: 'Warning', color: '#ff9800' },
            ]}
          />
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            Progressive Web App Features
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Installation Status
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemIcon>
                      {isInstalled ? <InstallIcon color="success" /> : <InstallIcon />}
                    </ListItemIcon>
                    <ListItemText
                      primary="App Installation"
                      secondary={
                        isInstalled ? 'Installed' : isInstallable ? 'Available' : 'Not available'
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      {isOnline ? <OnlineIcon color="success" /> : <OfflineIcon color="error" />}
                    </ListItemIcon>
                    <ListItemText
                      primary="Network Status"
                      secondary={isOnline ? 'Online' : 'Offline'}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      {isUpdateAvailable ? <UpdateIcon color="warning" /> : <UpdateIcon />}
                    </ListItemIcon>
                    <ListItemText
                      primary="Updates"
                      secondary={isUpdateAvailable ? 'Update available' : 'Up to date'}
                    />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  PWA Actions
                </Typography>
                <Stack spacing={2}>
                  {isInstallable && !isInstalled && (
                    <Button variant="contained" startIcon={<InstallIcon />} onClick={install}>
                      Install App
                    </Button>
                  )}
                  {isUpdateAvailable && (
                    <Button variant="outlined" startIcon={<UpdateIcon />} onClick={update}>
                      Update App
                    </Button>
                  )}
                  <Button variant="outlined" startIcon={<NotificationIcon />}>
                    Enable Notifications
                  </Button>
                </Stack>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            Accessibility Features
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Visual Accessibility
                </Typography>
                <Stack spacing={2}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={a11yPrefs.highContrast}
                        onChange={handleAccessibilityToggle('highContrast')}
                      />
                    }
                    label="High Contrast Mode"
                  />
                  <FormControlLabel
                    control={
                      <Switch
                        checked={a11yPrefs.reducedMotion}
                        onChange={handleAccessibilityToggle('reducedMotion')}
                      />
                    }
                    label="Reduced Motion"
                  />
                  <Button
                    variant="outlined"
                    onClick={() => announce('This is a test announcement for screen readers')}
                  >
                    Test Screen Reader Announcement
                  </Button>
                </Stack>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Navigation Features
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemText
                      primary="Skip Links"
                      secondary="Press Tab to see skip navigation links"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Keyboard Navigation"
                      secondary="Full keyboard support for all interactions"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Focus Management"
                      secondary="Automatic focus handling for dialogs and navigation"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="ARIA Labels"
                      secondary="Comprehensive ARIA labeling for screen readers"
                    />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={4}>
          <Typography variant="h6" gutterBottom>
            Round 3 Feature Overview
          </Typography>
          <Grid container spacing={3}>
            {round3Features.map((feature, index) => (
              <Grid item xs={12} md={6} key={index}>
                <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
                  <Box display="flex" alignItems="center" gap={2} mb={2}>
                    {feature.icon}
                    <Box>
                      <Typography variant="h6">{feature.title}</Typography>
                      <Chip label={feature.status} size="small" color="primary" />
                    </Box>
                  </Box>
                  <Typography variant="body2" color="text.secondary" mb={2}>
                    {feature.description}
                  </Typography>
                  <List dense>
                    {feature.benefits.map((benefit, idx) => (
                      <ListItem key={idx} sx={{ py: 0.5 }}>
                        <ListItemIcon sx={{ minWidth: 32 }}>
                          <ChartIcon color="success" fontSize="small" />
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

export default NextGenFeaturesDemo;

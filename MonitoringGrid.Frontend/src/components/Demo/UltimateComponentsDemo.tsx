import React, { useState, useEffect } from 'react';
import { Box, Typography, Grid, CardContent, Stack, MenuItem, Divider, Chip } from '@mui/material';
import {
  Speed as SpeedIcon,
  Security as SecurityIcon,
  ContactMail as ContactIcon,
  Refresh as RefreshIcon,
  Code as CodeIcon,
  Star as StarIcon,
  Rocket as RocketIcon,
  Analytics as AnalyticsIcon,
  Memory as MemoryIcon,
  Wifi as NetworkIcon,
  Error as ErrorIcon,
  CheckCircle as CheckIcon,
} from '@mui/icons-material';
import {
  Card,
  Button,
  Dialog,
  InputField,
  Select,
  MetricCard,
  Snackbar,
  DataTable,
  DataTableColumn,
} from '@/components';

export const UltimateComponentsDemo: React.FC = () => {
  // Dialog states
  const [primaryDialogOpen, setPrimaryDialogOpen] = useState(false);
  const [secondaryDialogOpen, setSecondaryDialogOpen] = useState(false);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<
    'success' | 'error' | 'warning' | 'info'
  >('success');

  // Form states
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    priority: 'Medium',
    category: 'Performance',
  });

  // Live metrics
  const [liveMetrics, setLiveMetrics] = useState({
    performance: 94.2,
    memory: 67.8,
    network: 89.5,
    errors: 2,
    uptime: 99.9,
    throughput: 15420,
  });

  // Sample data for DataTable
  const sampleTableData = [
    {
      id: 1,
      name: 'Ultimate KPI Monitor',
      status: 'Active',
      priority: 'High',
      owner: 'DevOps Team',
      lastRun: '2024-01-15 10:30',
      performance: 94.2,
      type: 'Performance',
    },
    {
      id: 2,
      name: 'Database Health Check',
      status: 'Active',
      priority: 'Critical',
      owner: 'Database Team',
      lastRun: '2024-01-15 10:25',
      performance: 98.7,
      type: 'Database',
    },
    {
      id: 3,
      name: 'API Response Monitor',
      status: 'Warning',
      priority: 'Medium',
      owner: 'API Team',
      lastRun: '2024-01-15 10:20',
      performance: 87.3,
      type: 'API',
    },
    {
      id: 4,
      name: 'Security Audit Trail',
      status: 'Active',
      priority: 'Critical',
      owner: 'Security Team',
      lastRun: '2024-01-15 10:15',
      performance: 99.1,
      type: 'Security',
    },
    {
      id: 5,
      name: 'Network Latency Check',
      status: 'Error',
      priority: 'High',
      owner: 'Network Team',
      lastRun: '2024-01-15 10:10',
      performance: 76.8,
      type: 'Network',
    },
    {
      id: 6,
      name: 'Memory Usage Monitor',
      status: 'Active',
      priority: 'Medium',
      owner: 'Infrastructure',
      lastRun: '2024-01-15 10:05',
      performance: 91.5,
      type: 'Infrastructure',
    },
    {
      id: 7,
      name: 'Disk Space Monitor',
      status: 'Active',
      priority: 'Low',
      owner: 'Infrastructure',
      lastRun: '2024-01-15 10:00',
      performance: 88.9,
      type: 'Infrastructure',
    },
    {
      id: 8,
      name: 'User Authentication',
      status: 'Active',
      priority: 'Critical',
      owner: 'Security Team',
      lastRun: '2024-01-15 09:55',
      performance: 97.2,
      type: 'Security',
    },
  ];

  const tableColumns: DataTableColumn[] = [
    { id: 'name', label: 'Monitor Name', sortable: true, filterable: true, width: 200 },
    {
      id: 'status',
      label: 'Status',
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Active', value: 'Active' },
        { label: 'Warning', value: 'Warning' },
        { label: 'Error', value: 'Error' },
      ],
      format: value => (
        <Chip
          label={value}
          color={value === 'Active' ? 'success' : value === 'Warning' ? 'warning' : 'error'}
          size="small"
        />
      ),
    },
    {
      id: 'priority',
      label: 'Priority',
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Low', value: 'Low' },
        { label: 'Medium', value: 'Medium' },
        { label: 'High', value: 'High' },
        { label: 'Critical', value: 'Critical' },
      ],
      format: value => (
        <Chip
          label={value}
          color={
            value === 'Critical'
              ? 'error'
              : value === 'High'
                ? 'warning'
                : value === 'Medium'
                  ? 'info'
                  : 'default'
          }
          size="small"
          variant="outlined"
        />
      ),
    },
    { id: 'owner', label: 'Owner Team', sortable: true, filterable: true },
    { id: 'lastRun', label: 'Last Run', sortable: true, width: 150 },
    {
      id: 'performance',
      label: 'Performance %',
      sortable: true,
      align: 'right' as const,
      format: value => `${value}%`,
    },
    {
      id: 'type',
      label: 'Type',
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Performance', value: 'Performance' },
        { label: 'Database', value: 'Database' },
        { label: 'API', value: 'API' },
        { label: 'Security', value: 'Security' },
        { label: 'Network', value: 'Network' },
        { label: 'Infrastructure', value: 'Infrastructure' },
      ],
    },
  ];

  // Update live metrics
  useEffect(() => {
    const interval = setInterval(() => {
      setLiveMetrics(prev => ({
        performance: Math.max(85, Math.min(99, prev.performance + (Math.random() - 0.5) * 2)),
        memory: Math.max(50, Math.min(90, prev.memory + (Math.random() - 0.5) * 3)),
        network: Math.max(80, Math.min(100, prev.network + (Math.random() - 0.5) * 2)),
        errors: Math.max(0, Math.min(10, prev.errors + Math.floor((Math.random() - 0.7) * 2))),
        uptime: Math.max(95, Math.min(100, prev.uptime + (Math.random() - 0.5) * 0.1)),
        throughput: Math.max(
          10000,
          Math.min(20000, prev.throughput + Math.floor((Math.random() - 0.5) * 1000))
        ),
      }));
    }, 3000);

    return () => clearInterval(interval);
  }, []);

  const handleFormSubmit = () => {
    if (!formData.name || !formData.description) {
      setSnackbarMessage('Please fill in all required fields');
      setSnackbarSeverity('error');
      setSnackbarOpen(true);
      return;
    }

    setPrimaryDialogOpen(false);
    setFormData({ name: '', description: '', priority: 'Medium', category: 'Performance' });
    setSnackbarMessage('Ultimate component created successfully! 🚀');
    setSnackbarSeverity('success');
    setSnackbarOpen(true);
  };

  const showNotification = (
    message: string,
    severity: 'success' | 'error' | 'warning' | 'info'
  ) => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  return (
    <Box sx={{ p: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 4, textAlign: 'center' }}>
        <Typography variant="h3" fontWeight="bold" gutterBottom>
          Ultimate Enterprise Components
        </Typography>
        <Typography variant="h6" color="text.secondary">
          Premium styled components with advanced animations and enterprise-grade design
        </Typography>
      </Box>

      {/* Live Metrics Dashboard */}
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        📊 Ultimate Metric Cards
      </Typography>
      <Grid container spacing={3} sx={{ mb: 6 }}>
        <Grid item xs={12} md={4}>
          <MetricCard
            title="Performance Score"
            value={`${liveMetrics.performance.toFixed(1)}%`}
            subtitle="Real-time system performance"
            icon={<SpeedIcon />}
            gradient="primary"
            chip={{ label: 'EXCELLENT', color: 'success' }}
            onClick={() => showNotification('Performance metrics accessed!', 'info')}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="Memory Usage"
            value={`${liveMetrics.memory.toFixed(1)}%`}
            subtitle="Intelligent memory monitoring"
            icon={<MemoryIcon />}
            gradient="info"
            chip={{ label: 'OPTIMAL', color: 'info' }}
            onClick={() => showNotification('Memory stats viewed!', 'info')}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="Network Health"
            value={`${liveMetrics.network.toFixed(1)}%`}
            subtitle="Enterprise network monitoring"
            icon={<NetworkIcon />}
            gradient="success"
            chip={{ label: 'HEALTHY', color: 'success' }}
            onClick={() => showNotification('Network status checked!', 'info')}
          />
        </Grid>
      </Grid>

      {/* Ultimate Cards Showcase */}
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        🎨 Ultimate Cards & Buttons
      </Typography>
      <Grid container spacing={3} sx={{ mb: 6 }}>
        <Grid item xs={12} md={6}>
          <Card gradient="primary" sx={{ height: '100%' }}>
            <CardContent sx={{ p: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                <CodeIcon sx={{ fontSize: 32, color: '#667eea' }} />
                <Typography variant="h5" fontWeight="bold">
                  Primary Actions
                </Typography>
              </Box>
              <Typography variant="body1" sx={{ mb: 3 }}>
                Showcase of primary gradient buttons with advanced styling and hover effects.
              </Typography>
              <Stack spacing={2}>
                <Button
                  gradient="primary"
                  fullWidth
                  icon={<RocketIcon />}
                  onClick={() => setPrimaryDialogOpen(true)}
                >
                  Open Ultimate Dialog
                </Button>
                <Button
                  gradient="secondary"
                  variant="outlined"
                  fullWidth
                  icon={<StarIcon />}
                  onClick={() => showNotification('Secondary action triggered! ⭐', 'warning')}
                >
                  Secondary Action
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card gradient="success" sx={{ height: '100%' }}>
            <CardContent sx={{ p: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                <CheckIcon sx={{ fontSize: 32, color: '#43e97b' }} />
                <Typography variant="h5" fontWeight="bold">
                  Success Operations
                </Typography>
              </Box>
              <Typography variant="body1" sx={{ mb: 3 }}>
                Enterprise-grade success operations with real-time feedback and notifications.
              </Typography>
              <Stack spacing={2}>
                <Button
                  gradient="success"
                  fullWidth
                  icon={<CheckIcon />}
                  onClick={() =>
                    showNotification('Operation completed successfully! ✅', 'success')
                  }
                >
                  Execute Success Action
                </Button>
                <Button
                  gradient="error"
                  variant="outlined"
                  fullWidth
                  icon={<ErrorIcon />}
                  onClick={() => showNotification('Error simulation triggered! ⚠️', 'error')}
                >
                  Simulate Error
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Additional Metrics */}
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        ⚡ Advanced Metrics
      </Typography>
      <Grid container spacing={3} sx={{ mb: 6 }}>
        <Grid item xs={12} md={4}>
          <MetricCard
            title="Active Errors"
            value={liveMetrics.errors}
            subtitle="Enterprise error tracking"
            icon={<ErrorIcon />}
            gradient={liveMetrics.errors > 5 ? 'error' : 'warning'}
            chip={{
              label:
                liveMetrics.errors > 5 ? 'CRITICAL' : liveMetrics.errors > 2 ? 'WARNING' : 'STABLE',
              color:
                liveMetrics.errors > 5 ? 'error' : liveMetrics.errors > 2 ? 'warning' : 'success',
            }}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="System Uptime"
            value={`${liveMetrics.uptime.toFixed(2)}%`}
            subtitle="Enterprise reliability metrics"
            icon={<AnalyticsIcon />}
            gradient="secondary"
            chip={{ label: 'ENTERPRISE', color: 'secondary' }}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="Throughput"
            value={`${(liveMetrics.throughput / 1000).toFixed(1)}K`}
            subtitle="Requests per second"
            icon={<SpeedIcon />}
            gradient="info"
            chip={{ label: 'REAL-TIME', color: 'info' }}
          />
        </Grid>
      </Grid>

      {/* Action Center */}
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        🎯 Action Center
      </Typography>
      <Card gradient="warning" sx={{ mb: 4 }}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" fontWeight="bold" sx={{ mb: 3 }}>
            Enterprise Control Panel
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={3}>
              <Button
                gradient="info"
                fullWidth
                icon={<ContactIcon />}
                onClick={() => setSecondaryDialogOpen(true)}
              >
                Contact Manager
              </Button>
            </Grid>
            <Grid item xs={12} md={3}>
              <Button
                gradient="warning"
                fullWidth
                icon={<RefreshIcon />}
                onClick={() => showNotification('System refreshed! 🔄', 'info')}
              >
                Refresh System
              </Button>
            </Grid>
            <Grid item xs={12} md={3}>
              <Button
                gradient="secondary"
                variant="outlined"
                fullWidth
                icon={<SecurityIcon />}
                onClick={() => showNotification('Security scan initiated! 🛡️', 'warning')}
              >
                Security Scan
              </Button>
            </Grid>
            <Grid item xs={12} md={3}>
              <Button
                gradient="success"
                variant="outlined"
                fullWidth
                icon={<AnalyticsIcon />}
                onClick={() => showNotification('Analytics generated! 📈', 'success')}
              >
                Generate Report
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Ultimate DataTable Showcase */}
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        📊 Ultimate DataTable
      </Typography>
      <DataTable
        title="Enterprise Monitoring Dashboard"
        subtitle="Advanced data table with sorting, filtering, search, export, and bulk actions"
        data={sampleTableData}
        columns={tableColumns}
        gradient="primary"
        searchable
        filterable
        exportable
        refreshable
        selectable
        pagination
        rowsPerPageOptions={[5, 10, 25]}
        defaultRowsPerPage={5}
        maxHeight={400}
        onRefresh={() => showNotification('Data refreshed! 🔄', 'info')}
        onExport={(data, format) =>
          showNotification(`Exported ${data.length} rows as ${format.toUpperCase()}! 📊`, 'success')
        }
        onRowClick={row => showNotification(`Clicked on: ${row.name}`, 'info')}
        onRowView={row => showNotification(`Viewing details for: ${row.name} 👁️`, 'info')}
        onRowEdit={row => showNotification(`Editing: ${row.name} ✏️`, 'warning')}
        onRowDelete={row => showNotification(`Deleted: ${row.name} 🗑️`, 'error')}
        onBulkAction={(rows, action) =>
          showNotification(`Bulk ${action} on ${rows.length} items! 📦`, 'warning')
        }
      />

      {/* Ultimate Dialogs */}

      {/* Primary Dialog - Form Demo */}
      <Dialog
        open={primaryDialogOpen}
        onClose={() => setPrimaryDialogOpen(false)}
        title="Ultimate Enterprise Form"
        subtitle="Advanced form with enterprise-grade validation and styling"
        icon={<RocketIcon />}
        gradient="primary"
        actions={
          <>
            <Button
              variant="outlined"
              gradient="primary"
              onClick={() => setPrimaryDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button gradient="primary" onClick={handleFormSubmit} icon={<CheckIcon />}>
              Create Enterprise Item
            </Button>
          </>
        }
      >
        <Stack spacing={4}>
          <InputField
            label="Component Name"
            value={formData.name}
            onChange={e => setFormData({ ...formData, name: e.target.value })}
            required
            placeholder="e.g., Ultimate Dashboard Widget"
            gradient="primary"
          />

          <InputField
            label="Description"
            value={formData.description}
            onChange={e => setFormData({ ...formData, description: e.target.value })}
            required
            multiline
            rows={3}
            placeholder="Describe the enterprise component and its capabilities..."
            gradient="secondary"
          />

          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Select
                label="Priority Level"
                value={formData.priority}
                onChange={e => setFormData({ ...formData, priority: e.target.value as string })}
                gradient="success"
              >
                <MenuItem value="Low">🟢 Low Priority</MenuItem>
                <MenuItem value="Medium">🟡 Medium Priority</MenuItem>
                <MenuItem value="High">🟠 High Priority</MenuItem>
                <MenuItem value="Critical">🔴 Critical Priority</MenuItem>
              </Select>
            </Grid>

            <Grid item xs={12} md={6}>
              <Select
                label="Category"
                value={formData.category}
                onChange={e => setFormData({ ...formData, category: e.target.value as string })}
                gradient="info"
              >
                <MenuItem value="Performance">⚡ Performance</MenuItem>
                <MenuItem value="Security">🛡️ Security</MenuItem>
                <MenuItem value="Analytics">📊 Analytics</MenuItem>
                <MenuItem value="Integration">🔗 Integration</MenuItem>
                <MenuItem value="Monitoring">👁️ Monitoring</MenuItem>
              </Select>
            </Grid>
          </Grid>
        </Stack>
      </Dialog>

      {/* Secondary Dialog - Contact Manager */}
      <Dialog
        open={secondaryDialogOpen}
        onClose={() => setSecondaryDialogOpen(false)}
        title="Enterprise Contact Manager"
        subtitle="Advanced contact management with role-based access control"
        icon={<ContactIcon />}
        gradient="secondary"
        actions={
          <>
            <Button
              variant="outlined"
              gradient="secondary"
              onClick={() => setSecondaryDialogOpen(false)}
            >
              Close
            </Button>
            <Button
              gradient="secondary"
              onClick={() => {
                setSecondaryDialogOpen(false);
                showNotification('Contact management accessed! 👥', 'info');
              }}
              icon={<StarIcon />}
            >
              Access Contact System
            </Button>
          </>
        }
      >
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <ContactIcon sx={{ fontSize: 80, color: '#f093fb', mb: 2 }} />
          <Typography variant="h5" fontWeight="bold" gutterBottom>
            Enterprise Contact Management
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            This demo showcases the Ultimate Enterprise Dialog component with advanced styling,
            animations, and professional presentation.
          </Typography>

          <Divider sx={{ my: 3 }} />

          <Grid container spacing={2} sx={{ mt: 2 }}>
            <Grid item xs={6}>
              <Button
                gradient="success"
                fullWidth
                icon={<CheckIcon />}
                onClick={() => showNotification('Feature activated! ✨', 'success')}
              >
                Activate Feature
              </Button>
            </Grid>
            <Grid item xs={6}>
              <Button
                gradient="warning"
                variant="outlined"
                fullWidth
                icon={<SecurityIcon />}
                onClick={() => showNotification('Security check completed! 🔒', 'warning')}
              >
                Security Check
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Dialog>

      {/* Ultimate Snackbar */}
      <Snackbar
        open={snackbarOpen}
        message={snackbarMessage}
        severity={snackbarSeverity}
        onClose={() => setSnackbarOpen(false)}
        position={{ vertical: 'top', horizontal: 'right' }}
      />
    </Box>
  );
};

export default UltimateComponentsDemo;

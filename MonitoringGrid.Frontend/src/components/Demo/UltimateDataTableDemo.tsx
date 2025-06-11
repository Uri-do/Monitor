import React, { useState, useEffect } from 'react';
import { Box, Typography, Grid, Chip, Avatar, LinearProgress, IconButton } from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Business as BusinessIcon,
  Computer as ComputerIcon,
  Storage as StorageIcon,
  Security as SecurityIcon,
  Wifi as NetworkIcon,
} from '@mui/icons-material';
import { DataTable, DataTableColumn, Snackbar } from '@/components';

export const UltimateDataTableDemo: React.FC = () => {
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<
    'success' | 'error' | 'warning' | 'info'
  >('success');

  // Generate comprehensive sample data
  const [tableData, setTableData] = useState(() => {
    const statuses = ['Active', 'Warning', 'Error', 'Maintenance'];
    const priorities = ['Low', 'Medium', 'High', 'Critical'];
    const types = ['Performance', 'Database', 'API', 'Security', 'Network', 'Infrastructure'];
    const teams = [
      'DevOps Team',
      'Database Team',
      'API Team',
      'Security Team',
      'Network Team',
      'Infrastructure Team',
    ];
    const locations = ['US-East', 'US-West', 'EU-Central', 'Asia-Pacific', 'Canada', 'Australia'];

    return Array.from({ length: 50 }, (_, index) => ({
      id: index + 1,
      name: `Monitor ${index + 1}: ${types[index % types.length]} Check`,
      status: statuses[Math.floor(Math.random() * statuses.length)],
      priority: priorities[Math.floor(Math.random() * priorities.length)],
      owner: teams[index % teams.length],
      location: locations[Math.floor(Math.random() * locations.length)],
      lastRun: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000)
        .toISOString()
        .slice(0, 16)
        .replace('T', ' '),
      performance: Math.round((Math.random() * 30 + 70) * 100) / 100,
      type: types[index % types.length],
      uptime: Math.round((Math.random() * 5 + 95) * 100) / 100,
      responseTime: Math.round(Math.random() * 500 + 50),
      errorCount: Math.floor(Math.random() * 20),
      trend: Math.random() > 0.5 ? 'up' : 'down',
      cpu: Math.round(Math.random() * 100 * 100) / 100,
      memory: Math.round(Math.random() * 100 * 100) / 100,
      disk: Math.round(Math.random() * 100 * 100) / 100,
    }));
  });

  // Update some data periodically to show live updates
  useEffect(() => {
    const interval = setInterval(() => {
      setTableData(prev =>
        prev.map(item => ({
          ...item,
          performance: Math.max(60, Math.min(100, item.performance + (Math.random() - 0.5) * 5)),
          responseTime: Math.max(
            10,
            Math.min(1000, item.responseTime + Math.floor((Math.random() - 0.5) * 50))
          ),
          cpu: Math.max(0, Math.min(100, item.cpu + (Math.random() - 0.5) * 10)),
          memory: Math.max(0, Math.min(100, item.memory + (Math.random() - 0.5) * 8)),
          disk: Math.max(0, Math.min(100, item.disk + (Math.random() - 0.5) * 3)),
        }))
      );
    }, 5000);

    return () => clearInterval(interval);
  }, []);

  const showNotification = (
    message: string,
    severity: 'success' | 'error' | 'warning' | 'info'
  ) => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  // Define comprehensive columns with advanced formatting
  const columns: DataTableColumn[] = [
    {
      id: 'name',
      label: 'Monitor Name',
      sortable: true,
      filterable: true,
      width: 250,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Avatar sx={{ width: 32, height: 32, bgcolor: getTypeColor(row.type) }}>
            {getTypeIcon(row.type)}
          </Avatar>
          <Box>
            <Typography variant="body2" fontWeight="bold">
              {value}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {row.type}
            </Typography>
          </Box>
        </Box>
      ),
    },
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
        { label: 'Maintenance', value: 'Maintenance' },
      ],
      format: value => (
        <Chip
          label={value}
          color={getStatusColor(value)}
          size="small"
          sx={{ fontWeight: 'bold' }}
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
        <Chip label={value} color={getPriorityColor(value)} size="small" variant="outlined" />
      ),
    },
    {
      id: 'performance',
      label: 'Performance',
      sortable: true,
      align: 'center' as const,
      width: 120,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Box sx={{ width: 60 }}>
            <LinearProgress
              variant="determinate"
              value={value}
              color={value > 90 ? 'success' : value > 70 ? 'warning' : 'error'}
              sx={{ height: 6, borderRadius: 3 }}
            />
          </Box>
          <Typography variant="caption" fontWeight="bold">
            {value}%
          </Typography>
          <IconButton size="small" sx={{ p: 0 }}>
            {row.trend === 'up' ? (
              <TrendingUpIcon fontSize="small" color="success" />
            ) : (
              <TrendingDownIcon fontSize="small" color="error" />
            )}
          </IconButton>
        </Box>
      ),
    },
    {
      id: 'owner',
      label: 'Owner Team',
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'DevOps Team', value: 'DevOps Team' },
        { label: 'Database Team', value: 'Database Team' },
        { label: 'API Team', value: 'API Team' },
        { label: 'Security Team', value: 'Security Team' },
        { label: 'Network Team', value: 'Network Team' },
        { label: 'Infrastructure Team', value: 'Infrastructure Team' },
      ],
    },
    {
      id: 'location',
      label: 'Location',
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'US-East', value: 'US-East' },
        { label: 'US-West', value: 'US-West' },
        { label: 'EU-Central', value: 'EU-Central' },
        { label: 'Asia-Pacific', value: 'Asia-Pacific' },
        { label: 'Canada', value: 'Canada' },
        { label: 'Australia', value: 'Australia' },
      ],
    },
    {
      id: 'uptime',
      label: 'Uptime %',
      sortable: true,
      align: 'right' as const,
      format: value => (
        <Typography
          variant="body2"
          color={value > 99 ? 'success.main' : value > 95 ? 'warning.main' : 'error.main'}
          fontWeight="bold"
        >
          {value}%
        </Typography>
      ),
    },
    {
      id: 'responseTime',
      label: 'Response (ms)',
      sortable: true,
      align: 'right' as const,
      format: value => (
        <Typography
          variant="body2"
          color={value < 100 ? 'success.main' : value < 300 ? 'warning.main' : 'error.main'}
        >
          {value}ms
        </Typography>
      ),
    },
    {
      id: 'errorCount',
      label: 'Errors',
      sortable: true,
      align: 'center' as const,
      format: value => (
        <Chip
          label={value}
          color={value === 0 ? 'success' : value < 5 ? 'warning' : 'error'}
          size="small"
          sx={{ minWidth: 50 }}
        />
      ),
    },
    { id: 'lastRun', label: 'Last Run', sortable: true, width: 150 },
  ];

  // Helper functions for styling
  function getStatusColor(status: string): 'success' | 'warning' | 'error' | 'default' {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Warning':
        return 'warning';
      case 'Error':
        return 'error';
      default:
        return 'default';
    }
  }

  function getPriorityColor(priority: string): 'error' | 'warning' | 'info' | 'default' {
    switch (priority) {
      case 'Critical':
        return 'error';
      case 'High':
        return 'warning';
      case 'Medium':
        return 'info';
      default:
        return 'default';
    }
  }

  function getTypeColor(type: string): string {
    const colors = {
      Performance: '#667eea',
      Database: '#f093fb',
      API: '#43e97b',
      Security: '#ff6b6b',
      Network: '#4facfe',
      Infrastructure: '#fa709a',
    };
    return colors[type as keyof typeof colors] || '#667eea';
  }

  function getTypeIcon(type: string) {
    const icons = {
      Performance: <TrendingUpIcon fontSize="small" />,
      Database: <StorageIcon fontSize="small" />,
      API: <ComputerIcon fontSize="small" />,
      Security: <SecurityIcon fontSize="small" />,
      Network: <NetworkIcon fontSize="small" />,
      Infrastructure: <BusinessIcon fontSize="small" />,
    };
    return icons[type as keyof typeof icons] || <ComputerIcon fontSize="small" />;
  }

  return (
    <Box sx={{ p: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 4, textAlign: 'center' }}>
        <Typography variant="h3" fontWeight="bold" gutterBottom>
          Ultimate DataTable Showcase
        </Typography>
        <Typography variant="h6" color="text.secondary">
          Enterprise-grade data table with advanced features, real-time updates, and professional
          styling
        </Typography>
      </Box>

      {/* Feature Overview */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        {[
          '🔍 Advanced Search & Filtering',
          '📊 Real-time Data Updates',
          '📤 Multi-format Export (CSV, Excel, PDF)',
          '✅ Bulk Selection & Actions',
          '🎨 Custom Cell Formatting',
          '📱 Responsive Design',
          '⚡ High Performance',
          '🎯 Enterprise Ready',
        ].map((feature, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <Chip
              label={feature}
              variant="outlined"
              sx={{ width: '100%', justifyContent: 'flex-start' }}
            />
          </Grid>
        ))}
      </Grid>

      {/* Ultimate DataTable */}
      <DataTable
        title="🚀 Enterprise Monitoring Dashboard"
        subtitle={`Real-time monitoring data with ${tableData.length} active monitors across multiple regions`}
        data={tableData}
        columns={columns}
        gradient="primary"
        searchable
        filterable
        exportable
        refreshable
        selectable
        pagination
        rowsPerPageOptions={[10, 25, 50, 100]}
        defaultRowsPerPage={10}
        maxHeight={600}
        onRefresh={() => {
          // Simulate data refresh
          setTableData(prev =>
            prev.map(item => ({
              ...item,
              lastRun: new Date().toISOString().slice(0, 16).replace('T', ' '),
            }))
          );
          showNotification('Data refreshed successfully! 🔄', 'success');
        }}
        onExport={(data, format) => {
          showNotification(
            `Exported ${data.length} rows as ${format.toUpperCase()}! 📊`,
            'success'
          );
        }}
        onRowClick={row => {
          showNotification(`Viewing details for: ${row.name}`, 'info');
        }}
        onRowView={row => {
          showNotification(`Opening detailed view for: ${row.name} 👁️`, 'info');
        }}
        onRowEdit={row => {
          showNotification(`Editing configuration for: ${row.name} ✏️`, 'warning');
        }}
        onRowDelete={row => {
          showNotification(`Monitor deleted: ${row.name} 🗑️`, 'error');
        }}
        onBulkAction={(rows, action) => {
          showNotification(`Bulk ${action} applied to ${rows.length} monitors! 📦`, 'warning');
        }}
      />

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

export default UltimateDataTableDemo;

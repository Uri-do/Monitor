import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  LinearProgress,
  Tooltip,
} from '@mui/material';
import {
  Computer as SystemIcon,
  CheckCircle as HealthyIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Memory as MemoryIcon,
  Storage as StorageIcon,
  Speed as PerformanceIcon,
  NetworkCheck as NetworkIcon,
} from '@mui/icons-material';

/**
 * System Status Card Component
 * Displays system health metrics and status indicators
 */

export interface SystemMetric {
  name: string;
  value: number;
  unit: string;
  status: 'healthy' | 'warning' | 'critical';
  threshold?: number;
  icon?: React.ReactNode;
}

export interface SystemStatusCardProps {
  overallStatus: 'healthy' | 'warning' | 'critical';
  uptime: string;
  metrics: SystemMetric[];
  lastUpdated?: Date;
}

const getStatusColor = (status: 'healthy' | 'warning' | 'critical') => {
  switch (status) {
    case 'healthy': return 'success';
    case 'warning': return 'warning';
    case 'critical': return 'error';
    default: return 'default';
  }
};

const getStatusIcon = (status: 'healthy' | 'warning' | 'critical') => {
  switch (status) {
    case 'healthy': return <HealthyIcon color="success" />;
    case 'warning': return <WarningIcon color="warning" />;
    case 'critical': return <ErrorIcon color="error" />;
    default: return <HealthyIcon />;
  }
};

const getDefaultIcon = (metricName: string) => {
  const name = metricName.toLowerCase();
  if (name.includes('memory') || name.includes('ram')) return <MemoryIcon />;
  if (name.includes('disk') || name.includes('storage')) return <StorageIcon />;
  if (name.includes('cpu') || name.includes('performance')) return <PerformanceIcon />;
  if (name.includes('network') || name.includes('connection')) return <NetworkIcon />;
  return <SystemIcon />;
};

export const SystemStatusCard: React.FC<SystemStatusCardProps> = ({
  overallStatus,
  uptime,
  metrics,
  lastUpdated,
}) => {
  const statusColor = getStatusColor(overallStatus);

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            {getStatusIcon(overallStatus)}
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              System Status
            </Typography>
          </Box>
          <Chip
            label={overallStatus.toUpperCase()}
            color={statusColor as any}
            sx={{ fontWeight: 600 }}
          />
        </Box>

        {/* Uptime */}
        <Box mb={2}>
          <Typography variant="body2" color="text.secondary">
            Uptime: <strong>{uptime}</strong>
          </Typography>
          {lastUpdated && (
            <Typography variant="caption" color="text.secondary">
              Last updated: {lastUpdated.toLocaleTimeString()}
            </Typography>
          )}
        </Box>

        {/* Metrics */}
        <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
          <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
            System Metrics
          </Typography>
          
          <List sx={{ p: 0 }}>
            {metrics.map((metric, index) => (
              <ListItem key={index} sx={{ px: 0, py: 1 }}>
                <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                  <Box sx={{ color: `${getStatusColor(metric.status)}.main` }}>
                    {metric.icon || getDefaultIcon(metric.name)}
                  </Box>
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                      <Typography variant="body2" fontWeight="medium">
                        {metric.name}
                      </Typography>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Typography variant="body2" fontWeight="bold">
                          {metric.value}{metric.unit}
                        </Typography>
                        <Chip
                          label={metric.status}
                          color={getStatusColor(metric.status) as any}
                          size="small"
                          sx={{ fontSize: '0.6rem', height: 16 }}
                        />
                      </Box>
                    </Box>
                  }
                  secondary={
                    metric.threshold && (
                      <Tooltip title={`Threshold: ${metric.threshold}${metric.unit}`}>
                        <LinearProgress
                          variant="determinate"
                          value={Math.min((metric.value / metric.threshold) * 100, 100)}
                          color={getStatusColor(metric.status) as any}
                          sx={{
                            height: 4,
                            borderRadius: 2,
                            backgroundColor: 'grey.200',
                          }}
                        />
                      </Tooltip>
                    )
                  }
                  primaryTypographyProps={{
                    component: 'div',
                  }}
                />
              </ListItem>
            ))}
          </List>
        </Box>

        {/* Status Summary */}
        <Box sx={{ mt: 2, p: 1, backgroundColor: `${statusColor}.50`, borderRadius: 1 }}>
          <Typography variant="caption" color={`${statusColor}.main`} sx={{ fontWeight: 600 }}>
            {overallStatus === 'healthy' && 'All systems operational'}
            {overallStatus === 'warning' && 'Some systems need attention'}
            {overallStatus === 'critical' && 'Critical issues detected'}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  );
};

// Predefined system metrics
export const createCpuMetric = (value: number): SystemMetric => ({
  name: 'CPU Usage',
  value,
  unit: '%',
  status: value > 80 ? 'critical' : value > 60 ? 'warning' : 'healthy',
  threshold: 100,
  icon: <PerformanceIcon />,
});

export const createMemoryMetric = (value: number): SystemMetric => ({
  name: 'Memory Usage',
  value,
  unit: '%',
  status: value > 85 ? 'critical' : value > 70 ? 'warning' : 'healthy',
  threshold: 100,
  icon: <MemoryIcon />,
});

export const createDiskMetric = (value: number): SystemMetric => ({
  name: 'Disk Usage',
  value,
  unit: '%',
  status: value > 90 ? 'critical' : value > 75 ? 'warning' : 'healthy',
  threshold: 100,
  icon: <StorageIcon />,
});

export const createNetworkMetric = (value: number): SystemMetric => ({
  name: 'Network Latency',
  value,
  unit: 'ms',
  status: value > 200 ? 'critical' : value > 100 ? 'warning' : 'healthy',
  threshold: 500,
  icon: <NetworkIcon />,
});

export default SystemStatusCard;

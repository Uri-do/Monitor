import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Alert,
  AlertTitle,
  CircularProgress,
  IconButton,
  Tooltip,
  Badge,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
  Checkbox
} from '@mui/material';
import {
  Warning as AlertTriangle,
  CheckCircle,
  Refresh,
  Notifications,
  NotificationsActive,
  Warning,
  Error,
  Info
} from '@mui/icons-material';
import { 
  useCriticalAlerts, 
  useUnresolvedAlerts, 
  useEnhancedAlertStatistics,
  useManualAlert 
} from '@/hooks/useEnhancedApi';
import { useAlerts } from '@/hooks/useAlerts';
import { useSignalR } from '@/services/signalRService';
import { AlertFilterDto } from '@/types/api';

interface EnhancedAlertManagementProps {
  className?: string;
}

export const EnhancedAlertManagement: React.FC<EnhancedAlertManagementProps> = ({ className }) => {
  const [selectedAlerts, setSelectedAlerts] = useState<number[]>([]);
  const [statisticsPeriod] = useState(30);
  
  // Filters
  const [filters] = useState<AlertFilterDto>({
    page: 1,
    pageSize: 20,
    sortDirection: 'desc'
  });

  const { data: criticalAlerts, loading: criticalLoading, refetch: refetchCritical } = useCriticalAlerts(true, 15000);
  const { data: unresolvedAlerts, loading: unresolvedLoading, refetch: refetchUnresolved } = useUnresolvedAlerts(true, 20000);
  const { data: alertStatistics } = useEnhancedAlertStatistics(statisticsPeriod);
  const { sendAlert: sendManualAlert, loading: sendingAlert } = useManualAlert();
  
  const { 
    data: alertsData, 
    loading: alertsLoading,
    refetch: refetchAlerts,
    resolveAlert,
    bulkResolveAlerts
  } = useAlerts(filters);

  const { isConnected } = useSignalR();

  const handleBulkResolve = async () => {
    if (selectedAlerts.length === 0) return;
    
    try {
      await bulkResolveAlerts({
        alertIds: selectedAlerts,
        resolvedBy: 'Current User',
        resolutionNotes: 'Bulk resolved from alert management interface'
      });
      setSelectedAlerts([]);
      refetchAlerts();
      refetchCritical();
      refetchUnresolved();
    } catch (error) {
      console.error('Failed to bulk resolve alerts:', error);
    }
  };

  const handleSendManualAlert = async () => {
    try {
      const result = await sendManualAlert(
        1, // This should be selected from a KPI dropdown
        'Manual test alert from enhanced interface',
        'This alert was sent manually for testing purposes',
        2
      );
      alert(`Manual alert sent successfully! Alert ID: ${result.alertId}`);
      refetchAlerts();
      refetchCritical();
      refetchUnresolved();
    } catch (error) {
      console.error('Failed to send manual alert:', error);
    }
  };

  const handleResolveAlert = async (alertId: number) => {
    try {
      await resolveAlert(alertId, {
        alertId,
        resolvedBy: 'Current User',
        resolutionNotes: 'Resolved from enhanced alert management'
      });
      refetchAlerts();
      refetchCritical();
      refetchUnresolved();
    } catch (error) {
      console.error('Failed to resolve alert:', error);
    }
  };

  if (alertsLoading && criticalLoading && unresolvedLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress size={40} />
      </Box>
    );
  }

  return (
    <Box className={className} sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Enhanced Alert Management
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
          <Chip
            icon={isConnected ? <NotificationsActive /> : <Notifications />}
            label={isConnected ? "Live Updates" : "Offline"}
            color={isConnected ? "success" : "error"}
            variant="outlined"
          />
          <Button
            onClick={handleSendManualAlert}
            disabled={sendingAlert}
            variant="outlined"
            startIcon={<AlertTriangle />}
          >
            {sendingAlert ? 'Sending...' : 'Send Test Alert'}
          </Button>
          <Tooltip title="Refresh All Data">
            <IconButton onClick={() => {
              refetchAlerts();
              refetchCritical();
              refetchUnresolved();
            }}>
              <Refresh />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Quick Stats */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Critical Alerts
                  </Typography>
                  <Typography variant="h4" component="div" color="error.main">
                    {criticalAlerts?.length || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Require immediate attention
                  </Typography>
                </Box>
                <Badge badgeContent={criticalAlerts?.length || 0} color="error">
                  <Error color="error" />
                </Badge>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Unresolved
                  </Typography>
                  <Typography variant="h4" component="div" color="warning.main">
                    {unresolvedAlerts?.length || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Pending resolution
                  </Typography>
                </Box>
                <Badge badgeContent={unresolvedAlerts?.length || 0} color="warning">
                  <Warning color="warning" />
                </Badge>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Total Alerts
                  </Typography>
                  <Typography variant="h4" component="div">
                    {alertsData?.totalCount || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    In current filter
                  </Typography>
                </Box>
                <Notifications color="primary" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography color="text.secondary" gutterBottom variant="body2">
                    Resolution Rate
                  </Typography>
                  <Typography variant="h4" component="div" color="success.main">
                    {alertStatistics ? 
                      ((alertStatistics.resolvedAlerts / alertStatistics.totalAlerts) * 100).toFixed(1) : 0}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Last {statisticsPeriod} days
                  </Typography>
                </Box>
                <CheckCircle color="success" />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Critical Alerts Banner */}
      {criticalAlerts && criticalAlerts.length > 0 && (
        <Alert severity="error" sx={{ mb: 3 }}>
          <AlertTitle>Critical Alerts</AlertTitle>
          <strong>{criticalAlerts.length} critical alerts</strong> require immediate attention.
        </Alert>
      )}

      {/* Bulk Actions */}
      {selectedAlerts.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="body1">
                {selectedAlerts.length} alert(s) selected
              </Typography>
              <Box display="flex" gap={2}>
                <Button 
                  variant="contained" 
                  color="success"
                  onClick={handleBulkResolve}
                  startIcon={<CheckCircle />}
                >
                  Bulk Resolve
                </Button>
                <Button 
                  variant="outlined" 
                  onClick={() => setSelectedAlerts([])}
                >
                  Clear Selection
                </Button>
              </Box>
            </Box>
          </CardContent>
        </Card>
      )}

      {/* Critical Alerts List */}
      {criticalAlerts && criticalAlerts.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom color="error.main">
              🚨 Critical Alerts
            </Typography>
            <List>
              {criticalAlerts.map((alert, index) => (
                <ListItem 
                  key={index}
                  sx={{
                    border: 1,
                    borderColor: 'error.main',
                    borderRadius: 1,
                    mb: 1,
                    backgroundColor: 'error.light',
                    opacity: 0.9
                  }}
                >
                  <ListItemIcon>
                    <Checkbox
                      checked={selectedAlerts.includes(alert.alertId)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedAlerts(prev => [...prev, alert.alertId]);
                        } else {
                          setSelectedAlerts(prev => prev.filter(id => id !== alert.alertId));
                        }
                      }}
                    />
                  </ListItemIcon>
                  <ListItemIcon>
                    <Error color="error" />
                  </ListItemIcon>
                  <ListItemText
                    primary={alert.message}
                    secondary={`KPI: ${alert.kpiIndicator} | Owner: ${alert.kpiOwner} | ${new Date(alert.triggerTime).toLocaleString()}`}
                  />
                  <ListItemSecondaryAction>
                    <Button
                      size="small"
                      variant="contained"
                      color="success"
                      onClick={() => handleResolveAlert(alert.alertId)}
                      startIcon={<CheckCircle />}
                    >
                      Resolve
                    </Button>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      )}

      {/* Unresolved Alerts List */}
      {unresolvedAlerts && unresolvedAlerts.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom color="warning.main">
              ⚠️ Unresolved Alerts
            </Typography>
            <List>
              {unresolvedAlerts.slice(0, 10).map((alert, index) => (
                <ListItem 
                  key={index}
                  sx={{
                    border: 1,
                    borderColor: 'warning.main',
                    borderRadius: 1,
                    mb: 1,
                    backgroundColor: 'warning.light',
                    opacity: 0.9
                  }}
                >
                  <ListItemIcon>
                    <Checkbox
                      checked={selectedAlerts.includes(alert.alertId)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedAlerts(prev => [...prev, alert.alertId]);
                        } else {
                          setSelectedAlerts(prev => prev.filter(id => id !== alert.alertId));
                        }
                      }}
                    />
                  </ListItemIcon>
                  <ListItemIcon>
                    <Warning color="warning" />
                  </ListItemIcon>
                  <ListItemText
                    primary={alert.message}
                    secondary={`KPI: ${alert.kpiIndicator} | Owner: ${alert.kpiOwner} | ${new Date(alert.triggerTime).toLocaleString()}`}
                  />
                  <ListItemSecondaryAction>
                    <Button
                      size="small"
                      variant="contained"
                      color="success"
                      onClick={() => handleResolveAlert(alert.alertId)}
                      startIcon={<CheckCircle />}
                    >
                      Resolve
                    </Button>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      )}

      {/* No Alerts */}
      {(!criticalAlerts || criticalAlerts.length === 0) && 
       (!unresolvedAlerts || unresolvedAlerts.length === 0) && (
        <Card>
          <CardContent>
            <Box textAlign="center" py={4}>
              <CheckCircle sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
              <Typography variant="h5" color="success.main" gutterBottom>
                ✅ All Clear!
              </Typography>
              <Typography color="text.secondary">
                No critical or unresolved alerts at this time.
              </Typography>
            </Box>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

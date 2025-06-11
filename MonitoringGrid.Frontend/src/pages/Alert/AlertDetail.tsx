import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  Stack,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Paper,
  Divider,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  CheckCircle as ResolveIcon,
  Visibility as ViewIcon,
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Email as EmailIcon,
  Sms as SmsIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Schedule as ScheduleIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { alertApi } from '@/services/api';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  PageHeader,
  StatusChip,
  LoadingSpinner,
} from '@/components/UI';

const AlertDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [resolveDialogOpen, setResolveDialogOpen] = useState(false);
  const [resolutionNotes, setResolutionNotes] = useState('');

  const alertId = parseInt(id || '0');

  // Fetch alert details
  const { data: alert, isLoading: alertLoading } = useQuery({
    queryKey: ['alert', alertId],
    queryFn: () => alertApi.getAlert(alertId),
    enabled: !!alertId,
  });

  // Resolve alert mutation
  const resolveMutation = useMutation({
    mutationFn: () =>
      alertApi.resolveAlert({
        alertId,
        resolvedBy: 'Current User', // TODO: Get from auth context
        resolutionNotes: resolutionNotes || undefined,
      }),
    onSuccess: () => {
      toast.success('Alert resolved successfully');
      setResolveDialogOpen(false);
      setResolutionNotes('');
      queryClient.invalidateQueries({ queryKey: ['alert', alertId] });
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to resolve alert');
    },
  });

  const handleResolve = () => {
    resolveMutation.mutate();
  };

  const getSeverityIcon = (severity: string | undefined) => {
    if (!severity) return <InfoIcon />;
    switch (severity.toLowerCase()) {
      case 'critical':
        return <ErrorIcon color="error" />;
      case 'high':
        return <WarningIcon color="warning" />;
      case 'medium':
        return <InfoIcon color="info" />;
      case 'low':
        return <InfoIcon color="success" />;
      default:
        return <InfoIcon />;
    }
  };

  const getSentViaIcon = (sentVia: number) => {
    switch (sentVia) {
      case 1:
        return <EmailIcon color="primary" />;
      case 2:
        return <SmsIcon color="primary" />;
      default:
        return <InfoIcon />;
    }
  };

  if (alertLoading) {
    return <LoadingSpinner />;
  }

  if (!alert) {
    return (
      <Box>
        <Alert severity="error">Alert not found or you don't have permission to view it.</Alert>
      </Box>
    );
  }

  const deviationTrend = alert.deviationPercent && alert.deviationPercent > 0 ? 'up' : 'down';

  return (
    <Box>
      <PageHeader
        title={`Alert #${alert.alertId}`}
        subtitle={`${alert.kpiIndicator} • ${alert.severity} severity`}
        breadcrumbs={[{ label: 'Alerts', href: '/alerts' }, { label: `Alert #${alert.alertId}` }]}
        primaryAction={
          !alert.isResolved
            ? {
                label: 'Resolve Alert',
                icon: <ResolveIcon />,
                onClick: () => setResolveDialogOpen(true),
                gradient: 'success',
              }
            : undefined
        }
        actions={[
          {
            label: 'View KPI',
            icon: <ViewIcon />,
            onClick: () => navigate(`/kpis/${alert.kpiId}`),
            variant: 'outlined',
          },
        ]}
      />

      <Grid container spacing={3}>
        {/* Alert Overview */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={2} mb={2}>
                {getSeverityIcon(alert.severity)}
                <Typography variant="h6">Alert Information</Typography>
                <StatusChip status={alert.isResolved ? 'resolved' : 'unresolved'} />
              </Stack>

              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <Typography variant="body2" color="textSecondary">
                    Message
                  </Typography>
                  <Paper variant="outlined" sx={{ p: 2, mt: 0.5, bgcolor: 'grey.50' }}>
                    <Typography variant="body1">{alert.message}</Typography>
                  </Paper>
                </Grid>

                {alert.details && (
                  <Grid item xs={12}>
                    <Typography variant="body2" color="textSecondary">
                      Details
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, mt: 0.5, bgcolor: 'grey.50' }}>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                        {alert.details}
                      </Typography>
                    </Paper>
                  </Grid>
                )}

                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    KPI
                  </Typography>
                  <Typography variant="body1">{alert.kpiIndicator}</Typography>
                  <Typography variant="caption" color="textSecondary">
                    Owner: {alert.kpiOwner}
                  </Typography>
                </Grid>

                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Severity
                  </Typography>
                  <Chip
                    icon={getSeverityIcon(alert.severity)}
                    label={alert.severity}
                    color={
                      alert.severity.toLowerCase() === 'critical'
                        ? 'error'
                        : alert.severity.toLowerCase() === 'high'
                          ? 'warning'
                          : alert.severity.toLowerCase() === 'medium'
                            ? 'info'
                            : 'success'
                    }
                    sx={{ mt: 0.5 }}
                  />
                </Grid>

                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Triggered
                  </Typography>
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <ScheduleIcon fontSize="small" color="action" />
                    <Typography variant="body1">
                      {format(new Date(alert.triggerTime), 'MMM dd, yyyy HH:mm:ss')}
                    </Typography>
                  </Stack>
                </Grid>

                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Notification
                  </Typography>
                  <Stack direction="row" alignItems="center" spacing={1}>
                    {getSentViaIcon(alert.sentVia)}
                    <Typography variant="body1">
                      {alert.sentViaName} to {alert.sentTo}
                    </Typography>
                  </Stack>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Metrics and Status */}
        <Grid item xs={12} md={4}>
          <Stack spacing={2}>
            {/* Deviation Metrics */}
            {(alert.currentValue !== undefined || alert.historicalValue !== undefined) && (
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Deviation Metrics
                  </Typography>
                  <Stack spacing={2}>
                    {alert.currentValue !== undefined && (
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          Current Value
                        </Typography>
                        <Typography variant="h6" color="primary">
                          {alert.currentValue.toLocaleString()}
                        </Typography>
                      </Box>
                    )}

                    {alert.historicalValue !== undefined && (
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          Historical Value
                        </Typography>
                        <Typography variant="h6">
                          {alert.historicalValue.toLocaleString()}
                        </Typography>
                      </Box>
                    )}

                    {alert.deviationPercent !== undefined && (
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          Deviation
                        </Typography>
                        <Stack direction="row" alignItems="center" spacing={1}>
                          {deviationTrend === 'up' ? (
                            <TrendingUpIcon color="error" />
                          ) : (
                            <TrendingDownIcon color="success" />
                          )}
                          <Typography
                            variant="h6"
                            color={deviationTrend === 'up' ? 'error' : 'success'}
                          >
                            {Math.abs(alert.deviationPercent).toFixed(2)}%
                          </Typography>
                        </Stack>
                      </Box>
                    )}
                  </Stack>
                </CardContent>
              </Card>
            )}

            {/* Resolution Status */}
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Resolution Status
                </Typography>
                {alert.isResolved ? (
                  <Stack spacing={1}>
                    <StatusChip status="resolved" />
                    {alert.resolvedTime && (
                      <Typography variant="body2">
                        Resolved: {format(new Date(alert.resolvedTime), 'MMM dd, yyyy HH:mm')}
                      </Typography>
                    )}
                    {alert.resolvedBy && (
                      <Typography variant="body2">By: {alert.resolvedBy}</Typography>
                    )}
                  </Stack>
                ) : (
                  <Stack spacing={2}>
                    <StatusChip status="unresolved" />
                    <Button
                      variant="contained"
                      color="success"
                      startIcon={<ResolveIcon />}
                      onClick={() => setResolveDialogOpen(true)}
                      fullWidth
                    >
                      Resolve Alert
                    </Button>
                  </Stack>
                )}
              </CardContent>
            </Card>
          </Stack>
        </Grid>
      </Grid>

      {/* Resolve Dialog */}
      <Dialog
        open={resolveDialogOpen}
        onClose={() => setResolveDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Resolve Alert</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Mark this alert as resolved. You can optionally add resolution notes.
          </Typography>
          <TextField
            autoFocus
            margin="dense"
            label="Resolution Notes (optional)"
            multiline
            rows={3}
            fullWidth
            variant="outlined"
            value={resolutionNotes}
            onChange={e => setResolutionNotes(e.target.value)}
            placeholder="Describe how the issue was resolved..."
            sx={{ mt: 2 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResolveDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleResolve}
            variant="contained"
            color="success"
            disabled={resolveMutation.isPending}
            startIcon={<ResolveIcon />}
          >
            {resolveMutation.isPending ? 'Resolving...' : 'Resolve Alert'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AlertDetail;

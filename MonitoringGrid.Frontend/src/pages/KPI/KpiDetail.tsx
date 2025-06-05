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
  Divider,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import {
  PlayArrow as ExecuteIcon,
  Edit as EditIcon,
  History as HistoryIcon,
  Assessment as MetricsIcon,
  Contacts as ContactsIcon,
  Schedule as ScheduleIcon,
  TrendingUp as TrendingUpIcon,
  Error as ErrorIcon,
  CheckCircle as SuccessIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { kpiApi } from '@/services/api';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  PageHeader,
  StatusChip,
  LoadingSpinner,
} from '@/components/Common';

const KpiDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [executeDialogOpen, setExecuteDialogOpen] = useState(false);
  const [customFrequency, setCustomFrequency] = useState('');

  const kpiId = parseInt(id || '0');

  // Fetch KPI details
  const { data: kpi, isLoading: kpiLoading } = useQuery({
    queryKey: ['kpi', kpiId],
    queryFn: () => kpiApi.getKpi(kpiId),
    enabled: !!kpiId,
  });

  // Fetch KPI metrics
  const { data: metrics, isLoading: metricsLoading } = useQuery({
    queryKey: ['kpi-metrics', kpiId],
    queryFn: () => kpiApi.getKpiMetrics(kpiId),
    enabled: !!kpiId,
  });

  // Execute KPI mutation
  const executeMutation = useMutation({
    mutationFn: (frequency?: number) => kpiApi.executeKpi({
      kpiId,
      customFrequency: frequency,
    }),
    onSuccess: (result) => {
      toast.success('KPI executed successfully');
      setExecuteDialogOpen(false);
      setCustomFrequency('');
      queryClient.invalidateQueries({ queryKey: ['kpi', kpiId] });
      queryClient.invalidateQueries({ queryKey: ['kpi-metrics', kpiId] });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to execute KPI');
    },
  });

  const handleExecute = () => {
    const frequency = customFrequency ? parseInt(customFrequency) : undefined;
    executeMutation.mutate(frequency);
  };

  if (kpiLoading) {
    return <LoadingSpinner />;
  }

  if (!kpi) {
    return (
      <Box>
        <Alert severity="error">
          KPI not found or you don't have permission to view it.
        </Alert>
      </Box>
    );
  }

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
      case 'active':
        return 'success';
      case 'due':
      case 'due soon':
        return 'warning';
      case 'error':
      case 'never run':
        return 'error';
      default:
        return 'default';
    }
  };

  return (
    <Box>
      <PageHeader
        title={kpi.indicator}
        subtitle={`Owner: ${kpi.owner} • Priority: ${kpi.priorityName}`}
        breadcrumbs={[
          { label: 'KPIs', href: '/kpis' },
          { label: kpi.indicator },
        ]}
        primaryAction={{
          label: 'Execute Now',
          icon: <ExecuteIcon />,
          onClick: () => setExecuteDialogOpen(true),
          disabled: !kpi.isActive || executeMutation.isPending,
        }}
        actions={[
          {
            label: 'Edit',
            icon: <EditIcon />,
            onClick: () => navigate(`/kpis/${kpiId}/edit`),
          },
        ]}
      />

      <Grid container spacing={3}>
        {/* KPI Overview */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                KPI Information
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Status
                  </Typography>
                  <StatusChip
                    status={kpi.isActive ? 'active' : 'inactive'}
                    sx={{ mt: 0.5 }}
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Priority
                  </Typography>
                  <Chip
                    label={kpi.priorityName}
                    color={kpi.priority <= 2 ? 'error' : kpi.priority === 3 ? 'warning' : 'success'}
                    size="small"
                    sx={{ mt: 0.5 }}
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Frequency
                  </Typography>
                  <Typography variant="body1">
                    Every {kpi.frequency} minutes
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Deviation Threshold
                  </Typography>
                  <Typography variant="body1">
                    {kpi.deviation}%
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Cooldown Period
                  </Typography>
                  <Typography variant="body1">
                    {kpi.cooldownMinutes} minutes
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Minimum Threshold
                  </Typography>
                  <Typography variant="body1">
                    {kpi.minimumThreshold || 'Not set'}
                  </Typography>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" color="textSecondary">
                    Stored Procedure
                  </Typography>
                  <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                    {kpi.spName}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Quick Stats */}
        <Grid item xs={12} md={4}>
          <Stack spacing={2}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Execution Status
                </Typography>
                <Stack spacing={1}>
                  <Box display="flex" justifyContent="space-between">
                    <Typography variant="body2" color="textSecondary">
                      Last Run
                    </Typography>
                    <Typography variant="body2">
                      {kpi.lastRun ? format(new Date(kpi.lastRun), 'MMM dd, HH:mm') : 'Never'}
                    </Typography>
                  </Box>
                  {metrics && (
                    <>
                      <Box display="flex" justifyContent="space-between">
                        <Typography variant="body2" color="textSecondary">
                          Success Rate
                        </Typography>
                        <Typography variant="body2">
                          {metrics.successRate.toFixed(1)}%
                        </Typography>
                      </Box>
                      <Box display="flex" justifyContent="space-between">
                        <Typography variant="body2" color="textSecondary">
                          Total Executions
                        </Typography>
                        <Typography variant="body2">
                          {metrics.totalExecutions}
                        </Typography>
                      </Box>
                      <Box display="flex" justifyContent="space-between">
                        <Typography variant="body2" color="textSecondary">
                          Total Alerts
                        </Typography>
                        <Typography variant="body2">
                          {metrics.totalAlerts}
                        </Typography>
                      </Box>
                    </>
                  )}
                </Stack>
              </CardContent>
            </Card>

            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Assigned Contacts
                </Typography>
                {kpi.contacts.length > 0 ? (
                  <Stack spacing={1}>
                    {kpi.contacts.map((contact) => (
                      <Box key={contact.contactId} display="flex" alignItems="center" gap={1}>
                        <ContactsIcon fontSize="small" color="action" />
                        <Typography variant="body2">
                          {contact.name}
                        </Typography>
                        <StatusChip
                          status={contact.isActive ? 'active' : 'inactive'}
                          size="small"
                        />
                      </Box>
                    ))}
                  </Stack>
                ) : (
                  <Typography variant="body2" color="textSecondary">
                    No contacts assigned
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Stack>
        </Grid>

        {/* Templates */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Notification Templates
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary" gutterBottom>
                    Subject Template
                  </Typography>
                  <Paper variant="outlined" sx={{ p: 2, bgcolor: 'grey.50' }}>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                      {kpi.subjectTemplate}
                    </Typography>
                  </Paper>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary" gutterBottom>
                    Description Template
                  </Typography>
                  <Paper variant="outlined" sx={{ p: 2, bgcolor: 'grey.50' }}>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                      {kpi.descriptionTemplate}
                    </Typography>
                  </Paper>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Metrics and Trends */}
        {metrics && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Performance Metrics
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center" p={2}>
                      <Typography variant="h4" color="primary">
                        {metrics.totalExecutions}
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        Total Executions
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center" p={2}>
                      <Typography variant="h4" color="success.main">
                        {metrics.successfulExecutions}
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        Successful
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center" p={2}>
                      <Typography variant="h4" color="error.main">
                        {metrics.failedExecutions}
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        Failed
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Box textAlign="center" p={2}>
                      <Typography variant="h4" color="warning.main">
                        {metrics.totalAlerts}
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        Alerts Generated
                      </Typography>
                    </Box>
                  </Grid>
                </Grid>

                <Divider sx={{ my: 2 }} />

                <Typography variant="body2" color="textSecondary">
                  Average Execution Time: {metrics.averageExecutionTime}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        )}

        {/* Audit Information */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Audit Information
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Created Date
                  </Typography>
                  <Typography variant="body1">
                    {format(new Date(kpi.createdDate), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Typography variant="body2" color="textSecondary">
                    Last Modified
                  </Typography>
                  <Typography variant="body1">
                    {format(new Date(kpi.modifiedDate), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Execute Dialog */}
      <Dialog open={executeDialogOpen} onClose={() => setExecuteDialogOpen(false)}>
        <DialogTitle>Execute KPI</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Execute "{kpi.indicator}" manually. You can optionally specify a custom frequency.
          </Typography>
          <TextField
            autoFocus
            margin="dense"
            label="Custom Frequency (minutes)"
            type="number"
            fullWidth
            variant="outlined"
            value={customFrequency}
            onChange={(e) => setCustomFrequency(e.target.value)}
            helperText="Leave empty to use default frequency"
            sx={{ mt: 2 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setExecuteDialogOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleExecute}
            variant="contained"
            disabled={executeMutation.isPending}
            startIcon={executeMutation.isPending ? <CircularProgress size={16} /> : <ExecuteIcon />}
          >
            Execute
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default KpiDetail;

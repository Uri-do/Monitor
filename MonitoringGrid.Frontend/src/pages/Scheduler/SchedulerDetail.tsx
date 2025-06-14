import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Alert,
  Button,
  Grid,
  Chip,
  Divider,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  CircularProgress,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow,
  Stop,
  Visibility as ViewIcon,
  Schedule as ScheduleIcon,
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { schedulerApi, indicatorApi } from '@/services/api';
import { SchedulerDto } from '@/types/api';
import { PageHeader, LoadingSpinner, StatusChip } from '@/components';

/**
 * SchedulerDetail component for viewing scheduler information
 */
const SchedulerDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const schedulerId = id ? parseInt(id, 10) : undefined;

  // Fetch scheduler data
  const {
    data: scheduler,
    isLoading,
    error: loadError,
  } = useQuery({
    queryKey: ['scheduler', schedulerId],
    queryFn: () => schedulerApi.getScheduler(schedulerId!),
    enabled: !!schedulerId,
  });

  // Fetch indicators using this scheduler
  const { data: indicators, isLoading: isLoadingIndicators } = useQuery({
    queryKey: ['indicators'],
    queryFn: () => indicatorApi.getIndicators(),
  });

  // Delete scheduler mutation
  const deleteMutation = useMutation({
    mutationFn: (id: number) => schedulerApi.deleteScheduler(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      navigate('/schedulers');
    },
    onError: (error: any) => {
      console.error('Failed to delete scheduler:', error);
    },
  });

  // Toggle scheduler mutation
  const toggleMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: number; enabled: boolean }) =>
      schedulerApi.toggleScheduler(id, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduler', schedulerId] });
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
    },
    onError: (error: any) => {
      console.error('Failed to toggle scheduler:', error);
    },
  });

  const handleDelete = () => {
    if (
      scheduler &&
      window.confirm(`Are you sure you want to delete scheduler "${scheduler.schedulerName}"?`)
    ) {
      deleteMutation.mutate(schedulerId!);
    }
  };

  const handleToggle = () => {
    if (scheduler) {
      toggleMutation.mutate({ id: schedulerId!, enabled: !scheduler.isEnabled });
    }
  };

  const getScheduleTypeColor = (scheduleType: string) => {
    switch (scheduleType) {
      case 'interval':
        return 'primary';
      case 'cron':
        return 'secondary';
      case 'onetime':
        return 'warning';
      default:
        return 'default';
    }
  };

  const getStatusColor = (isEnabled: boolean) => {
    return isEnabled ? 'success' : 'error';
  };

  // Filter indicators that use this scheduler
  const schedulerIndicators = indicators?.filter(ind => ind.schedulerID === schedulerId) || [];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (loadError || !scheduler) {
    return (
      <Box>
        <Alert severity="error">Failed to load scheduler details. Please try again.</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title={scheduler.schedulerName}
        subtitle={scheduler.schedulerDescription || 'Scheduler configuration details'}
        icon={<ScheduleIcon />}
        backAction={{
          label: 'Back to Schedulers',
          icon: <BackIcon />,
          onClick: () => navigate('/schedulers'),
        }}
        primaryAction={
          !toggleMutation.isPending
            ? {
                label: scheduler.isEnabled ? 'Disable' : 'Enable',
                icon: scheduler.isEnabled ? <Stop /> : <PlayArrow />,
                onClick: handleToggle,
                gradient: scheduler.isEnabled ? 'warning' : 'success',
              }
            : undefined
        }
        actions={[
          {
            label: 'Edit',
            icon: <EditIcon />,
            onClick: () => navigate(`/schedulers/${schedulerId}/edit`),
          },
          ...(deleteMutation.isPending || schedulerIndicators.length > 0
            ? []
            : [
                {
                  label: 'Delete',
                  icon: <DeleteIcon />,
                  onClick: handleDelete,
                  gradient: 'error' as const,
                },
              ]),
        ]}
      />

      <Grid container spacing={3}>
        {/* Scheduler Information */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Scheduler Configuration
              </Typography>
              <Divider sx={{ mb: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Typography variant="body2" color="text.secondary">
                    Schedule Type
                  </Typography>
                  <StatusChip status={scheduler.scheduleType} />
                </Grid>

                <Grid item xs={6}>
                  <Typography variant="body2" color="text.secondary">
                    Status
                  </Typography>
                  <StatusChip status={scheduler.isEnabled ? 'active' : 'inactive'} />
                </Grid>

                <Grid item xs={12}>
                  <Typography variant="body2" color="text.secondary">
                    Schedule Display
                  </Typography>
                  <Typography variant="body1" sx={{ mt: 0.5 }}>
                    {scheduler.displayText}
                  </Typography>
                </Grid>

                {scheduler.timezone && (
                  <Grid item xs={12}>
                    <Typography variant="body2" color="text.secondary">
                      Timezone
                    </Typography>
                    <Typography variant="body1" sx={{ mt: 0.5 }}>
                      {scheduler.timezone}
                    </Typography>
                  </Grid>
                )}

                {scheduler.nextExecutionTime && (
                  <Grid item xs={12}>
                    <Typography variant="body2" color="text.secondary">
                      Next Execution
                    </Typography>
                    <Typography variant="body1" sx={{ mt: 0.5 }}>
                      {new Date(scheduler.nextExecutionTime).toLocaleString()}
                    </Typography>
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Assigned Indicators */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Assigned Indicators ({schedulerIndicators.length})
              </Typography>
              <Divider sx={{ mb: 2 }} />

              {isLoadingIndicators ? (
                <CircularProgress size={24} />
              ) : schedulerIndicators.length > 0 ? (
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Indicator</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell align="right">Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {schedulerIndicators.map(indicator => (
                        <TableRow key={indicator.indicatorID}>
                          <TableCell>
                            <Typography variant="body2" fontWeight="medium">
                              {indicator.indicatorName}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {indicator.indicatorCode}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <StatusChip status={indicator.isActive ? 'active' : 'inactive'} />
                          </TableCell>
                          <TableCell align="right">
                            <Button
                              size="small"
                              startIcon={<ViewIcon />}
                              onClick={() => navigate(`/indicators/${indicator.indicatorID}`)}
                            >
                              View
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No indicators are currently assigned to this scheduler.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Metadata */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Metadata
              </Typography>
              <Divider sx={{ mb: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">
                    Created Date
                  </Typography>
                  <Typography variant="body1">
                    {new Date(scheduler.createdDate).toLocaleString()}
                  </Typography>
                </Grid>

                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">
                    Created By
                  </Typography>
                  <Typography variant="body1">{scheduler.createdBy}</Typography>
                </Grid>

                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">
                    Modified Date
                  </Typography>
                  <Typography variant="body1">
                    {new Date(scheduler.modifiedDate).toLocaleString()}
                  </Typography>
                </Grid>

                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">
                    Modified By
                  </Typography>
                  <Typography variant="body1">{scheduler.modifiedBy}</Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default SchedulerDetail;

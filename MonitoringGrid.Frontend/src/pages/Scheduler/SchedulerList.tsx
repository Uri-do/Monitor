import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Button
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Schedule,
  PlayArrow,
  Stop,
  Visibility as ViewIcon
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { schedulerApi } from '@/services/api';
import { SchedulerDto, CreateSchedulerRequest } from '@/types/api';
import {
  DataTable,
  DataTableColumn,
  PageHeader,
  StatusChip,
  LoadingSpinner,
} from '@/components';

/**
 * SchedulerList component for managing schedulers
 */
const SchedulerList: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filters, setFilters] = useState({
    search: '',
  });
  const [selectedRows, setSelectedRows] = useState<SchedulerDto[]>([]);

  // Fetch schedulers
  const {
    data: schedulers = [],
    isLoading,
    error: loadError,
    refetch
  } = useQuery({
    queryKey: ['schedulers'],
    queryFn: () => schedulerApi.getSchedulers(),
  });

  // Form for creating new scheduler
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateSchedulerRequest>({
    defaultValues: {
      schedulerName: '',
      schedulerDescription: '',
      scheduleType: 'interval',
      intervalMinutes: 60,
      cronExpression: '',
      timezone: 'UTC',
      isEnabled: true,
    },
  });

  // Create scheduler mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateSchedulerRequest) => schedulerApi.createScheduler(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      setCreateDialogOpen(false);
      reset();
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to create scheduler');
    },
  });

  // Filter schedulers based on search
  const filteredSchedulers = useMemo(() => {
    return schedulers.filter(scheduler => {
      const matchesSearch = !filters.search ||
        scheduler.schedulerName.toLowerCase().includes(filters.search.toLowerCase()) ||
        scheduler.schedulerDescription?.toLowerCase().includes(filters.search.toLowerCase()) ||
        scheduler.scheduleType.toLowerCase().includes(filters.search.toLowerCase());

      return matchesSearch;
    });
  }, [schedulers, filters.search]);

  // Delete scheduler mutation
  const deleteMutation = useMutation({
    mutationFn: (schedulerId: number) => schedulerApi.deleteScheduler(schedulerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to delete scheduler');
    },
  });

  const handleCreateScheduler = (data: CreateSchedulerRequest) => {
    setError(null);
    createMutation.mutate(data);
  };

  const handleDeleteScheduler = (scheduler: SchedulerDto) => {
    if (window.confirm(`Are you sure you want to delete scheduler "${scheduler.schedulerName}"?`)) {
      deleteMutation.mutate(scheduler.schedulerID);
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

  const getSchedulerStatus = (scheduler: SchedulerDto): string => {
    return scheduler.isEnabled ? 'active' : 'inactive';
  };

  // Define table columns
  const columns: DataTableColumn<SchedulerDto>[] = [
    {
      id: 'schedulerName',
      label: 'Name',
      sortable: true,
      width: 200,
      render: (value, row) => (
        <Box>
          <Box sx={{ fontWeight: 'medium', mb: 0.5 }}>{value}</Box>
          {row.schedulerDescription && (
            <Box sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>
              {row.schedulerDescription}
            </Box>
          )}
        </Box>
      ),
    },
    {
      id: 'scheduleType',
      label: 'Type',
      sortable: true,
      width: 100,
      render: (value) => <StatusChip status={value} />,
    },
    {
      id: 'displayText',
      label: 'Schedule',
      sortable: false,
      width: 200,
    },
    {
      id: 'isEnabled',
      label: 'Status',
      sortable: true,
      width: 100,
      render: (value, row) => <StatusChip status={getSchedulerStatus(row)} />,
    },
    {
      id: 'indicatorCount',
      label: 'Indicators',
      sortable: true,
      width: 100,
      render: value => `${value} indicators`,
    },
    {
      id: 'nextExecutionTime',
      label: 'Next Run',
      sortable: true,
      width: 150,
      render: value => value ? new Date(value).toLocaleString() : 'N/A',
    },
  ];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (loadError) {
    return (
      <Box>
        <Alert severity="error">
          Failed to load schedulers. Please try again.
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Schedulers"
        subtitle="Manage scheduling configurations for indicators"
        icon={<Schedule />}
        primaryAction={{
          label: 'Create Scheduler',
          icon: <AddIcon />,
          onClick: () => setCreateDialogOpen(true),
        }}
        actions={[
          {
            label: 'Refresh',
            onClick: () => refetch(),
          },
        ]}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <DataTable
        columns={columns}
        data={filteredSchedulers}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        defaultActions={{
          view: scheduler => navigate(`/schedulers/${scheduler.schedulerID}`),
          edit: scheduler => navigate(`/schedulers/${scheduler.schedulerID}/edit`),
          delete: handleDeleteScheduler,
        }}
        actions={[
          {
            label: 'Toggle Status',
            icon: <PlayArrow />,
            onClick: scheduler => {
              // TODO: Implement toggle functionality
              console.log('Toggle scheduler:', scheduler.schedulerID);
            },
          },
        ]}
        emptyMessage="No schedulers found. Create your first scheduler to get started."
        rowKey="schedulerID"
      />

      {/* Create Scheduler Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Schedule sx={{ mr: 1 }} />
            Create New Scheduler
          </Box>
        </DialogTitle>
        <form onSubmit={handleSubmit(handleCreateScheduler)}>
          <DialogContent>
            <Grid container spacing={3} sx={{ mt: 1 }}>
              <Grid item xs={12} md={6}>
                <Controller
                  name="schedulerName"
                  control={control}
                  rules={{ required: 'Scheduler name is required' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Scheduler Name"
                      fullWidth
                      error={!!errors.schedulerName}
                      helperText={errors.schedulerName?.message}
                      required
                    />
                  )}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Controller
                  name="scheduleType"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Schedule Type</InputLabel>
                      <Select {...field} label="Schedule Type" required>
                        <MenuItem value="interval">Interval</MenuItem>
                        <MenuItem value="cron">Cron Expression</MenuItem>
                        <MenuItem value="onetime">One Time</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
              
              <Grid item xs={12}>
                <Controller
                  name="schedulerDescription"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Description"
                      fullWidth
                      multiline
                      rows={2}
                    />
                  )}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Controller
                  name="intervalMinutes"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Interval (minutes)"
                      type="number"
                      fullWidth
                      inputProps={{ min: 1 }}
                      helperText="For interval-based schedules"
                    />
                  )}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Controller
                  name="timezone"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Timezone</InputLabel>
                      <Select {...field} label="Timezone">
                        <MenuItem value="UTC">UTC</MenuItem>
                        <MenuItem value="America/New_York">Eastern Time</MenuItem>
                        <MenuItem value="America/Chicago">Central Time</MenuItem>
                        <MenuItem value="America/Denver">Mountain Time</MenuItem>
                        <MenuItem value="America/Los_Angeles">Pacific Time</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating...' : 'Create Scheduler'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
};

export default SchedulerList;

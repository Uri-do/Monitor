import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Paper,
  Alert,
  CircularProgress,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid
} from '@mui/material';
import {
  Add,
  Edit,
  Delete,
  Schedule,
  PlayArrow,
  Stop,
  Visibility
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { schedulerApi } from '@/services/api';
import { SchedulerDto, CreateSchedulerRequest } from '@/types/api';

/**
 * SchedulerList component for managing schedulers
 */
const SchedulerList: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  // Fetch schedulers
  const {
    data: schedulers,
    isLoading,
    error: loadError
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

  const handleDeleteScheduler = (schedulerId: number) => {
    if (window.confirm('Are you sure you want to delete this scheduler?')) {
      deleteMutation.mutate(schedulerId);
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

  if (isLoading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (loadError) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Alert severity="error">
          Failed to load schedulers. Please try again.
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Page Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            Schedulers
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage scheduling configurations for indicators
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
        >
          Create Scheduler
        </Button>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Schedulers Table */}
      <Paper elevation={1}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Schedule</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Indicators</TableCell>
                <TableCell>Next Run</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {schedulers?.map((scheduler) => (
                <TableRow key={scheduler.schedulerID} hover>
                  <TableCell>
                    <Box>
                      <Typography variant="body2" fontWeight="medium">
                        {scheduler.schedulerName}
                      </Typography>
                      {scheduler.schedulerDescription && (
                        <Typography variant="caption" color="text.secondary">
                          {scheduler.schedulerDescription}
                        </Typography>
                      )}
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={scheduler.scheduleType}
                      color={getScheduleTypeColor(scheduler.scheduleType)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {scheduler.displayText}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={scheduler.isEnabled ? 'Enabled' : 'Disabled'}
                      color={getStatusColor(scheduler.isEnabled)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {scheduler.indicatorCount} indicators
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {scheduler.nextExecutionTime 
                        ? new Date(scheduler.nextExecutionTime).toLocaleString()
                        : 'N/A'
                      }
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="View Details">
                      <IconButton
                        size="small"
                        onClick={() => navigate(`/schedulers/${scheduler.schedulerID}`)}
                      >
                        <Visibility />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit">
                      <IconButton
                        size="small"
                        onClick={() => navigate(`/schedulers/${scheduler.schedulerID}/edit`)}
                      >
                        <Edit />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDeleteScheduler(scheduler.schedulerID)}
                        disabled={scheduler.indicatorCount > 0}
                      >
                        <Delete />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {schedulers?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <Typography variant="body2" color="text.secondary">
                      No schedulers found. Create your first scheduler to get started.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

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
    </Container>
  );
};

export default SchedulerList;

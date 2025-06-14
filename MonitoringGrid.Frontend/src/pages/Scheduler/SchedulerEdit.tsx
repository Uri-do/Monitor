import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Alert,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  FormControlLabel,
  Switch,
  InputAdornment,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Schedule as ScheduleIcon,
  AccessTime as TimeIcon,
  Description as DescriptionIcon,
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { schedulerApi } from '@/services/api';
import { UpdateSchedulerRequest } from '@/types/api';
import toast from 'react-hot-toast';
import {
  PageHeader,
  LoadingSpinner,
  FormLayout,
  FormSection,
  FormActions,
} from '@/components';

/**
 * SchedulerEdit component for editing scheduler information
 */
const SchedulerEdit: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const schedulerId = id ? parseInt(id, 10) : undefined;
  const [error, setError] = useState<string | null>(null);

  // Fetch scheduler data
  const {
    data: scheduler,
    isLoading,
    error: loadError
  } = useQuery({
    queryKey: ['scheduler', schedulerId],
    queryFn: () => schedulerApi.getScheduler(schedulerId!),
    enabled: !!schedulerId,
  });

  // Remove validation schema and use direct form handling

  // Form setup
  const {
    control,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<UpdateSchedulerRequest>({
    defaultValues: {
      schedulerID: 0,
      schedulerName: '',
      schedulerDescription: '',
      scheduleType: 'interval',
      intervalMinutes: 60,
      cronExpression: '',
      executionDateTime: '',
      startDate: '',
      endDate: '',
      timezone: 'UTC',
      isEnabled: true,
    },
  });

  // Watch schedule type to show/hide relevant fields
  const scheduleType = watch('scheduleType');

  // Reset form when scheduler data loads
  useEffect(() => {
    if (scheduler) {
      reset({
        schedulerID: scheduler.schedulerID,
        schedulerName: scheduler.schedulerName,
        schedulerDescription: scheduler.schedulerDescription || '',
        scheduleType: scheduler.scheduleType,
        intervalMinutes: scheduler.intervalMinutes || 60,
        cronExpression: scheduler.cronExpression || '',
        executionDateTime: scheduler.executionDateTime || '',
        startDate: scheduler.startDate || '',
        endDate: scheduler.endDate || '',
        timezone: scheduler.timezone,
        isEnabled: scheduler.isEnabled,
      });
    }
  }, [scheduler, reset]);

  // Update scheduler mutation
  const updateMutation = useMutation({
    mutationFn: (data: UpdateSchedulerRequest) => schedulerApi.updateScheduler(data),
    onSuccess: () => {
      toast.success('Scheduler updated successfully');
      queryClient.invalidateQueries({ queryKey: ['scheduler', schedulerId] });
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      navigate(`/schedulers/${schedulerId}`);
    },
    onError: (error: any) => {
      const errorMessage = error.response?.data?.message || 'Failed to update scheduler';
      setError(errorMessage);
      toast.error(errorMessage);
    },
  });

  const onSubmit = (data: UpdateSchedulerRequest) => {
    setError(null);
    updateMutation.mutate(data);
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (loadError || !scheduler) {
    return (
      <Box>
        <Alert severity="error">
          Failed to load scheduler details. Please try again.
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Edit Scheduler"
        subtitle="Modify scheduler configuration and settings"
        icon={<ScheduleIcon />}
        backAction={{
          label: 'Back to Scheduler',
          icon: <BackIcon />,
          onClick: () => navigate(`/schedulers/${schedulerId}`),
        }}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)}>
        <FormLayout fullWidth spacing={3}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <FormSection
              title="Basic Information"
              subtitle="Configure scheduler name and description"
              icon={<ScheduleIcon />}
            >
              <Grid item xs={12}>
                <Controller
                  name="schedulerName"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Scheduler Name"
                      fullWidth
                      error={!!errors.schedulerName}
                      helperText={errors.schedulerName?.message}
                      placeholder="e.g., Daily at 9 AM"
                      InputProps={{
                        startAdornment: (
                          <InputAdornment position="start">
                            <ScheduleIcon color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="scheduleType"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth error={!!errors.scheduleType}>
                      <InputLabel>Schedule Type</InputLabel>
                      <Select {...field} label="Schedule Type">
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
                      rows={3}
                      error={!!errors.schedulerDescription}
                      helperText={errors.schedulerDescription?.message || 'Optional description of when and why this scheduler runs'}
                      placeholder="Describe the purpose and timing of this scheduler..."
                      InputProps={{
                        startAdornment: (
                          <InputAdornment position="start">
                            <DescriptionIcon color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Schedule Configuration */}
          <Grid item xs={12}>
            <FormSection
              title="Schedule Configuration"
              subtitle="Define when and how often the scheduler should run"
              icon={<TimeIcon />}
            >
              {scheduleType === 'interval' && (
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
                        error={!!errors.intervalMinutes}
                        helperText={errors.intervalMinutes?.message || 'How often to run (in minutes)'}
                        inputProps={{ min: 1 }}
                      />
                    )}
                  />
                </Grid>
              )}

              {scheduleType === 'cron' && (
                <Grid item xs={12} md={8}>
                  <Controller
                    name="cronExpression"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Cron Expression"
                        fullWidth
                        error={!!errors.cronExpression}
                        helperText={errors.cronExpression?.message || "e.g., '0 */6 * * *' for every 6 hours"}
                        placeholder="0 */6 * * *"
                      />
                    )}
                  />
                </Grid>
              )}

              {scheduleType === 'onetime' && (
                <Grid item xs={12} md={6}>
                  <Controller
                    name="executionDateTime"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Execution Date/Time"
                        type="datetime-local"
                        fullWidth
                        error={!!errors.executionDateTime}
                        helperText={errors.executionDateTime?.message}
                        InputLabelProps={{ shrink: true }}
                      />
                    )}
                  />
                </Grid>
              )}

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
                        <MenuItem value="Europe/London">London</MenuItem>
                        <MenuItem value="Europe/Paris">Paris</MenuItem>
                        <MenuItem value="Asia/Tokyo">Tokyo</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Settings */}
          <Grid item xs={12}>
            <FormSection
              title="Settings"
              subtitle="Configure scheduler behavior and status"
            >
              <Grid item xs={12}>
                <Controller
                  name="isEnabled"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Switch {...field} checked={field.value} />}
                      label="Enable this scheduler"
                      sx={{ mt: 1 }}
                    />
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Form Actions */}
          <Grid item xs={12}>
            <FormActions
              secondaryActions={[
                {
                  label: 'Cancel',
                  variant: 'outlined',
                  startIcon: <CancelIcon />,
                  onClick: () => navigate(`/schedulers/${schedulerId}`),
                  disabled: isSubmitting,
                },
              ]}
              primaryAction={{
                label: isSubmitting ? 'Saving...' : 'Save Changes',
                type: 'submit',
                startIcon: <SaveIcon />,
                disabled: isSubmitting,
                loading: isSubmitting,
              }}
            />
          </Grid>
        </FormLayout>
      </form>
    </Box>
  );
};

export default SchedulerEdit;

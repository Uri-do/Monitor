import React, { useState } from 'react';
import {
  Box,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Alert,
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
import { useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { schedulerApi } from '@/services/api';
import { CreateSchedulerRequest } from '@/types/api';
import toast from 'react-hot-toast';
import { PageHeader, FormLayout, FormSection, FormActions } from '@/components';

// Validation schema
const schedulerSchema = yup.object({
  schedulerName: yup.string().required('Scheduler name is required').max(100, 'Name must be less than 100 characters'),
  schedulerDescription: yup.string().max(500, 'Description must be less than 500 characters').nullable(),
  scheduleType: yup.string().oneOf(['interval', 'cron', 'onetime']).required('Schedule type is required'),
  intervalMinutes: yup.number().when('scheduleType', {
    is: 'interval',
    then: (schema) => schema.required('Interval is required').min(1, 'Interval must be at least 1 minute'),
    otherwise: (schema) => schema.nullable(),
  }),
  cronExpression: yup.string().when('scheduleType', {
    is: 'cron',
    then: (schema) => schema.required('Cron expression is required'),
    otherwise: (schema) => schema.nullable(),
  }),
  timezone: yup.string().nullable(),
  isEnabled: yup.boolean(),
});

type SchedulerFormData = yup.InferType<typeof schedulerSchema>;

const SchedulerCreate: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  // Form setup
  const {
    control,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<SchedulerFormData>({
    resolver: yupResolver(schedulerSchema),
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

  const scheduleType = watch('scheduleType');

  // Create scheduler mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateSchedulerRequest) => schedulerApi.createScheduler(data),
    onSuccess: (newScheduler) => {
      toast.success('Scheduler created successfully');
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      navigate(`/schedulers/${newScheduler.schedulerID}`);
    },
    onError: (error: any) => {
      const errorMessage = error.response?.data?.message || 'Failed to create scheduler';
      setError(errorMessage);
      toast.error(errorMessage);
    },
  });

  const onSubmit = (data: SchedulerFormData) => {
    setError(null);
    
    const requestData: CreateSchedulerRequest = {
      schedulerName: data.schedulerName,
      schedulerDescription: data.schedulerDescription || undefined,
      scheduleType: data.scheduleType,
      intervalMinutes: data.scheduleType === 'interval' ? data.intervalMinutes : undefined,
      cronExpression: data.scheduleType === 'cron' ? data.cronExpression : undefined,
      timezone: data.timezone || 'UTC',
      isEnabled: data.isEnabled ?? true,
    };

    createMutation.mutate(requestData);
  };

  return (
    <Box>
      <PageHeader
        title="Create Scheduler"
        subtitle="Create a new scheduler for automated indicator execution"
        icon={<ScheduleIcon />}
        backAction={{
          label: 'Back to Schedulers',
          icon: <BackIcon />,
          onClick: () => navigate('/schedulers'),
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
                  onClick: () => navigate('/schedulers'),
                  disabled: isSubmitting,
                },
              ]}
              primaryAction={{
                label: isSubmitting ? 'Creating...' : 'Create Scheduler',
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

export default SchedulerCreate;

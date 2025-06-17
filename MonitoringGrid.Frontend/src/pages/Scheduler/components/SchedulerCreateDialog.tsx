import React, { useState } from 'react';
import {
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
  Button,
  Box,
  Typography,
  Alert,
  Chip,
  Stack,
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
  AccessTime as TimeIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CreateSchedulerRequest } from '@/types/api';

interface SchedulerCreateDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateSchedulerRequest) => Promise<void>;
  loading?: boolean;
}

// Validation schema
const validationSchema = yup.object({
  schedulerName: yup.string().required('Scheduler name is required'),
  schedulerDescription: yup.string(),
  scheduleType: yup.string().oneOf(['interval', 'cron', 'onetime']).required(),
  intervalMinutes: yup.number().when('scheduleType', {
    is: 'interval',
    then: (schema) => schema.required('Interval is required').min(1, 'Minimum 1 minute'),
    otherwise: (schema) => schema.notRequired(),
  }),
  cronExpression: yup.string().when('scheduleType', {
    is: 'cron',
    then: (schema) => schema.required('Cron expression is required'),
    otherwise: (schema) => schema.notRequired(),
  }),
  executionDateTime: yup.string().when('scheduleType', {
    is: 'onetime',
    then: (schema) => schema.required('Execution date/time is required'),
    otherwise: (schema) => schema.notRequired(),
  }),
  timezone: yup.string().required('Timezone is required'),
});

export const SchedulerCreateDialog: React.FC<SchedulerCreateDialogProps> = ({
  open,
  onClose,
  onSubmit,
  loading = false,
}) => {
  const [scheduleType, setScheduleType] = useState<string>('interval');

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateSchedulerRequest>({
    resolver: yupResolver(validationSchema),
    defaultValues: {
      schedulerName: '',
      schedulerDescription: '',
      scheduleType: 'interval',
      intervalMinutes: 60,
      cronExpression: '',
      executionDateTime: '',
      timezone: 'UTC',
      isActive: true,
    },
  });

  const watchedScheduleType = watch('scheduleType');

  React.useEffect(() => {
    setScheduleType(watchedScheduleType);
  }, [watchedScheduleType]);

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleFormSubmit = async (data: CreateSchedulerRequest) => {
    try {
      await onSubmit(data);
      handleClose();
    } catch (error) {
      // Error handling is done in parent component
    }
  };

  // Common cron expressions
  const commonCronExpressions = [
    { label: 'Every minute', value: '* * * * *' },
    { label: 'Every 5 minutes', value: '*/5 * * * *' },
    { label: 'Every 15 minutes', value: '*/15 * * * *' },
    { label: 'Every 30 minutes', value: '*/30 * * * *' },
    { label: 'Every hour', value: '0 * * * *' },
    { label: 'Every 6 hours', value: '0 */6 * * *' },
    { label: 'Daily at midnight', value: '0 0 * * *' },
    { label: 'Daily at 9 AM', value: '0 9 * * *' },
    { label: 'Weekly on Monday', value: '0 0 * * 1' },
    { label: 'Monthly on 1st', value: '0 0 1 * *' },
  ];

  const timezones = [
    { label: 'UTC', value: 'UTC' },
    { label: 'Eastern Time', value: 'America/New_York' },
    { label: 'Central Time', value: 'America/Chicago' },
    { label: 'Mountain Time', value: 'America/Denver' },
    { label: 'Pacific Time', value: 'America/Los_Angeles' },
    { label: 'London', value: 'Europe/London' },
    { label: 'Paris', value: 'Europe/Paris' },
    { label: 'Tokyo', value: 'Asia/Tokyo' },
    { label: 'Sydney', value: 'Australia/Sydney' },
  ];

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: { minHeight: 500 },
      }}
    >
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          <ScheduleIcon />
          <Typography variant="h6">Create New Scheduler</Typography>
        </Box>
      </DialogTitle>

      <form onSubmit={handleSubmit(handleFormSubmit)}>
        <DialogContent>
          <Grid container spacing={3} sx={{ mt: 1 }}>
            {/* Basic Information */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" sx={{ mb: 2, fontWeight: 600 }}>
                Basic Information
              </Typography>
            </Grid>

            <Grid item xs={12} md={8}>
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
                    placeholder="e.g., Daily Report Generation"
                    required
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={4}>
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
                    placeholder="Optional description of what this scheduler does"
                  />
                )}
              />
            </Grid>

            {/* Schedule Configuration */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" sx={{ mb: 2, fontWeight: 600 }}>
                Schedule Configuration
              </Typography>
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
                      required
                    />
                  )}
                />
              </Grid>
            )}

            {scheduleType === 'cron' && (
              <>
                <Grid item xs={12}>
                  <Controller
                    name="cronExpression"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Cron Expression"
                        fullWidth
                        error={!!errors.cronExpression}
                        helperText={errors.cronExpression?.message || "Format: minute hour day month dayOfWeek"}
                        placeholder="0 */6 * * *"
                        required
                      />
                    )}
                  />
                </Grid>
                
                <Grid item xs={12}>
                  <Alert severity="info" sx={{ mb: 2 }}>
                    <Typography variant="body2" sx={{ mb: 1 }}>
                      <strong>Common Cron Expressions:</strong>
                    </Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      {commonCronExpressions.map((expr) => (
                        <Chip
                          key={expr.value}
                          label={`${expr.label}: ${expr.value}`}
                          size="small"
                          variant="outlined"
                          onClick={() => {
                            // Set the cron expression
                            const event = { target: { value: expr.value } };
                            control._formValues.cronExpression = expr.value;
                            control._subjects.values.next(control._formValues);
                          }}
                          sx={{ cursor: 'pointer' }}
                        />
                      ))}
                    </Stack>
                  </Alert>
                </Grid>
              </>
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
                      required
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
                    <Select {...field} label="Timezone" required>
                      {timezones.map((tz) => (
                        <MenuItem key={tz.value} value={tz.value}>
                          {tz.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              />
            </Grid>
          </Grid>
        </DialogContent>

        <DialogActions sx={{ p: 3 }}>
          <Button onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting || loading}
            startIcon={<ScheduleIcon />}
          >
            {isSubmitting ? 'Creating...' : 'Create Scheduler'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default SchedulerCreateDialog;

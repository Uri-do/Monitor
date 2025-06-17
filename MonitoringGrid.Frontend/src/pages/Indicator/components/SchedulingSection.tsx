import React from 'react';
import {
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  InputAdornment,
  Box,
} from '@mui/material';
import {
  Schedule as SchedulerIcon,
  Timer as TimerIcon,
} from '@mui/icons-material';
import { Controller, Control, FieldErrors } from 'react-hook-form';
import { FormSection } from '@/components';
import { SchedulerDto } from '@/types/api';

interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID: number;
  collectorItemName: string;
  schedulerID: number;
  lastMinutes?: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: string;
  isActive?: boolean;
}

interface SchedulingSectionProps {
  control: Control<IndicatorFormData>;
  errors: FieldErrors<IndicatorFormData>;
  schedulers: SchedulerDto[];
}

export const SchedulingSection: React.FC<SchedulingSectionProps> = ({
  control,
  errors,
  schedulers,
}) => {
  return (
    <Grid item xs={12}>
      <FormSection
        title="Scheduling"
        subtitle="Configure when and how often to check this indicator"
        icon={<SchedulerIcon />}
      >
        <Grid item xs={12} md={6}>
          <Controller
            name="schedulerID"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.schedulerID}>
                <InputLabel>Scheduler</InputLabel>
                <Select {...field} label="Scheduler">
                  <MenuItem value={0}>
                    <em>Select a scheduler</em>
                  </MenuItem>
                  {schedulers.map((scheduler) => (
                    <MenuItem key={scheduler.schedulerID} value={scheduler.schedulerID}>
                      {scheduler.schedulerName}
                    </MenuItem>
                  ))}
                </Select>
                {errors.schedulerID && (
                  <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                    {errors.schedulerID.message}
                  </Box>
                )}
              </FormControl>
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="lastMinutes"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Data Window (Minutes)"
                type="number"
                fullWidth
                error={!!errors.lastMinutes}
                helperText={errors.lastMinutes?.message || 'How far back to look for data'}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <TimerIcon color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            )}
          />
        </Grid>
      </FormSection>
    </Grid>
  );
};

export default SchedulingSection;

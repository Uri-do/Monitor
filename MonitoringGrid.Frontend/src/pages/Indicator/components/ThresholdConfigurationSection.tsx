import React from 'react';
import {
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Box,
} from '@mui/material';
import {
  TrendingUp as ThresholdIcon,
} from '@mui/icons-material';
import { Controller, Control, FieldErrors } from 'react-hook-form';
import { FormSection } from '@/components';

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

interface ThresholdConfigurationSectionProps {
  control: Control<IndicatorFormData>;
  errors: FieldErrors<IndicatorFormData>;
}

export const ThresholdConfigurationSection: React.FC<ThresholdConfigurationSectionProps> = ({
  control,
  errors,
}) => {
  return (
    <Grid item xs={12}>
      <FormSection
        title="Threshold Configuration"
        subtitle="Define when this indicator should trigger alerts"
        icon={<ThresholdIcon />}
      >
        <Grid item xs={12} md={4}>
          <Controller
            name="thresholdType"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.thresholdType}>
                <InputLabel>Threshold Type</InputLabel>
                <Select {...field} label="Threshold Type">
                  <MenuItem value="count">Count</MenuItem>
                  <MenuItem value="sum">Sum</MenuItem>
                  <MenuItem value="average">Average</MenuItem>
                  <MenuItem value="min">Minimum</MenuItem>
                  <MenuItem value="max">Maximum</MenuItem>
                </Select>
                {errors.thresholdType && (
                  <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                    {errors.thresholdType.message}
                  </Box>
                )}
              </FormControl>
            )}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="thresholdField"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Threshold Field"
                fullWidth
                error={!!errors.thresholdField}
                helperText={errors.thresholdField?.message}
                placeholder="e.g., value, count"
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="thresholdComparison"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.thresholdComparison}>
                <InputLabel>Comparison</InputLabel>
                <Select {...field} label="Comparison">
                  <MenuItem value="gt">Greater Than (&gt;)</MenuItem>
                  <MenuItem value="gte">Greater Than or Equal (&gt;=)</MenuItem>
                  <MenuItem value="lt">Less Than (&lt;)</MenuItem>
                  <MenuItem value="lte">Less Than or Equal (&lt;=)</MenuItem>
                  <MenuItem value="eq">Equal (=)</MenuItem>
                  <MenuItem value="ne">Not Equal (≠)</MenuItem>
                </Select>
                {errors.thresholdComparison && (
                  <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                    {errors.thresholdComparison.message}
                  </Box>
                )}
              </FormControl>
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="thresholdValue"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Threshold Value"
                type="number"
                fullWidth
                error={!!errors.thresholdValue}
                helperText={errors.thresholdValue?.message}
                placeholder="e.g., 100"
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="priority"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.priority}>
                <InputLabel>Priority</InputLabel>
                <Select {...field} label="Priority">
                  <MenuItem value="low">Low</MenuItem>
                  <MenuItem value="medium">Medium</MenuItem>
                  <MenuItem value="high">High</MenuItem>
                  <MenuItem value="critical">Critical</MenuItem>
                </Select>
                {errors.priority && (
                  <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                    {errors.priority.message}
                  </Box>
                )}
              </FormControl>
            )}
          />
        </Grid>
      </FormSection>
    </Grid>
  );
};

export default ThresholdConfigurationSection;

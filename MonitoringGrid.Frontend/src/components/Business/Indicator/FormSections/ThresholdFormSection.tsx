import React from 'react';
import {
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
} from '@mui/material';
import { useFormContext, Controller } from 'react-hook-form';
import { TrendingUp as ThresholdIcon } from '@mui/icons-material';

interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorItemName: string;
  schedulerID?: number;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
}

// Threshold types and comparisons
const thresholdTypes = [
  { value: 'count', label: 'Count' },
  { value: 'average', label: 'Average' },
  { value: 'sum', label: 'Sum' },
  { value: 'min', label: 'Minimum' },
  { value: 'max', label: 'Maximum' },
];

const thresholdComparisons = [
  { value: 'gt', label: 'Greater Than (>)' },
  { value: 'gte', label: 'Greater Than or Equal (>=)' },
  { value: 'lt', label: 'Less Than (<)' },
  { value: 'lte', label: 'Less Than or Equal (<=)' },
  { value: 'eq', label: 'Equal To (=)' },
  { value: 'ne', label: 'Not Equal To (!=)' },
];

export const ThresholdFormSection: React.FC = () => {
  const { control } = useFormContext<IndicatorFormData>();

  return (
    <>
      {/* Threshold Configuration */}
      <Grid item xs={12}>
        <Typography 
          variant="h6" 
          gutterBottom 
          sx={{ 
            mt: 2,
            display: 'flex', 
            alignItems: 'center', 
            gap: 1 
          }}
        >
          <ThresholdIcon color="primary" />
          Threshold Configuration
        </Typography>
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdType"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Threshold Type *</InputLabel>
              <Select {...field} label="Threshold Type *">
                {thresholdTypes.map(type => (
                  <MenuItem key={type.value} value={type.value}>
                    {type.label}
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdField"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Threshold Field"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'Field name to evaluate'}
              placeholder="e.g., value, count"
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdComparison"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Comparison *</InputLabel>
              <Select {...field} label="Comparison *">
                {thresholdComparisons.map(comp => (
                  <MenuItem key={comp.value} value={comp.value}>
                    {comp.label}
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdValue"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Threshold Value"
              type="number"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
              placeholder="100"
            />
          )}
        />
      </Grid>
    </>
  );
};

export default ThresholdFormSection;

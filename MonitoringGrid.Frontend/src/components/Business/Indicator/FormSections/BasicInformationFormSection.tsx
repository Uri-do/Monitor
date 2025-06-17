import React from 'react';
import {
  Grid,
  TextField,
  Typography,
} from '@mui/material';
import { useFormContext, Controller } from 'react-hook-form';
import { Assessment as IndicatorIcon } from '@mui/icons-material';

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

export const BasicInformationFormSection: React.FC = () => {
  const { control } = useFormContext<IndicatorFormData>();

  return (
    <>
      {/* Basic Information */}
      <Grid item xs={12}>
        <Typography
          variant="h6"
          gutterBottom
          sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
        >
          <IndicatorIcon color="primary" />
          Basic Information
        </Typography>
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="indicatorName"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Indicator Name"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
              placeholder="e.g., Database Connection Count"
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="indicatorCode"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Indicator Code"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
              placeholder="e.g., DB_CONN_COUNT"
            />
          )}
        />
      </Grid>

      <Grid item xs={12}>
        <Controller
          name="indicatorDesc"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Description"
              multiline
              rows={3}
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
              placeholder="Describe what this indicator monitors..."
            />
          )}
        />
      </Grid>
    </>
  );
};

export default BasicInformationFormSection;

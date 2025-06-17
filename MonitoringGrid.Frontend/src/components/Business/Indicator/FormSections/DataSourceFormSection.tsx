import React from 'react';
import {
  Grid,
  TextField,
  Typography,
} from '@mui/material';
import { useFormContext, Controller } from 'react-hook-form';
import { Storage as CollectorIcon } from '@mui/icons-material';
import { CollectorSelector } from '../../../CollectorSelector';

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

export const DataSourceFormSection: React.FC = () => {
  const { control, watch, setValue } = useFormContext<IndicatorFormData>();

  const watchedCollectorId = watch('collectorID');

  return (
    <>
      {/* Data Source Configuration */}
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
          <CollectorIcon color="primary" />
          Data Source Configuration
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <CollectorSelector
          selectedCollectorId={watchedCollectorId}
          selectedItemName={watch('collectorItemName')}
          onCollectorChange={collectorId => setValue('collectorID', collectorId)}
          onItemNameChange={itemName => setValue('collectorItemName', itemName)}
          required
          variant="detailed"
          showRefreshButton
          showCollectorInfo
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="lastMinutes"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Data Window (Minutes)"
              type="number"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'How far back to look for data'}
              placeholder="60"
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="averageLastDays"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Average Last Days"
              type="number"
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'Days to calculate average (optional)'}
              placeholder="7"
            />
          )}
        />
      </Grid>
    </>
  );
};

export default DataSourceFormSection;

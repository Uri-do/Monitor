import React from 'react';
import {
  Grid,
  Typography,
} from '@mui/material';
import { useFormContext } from 'react-hook-form';
import { Schedule as SchedulerIcon } from '@mui/icons-material';
import { SchedulerSelector } from '../SchedulerSelector';

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

export const SchedulingFormSection: React.FC = () => {
  const { watch, setValue } = useFormContext<IndicatorFormData>();

  const watchedSchedulerId = watch('schedulerID');

  return (
    <>
      {/* Scheduling */}
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
          <SchedulerIcon color="primary" />
          Scheduling
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <SchedulerSelector
          selectedSchedulerId={watchedSchedulerId}
          onSchedulerChange={schedulerId => setValue('schedulerID', schedulerId)}
          variant="detailed"
          showCreateButton
          showRefreshButton
          showSchedulerInfo
        />
      </Grid>
    </>
  );
};

export default SchedulingFormSection;

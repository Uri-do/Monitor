import React from 'react';
import {
  Grid,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  PriorityHigh as PriorityIcon,
} from '@mui/icons-material';
import { Controller, Control } from 'react-hook-form';
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

interface StatusSectionProps {
  control: Control<IndicatorFormData>;
}

export const StatusSection: React.FC<StatusSectionProps> = ({
  control,
}) => {
  return (
    <Grid item xs={12}>
      <FormSection
        title="Status"
        subtitle="Configure indicator status and activation"
        icon={<PriorityIcon />}
      >
        <Grid item xs={12}>
          <Controller
            name="isActive"
            control={control}
            render={({ field }) => (
              <FormControlLabel
                control={<Switch {...field} checked={field.value} />}
                label="Active"
              />
            )}
          />
        </Grid>
      </FormSection>
    </Grid>
  );
};

export default StatusSection;

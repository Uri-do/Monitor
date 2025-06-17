import React from 'react';
import {
  Grid,
  TextField,
  InputAdornment,
} from '@mui/material';
import {
  Assessment as IndicatorIcon,
  Code as CodeIcon,
  Description as DescriptionIcon,
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

interface BasicInformationSectionProps {
  control: Control<IndicatorFormData>;
  errors: FieldErrors<IndicatorFormData>;
}

export const BasicInformationSection: React.FC<BasicInformationSectionProps> = ({
  control,
  errors,
}) => {
  return (
    <Grid item xs={12}>
      <FormSection
        title="Basic Information"
        subtitle="Configure indicator name, code, and description"
        icon={<IndicatorIcon />}
      >
        <Grid item xs={12} md={6}>
          <Controller
            name="indicatorName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Indicator Name"
                fullWidth
                error={!!errors.indicatorName}
                helperText={errors.indicatorName?.message}
                placeholder="e.g., Database Connection Count"
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <IndicatorIcon color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="indicatorCode"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Indicator Code"
                fullWidth
                error={!!errors.indicatorCode}
                helperText={errors.indicatorCode?.message}
                placeholder="e.g., DB_CONN_COUNT"
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <CodeIcon color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            )}
          />
        </Grid>

        <Grid item xs={12}>
          <Controller
            name="indicatorDesc"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Description"
                fullWidth
                multiline
                rows={3}
                error={!!errors.indicatorDesc}
                helperText={errors.indicatorDesc?.message}
                placeholder="Describe what this indicator monitors..."
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
  );
};

export default BasicInformationSection;

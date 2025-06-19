import React from 'react';
import {
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Box,
} from '@mui/material';
import {
  Storage as CollectorIcon,
} from '@mui/icons-material';
import { Controller, Control, FieldErrors } from 'react-hook-form';
import { FormSection } from '@/components';
import { CollectorDto, MonitorStatisticsCollector } from '@/types/api';

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

interface DataSourceSectionProps {
  control: Control<IndicatorFormData>;
  errors: FieldErrors<IndicatorFormData>;
  collectors: MonitorStatisticsCollector[];
  collectorItems: string[];
  selectedCollectorID: number;
}

export const DataSourceSection: React.FC<DataSourceSectionProps> = ({
  control,
  errors,
  collectors,
  collectorItems,
  selectedCollectorID,
}) => {
  return (
    <Grid item xs={12}>
      <FormSection
        title="Data Source"
        subtitle="Configure collector and data item"
        icon={<CollectorIcon />}
      >
        <Grid item xs={12} md={6}>
          <Controller
            name="collectorID"
            control={control}
            render={({ field }) => (
                <FormControl fullWidth error={!!errors.collectorID}>
                  <InputLabel>Collector</InputLabel>
                  <Select
                    {...field}
                    label="Collector"
                    value={field.value || ''}
                    onChange={(e) => {
                      const value = e.target.value;
                      // Convert to number if it's a valid number, otherwise keep as empty string
                      const numericValue = value === '' ? undefined : Number(value);
                      field.onChange(numericValue);
                    }}
                  >
                    <MenuItem value="">
                      <em>Select a collector</em>
                    </MenuItem>
                    {collectors.map((collector) => (
                      <MenuItem key={collector.collectorID} value={collector.collectorID}>
                        {collector.displayName}
                      </MenuItem>
                    ))}
                  </Select>
                  {errors.collectorID && (
                    <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                      {errors.collectorID.message}
                    </Box>
                  )}
                </FormControl>
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="collectorItemName"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.collectorItemName}>
                <InputLabel>Collector Item Name</InputLabel>
                <Select {...field} label="Collector Item Name" disabled={!selectedCollectorID}>
                  <MenuItem value="">
                    <em>Select an item</em>
                  </MenuItem>
                  {collectorItems.map((item) => (
                    <MenuItem key={item} value={item}>
                      {item}
                    </MenuItem>
                  ))}
                </Select>
                {errors.collectorItemName && (
                  <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                    {errors.collectorItemName.message}
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

export default DataSourceSection;

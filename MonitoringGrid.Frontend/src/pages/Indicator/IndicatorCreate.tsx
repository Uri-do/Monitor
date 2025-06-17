import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Grid,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Assessment as IndicatorIcon,
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import toast from 'react-hot-toast';

import { useCreateIndicator } from '@/hooks/useIndicators';
import { useActiveCollectors, useCollectorItemNames } from '@/hooks/useMonitorStatistics';
import { useSchedulers } from '@/hooks/useSchedulers';
import { PageHeader, FormLayout, FormActions } from '@/components';
import { CreateIndicatorRequest } from '@/types/api';

// Import form sections
import {
  BasicInformationSection,
  DataSourceSection,
  SchedulingSection,
  ThresholdConfigurationSection,
  StatusSection,
} from './components';

// Validation schema
const indicatorValidationSchema = yup.object({
  indicatorName: yup
    .string()
    .required('Indicator name is required')
    .max(100, 'Name must be less than 100 characters'),
  indicatorCode: yup
    .string()
    .required('Indicator code is required')
    .max(50, 'Code must be less than 50 characters'),
  indicatorDesc: yup.string().max(500, 'Description must be less than 500 characters'),
  collectorID: yup.number().required('Collector is required').min(1, 'Please select a collector'),
  collectorItemName: yup.string().required('Collector item is required'),
  schedulerID: yup.number().required('Scheduler is required').min(1, 'Please select a scheduler'),
  lastMinutes: yup.number().min(1, 'Must be at least 1 minute').max(10080, 'Cannot exceed 7 days'),
  thresholdType: yup.string().required('Threshold type is required'),
  thresholdField: yup.string().required('Threshold field is required'),
  thresholdComparison: yup.string().required('Threshold comparison is required'),
  thresholdValue: yup.number().required('Threshold value is required'),
  priority: yup.string().required('Priority is required'),
  isActive: yup.boolean(),
});

type IndicatorFormData = yup.InferType<typeof indicatorValidationSchema>;

/**
 * IndicatorCreate component - handles creating new indicators
 */
const IndicatorCreate: React.FC = () => {
  const navigate = useNavigate();

  const { data: collectors = [] } = useActiveCollectors();
  const { data: schedulers = [] } = useSchedulers();
  const createMutation = useCreateIndicator();

  // Form setup
  const {
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<IndicatorFormData>({
    resolver: yupResolver(indicatorValidationSchema),
    defaultValues: {
      indicatorName: '',
      indicatorCode: '',
      indicatorDesc: '',
      collectorID: 0,
      collectorItemName: '',
      schedulerID: 0,
      lastMinutes: 60,
      thresholdType: 'count',
      thresholdField: '',
      thresholdComparison: 'gt',
      thresholdValue: 0,
      priority: 'medium',
      isActive: true,
    },
  });

  // Get collector items based on selected collector
  const { data: collectorItems = [] } = useCollectorItemNames(watch('collectorID') || 0);

  const onSubmit = (data: IndicatorFormData) => {
    const createData: CreateIndicatorRequest = {
      indicatorName: data.indicatorName,
      indicatorCode: data.indicatorCode,
      indicatorDesc: data.indicatorDesc || undefined,
      collectorID: data.collectorID,
      collectorItemName: data.collectorItemName,
      schedulerID: data.schedulerID || undefined,
      isActive: data.isActive ?? true,
      lastMinutes: data.lastMinutes || 60,
      thresholdType: data.thresholdType,
      thresholdField: data.thresholdField,
      thresholdComparison: data.thresholdComparison,
      thresholdValue: data.thresholdValue,
      priority: data.priority, // Keep as string - API expects string
      ownerContactId: 1, // Default owner contact - this should be configurable
      averageLastDays: undefined,
      contactIds: [], // Empty array for now - contacts feature can be added later
    };

    createMutation.mutate(createData, {
      onSuccess: (newIndicator) => {
        toast.success('Indicator created successfully');
        navigate(`/indicators/${newIndicator.indicatorID}`);
      },
      onError: (error: any) => {
        const errorMessage = error.response?.data?.message || 'Failed to create indicator';
        toast.error(errorMessage);
      },
    });
  };

  return (
    <Box>
      <PageHeader
        title="Create New Indicator"
        subtitle="Configure a new indicator to monitor your system metrics and performance."
        icon={<IndicatorIcon />}
        backAction={{
          label: 'Back to Indicators',
          icon: <BackIcon />,
          onClick: () => navigate('/indicators'),
        }}
      />

      <form onSubmit={handleSubmit(onSubmit)}>
        <FormLayout fullWidth spacing={3}>
          {/* Basic Information */}
          <BasicInformationSection control={control} errors={errors} />

          {/* Data Source Configuration */}
          <DataSourceSection
            control={control}
            errors={errors}
            collectors={collectors}
            collectorItems={collectorItems}
            selectedCollectorID={watch('collectorID')}
          />

          {/* Scheduling Configuration */}
          <SchedulingSection
            control={control}
            errors={errors}
            schedulers={schedulers}
          />

          {/* Threshold Configuration */}
          <ThresholdConfigurationSection
            control={control}
            errors={errors}
          />

          {/* Status Configuration */}
          <StatusSection control={control} />

          {/* Form Actions */}
          <Grid item xs={12}>
            <FormActions
              primaryAction={{
                label: "Create Indicator",
                onClick: handleSubmit(onSubmit),
                variant: "contained",
                color: "primary",
                startIcon: <SaveIcon />,
                loading: isSubmitting || createMutation.isPending,
                type: "submit"
              }}
              secondaryActions={[
                {
                  label: "Cancel",
                  onClick: () => navigate('/indicators'),
                  variant: "outlined",
                  color: "secondary",
                  startIcon: <CancelIcon />
                }
              ]}
            />
          </Grid>
        </FormLayout>
      </form>
    </Box>
  );
};

export default IndicatorCreate;

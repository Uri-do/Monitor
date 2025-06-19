import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Alert,
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

import { useIndicator } from '@/hooks/useIndicators';
import { useActiveCollectors, useCollectorItemNames } from '@/hooks/useMonitorStatistics';
import { useSchedulers } from '@/hooks/useSchedulers';
import { LoadingSpinner, PageHeader, FormLayout, FormActions } from '@/components';
import { indicatorApi } from '@/services/api';
import { UpdateIndicatorRequest } from '@/types/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';

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
 * IndicatorEdit component using full-page form layout
 */
const IndicatorEdit: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const indicatorId = parseInt(id || '0');

  // Redirect if invalid ID
  React.useEffect(() => {
    if (!id || indicatorId <= 0) {
      console.error('Invalid indicator ID:', id);
      navigate('/indicators');
      return;
    }
  }, [id, indicatorId, navigate]);

  const {
    data: indicator,
    isLoading,
    error,
  } = useIndicator(indicatorId, !!id && indicatorId > 0);

  const { data: collectors = [] } = useActiveCollectors();
  const { data: schedulers = [] } = useSchedulers();

  const queryClient = useQueryClient();
  const updateMutation = useMutation({
    mutationFn: (data: UpdateIndicatorRequest) => indicatorApi.updateIndicator(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['indicators'] });
      queryClient.invalidateQueries({ queryKey: ['indicators', indicatorId] });
    },
  });

  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<IndicatorFormData>({
    resolver: yupResolver(indicatorValidationSchema),
    defaultValues: {
      indicatorName: '',
      indicatorCode: '',
      indicatorDesc: '',
      collectorID: undefined, // Don't default to 0
      collectorItemName: '',
      schedulerID: undefined, // Don't default to 0
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

  // Update form when indicator data is loaded
  useEffect(() => {
    if (indicator) {
      reset({
        indicatorName: indicator.indicatorName || '',
        indicatorCode: indicator.indicatorCode || '',
        indicatorDesc: indicator.indicatorDesc || '',
        collectorID: indicator.collectorID || 0,
        collectorItemName: indicator.collectorItemName || '',
        schedulerID: indicator.schedulerID || 0,
        lastMinutes: indicator.lastMinutes || 60,
        thresholdType: indicator.thresholdType || 'count',
        thresholdField: indicator.thresholdField || '',
        thresholdComparison: indicator.thresholdComparison || 'gt',
        thresholdValue: indicator.thresholdValue || 0,
        priority: typeof indicator.priority === 'string' ? indicator.priority : 'medium',
        isActive: indicator.isActive ?? true,
      });
    }
  }, [indicator, reset]);

  const onSubmit = (data: IndicatorFormData) => {
    // Prepare the update data to match the API exactly
    const updateData = {
      indicatorID: indicatorId,
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
      ownerContactId: indicator?.ownerContactId || 1, // Use existing or default
      averageLastDays: indicator?.averageLastDays || undefined,
      contactIds: [], // Empty array for now - contacts feature can be added later
    };

    updateMutation.mutate(updateData, {
      onSuccess: () => {
        toast.success('Indicator updated successfully');
        navigate(`/indicators/${indicatorId}`);
      },
      onError: (error: any) => {
        const errorMessage = error.response?.data?.message || 'Failed to update indicator';
        toast.error(errorMessage);
      },
    });
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (error || !indicator) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load indicator data
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Edit Indicator"
        subtitle={`Editing: ${indicator.indicatorName}`}
        icon={<IndicatorIcon />}
        backAction={{
          label: 'Back to Indicator',
          icon: <BackIcon />,
          onClick: () => navigate(`/indicators/${indicatorId}`),
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
                label: "Update Indicator",
                onClick: handleSubmit(onSubmit),
                variant: "contained",
                color: "primary",
                startIcon: <SaveIcon />,
                loading: isSubmitting || updateMutation.isPending,
                type: "submit"
              }}
              secondaryActions={[
                {
                  label: "Cancel",
                  onClick: () => navigate(`/indicators/${indicatorId}`),
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

export default IndicatorEdit;

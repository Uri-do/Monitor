import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Alert,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  FormControlLabel,
  Switch,
  InputAdornment,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Assessment as IndicatorIcon,
  ArrowBack as BackIcon,
  Code as CodeIcon,
  Description as DescriptionIcon,
  Storage as CollectorIcon,
  Schedule as SchedulerIcon,
  Timer as TimerIcon,
  TrendingUp as ThresholdIcon,
  PriorityHigh as PriorityIcon,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import toast from 'react-hot-toast';

import { useCreateIndicator } from '@/hooks/useIndicators';
import { useActiveCollectors, useCollectorItemNames } from '@/hooks/useMonitorStatistics';
import { useSchedulers } from '@/hooks/useSchedulers';
import { LoadingSpinner, PageHeader, FormLayout, FormSection, FormActions } from '@/components';
import { CreateIndicatorRequest } from '@/types/api';

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

          {/* Data Source Configuration */}
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
                      <Select {...field} label="Collector">
                        <MenuItem value={0}>
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
                      <Select {...field} label="Collector Item Name" disabled={!watch('collectorID')}>
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

          {/* Scheduling Configuration */}
          <Grid item xs={12}>
            <FormSection
              title="Scheduling"
              subtitle="Configure when and how often to check this indicator"
              icon={<SchedulerIcon />}
            >
              <Grid item xs={12} md={6}>
                <Controller
                  name="schedulerID"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth error={!!errors.schedulerID}>
                      <InputLabel>Scheduler</InputLabel>
                      <Select {...field} label="Scheduler">
                        <MenuItem value={0}>
                          <em>Select a scheduler</em>
                        </MenuItem>
                        {schedulers.map((scheduler) => (
                          <MenuItem key={scheduler.schedulerID} value={scheduler.schedulerID}>
                            {scheduler.schedulerName}
                          </MenuItem>
                        ))}
                      </Select>
                      {errors.schedulerID && (
                        <Box sx={{ color: 'error.main', fontSize: '0.75rem', mt: 0.5 }}>
                          {errors.schedulerID.message}
                        </Box>
                      )}
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="lastMinutes"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Data Window (Minutes)"
                      type="number"
                      fullWidth
                      error={!!errors.lastMinutes}
                      helperText={errors.lastMinutes?.message || 'How far back to look for data'}
                      InputProps={{
                        startAdornment: (
                          <InputAdornment position="start">
                            <TimerIcon color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Threshold Configuration */}
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

          {/* Status Configuration */}
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

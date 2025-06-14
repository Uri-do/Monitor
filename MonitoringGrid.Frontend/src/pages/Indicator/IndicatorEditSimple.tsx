import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Paper,
  Alert,
  CircularProgress,
  Breadcrumbs,
  Link,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Button,
  Divider,
  Chip
} from '@mui/material';
import { Home, Assessment, Edit, Save, Cancel, ArrowBack as BackIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { indicatorApi, schedulerApi } from '@/services/api';
import { IndicatorDto, UpdateIndicatorRequest } from '@/types/api';
import { CollectorSelector } from '@/components/CollectorSelector';
import { PageHeader, FormLayout, FormSection, FormActions } from '@/components';

// Form data interface
interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID: number; // Changed from optional to required
  collectorItemName: string;
  schedulerID?: number | null;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number | null;
  isActive: boolean;
}

/**
 * Simple IndicatorEdit component without wizard/stepper
 */
const IndicatorEditSimple: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const indicatorId = id ? parseInt(id, 10) : undefined;

  // Fetch existing indicator data
  const {
    data: existingIndicator,
    isLoading: isLoadingIndicator,
    error: loadError
  } = useQuery({
    queryKey: ['indicator', indicatorId],
    queryFn: () => indicatorApi.getIndicator(indicatorId!),
    enabled: !!indicatorId,
  });

  // Fetch schedulers for dropdown
  const {
    data: schedulers,
    isLoading: isLoadingSchedulers
  } = useQuery({
    queryKey: ['schedulers'],
    queryFn: () => schedulerApi.getSchedulers(),
  });

  // Form setup
  const {
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<IndicatorFormData>({
    defaultValues: {
      indicatorName: '',
      indicatorCode: '',
      indicatorDesc: '',
      collectorID: 0, // Will be set when data loads
      collectorItemName: '',
      schedulerID: null,
      lastMinutes: 60,
      thresholdType: 'volume_average',
      thresholdField: 'Total',
      thresholdComparison: 'lt',
      thresholdValue: 0,
      priority: 2, // medium priority
      ownerContactId: 1,
      averageLastDays: null,
      isActive: true,
    },
  });

  // Update form when data loads
  React.useEffect(() => {
    if (existingIndicator) {
      setValue('indicatorName', existingIndicator.indicatorName);
      setValue('indicatorCode', existingIndicator.indicatorCode);
      setValue('indicatorDesc', existingIndicator.indicatorDesc || '');
      setValue('collectorID', existingIndicator.collectorID || 0);
      setValue('collectorItemName', existingIndicator.collectorItemName);
      setValue('schedulerID', existingIndicator.schedulerID || null);
      setValue('lastMinutes', existingIndicator.lastMinutes);
      setValue('thresholdType', existingIndicator.thresholdType || 'volume_average');
      setValue('thresholdField', existingIndicator.thresholdField || 'Total');
      setValue('thresholdComparison', existingIndicator.thresholdComparison || 'lt');
      setValue('thresholdValue', existingIndicator.thresholdValue || 0);
      // Convert string priority to number for form
      const priorityValue = typeof existingIndicator.priority === 'string'
        ? (existingIndicator.priority === 'high' ? 3 : existingIndicator.priority === 'medium' ? 2 : 1)
        : existingIndicator.priority;
      setValue('priority', priorityValue);
      setValue('ownerContactId', existingIndicator.ownerContactId);
      setValue('averageLastDays', existingIndicator.averageLastDays || null);
      setValue('isActive', existingIndicator.isActive);
    }
  }, [existingIndicator, setValue]);

  const watchedValues = watch();

  // Update indicator mutation
  const updateMutation = useMutation({
    mutationFn: (data: UpdateIndicatorRequest) => indicatorApi.updateIndicator(data),
    onSuccess: (updatedIndicator) => {
      queryClient.invalidateQueries({ queryKey: ['indicators'] });
      queryClient.invalidateQueries({ queryKey: ['indicator', indicatorId] });
      navigate(`/indicators/${updatedIndicator.indicatorID}`);
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to update indicator');
    },
  });

  const handleFormSubmit = (formData: IndicatorFormData) => {
    setError(null);

    // Convert priority number to string as expected by backend
    const priorityString = formData.priority === 3 ? 'high' :
                          formData.priority === 2 ? 'medium' : 'low';

    // Validate required fields
    if (!formData.collectorID || formData.collectorID === 0) {
      setError('Please select a valid collector');
      return;
    }

    // Transform form data to API request format
    const requestData = {
      indicatorID: indicatorId!,
      indicatorName: formData.indicatorName,
      indicatorCode: formData.indicatorCode,
      indicatorDesc: formData.indicatorDesc,
      collectorID: formData.collectorID,
      collectorItemName: formData.collectorItemName,
      schedulerID: formData.schedulerID ?? undefined,
      isActive: formData.isActive,
      lastMinutes: formData.lastMinutes,
      thresholdType: formData.thresholdType || 'volume_average',
      thresholdField: formData.thresholdField || 'Total',
      thresholdComparison: formData.thresholdComparison || 'lt',
      thresholdValue: formData.thresholdValue || 0,
      priority: priorityString, // Convert number to string
      ownerContactId: formData.ownerContactId,
      averageLastDays: formData.averageLastDays ?? undefined,
      contactIds: [], // For now, use empty array. TODO: Add contact selection to form
    };

    updateMutation.mutate(requestData);
  };

  const handleCancel = () => {
    navigate(`/indicators/${indicatorId}`);
  };

  const isLoading = isLoadingIndicator || updateMutation.isPending;

  if (isLoadingIndicator) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (loadError) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Alert severity="error">
          Failed to load indicator data. Please try again.
        </Alert>
      </Container>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Edit Indicator"
        subtitle={`Editing: ${existingIndicator?.indicatorName || 'Indicator'}`}
        icon={<Assessment />}
        backAction={{
          label: 'Back to Indicator',
          icon: <BackIcon />,
          onClick: () => navigate(`/indicators/${indicatorId}`),
        }}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit(handleFormSubmit)}>
        <FormLayout fullWidth spacing={3}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <FormSection
              title="Basic Information"
              subtitle="Configure indicator name, code, and description"
              icon={<Assessment />}
            >
              <Grid item xs={12} md={6}>
                <Controller
                  name="indicatorName"
                  control={control}
                  rules={{ required: 'Indicator name is required' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Indicator Name"
                      fullWidth
                      error={!!errors.indicatorName}
                      helperText={errors.indicatorName?.message}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="indicatorCode"
                  control={control}
                  rules={{ required: 'Indicator code is required' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Indicator Code"
                      fullWidth
                      error={!!errors.indicatorCode}
                      helperText={errors.indicatorCode?.message}
                      required
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
                    />
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Data Source */}
          <Grid item xs={12}>
            <FormSection
              title="Data Source"
              subtitle="Select the data collector and specific item to monitor"
            >
              <Grid item xs={12}>
                <CollectorSelector
                  selectedCollectorId={watchedValues.collectorID}
                  selectedItemName={watchedValues.collectorItemName}
                  onCollectorChange={(collectorId) => {
                    if (collectorId) {
                      setValue('collectorID', collectorId);
                    } else {
                      setValue('collectorID', 0); // Use 0 to indicate no selection
                    }
                  }}
                  onItemNameChange={(itemName) => {
                    setValue('collectorItemName', itemName);
                  }}
                  required
                  variant="detailed"
                  showRefreshButton
                  title="Data Collector"
                  subtitle="Select the data collector and specific item to monitor"
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Scheduling */}
          <Grid item xs={12}>
            <FormSection
              title="Scheduling"
              subtitle="Configure when and how often the indicator should run"
            >
              <Grid item xs={12} md={6}>
                <Controller
                  name="schedulerID"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Scheduler</InputLabel>
                      <Select
                        {...field}
                        value={field.value || ''}
                        label="Scheduler"
                        disabled={isLoadingSchedulers}
                      >
                        <MenuItem value="">
                          <em>No scheduler (manual execution only)</em>
                        </MenuItem>
                        {schedulers?.map((scheduler) => (
                          <MenuItem key={scheduler.schedulerID} value={scheduler.schedulerID}>
                            <Box>
                              <Typography variant="body2" fontWeight="medium">
                                {scheduler.schedulerName}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {scheduler.displayText}
                              </Typography>
                            </Box>
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="lastMinutes"
                  control={control}
                  rules={{ required: 'Data window is required', min: { value: 1, message: 'Must be at least 1 minute' } }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Data Window (minutes)"
                      type="number"
                      fullWidth
                      error={!!errors.lastMinutes}
                      helperText={errors.lastMinutes?.message || 'How far back to look for data'}
                      inputProps={{ min: 1 }}
                      required
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
              subtitle="Configure alert thresholds and conditions"
            >
              <Grid item xs={12} md={4}>
                <Controller
                  name="thresholdType"
                  control={control}
                  rules={{ required: 'Threshold type is required' }}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Threshold Type</InputLabel>
                      <Select {...field} label="Threshold Type" required>
                        <MenuItem value="volume_average">Volume Average</MenuItem>
                        <MenuItem value="threshold_value">Threshold Value</MenuItem>
                        <MenuItem value="percentage">Percentage</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={12} md={4}>
                <Controller
                  name="thresholdField"
                  control={control}
                  rules={{ required: 'Threshold field is required' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Threshold Field"
                      fullWidth
                      error={!!errors.thresholdField}
                      helperText={errors.thresholdField?.message || 'Field to evaluate (e.g., Total, Count)'}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={4}>
                <Controller
                  name="thresholdComparison"
                  control={control}
                  rules={{ required: 'Comparison operator is required' }}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Comparison</InputLabel>
                      <Select {...field} label="Comparison" required>
                        <MenuItem value="gt">Greater Than (&gt;)</MenuItem>
                        <MenuItem value="gte">Greater Than or Equal (&gt;=)</MenuItem>
                        <MenuItem value="lt">Less Than (&lt;)</MenuItem>
                        <MenuItem value="lte">Less Than or Equal (&lt;=)</MenuItem>
                        <MenuItem value="eq">Equal (=)</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="thresholdValue"
                  control={control}
                  rules={{ required: 'Threshold value is required', min: { value: 0, message: 'Must be non-negative' } }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Threshold Value"
                      type="number"
                      fullWidth
                      error={!!errors.thresholdValue}
                      helperText={errors.thresholdValue?.message || 'Value to compare against'}
                      inputProps={{ min: 0, step: 0.01 }}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="priority"
                  control={control}
                  rules={{ required: 'Priority is required' }}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel>Priority</InputLabel>
                      <Select {...field} label="Priority" required>
                        <MenuItem value={3}>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip label="High" color="error" size="small" />
                            High Priority
                          </Box>
                        </MenuItem>
                        <MenuItem value={2}>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip label="Medium" color="warning" size="small" />
                            Medium Priority
                          </Box>
                        </MenuItem>
                        <MenuItem value={1}>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip label="Low" color="info" size="small" />
                            Low Priority
                          </Box>
                        </MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Form Actions */}
          <Grid item xs={12}>
            <FormActions
              secondaryActions={[
                {
                  label: 'Cancel',
                  variant: 'outlined',
                  startIcon: <Cancel />,
                  onClick: handleCancel,
                  disabled: isSubmitting || isLoading,
                },
              ]}
              primaryAction={{
                label: isSubmitting || isLoading ? 'Saving...' : 'Update Indicator',
                type: 'submit',
                startIcon: <Save />,
                disabled: isSubmitting || isLoading,
                loading: isSubmitting || isLoading,
              }}
            />
          </Grid>
        </FormLayout>
      </form>
    </Box>
  );
};

export default IndicatorEditSimple;

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
  Divider
} from '@mui/material';
import { Home, Assessment, Edit, Save, Cancel } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { indicatorApi, schedulerApi } from '@/services/api';
import { IndicatorDto, UpdateIndicatorRequest } from '@/types/api';
import { CollectorSelector } from '@/components/CollectorSelector';

// Form data interface
interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number | null;
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
      collectorID: null,
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
      setValue('collectorID', existingIndicator.collectorID || null);
      setValue('collectorItemName', existingIndicator.collectorItemName);
      setValue('schedulerID', existingIndicator.schedulerID || null);
      setValue('lastMinutes', existingIndicator.lastMinutes);
      setValue('thresholdType', existingIndicator.thresholdType || 'volume_average');
      setValue('thresholdField', existingIndicator.thresholdField || 'Total');
      setValue('thresholdComparison', existingIndicator.thresholdComparison || 'lt');
      setValue('thresholdValue', existingIndicator.thresholdValue || 0);
      setValue('priority', typeof existingIndicator.priority === 'string' 
        ? (existingIndicator.priority === 'high' ? 3 : existingIndicator.priority === 'medium' ? 2 : 1)
        : existingIndicator.priority);
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
    
    // Transform form data to API request format
    const requestData: UpdateIndicatorRequest = {
      ...formData,
      indicatorID: indicatorId!,
      collectorID: formData.collectorID ?? undefined,
      schedulerID: formData.schedulerID ?? undefined,
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
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 3 }}>
        <Link
          color="inherit"
          href="/"
          onClick={(e) => {
            e.preventDefault();
            navigate('/');
          }}
          sx={{ display: 'flex', alignItems: 'center' }}
        >
          <Home sx={{ mr: 0.5 }} fontSize="inherit" />
          Home
        </Link>
        <Link
          color="inherit"
          href="/indicators"
          onClick={(e) => {
            e.preventDefault();
            navigate('/indicators');
          }}
          sx={{ display: 'flex', alignItems: 'center' }}
        >
          <Assessment sx={{ mr: 0.5 }} fontSize="inherit" />
          Indicators
        </Link>
        <Link
          color="inherit"
          href={`/indicators/${indicatorId}`}
          onClick={(e) => {
            e.preventDefault();
            navigate(`/indicators/${indicatorId}`);
          }}
        >
          {existingIndicator?.indicatorName || `Indicator ${indicatorId}`}
        </Link>
        <Typography color="text.primary" sx={{ display: 'flex', alignItems: 'center' }}>
          <Edit sx={{ mr: 0.5 }} fontSize="inherit" />
          Edit
        </Typography>
      </Breadcrumbs>

      {/* Page Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Edit Indicator
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Update the indicator configuration and settings.
        </Typography>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Form */}
      <Paper elevation={1} sx={{ p: 3 }}>
        <form onSubmit={handleSubmit(handleFormSubmit)}>
          <Grid container spacing={3}>
            {/* Basic Information */}
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom>
                Basic Information
              </Typography>
            </Grid>
            
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

            {/* Data Source */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>
                Data Source
              </Typography>
            </Grid>
            
            <Grid item xs={12}>
              <CollectorSelector
                selectedCollectorId={watchedValues.collectorID ?? undefined}
                selectedItemName={watchedValues.collectorItemName}
                onCollectorChange={(collectorId) => setValue('collectorID', collectorId || null)}
                onItemNameChange={(itemName) => setValue('collectorItemName', itemName)}
                required
                variant="detailed"
                showRefreshButton
                title="Data Collector"
                subtitle="Select the data collector and specific item to monitor"
              />
            </Grid>

            {/* Scheduling */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>
                Scheduling
              </Typography>
            </Grid>
            
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

            {/* Action Buttons */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Button
                  onClick={handleCancel}
                  startIcon={<Cancel />}
                  disabled={isSubmitting || isLoading}
                >
                  Cancel
                </Button>

                <Button
                  type="submit"
                  variant="contained"
                  startIcon={<Save />}
                  disabled={isSubmitting || isLoading}
                >
                  {isSubmitting || isLoading ? 'Saving...' : 'Update Indicator'}
                </Button>
              </Box>
            </Grid>
          </Grid>
        </form>
      </Paper>
    </Container>
  );
};

export default IndicatorEditSimple;

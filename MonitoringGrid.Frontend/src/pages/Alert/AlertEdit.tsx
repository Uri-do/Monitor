import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Alert,
  InputAdornment,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Message as MessageIcon,
  Details as DetailsIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { alertApi } from '@/services/api';
import { ResolveAlertRequest } from '@/types/api';
import toast from 'react-hot-toast';
import { PageHeader, LoadingSpinner, FormLayout, FormSection, FormActions } from '@/components';

// Validation schema - Only allow resolving alerts, not editing content
const alertSchema = yup.object({
  isResolved: yup.boolean(),
  resolutionNotes: yup.string().max(1000, 'Resolution notes must be less than 1000 characters').nullable(),
});

type AlertFormData = yup.InferType<typeof alertSchema>;

const AlertEdit: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const alertId = parseInt(id || '0');

  // Fetch alert data
  const { data: alert, isLoading: alertLoading } = useQuery({
    queryKey: ['alert', alertId],
    queryFn: () => alertApi.getAlert(alertId),
    enabled: !!alertId,
  });

  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<AlertFormData>({
    resolver: yupResolver(alertSchema),
    defaultValues: {
      isResolved: false,
      resolutionNotes: '',
    },
  });

  const watchedIsResolved = watch('isResolved');

  // Update form when alert data is loaded
  useEffect(() => {
    if (alert) {
      reset({
        isResolved: alert.isResolved || false,
        resolutionNotes: '',
      });
    }
  }, [alert, reset]);

  // Resolve alert mutation
  const resolveMutation = useMutation({
    mutationFn: (data: ResolveAlertRequest) => alertApi.resolveAlert(data),
    onSuccess: () => {
      toast.success('Alert resolved successfully');
      queryClient.invalidateQueries({ queryKey: ['alert', alertId] });
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
      navigate(`/alerts/${alertId}`);
    },
    onError: (error: any) => {
      const errorMessage = error.response?.data?.message || 'Failed to resolve alert';
      setError(errorMessage);
      toast.error(errorMessage);
    },
  });

  const onSubmit = (data: AlertFormData) => {
    setError(null);
    if (data.isResolved) {
      resolveMutation.mutate({
        alertId: alertId,
        resolvedBy: 'Current User', // TODO: Get from auth context
        resolutionNotes: data.resolutionNotes || undefined,
      });
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'critical':
        return <ErrorIcon color="error" />;
      case 'high':
        return <WarningIcon color="warning" />;
      case 'medium':
        return <InfoIcon color="info" />;
      case 'low':
        return <InfoIcon color="success" />;
      default:
        return <InfoIcon />;
    }
  };

  if (alertLoading) {
    return <LoadingSpinner />;
  }

  if (!alert) {
    return (
      <Box>
        <Alert severity="error">Alert not found or you don't have permission to edit it.</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Resolve Alert"
        subtitle={`Resolving Alert #${alert.alertID} • ${alert.indicatorName}`}
        breadcrumbs={[
          { label: 'Alerts', href: '/alerts' },
          { label: `Alert #${alert.alertID}`, href: `/alerts/${alertId}` },
          { label: 'Resolve' },
        ]}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)}>
        <FormLayout fullWidth spacing={3}>
          {/* Alert Information (Read-only) */}
          <Grid item xs={12}>
            <FormSection
              title="Alert Information"
              subtitle="Current alert details (read-only)"
              icon={getSeverityIcon(alert.severity)}
            >
              <Grid item xs={12}>
                <TextField
                  label="Alert Message"
                  value={alert.message}
                  fullWidth
                  multiline
                  rows={3}
                  InputProps={{ readOnly: true }}
                  variant="outlined"
                />
              </Grid>

              {alert.details && (
                <Grid item xs={12}>
                  <TextField
                    label="Additional Details"
                    value={alert.details}
                    fullWidth
                    multiline
                    rows={4}
                    InputProps={{ readOnly: true }}
                    variant="outlined"
                  />
                </Grid>
              )}

              <Grid item xs={12} md={6}>
                <TextField
                  label="Severity"
                  value={alert.severity}
                  fullWidth
                  InputProps={{ readOnly: true }}
                  variant="outlined"
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <TextField
                  label="Triggered"
                  value={new Date(alert.triggerTime).toLocaleString()}
                  fullWidth
                  InputProps={{ readOnly: true }}
                  variant="outlined"
                />
              </Grid>
            </FormSection>
          </Grid>

          {/* Resolution Status */}
          <Grid item xs={12}>
            <FormSection
              title="Resolution Status"
              subtitle="Mark alert as resolved and add resolution notes"
            >
              <Grid item xs={12}>
                <Controller
                  name="isResolved"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Switch {...field} checked={field.value} />}
                      label="Mark as Resolved"
                      sx={{ mt: 1 }}
                    />
                  )}
                />
              </Grid>

              {watchedIsResolved && (
                <Grid item xs={12}>
                  <Controller
                    name="resolutionNotes"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Resolution Notes"
                        fullWidth
                        multiline
                        rows={3}
                        error={!!errors.resolutionNotes}
                        helperText={errors.resolutionNotes?.message || 'Describe how the issue was resolved'}
                        placeholder="Explain the resolution steps taken..."
                      />
                    )}
                  />
                </Grid>
              )}
            </FormSection>
          </Grid>

          {/* Form Actions */}
          <Grid item xs={12}>
            <FormActions
              secondaryActions={[
                {
                  label: 'Cancel',
                  variant: 'outlined',
                  startIcon: <CancelIcon />,
                  onClick: () => navigate(`/alerts/${alertId}`),
                  disabled: isSubmitting,
                },
              ]}
              primaryAction={{
                label: isSubmitting ? 'Resolving...' : 'Resolve Alert',
                type: 'submit',
                startIcon: <SaveIcon />,
                disabled: isSubmitting || !watchedIsResolved,
                loading: isSubmitting,
              }}
            />
          </Grid>
        </FormLayout>
      </form>
    </Box>
  );
};

export default AlertEdit;

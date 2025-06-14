import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Paper, Alert, CircularProgress } from '@mui/material';
import { Assessment, ArrowBack as BackIcon } from '@mui/icons-material';
import { PageHeader } from '@/components/UI';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { EnhancedIndicatorForm } from '@/components/Business/Indicator/EnhancedIndicatorForm';
import { indicatorApi } from '@/services/api';
import { CreateIndicatorRequest } from '@/types/api';

// Define the form data type that matches what the form component expects
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
 * IndicatorCreate component - handles both creating new indicators and editing existing ones
 */
const IndicatorCreate: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const isEditMode = Boolean(id);
  const indicatorId = id ? parseInt(id, 10) : undefined;

  // Fetch existing indicator data if in edit mode
  const {
    data: existingIndicator,
    isLoading: isLoadingIndicator,
    error: loadError,
  } = useQuery({
    queryKey: ['indicator', indicatorId],
    queryFn: () => indicatorApi.getIndicator(indicatorId!),
    enabled: isEditMode && !!indicatorId,
  });

  // Create indicator mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateIndicatorRequest) => indicatorApi.createIndicator(data),
    onSuccess: newIndicator => {
      queryClient.invalidateQueries({ queryKey: ['indicators'] });
      navigate(`/indicators/${newIndicator.indicatorID}`);
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to create indicator');
    },
  });

  // Update indicator mutation
  const updateMutation = useMutation({
    mutationFn: (data: CreateIndicatorRequest) =>
      indicatorApi.updateIndicator({ ...data, indicatorID: indicatorId! }),
    onSuccess: updatedIndicator => {
      queryClient.invalidateQueries({ queryKey: ['indicators'] });
      queryClient.invalidateQueries({ queryKey: ['indicator', indicatorId] });
      navigate(`/indicators/${updatedIndicator.indicatorID}`);
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to update indicator');
    },
  });

  const handleSubmit = (formData: IndicatorFormData) => {
    setError(null);

    // Convert priority number to string as expected by backend
    const priorityString =
      formData.priority === 3 ? 'high' : formData.priority === 2 ? 'medium' : 'low';

    // Validate required fields
    if (!formData.collectorID) {
      setError('Collector is required');
      return;
    }

    // Transform form data to API request format
    const requestData: CreateIndicatorRequest = {
      ...formData,
      priority: priorityString, // Convert number to string
      collectorID: formData.collectorID, // Now required
      schedulerID: formData.schedulerID ?? undefined,
      averageLastDays: formData.averageLastDays ?? undefined,
      contactIds: [], // For now, use empty array. TODO: Add contact selection to form
      // Ensure required fields have values
      thresholdType: formData.thresholdType || 'volume_average',
      thresholdField: formData.thresholdField || 'Total',
      thresholdComparison: formData.thresholdComparison || 'lt',
      thresholdValue: formData.thresholdValue || 0,
    };

    if (isEditMode) {
      updateMutation.mutate(requestData);
    } else {
      createMutation.mutate(requestData);
    }
  };

  const handleCancel = () => {
    if (isEditMode) {
      navigate(`/indicators/${indicatorId}`);
    } else {
      navigate('/indicators');
    }
  };

  const isLoading = isLoadingIndicator || createMutation.isPending || updateMutation.isPending;

  if (isEditMode && isLoadingIndicator) {
    return (
      <Box sx={{ py: 4 }}>
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <CircularProgress />
        </Box>
      </Box>
    );
  }

  if (isEditMode && loadError) {
    return (
      <Box sx={{ py: 4 }}>
        <Alert severity="error">Failed to load indicator data. Please try again.</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title={isEditMode ? 'Edit Indicator' : 'Create New Indicator'}
        subtitle={
          isEditMode
            ? `Editing: ${existingIndicator?.indicatorName || 'Indicator'}`
            : 'Configure a new indicator to monitor your system metrics and performance.'
        }
        icon={<Assessment />}
        backAction={{
          label: isEditMode ? 'Back to Indicator' : 'Back to Indicators',
          icon: <BackIcon />,
          onClick: () => navigate(isEditMode ? `/indicators/${indicatorId}` : '/indicators'),
        }}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Form */}
      <Paper elevation={1} sx={{ p: 3 }}>
        <EnhancedIndicatorForm
          initialData={
            existingIndicator
              ? {
                  ...existingIndicator,
                  priority:
                    typeof existingIndicator.priority === 'string'
                      ? existingIndicator.priority === 'high'
                        ? 3
                        : existingIndicator.priority === 'medium'
                          ? 2
                          : 1
                      : existingIndicator.priority,
                }
              : undefined
          }
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isEdit={isEditMode}
          loading={isLoading}
        />
      </Paper>
    </Box>
  );
};

export default IndicatorCreate;

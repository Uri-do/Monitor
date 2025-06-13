import React from 'react';
import { FormControlLabel, Switch, Grid, Typography } from '@mui/material';
import { Save as SaveIcon, Cancel as CancelIcon, Assessment as KpiIcon } from '@mui/icons-material';
import { Dialog, InputField, Select, Button } from '@/components';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CreateIndicatorRequest, UpdateIndicatorRequest } from '@/types/api';

// Validation schema
const indicatorSchema = yup.object({
  indicator: yup.string().required('Indicator name is required'),
  owner: yup.string().required('Owner is required'),
  priority: yup.number().required('Priority is required').min(1).max(4),
  frequency: yup.number().required('Frequency is required').min(1),
  lastMinutes: yup.number().required('Data Window is required').min(1),
  deviation: yup.number().required('Deviation Threshold is required').min(0),
  spName: yup.string().required('Stored Procedure Name is required'),
  subjectTemplate: yup.string().required('Subject Template is required'),
  descriptionTemplate: yup.string().required('Description Template is required'),
  cooldownMinutes: yup.number().required('Cooldown Period is required').min(0),
  minimumThreshold: yup.number().nullable(),
  isActive: yup.boolean().required(),
});

export type IndicatorFormData = yup.InferType<typeof indicatorSchema>;

interface IndicatorFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateIndicatorRequest | UpdateIndicatorRequest) => void;
  initialData?: Partial<IndicatorFormData>;
  isEdit?: boolean;
  loading?: boolean;
}

const priorityOptions = [
  { value: 1, label: 'Critical', color: 'error' as const },
  { value: 2, label: 'High', color: 'warning' as const },
  { value: 3, label: 'Medium', color: 'info' as const },
  { value: 4, label: 'Low', color: 'success' as const },
];

export const IndicatorFormDialog: React.FC<IndicatorFormDialogProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  isEdit = false,
  loading: _loading = false,
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<IndicatorFormData>({
    resolver: yupResolver(indicatorSchema),
    defaultValues: {
      indicator: '',
      owner: '',
      priority: 3,
      frequency: 60,
      lastMinutes: 1440,
      deviation: 10,
      spName: '',
      subjectTemplate: '',
      descriptionTemplate: '',
      cooldownMinutes: 30,
      minimumThreshold: null,
      isActive: true,
      ...initialData,
    },
  });

  React.useEffect(() => {
    if (open && initialData) {
      reset(initialData);
    }
  }, [open, initialData, reset]);

  const handleFormSubmit = (data: IndicatorFormData) => {
    // Convert priority number to string as expected by backend
    const priorityString = data.priority === 1 ? 'critical' :
                          data.priority === 2 ? 'high' :
                          data.priority === 3 ? 'medium' : 'low';

    // Transform form data to API request format
    const apiData: CreateIndicatorRequest = {
      indicatorName: data.indicator,
      indicatorCode: data.indicator.toLowerCase().replace(/\s+/g, '_'),
      indicatorDesc: data.indicator,
      collectorID: 1, // Default collector ID - TODO: Get from form
      collectorItemName: data.spName,
      priority: priorityString, // Convert number to string
      schedulerID: undefined, // TODO: Add scheduler selection to form
      lastMinutes: data.lastMinutes,
      thresholdType: 'threshold_value', // Default threshold type
      thresholdField: 'Total', // Default threshold field
      thresholdComparison: 'gte', // Default comparison
      thresholdValue: data.minimumThreshold || 0,
      isActive: data.isActive,
      ownerContactId: 1, // TODO: Get from form or context
      contactIds: [], // TODO: Get from form
    };

    if (isEdit && initialData) {
      const updateData: UpdateIndicatorRequest = {
        ...apiData,
        indicatorID: (initialData as any).indicatorID || 0, // TODO: Pass indicatorID properly
      };
      onSubmit(updateData);
    } else {
      onSubmit(apiData);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      reset();
      onClose();
    }
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      title={isEdit ? 'Edit Indicator' : 'Create New Indicator'}
      subtitle={
        isEdit
          ? 'Update Indicator configuration and monitoring settings'
          : 'Create a new Indicator'
      }
      icon={<KpiIcon />}
      gradient="primary"
      maxWidth="md"
      actions={
        <>
          <Button
            variant="outlined"
            gradient="secondary"
            startIcon={<CancelIcon />}
            onClick={handleClose}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            gradient="primary"
            startIcon={<SaveIcon />}
            disabled={isSubmitting}
            onClick={handleSubmit(handleFormSubmit)}
          >
            {isSubmitting ? 'Saving...' : isEdit ? 'Update Indicator' : 'Create Indicator'}
          </Button>
        </>
      }
    >
      <Grid container spacing={3}>
        {/* Basic Information */}
        <Grid item xs={12}>
          <Typography variant="h6" gutterBottom>
            Basic Information
          </Typography>
        </Grid>

        <Grid item xs={12} md={8}>
          <Controller
            name="indicator"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Indicator Name"
                fullWidth
                error={!!errors.indicator}
                helperText={errors.indicator?.message}
                placeholder="e.g., Daily Sales Performance"
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="owner"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Owner"
                fullWidth
                error={!!errors.owner}
                helperText={errors.owner?.message}
                placeholder="e.g., Sales Team"
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="priority"
            control={control}
            render={({ field }) => (
              <Select
                {...field}
                label="Priority"
                fullWidth
                error={!!errors.priority}
                options={priorityOptions.map(option => ({
                  value: option.value,
                  label: option.label,
                }))}
              />
            )}
          />
          {errors.priority && (
            <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1.5 }}>
              {errors.priority.message}
            </Typography>
          )}
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="isActive"
            control={control}
            render={({ field }) => (
              <FormControlLabel
                control={<Switch {...field} checked={field.value} />}
                label="Active"
                sx={{ mt: 1 }}
              />
            )}
          />
        </Grid>

        {/* Monitoring Configuration */}
        <Grid item xs={12}>
          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
            Monitoring Configuration
          </Typography>
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="frequency"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Frequency (minutes)"
                type="number"
                fullWidth
                error={!!errors.frequency}
                helperText={errors.frequency?.message || 'How often to check this Indicator'}
                inputProps={{ min: 1 }}
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="lastMinutes"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Data Window (minutes)"
                type="number"
                fullWidth
                error={!!errors.lastMinutes}
                helperText={errors.lastMinutes?.message || 'How far back to look for data'}
                inputProps={{ min: 1 }}
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Controller
            name="deviation"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Deviation Threshold (%)"
                type="number"
                fullWidth
                error={!!errors.deviation}
                helperText={
                  errors.deviation?.message || 'Alert when deviation exceeds this percentage'
                }
                inputProps={{ min: 0, step: 0.1 }}
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="spName"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Stored Procedure Name"
                fullWidth
                error={!!errors.spName}
                helperText={errors.spName?.message || 'Database stored procedure to execute'}
                placeholder="e.g., sp_CheckDailySales"
              />
            )}
          />
        </Grid>

        <Grid item xs={12} md={6}>
          <Controller
            name="cooldownMinutes"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Cooldown Period (minutes)"
                type="number"
                fullWidth
                error={!!errors.cooldownMinutes}
                helperText={errors.cooldownMinutes?.message || 'Minimum time between alerts'}
                inputProps={{ min: 0 }}
              />
            )}
          />
        </Grid>

        {/* Notification Templates */}
        <Grid item xs={12}>
          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
            Notification Templates
          </Typography>
        </Grid>

        <Grid item xs={12}>
          <Controller
            name="subjectTemplate"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Subject Template"
                fullWidth
                error={!!errors.subjectTemplate}
                helperText={errors.subjectTemplate?.message || 'Email subject template for alerts'}
                placeholder="e.g., Alert: {indicator} deviation detected"
              />
            )}
          />
        </Grid>

        <Grid item xs={12}>
          <Controller
            name="descriptionTemplate"
            control={control}
            render={({ field }) => (
              <InputField
                {...field}
                label="Description Template"
                fullWidth
                multiline
                rows={3}
                error={!!errors.descriptionTemplate}
                helperText={errors.descriptionTemplate?.message || 'Email body template for alerts'}
                placeholder="e.g., The Indicator {indicator} has deviated by {deviation}% from the expected value..."
              />
            )}
          />
        </Grid>
      </Grid>
    </Dialog>
  );
};

export default IndicatorFormDialog;

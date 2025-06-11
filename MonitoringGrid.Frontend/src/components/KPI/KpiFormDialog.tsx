import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Grid,
  Box,
  Typography,
  Chip,
} from '@mui/material';
import { Save as SaveIcon, Cancel as CancelIcon } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CreateKpiRequest, UpdateKpiRequest, KpiType } from '@/types/api';

// Validation schema
const kpiSchema = yup.object({
  indicator: yup.string().required('KPI Indicator is required'),
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

type KpiFormData = yup.InferType<typeof kpiSchema>;

interface KpiFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateKpiRequest | UpdateKpiRequest) => void;
  initialData?: Partial<KpiFormData>;
  isEdit?: boolean;
  loading?: boolean;
}

const priorityOptions = [
  { value: 1, label: 'Critical', color: 'error' as const },
  { value: 2, label: 'High', color: 'warning' as const },
  { value: 3, label: 'Medium', color: 'info' as const },
  { value: 4, label: 'Low', color: 'success' as const },
];

export const KpiFormDialog: React.FC<KpiFormDialogProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  isEdit = false,
  loading = false,
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<KpiFormData>({
    resolver: yupResolver(kpiSchema),
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

  const handleFormSubmit = (data: KpiFormData) => {
    onSubmit(data as CreateKpiRequest | UpdateKpiRequest);
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
      maxWidth="md"
      fullWidth
      PaperProps={{
        component: 'form',
        onSubmit: handleSubmit(handleFormSubmit),
      }}
    >
      <DialogTitle>
        {isEdit ? 'Edit KPI' : 'Create New KPI'}
      </DialogTitle>

      <DialogContent dividers>
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
                <TextField
                  {...field}
                  label="KPI Indicator"
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
                <TextField
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
                <FormControl fullWidth error={!!errors.priority}>
                  <InputLabel>Priority</InputLabel>
                  <Select {...field} label="Priority">
                    {priorityOptions.map(option => (
                      <MenuItem key={option.value} value={option.value}>
                        <Box display="flex" alignItems="center" gap={1}>
                          <Chip label={option.label} color={option.color} size="small" />
                        </Box>
                      </MenuItem>
                    ))}
                  </Select>
                  {errors.priority && (
                    <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1.5 }}>
                      {errors.priority.message}
                    </Typography>
                  )}
                </FormControl>
              )}
            />
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
                <TextField
                  {...field}
                  label="Frequency (minutes)"
                  type="number"
                  fullWidth
                  error={!!errors.frequency}
                  helperText={errors.frequency?.message || 'How often to check this KPI'}
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
                <TextField
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
                <TextField
                  {...field}
                  label="Deviation Threshold (%)"
                  type="number"
                  fullWidth
                  error={!!errors.deviation}
                  helperText={errors.deviation?.message || 'Alert when deviation exceeds this percentage'}
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
                <TextField
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
                <TextField
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
                <TextField
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
                <TextField
                  {...field}
                  label="Description Template"
                  fullWidth
                  multiline
                  rows={3}
                  error={!!errors.descriptionTemplate}
                  helperText={errors.descriptionTemplate?.message || 'Email body template for alerts'}
                  placeholder="e.g., The KPI {indicator} has deviated by {deviation}% from the expected value..."
                />
              )}
            />
          </Grid>
        </Grid>
      </DialogContent>

      <DialogActions>
        <Button
          onClick={handleClose}
          disabled={isSubmitting || loading}
          startIcon={<CancelIcon />}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          variant="contained"
          disabled={isSubmitting || loading}
          startIcon={<SaveIcon />}
        >
          {isSubmitting || loading ? 'Saving...' : isEdit ? 'Update KPI' : 'Create KPI'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default KpiFormDialog;

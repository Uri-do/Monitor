import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Button,
  Typography,
  Alert,
  Chip,
  Autocomplete,
  Stack,
  Divider,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  PlayArrow as TestIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { kpiApi, contactApi } from '@/services/api';
import { CreateKpiRequest, UpdateKpiRequest, ContactDto } from '@/types/api';
import toast from 'react-hot-toast';
import {
  PageHeader,
  LoadingSpinner,
} from '@/components/Common';

// Validation schema
const kpiSchema = yup.object({
  indicator: yup.string().required('Indicator is required').max(255, 'Indicator must be less than 255 characters'),
  owner: yup.string().required('Owner is required').max(100, 'Owner must be less than 100 characters'),
  priority: yup.number().required('Priority is required').min(1, 'Priority must be between 1-4').max(4, 'Priority must be between 1-4'),
  frequency: yup.number().required('Frequency is required').min(1, 'Frequency must be at least 1 minute'),
  deviation: yup.number().required('Deviation threshold is required').min(0, 'Deviation must be positive'),
  spName: yup.string().required('Stored procedure name is required').max(255, 'SP name must be less than 255 characters'),
  subjectTemplate: yup.string().required('Subject template is required').max(500, 'Subject template must be less than 500 characters'),
  descriptionTemplate: yup.string().required('Description template is required').max(2000, 'Description template must be less than 2000 characters'),
  cooldownMinutes: yup.number().required('Cooldown period is required').min(0, 'Cooldown must be positive'),
  minimumThreshold: yup.number().nullable().min(0, 'Minimum threshold must be positive'),
  isActive: yup.boolean(),
  contactIds: yup.array().of(yup.number()),
});

type KpiFormData = yup.InferType<typeof kpiSchema>;

const priorityOptions = [
  { value: 1, label: 'Critical', color: 'error' as const },
  { value: 2, label: 'High', color: 'warning' as const },
  { value: 3, label: 'Medium', color: 'info' as const },
  { value: 4, label: 'Low', color: 'success' as const },
];

const KpiCreate: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = Boolean(id);
  const kpiId = parseInt(id || '0');

  const [selectedContacts, setSelectedContacts] = useState<ContactDto[]>([]);

  // Fetch KPI for editing
  const { data: kpi, isLoading: kpiLoading } = useQuery({
    queryKey: ['kpi', kpiId],
    queryFn: () => kpiApi.getKpi(kpiId),
    enabled: isEdit && !!kpiId,
  });

  // Fetch all contacts for assignment
  const { data: contacts = [], isLoading: contactsLoading } = useQuery({
    queryKey: ['contacts'],
    queryFn: () => contactApi.getContacts({ isActive: true }),
  });

  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<KpiFormData>({
    resolver: yupResolver(kpiSchema),
    defaultValues: {
      indicator: '',
      owner: '',
      priority: 3,
      frequency: 60,
      deviation: 10,
      spName: '',
      subjectTemplate: '',
      descriptionTemplate: '',
      cooldownMinutes: 30,
      minimumThreshold: null,
      isActive: true,
      contactIds: [],
    },
  });

  // Watch priority for color display
  const watchedPriority = watch('priority');

  // Update form when KPI data is loaded
  useEffect(() => {
    if (kpi && isEdit) {
      reset({
        indicator: kpi.indicator,
        owner: kpi.owner,
        priority: kpi.priority,
        frequency: kpi.frequency,
        deviation: kpi.deviation,
        spName: kpi.spName,
        subjectTemplate: kpi.subjectTemplate,
        descriptionTemplate: kpi.descriptionTemplate,
        cooldownMinutes: kpi.cooldownMinutes,
        minimumThreshold: kpi.minimumThreshold,
        isActive: kpi.isActive,
        contactIds: kpi.contacts.map(c => c.contactId),
      });
      setSelectedContacts(kpi.contacts);
    }
  }, [kpi, isEdit, reset]);

  // Create/Update mutations
  const createMutation = useMutation({
    mutationFn: (data: CreateKpiRequest) => kpiApi.createKpi(data),
    onSuccess: () => {
      toast.success('KPI created successfully');
      queryClient.invalidateQueries({ queryKey: ['kpis'] });
      navigate('/kpis');
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create KPI');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateKpiRequest) => kpiApi.updateKpi(data),
    onSuccess: () => {
      toast.success('KPI updated successfully');
      queryClient.invalidateQueries({ queryKey: ['kpis'] });
      queryClient.invalidateQueries({ queryKey: ['kpi', kpiId] });
      navigate(`/kpis/${kpiId}`);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update KPI');
    },
  });

  // Test KPI mutation
  const testMutation = useMutation({
    mutationFn: () => kpiApi.executeKpi({ kpiId }),
    onSuccess: (result) => {
      if (result.isSuccessful) {
        toast.success('KPI test completed successfully');
      } else {
        toast.error(`KPI test failed: ${result.errorMessage}`);
      }
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to test KPI');
    },
  });

  const onSubmit = (data: KpiFormData) => {
    const formData = {
      ...data,
      contactIds: selectedContacts.map(c => c.contactId),
    };

    if (isEdit) {
      updateMutation.mutate({
        ...formData,
        kpiId,
        isActive: formData.isActive ?? true
      });
    } else {
      createMutation.mutate({
        ...formData,
        isActive: formData.isActive ?? true
      });
    }
  };

  const handleContactChange = (event: any, newValue: ContactDto[]) => {
    setSelectedContacts(newValue);
  };

  const handleTest = () => {
    if (isEdit && kpiId) {
      testMutation.mutate();
    } else {
      toast.info('Save the KPI first to test it');
    }
  };

  if (kpiLoading || contactsLoading) {
    return <LoadingSpinner />;
  }

  const selectedPriority = priorityOptions.find(p => p.value === watchedPriority);

  return (
    <Box>
      <PageHeader
        title={isEdit ? 'Edit KPI' : 'Create KPI'}
        subtitle={isEdit ? `Editing: ${kpi?.indicator}` : 'Create a new Key Performance Indicator'}
        breadcrumbs={[
          { label: 'KPIs', href: '/kpis' },
          { label: isEdit ? 'Edit' : 'Create' },
        ]}
        actions={isEdit ? [
          {
            label: 'Test KPI',
            icon: <TestIcon />,
            onClick: handleTest,
            disabled: testMutation.isPending,
            variant: 'outlined',
          },
        ] : []}
      />

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Basic Information
                </Typography>
                <Grid container spacing={2}>
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
                            {priorityOptions.map((option) => (
                              <MenuItem key={option.value} value={option.value}>
                                <Box display="flex" alignItems="center" gap={1}>
                                  <Chip
                                    label={option.label}
                                    color={option.color}
                                    size="small"
                                  />
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
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* Monitoring Configuration */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Monitoring Configuration
                </Typography>
                <Grid container spacing={2}>
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
                  <Grid item xs={12} md={4}>
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
                      name="minimumThreshold"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Minimum Threshold (optional)"
                          type="number"
                          fullWidth
                          error={!!errors.minimumThreshold}
                          helperText={errors.minimumThreshold?.message || 'Minimum value to consider for alerts'}
                          inputProps={{ min: 0, step: 0.01 }}
                          value={field.value || ''}
                          onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* Notification Templates */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Notification Templates
                </Typography>
                <Grid container spacing={2}>
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
                          rows={4}
                          error={!!errors.descriptionTemplate}
                          helperText={errors.descriptionTemplate?.message || 'Email body template for alerts'}
                          placeholder="e.g., The KPI {indicator} has deviated by {deviation}% from the expected value..."
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* Contact Assignment */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Contact Assignment
                </Typography>
                <Autocomplete
                  multiple
                  options={contacts}
                  getOptionLabel={(option) => option.name}
                  value={selectedContacts}
                  onChange={handleContactChange}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label="Assigned Contacts"
                      placeholder="Select contacts to notify"
                      helperText="Choose contacts who will receive alerts for this KPI"
                    />
                  )}
                  renderTags={(value, getTagProps) =>
                    value.map((option, index) => (
                      <Chip
                        variant="outlined"
                        label={option.name}
                        {...getTagProps({ index })}
                        key={option.contactId}
                      />
                    ))
                  }
                  renderOption={(props, option) => (
                    <Box component="li" {...props}>
                      <Box>
                        <Typography variant="body2">{option.name}</Typography>
                        <Typography variant="caption" color="textSecondary">
                          {option.email} {option.phone && `• ${option.phone}`}
                        </Typography>
                      </Box>
                    </Box>
                  )}
                />
              </CardContent>
            </Card>
          </Grid>

          {/* Form Actions */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Stack direction="row" spacing={2} justifyContent="flex-end">
                  <Button
                    variant="outlined"
                    startIcon={<CancelIcon />}
                    onClick={() => navigate('/kpis')}
                    disabled={isSubmitting}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    variant="contained"
                    startIcon={<SaveIcon />}
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? 'Saving...' : (isEdit ? 'Update KPI' : 'Create KPI')}
                  </Button>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};

export default KpiCreate;

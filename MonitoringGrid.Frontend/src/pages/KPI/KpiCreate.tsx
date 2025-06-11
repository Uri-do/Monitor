import React, { useState, useEffect } from 'react';
import {
  Box,
  CardContent,
  Grid,
  FormControlLabel,
  Switch,
  Typography,
  Alert,
  Chip,
  Autocomplete,
  Stack,
  Divider,
  TextField,
} from '@mui/material';
import { Save as SaveIcon, Cancel as CancelIcon, PlayArrow as TestIcon } from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { kpiApi } from '@/services/api';
import { useKpi } from '@/hooks/useKpis';
import { useActiveContacts } from '@/hooks/useContacts';
import { useCreateKpi, useUpdateKpi, useExecuteKpi } from '@/hooks/mutations';
import {
  CreateKpiRequest,
  UpdateKpiRequest,
  ContactDto,
  KpiType,
  ScheduleConfiguration,
  ScheduleType,
} from '@/types/api';
import toast from 'react-hot-toast';
import { PageHeader, LoadingSpinner, Card, InputField, Select, Button } from '@/components';
import { SchedulerComponent, KpiTypeSelector } from '@/components/Business';

// Validation schema
const kpiSchema = yup.object({
  indicator: yup
    .string()
    .required('Indicator is required')
    .max(255, 'Indicator must be less than 255 characters'),
  owner: yup
    .string()
    .required('Owner is required')
    .max(100, 'Owner must be less than 100 characters'),
  priority: yup
    .number()
    .required('Priority is required')
    .min(1, 'Priority must be between 1-4')
    .max(4, 'Priority must be between 1-4'),
  frequency: yup
    .number()
    .required('Frequency is required')
    .min(1, 'Frequency must be at least 1 minute'),
  lastMinutes: yup
    .number()
    .required('Data window is required')
    .min(1, 'Data window must be at least 1 minute'),
  deviation: yup
    .number()
    .required('Deviation threshold is required')
    .min(0, 'Deviation must be positive'),
  spName: yup
    .string()
    .required('Stored procedure name is required')
    .max(255, 'SP name must be less than 255 characters'),
  subjectTemplate: yup
    .string()
    .required('Subject template is required')
    .max(500, 'Subject template must be less than 500 characters'),
  descriptionTemplate: yup
    .string()
    .required('Description template is required')
    .max(2000, 'Description template must be less than 2000 characters'),
  cooldownMinutes: yup
    .number()
    .required('Cooldown period is required')
    .min(0, 'Cooldown must be positive'),
  minimumThreshold: yup.number().nullable().min(0, 'Minimum threshold must be positive'),
  isActive: yup.boolean(),
  contactIds: yup.array().of(yup.number()),
  kpiType: yup.string().oneOf(Object.values(KpiType)),
  thresholdValue: yup.number().nullable().min(0, 'Threshold value must be positive'),
  comparisonOperator: yup.string().oneOf(['gt', 'lt', 'eq', 'gte', 'lte']),
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
  const isEdit = Boolean(id);
  const kpiId = parseInt(id || '0');

  const [selectedContacts, setSelectedContacts] = useState<ContactDto[]>([]);
  const [kpiType, setKpiType] = useState<KpiType>(KpiType.SuccessRate);
  const [scheduleConfig, setScheduleConfig] = useState<ScheduleConfiguration>({
    scheduleType: ScheduleType.Interval,
    intervalMinutes: 60,
    isEnabled: false,
    timezone: 'UTC',
  });
  const [thresholdValue, setThresholdValue] = useState<number | undefined>();
  const [comparisonOperator, setComparisonOperator] = useState<'gt' | 'lt' | 'eq' | 'gte' | 'lte'>(
    'gt'
  );

  // Use our enhanced hooks
  const { data: kpi, isLoading: kpiLoading } = useKpi(kpiId);
  const { data: contacts = [], isLoading: contactsLoading } = useActiveContacts();

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
      lastMinutes: 1440,
      deviation: 10,
      spName: '',
      subjectTemplate: '',
      descriptionTemplate: '',
      cooldownMinutes: 30,
      minimumThreshold: null,
      isActive: true,
      contactIds: [],
      kpiType: KpiType.SuccessRate,
      thresholdValue: null,
      comparisonOperator: 'gt',
    },
  });

  // Watch priority for color display
  const watchedPriority = watch('priority');

  // Watch frequency field to sync with schedule configuration
  const watchedFrequency = watch('frequency');

  // Update form when KPI data is loaded
  useEffect(() => {
    if (kpi && isEdit) {
      reset({
        indicator: kpi.indicator,
        owner: kpi.owner,
        priority: kpi.priority,
        frequency: kpi.frequency,
        lastMinutes: kpi.lastMinutes,
        deviation: kpi.deviation,
        spName: kpi.spName,
        subjectTemplate: kpi.subjectTemplate,
        descriptionTemplate: kpi.descriptionTemplate,
        cooldownMinutes: kpi.cooldownMinutes,
        minimumThreshold: kpi.minimumThreshold,
        isActive: kpi.isActive,
        contactIds: kpi.contacts.map(c => c.contactId),
        kpiType: kpi.kpiType || KpiType.SuccessRate,
        thresholdValue: kpi.thresholdValue,
        comparisonOperator: kpi.comparisonOperator || 'gt',
      });
      setSelectedContacts(kpi.contacts);
      setKpiType(kpi.kpiType || KpiType.SuccessRate);
      setScheduleConfig(
        kpi.scheduleConfiguration || {
          scheduleType: ScheduleType.Interval,
          intervalMinutes: kpi.frequency,
          isEnabled: false,
          timezone: 'UTC',
        }
      );
      setThresholdValue(kpi.thresholdValue);
      setComparisonOperator(kpi.comparisonOperator || 'gt');
    }
  }, [kpi, isEdit, reset]);

  // Use our custom mutation hooks
  const createKpiMutation = useCreateKpi();
  const updateKpiMutation = useUpdateKpi();
  const executeKpiMutation = useExecuteKpi();

  // Override success callbacks to handle navigation
  const createMutation = {
    ...createKpiMutation,
    mutate: (data: CreateKpiRequest) => {
      createKpiMutation.mutate(data, {
        onSuccess: () => navigate('/kpis'),
      });
    },
  };

  const updateMutation = {
    ...updateKpiMutation,
    mutate: (data: UpdateKpiRequest) => {
      updateKpiMutation.mutate(data, {
        onSuccess: () => navigate(`/kpis/${kpiId}`),
      });
    },
  };

  const testMutation = {
    ...executeKpiMutation,
    mutate: () => {
      if (isEdit && kpiId) {
        executeKpiMutation.mutate({ kpiId });
      }
    },
  };

  const onSubmit = (data: KpiFormData) => {
    const formData = {
      ...data,
      contactIds: selectedContacts.map(c => c.contactId),
      kpiType,
      scheduleConfiguration: scheduleConfig,
      thresholdValue,
      comparisonOperator,
    };

    if (isEdit) {
      updateMutation.mutate({
        ...formData,
        kpiId,
        isActive: formData.isActive ?? true,
        minimumThreshold: formData.minimumThreshold ?? undefined,
      });
    } else {
      createMutation.mutate({
        ...formData,
        isActive: formData.isActive ?? true,
        minimumThreshold: formData.minimumThreshold ?? undefined,
      });
    }
  };

  const handleContactChange = (event: any, newValue: ContactDto[]) => {
    setSelectedContacts(newValue);
  };

  // Handle schedule configuration changes and sync with frequency field
  const handleScheduleConfigChange = (newScheduleConfig: ScheduleConfiguration) => {
    setScheduleConfig(newScheduleConfig);

    // Sync the frequency field with the schedule configuration
    if (
      newScheduleConfig.scheduleType === ScheduleType.Interval &&
      newScheduleConfig.intervalMinutes
    ) {
      // Update the form's frequency field to match the schedule interval
      reset({
        ...watch(),
        frequency: newScheduleConfig.intervalMinutes,
      });
    }
  };

  // Sync frequency field changes back to schedule configuration
  React.useEffect(() => {
    if (
      scheduleConfig.scheduleType === ScheduleType.Interval &&
      watchedFrequency !== scheduleConfig.intervalMinutes
    ) {
      setScheduleConfig(prev => ({
        ...prev,
        intervalMinutes: watchedFrequency,
      }));
    }
  }, [watchedFrequency, scheduleConfig.scheduleType, scheduleConfig.intervalMinutes]);

  const handleTest = () => {
    if (isEdit && kpiId) {
      testMutation.mutate();
    } else {
      toast('Save the KPI first to test it', { icon: 'ℹ️' });
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
          { label: 'KPIs', onClick: () => navigate('/kpis') },
          { label: isEdit ? 'Edit' : 'Create' },
        ]}
        secondaryActions={
          isEdit
            ? [
                {
                  label: 'Test KPI',
                  icon: <TestIcon />,
                  onClick: handleTest,
                  gradient: 'info',
                },
              ]
            : []
        }
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
                        <InputField
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
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* KPI Type Configuration */}
          <Grid item xs={12}>
            <KpiTypeSelector
              selectedType={kpiType}
              onTypeChange={setKpiType}
              thresholdValue={thresholdValue}
              onThresholdChange={setThresholdValue}
              comparisonOperator={comparisonOperator}
              onOperatorChange={setComparisonOperator}
            />
          </Grid>

          {/* Schedule Configuration */}
          <Grid item xs={12}>
            <SchedulerComponent
              value={scheduleConfig}
              onChange={handleScheduleConfigChange}
              showAdvanced={true}
            />
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
                        <InputField
                          {...field}
                          label="Frequency (minutes)"
                          type="number"
                          fullWidth
                          error={!!errors.frequency}
                          helperText={
                            errors.frequency?.message ||
                            'How often to check this KPI. Uses whole time scheduling (e.g., 5min = xx:00, xx:05, xx:10...)'
                          }
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
                          helperText={
                            errors.lastMinutes?.message ||
                            'How far back to look for data (e.g., 1440 = 24 hours)'
                          }
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
                            errors.deviation?.message ||
                            'Alert when deviation exceeds this percentage'
                          }
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
                        <InputField
                          {...field}
                          label="Cooldown Period (minutes)"
                          type="number"
                          fullWidth
                          error={!!errors.cooldownMinutes}
                          helperText={
                            errors.cooldownMinutes?.message || 'Minimum time between alerts'
                          }
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
                        <InputField
                          {...field}
                          label="Stored Procedure Name"
                          fullWidth
                          error={!!errors.spName}
                          helperText={
                            errors.spName?.message || 'Database stored procedure to execute'
                          }
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
                        <InputField
                          {...field}
                          label="Minimum Threshold (optional)"
                          type="number"
                          fullWidth
                          error={!!errors.minimumThreshold}
                          helperText={
                            errors.minimumThreshold?.message ||
                            'Minimum value to consider for alerts'
                          }
                          inputProps={{ min: 0, step: 0.01 }}
                          value={field.value || ''}
                          onChange={e =>
                            field.onChange(e.target.value ? parseFloat(e.target.value) : null)
                          }
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
                        <InputField
                          {...field}
                          label="Subject Template"
                          fullWidth
                          error={!!errors.subjectTemplate}
                          helperText={
                            errors.subjectTemplate?.message || 'Email subject template for alerts'
                          }
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
                          rows={4}
                          error={!!errors.descriptionTemplate}
                          helperText={
                            errors.descriptionTemplate?.message || 'Email body template for alerts'
                          }
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
                  getOptionLabel={option => option.name}
                  value={selectedContacts}
                  onChange={handleContactChange}
                  isOptionEqualToValue={(option, value) => option.contactId === value.contactId}
                  renderInput={params => (
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
                    gradient="secondary"
                    startIcon={<CancelIcon />}
                    onClick={() => navigate('/kpis')}
                    disabled={isSubmitting}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    gradient="primary"
                    startIcon={<SaveIcon />}
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? 'Saving...' : isEdit ? 'Update KPI' : 'Create KPI'}
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

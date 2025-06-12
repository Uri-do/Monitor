import React, { useState, useEffect } from 'react';
import {
  Box,
  CardContent,
  Grid,
  FormControlLabel,
  Switch,
  Typography,
  Alert,
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
import { useIndicator, useCollectors, useCollectorItemNames } from '@/hooks/useIndicators';
import { useActiveContacts } from '@/hooks/useContacts';
import { useCreateIndicator, useUpdateIndicator, useExecuteIndicator } from '@/hooks/useIndicatorMutations';
import {
  CreateIndicatorRequest,
  UpdateIndicatorRequest,
  ContactDto,
} from '@/types/api';
import toast from 'react-hot-toast';
import { PageHeader, LoadingSpinner, Card, InputField, Select, Button } from '@/components';

// Form validation schema
const indicatorSchema = yup.object().shape({
  indicatorName: yup.string().required('Indicator name is required').max(255),
  indicatorCode: yup.string().required('Indicator code is required').max(100),
  indicatorDesc: yup.string().optional().max(500),
  collectorId: yup.number().required('Collector is required').min(1),
  collectorItemName: yup.string().required('Collector item is required'),
  scheduleConfiguration: yup.string().required('Schedule configuration is required'),
  lastMinutes: yup.number().required('Time range is required').min(1),
  thresholdType: yup.string().optional(),
  thresholdField: yup.string().optional(),
  thresholdComparison: yup.string().optional(),
  thresholdValue: yup.number().optional(),
  priority: yup.number().required('Priority is required').min(1).max(5),
  ownerContactId: yup.number().required('Owner contact is required').min(1),
  averageLastDays: yup.number().optional().min(1),
  isActive: yup.boolean().default(true),
  contactIds: yup.array().of(yup.number().required()).default([]),
});

interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorId: number;
  collectorItemName: string;
  scheduleConfiguration: string;
  lastMinutes: number;
  thresholdType?: string;
  thresholdField?: string;
  thresholdComparison?: string;
  thresholdValue?: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
  contactIds: number[];
}

const IndicatorCreate: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const indicatorId = id ? parseInt(id, 10) : undefined;
  const isEdit = !!indicatorId;

  const [selectedContacts, setSelectedContacts] = useState<ContactDto[]>([]);
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | null>(null);

  // Use our enhanced hooks
  const { data: indicator, isLoading: indicatorLoading } = useIndicator(indicatorId || 0);
  const { data: contacts = [], isLoading: contactsLoading } = useActiveContacts();
  const { data: collectors = [], isLoading: collectorsLoading } = useCollectors();
  const { data: collectorItems = [], isLoading: itemsLoading } = useCollectorItemNames(selectedCollectorId || 0);

  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(indicatorSchema),
    defaultValues: {
      indicatorName: '',
      indicatorCode: '',
      indicatorDesc: '',
      collectorId: 0,
      collectorItemName: '',
      scheduleConfiguration: JSON.stringify({
        scheduleType: 'interval',
        intervalMinutes: 60,
        isEnabled: true,
        timezone: 'UTC',
      }),
      lastMinutes: 60,
      priority: 3,
      ownerContactId: 0,
      isActive: true,
      contactIds: [],
    },
  });

  const watchedCollectorId = watch('collectorId');

  // Update selected collector when form value changes
  useEffect(() => {
    if (watchedCollectorId && watchedCollectorId !== selectedCollectorId) {
      setSelectedCollectorId(watchedCollectorId);
      // Clear collector item name when collector changes
      setValue('collectorItemName', '');
    }
  }, [watchedCollectorId, selectedCollectorId, setValue]);

  // Populate form when editing
  useEffect(() => {
    if (isEdit && indicator) {
      reset({
        indicatorName: indicator.indicatorName,
        indicatorCode: indicator.indicatorCode,
        indicatorDesc: indicator.indicatorDesc || '',
        collectorId: indicator.collectorId,
        collectorItemName: indicator.collectorItemName,
        scheduleConfiguration: indicator.scheduleConfiguration,
        lastMinutes: indicator.lastMinutes,
        thresholdType: indicator.thresholdType || '',
        thresholdField: indicator.thresholdField || '',
        thresholdComparison: indicator.thresholdComparison || '',
        thresholdValue: indicator.thresholdValue,
        priority: indicator.priority,
        ownerContactId: indicator.ownerContactId,
        averageLastDays: indicator.averageLastDays,
        isActive: indicator.isActive,
        contactIds: indicator.contacts.map(c => c.contactId),
      });
      setSelectedContacts(indicator.contacts);
      setSelectedCollectorId(indicator.collectorId);
    }
  }, [indicator, isEdit, reset]);

  // Use our custom mutation hooks
  const createIndicatorMutation = useCreateIndicator();
  const updateIndicatorMutation = useUpdateIndicator();
  const executeIndicatorMutation = useExecuteIndicator();

  const onSubmit = (data: IndicatorFormData) => {
    const formData = {
      ...data,
      contactIds: selectedContacts.map(c => c.contactId),
    };

    if (isEdit && indicatorId) {
      updateIndicatorMutation.mutate({
        ...formData,
        indicatorId,
      }, {
        onSuccess: () => navigate(`/indicators/${indicatorId}`),
      });
    } else {
      createIndicatorMutation.mutate(formData, {
        onSuccess: () => navigate('/indicators'),
      });
    }
  };

  const handleTest = () => {
    if (isEdit && indicatorId) {
      executeIndicatorMutation.mutate({ indicatorId });
    }
  };

  const handleContactChange = (event: any, newValue: ContactDto[]) => {
    setSelectedContacts(newValue);
  };

  if (indicatorLoading || contactsLoading || collectorsLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title={isEdit ? 'Edit Indicator' : 'Create Indicator'}
        subtitle={isEdit ? `Editing ${indicator?.indicatorName}` : 'Create a new performance indicator'}
        actions={[
          {
            label: 'Cancel',
            icon: <CancelIcon />,
            onClick: () => navigate(isEdit ? `/indicators/${indicatorId}` : '/indicators'),
          },
        ]}
      />

      <Card>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)}>
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
                  render={({ field }) => (
                    <InputField
                      {...field}
                      label="Indicator Name"
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
                  render={({ field }) => (
                    <InputField
                      {...field}
                      label="Indicator Code"
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
                    <InputField
                      {...field}
                      label="Description"
                      multiline
                      rows={3}
                      error={!!errors.indicatorDesc}
                      helperText={errors.indicatorDesc?.message}
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Divider />
              </Grid>

              {/* Collector Configuration */}
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom>
                  Data Source Configuration
                </Typography>
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="collectorId"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Collector"
                      options={collectors.map(c => ({ value: c.collectorId, label: c.collectorName }))}
                      error={!!errors.collectorId}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="collectorItemName"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Collector Item"
                      options={collectorItems.map(item => ({ value: item, label: item }))}
                      error={!!errors.collectorItemName}
                      disabled={!selectedCollectorId || itemsLoading}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Divider />
              </Grid>

              {/* Configuration */}
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom>
                  Configuration
                </Typography>
              </Grid>

              <Grid item xs={12} md={4}>
                <Controller
                  name="lastMinutes"
                  control={control}
                  render={({ field }) => (
                    <InputField
                      {...field}
                      label="Time Range (minutes)"
                      type="number"
                      error={!!errors.lastMinutes}
                      helperText={errors.lastMinutes?.message}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={4}>
                <Controller
                  name="priority"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Priority"
                      options={[
                        { value: 1, label: 'High (1)' },
                        { value: 2, label: 'Medium (2)' },
                        { value: 3, label: 'Normal (3)' },
                        { value: 4, label: 'Low (4)' },
                        { value: 5, label: 'Very Low (5)' },
                      ]}
                      error={!!errors.priority}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={4}>
                <Controller
                  name="ownerContactId"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Owner Contact"
                      options={contacts.map(c => ({ value: c.contactId, label: c.name }))}
                      error={!!errors.ownerContactId}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Autocomplete
                  multiple
                  options={contacts}
                  getOptionLabel={(option) => option.name}
                  value={selectedContacts}
                  onChange={handleContactChange}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label="Additional Contacts"
                      placeholder="Select contacts to notify"
                    />
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
                    />
                  )}
                />
              </Grid>

              {/* Actions */}
              <Grid item xs={12}>
                <Stack direction="row" spacing={2} justifyContent="flex-end">
                  {isEdit && (
                    <Button
                      variant="outlined"
                      startIcon={<TestIcon />}
                      onClick={handleTest}
                      disabled={executeIndicatorMutation.isPending}
                    >
                      Test Execute
                    </Button>
                  )}
                  <Button
                    type="submit"
                    variant="contained"
                    startIcon={<SaveIcon />}
                    disabled={isSubmitting || createIndicatorMutation.isPending || updateIndicatorMutation.isPending}
                  >
                    {isEdit ? 'Update' : 'Create'} Indicator
                  </Button>
                </Stack>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default IndicatorCreate;

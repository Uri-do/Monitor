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
  Paper,
  IconButton,
  Tooltip,
  MenuItem,
} from '@mui/material';
import { Save as SaveIcon, Cancel as CancelIcon, PlayArrow as TestIcon, Help as HelpIcon } from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useIndicator } from '@/hooks/useIndicators';
import { CollectorSelector } from '@/components/CollectorSelector';
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
  thresholdType: yup.string().required('Threshold type is required'),
  thresholdField: yup.string().required('Threshold field is required'),
  thresholdComparison: yup.string().required('Threshold comparison is required'),
  thresholdValue: yup.number().required('Threshold value is required'),
  priority: yup.string().required('Priority is required'),
  ownerContactId: yup.number().required('Owner contact is required').min(1),
  averageLastDays: yup.number().optional().min(1),
  averageOfCurrHour: yup.boolean().default(false),
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
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: string;
  ownerContactId: number;
  averageLastDays?: number;
  averageOfCurrHour: boolean;
  isActive: boolean;
  contactIds: number[];
}

// Schedule configuration helper component
const ScheduleConfigHelper: React.FC<{ value: string; onChange: (value: string) => void }> = ({ value, onChange }) => {
  const [scheduleType, setScheduleType] = useState('interval');
  const [intervalMinutes, setIntervalMinutes] = useState(60);
  const [cronExpression, setCronExpression] = useState('0 * * * *');
  const [isEnabled, setIsEnabled] = useState(true);

  useEffect(() => {
    try {
      const config = JSON.parse(value);
      setScheduleType(config.scheduleType || 'interval');
      setIntervalMinutes(config.intervalMinutes || 60);
      setCronExpression(config.cronExpression || '0 * * * *');
      setIsEnabled(config.isEnabled !== false);
    } catch {
      // Invalid JSON, use defaults
    }
  }, [value]);

  useEffect(() => {
    const config = {
      scheduleType,
      intervalMinutes: scheduleType === 'interval' ? intervalMinutes : undefined,
      cronExpression: scheduleType === 'cron' ? cronExpression : undefined,
      isEnabled,
      timezone: 'UTC',
    };
    onChange(JSON.stringify(config, null, 2));
  }, [scheduleType, intervalMinutes, cronExpression, isEnabled, onChange]);

  return (
    <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <TextField
            select
            value={scheduleType}
            onChange={(e) => setScheduleType(e.target.value)}
            label="Schedule Type"
            fullWidth
          >
            <MenuItem value="interval">Interval (Minutes)</MenuItem>
            <MenuItem value="cron">Cron Expression</MenuItem>
          </TextField>
        </Grid>
        <Grid item xs={12} md={6}>
          <FormControlLabel
            control={<Switch checked={isEnabled} onChange={(e) => setIsEnabled(e.target.checked)} />}
            label="Enabled"
          />
        </Grid>
        {scheduleType === 'interval' && (
          <Grid item xs={12} md={6}>
            <TextField
              value={intervalMinutes}
              onChange={(e) => setIntervalMinutes(Number(e.target.value))}
              label="Interval (Minutes)"
              type="number"
              helperText="How often to run the indicator"
              fullWidth
            />
          </Grid>
        )}
        {scheduleType === 'cron' && (
          <Grid item xs={12}>
            <TextField
              value={cronExpression}
              onChange={(e) => setCronExpression(e.target.value)}
              label="Cron Expression"
              helperText="Standard cron format (e.g., '0 * * * *' for every hour)"
              fullWidth
            />
          </Grid>
        )}
      </Grid>
    </Paper>
  );
};

const IndicatorCreate: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const indicatorId = id ? parseInt(id, 10) : undefined;
  const isEdit = !!indicatorId;

  const [selectedContacts, setSelectedContacts] = useState<ContactDto[]>([]);
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | null>(null);
  const [showScheduleHelper, setShowScheduleHelper] = useState(true);

  // Use our enhanced hooks
  const { data: indicator, isLoading: indicatorLoading } = useIndicator(indicatorId || 0);
  const { data: contacts = [], isLoading: contactsLoading } = useActiveContacts();

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
      thresholdType: '',
      thresholdField: '',
      thresholdComparison: '',
      thresholdValue: 0,
      priority: 'medium',
      ownerContactId: 0,
      averageOfCurrHour: false,
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
        thresholdValue: indicator.thresholdValue || 0,
        priority: indicator.priority || 'medium',
        ownerContactId: indicator.ownerContactId,
        averageLastDays: indicator.averageLastDays,
        averageOfCurrHour: indicator.averageOfCurrHour || false,
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

  const handleCollectorChange = (collectorId: number) => {
    setValue('collectorId', collectorId);
    setSelectedCollectorId(collectorId);
  };

  const handleItemNameChange = (itemName: string) => {
    setValue('collectorItemName', itemName);
  };

  if (indicatorLoading || contactsLoading) {
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

              <Grid item xs={12}>
                <CollectorSelector
                  selectedCollectorId={selectedCollectorId || undefined}
                  selectedItemName={watch('collectorItemName')}
                  onCollectorChange={handleCollectorChange}
                  onItemNameChange={handleItemNameChange}
                  disabled={isSubmitting}
                />
                {errors.collectorId && (
                  <Typography color="error" variant="caption" sx={{ mt: 1, display: 'block' }}>
                    {errors.collectorId.message}
                  </Typography>
                )}
                {errors.collectorItemName && (
                  <Typography color="error" variant="caption" sx={{ mt: 1, display: 'block' }}>
                    {errors.collectorItemName.message}
                  </Typography>
                )}
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
                        { value: 'high', label: 'High' },
                        { value: 'medium', label: 'Medium' },
                        { value: 'low', label: 'Low' },
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

              <Grid item xs={12} md={6}>
                <Controller
                  name="averageLastDays"
                  control={control}
                  render={({ field }) => (
                    <InputField
                      {...field}
                      label="Average Last Days"
                      type="number"
                      error={!!errors.averageLastDays}
                      helperText={errors.averageLastDays?.message || "Number of days to look back for average calculation"}
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="averageOfCurrHour"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Switch {...field} checked={field.value} />}
                      label="Average of Current Hour"
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Divider />
              </Grid>

              {/* Threshold Configuration */}
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom>
                  Threshold Configuration
                </Typography>
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="thresholdType"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Threshold Type"
                      options={[
                        { value: 'volume_average', label: 'Volume Average' },
                        { value: 'threshold_value', label: 'Threshold Value' },
                        { value: 'percentage_change', label: 'Percentage Change' },
                        { value: 'absolute_value', label: 'Absolute Value' },
                      ]}
                      error={!!errors.thresholdType}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="thresholdField"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Threshold Field"
                      options={[
                        { value: 'Total', label: 'Total' },
                        { value: 'Marked', label: 'Marked' },
                        { value: 'MarkedPercent', label: 'Marked Percent' },
                        { value: 'Average', label: 'Average' },
                      ]}
                      error={!!errors.thresholdField}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="thresholdComparison"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      label="Comparison Operator"
                      options={[
                        { value: 'gt', label: 'Greater Than (>)' },
                        { value: 'gte', label: 'Greater Than or Equal (>=)' },
                        { value: 'lt', label: 'Less Than (<)' },
                        { value: 'lte', label: 'Less Than or Equal (<=)' },
                        { value: 'eq', label: 'Equal To (=)' },
                      ]}
                      error={!!errors.thresholdComparison}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <Controller
                  name="thresholdValue"
                  control={control}
                  render={({ field }) => (
                    <InputField
                      {...field}
                      label="Threshold Value"
                      type="number"
                      error={!!errors.thresholdValue}
                      helperText={errors.thresholdValue?.message}
                      required
                    />
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Divider />
              </Grid>

              {/* Schedule Configuration */}
              <Grid item xs={12}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography variant="h6" gutterBottom>
                    Schedule Configuration
                  </Typography>
                  <Tooltip title="Toggle between visual editor and JSON editor">
                    <IconButton
                      size="small"
                      onClick={() => setShowScheduleHelper(!showScheduleHelper)}
                    >
                      <HelpIcon />
                    </IconButton>
                  </Tooltip>
                </Box>
              </Grid>

              <Grid item xs={12}>
                <Controller
                  name="scheduleConfiguration"
                  control={control}
                  render={({ field }) => (
                    showScheduleHelper ? (
                      <ScheduleConfigHelper
                        value={field.value}
                        onChange={field.onChange}
                      />
                    ) : (
                      <InputField
                        {...field}
                        label="Schedule Configuration (JSON)"
                        multiline
                        rows={6}
                        error={!!errors.scheduleConfiguration}
                        helperText={errors.scheduleConfiguration?.message || 'JSON configuration for scheduling (interval, cron, etc.)'}
                        required
                      />
                    )
                  )}
                />
              </Grid>

              <Grid item xs={12}>
                <Divider />
              </Grid>

              {/* Additional Contacts */}
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom>
                  Notification Contacts
                </Typography>
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

import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Switch,
  FormControlLabel,
  Button,
  Divider,
  Alert,
  Stepper,
  Step,
  StepLabel,
  Paper,
  Chip,
  useTheme
} from '@mui/material';
import {
  Save,
  Cancel,
  DataObject,
  Schedule,
  Notifications,
  Settings
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CollectorSelector } from '../../CollectorSelector';
import { SchedulerSelector } from './SchedulerSelector';

// Form validation schema
const indicatorSchema = yup.object({
  indicatorName: yup.string().required('Indicator name is required'),
  indicatorCode: yup.string().required('Indicator code is required'),
  indicatorDesc: yup.string(),
  collectorID: yup.number().nullable().optional(),
  collectorItemName: yup.string().required('Collector item name is required'),
  schedulerID: yup.number().nullable().optional(),
  lastMinutes: yup.number().min(1, 'Must be at least 1 minute').required(),
  thresholdType: yup.string().required('Threshold type is required'),
  thresholdField: yup.string().required('Threshold field is required'),
  thresholdComparison: yup.string().required('Threshold comparison is required'),
  thresholdValue: yup.number().required('Threshold value is required'),
  priority: yup.number().required('Priority is required'),
  ownerContactId: yup.number().required('Owner contact is required'),
  averageLastDays: yup.number().nullable().optional(),
  isActive: yup.boolean().required(),
});

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

interface EnhancedIndicatorFormProps {
  initialData?: Partial<IndicatorFormData>;
  onSubmit: (data: IndicatorFormData) => void;
  onCancel: () => void;
  isEdit?: boolean;
  loading?: boolean;
}

const steps = [
  'Data Source',
  'Scheduling',
  'Thresholds',
  'Notifications'
];

export const EnhancedIndicatorForm: React.FC<EnhancedIndicatorFormProps> = ({
  initialData,
  onSubmit,
  onCancel,
  isEdit = false,
  loading = false,
}) => {
  const theme = useTheme();
  const [activeStep, setActiveStep] = useState(0);

  const {
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<IndicatorFormData>({
    // resolver: yupResolver(indicatorSchema), // Temporarily disabled due to type conflicts
    defaultValues: {
      indicatorName: '',
      indicatorCode: '',
      indicatorDesc: '',
      collectorID: 1, // Default to first collector
      collectorItemName: '',
      schedulerID: undefined,
      lastMinutes: 60,
      thresholdType: 'volume_average',
      thresholdField: 'Total',
      thresholdComparison: 'lt',
      thresholdValue: 0,
      priority: 2, // medium priority
      ownerContactId: 1,
      averageLastDays: undefined,
      isActive: true,
      ...initialData,
    },
  });

  const watchedValues = watch();

  const handleNext = () => {
    setActiveStep((prevStep) => prevStep + 1);
  };

  const handleBack = () => {
    setActiveStep((prevStep) => prevStep - 1);
  };

  const handleFormSubmit = (data: IndicatorFormData) => {
    onSubmit(data);
  };

  const renderStepContent = (step: number) => {
    switch (step) {
      case 0:
        return (
          <Card variant="outlined">
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <DataObject color="primary" />
                Data Source Configuration
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Configure the data source and basic indicator information.
              </Typography>

              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Controller
                    name="indicatorName"
                    control={control}
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
                <Grid item xs={12}>
                  <Divider sx={{ my: 2 }} />
                  <CollectorSelector
                    selectedCollectorId={watchedValues.collectorID}
                    selectedItemName={watchedValues.collectorItemName}
                    onCollectorChange={(collectorId) => setValue('collectorID', collectorId || 1)}
                    onItemNameChange={(itemName) => setValue('collectorItemName', itemName)}
                    required
                    variant="detailed"
                    showRefreshButton
                    title="Data Collector"
                    subtitle="Select the data collector and specific item to monitor"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        );

      case 1:
        return (
          <Card variant="outlined">
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Schedule color="primary" />
                Scheduling Configuration
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Configure when and how often this indicator should be executed.
              </Typography>

              <Grid container spacing={3}>
                <Grid item xs={12}>
                  <SchedulerSelector
                    selectedSchedulerId={watchedValues.schedulerID ?? undefined}
                    onSchedulerChange={(schedulerId) => setValue('schedulerID', schedulerId)}
                    variant="detailed"
                    showCreateButton
                    showScheduleInfo
                  />
                </Grid>
                <Grid item xs={12} md={6}>
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
                      <TextField
                        {...field}
                        label="Average Last Days"
                        type="number"
                        fullWidth
                        error={!!errors.averageLastDays}
                        helperText={errors.averageLastDays?.message || 'Number of days to average (optional)'}
                        inputProps={{ min: 1 }}
                      />
                    )}
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        );

      case 2:
        return (
          <Card variant="outlined">
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Settings color="primary" />
                Threshold Configuration
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Define the conditions that will trigger alerts.
              </Typography>

              <Grid container spacing={3}>
                <Grid item xs={12} md={4}>
                  <Controller
                    name="thresholdType"
                    control={control}
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
                    render={({ field }) => (
                      <FormControl fullWidth>
                        <InputLabel>Threshold Field</InputLabel>
                        <Select {...field} label="Threshold Field" required>
                          <MenuItem value="Total">Total</MenuItem>
                          <MenuItem value="Marked">Marked</MenuItem>
                          <MenuItem value="MarkedPercent">Marked Percent</MenuItem>
                        </Select>
                      </FormControl>
                    )}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <Controller
                    name="thresholdComparison"
                    control={control}
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
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Threshold Value"
                        type="number"
                        fullWidth
                        error={!!errors.thresholdValue}
                        helperText={errors.thresholdValue?.message}
                        required
                      />
                    )}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Controller
                    name="priority"
                    control={control}
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
              </Grid>
            </CardContent>
          </Card>
        );

      case 3:
        return (
          <Card variant="outlined">
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Notifications color="primary" />
                Notification Settings
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Configure notification settings and finalize the indicator.
              </Typography>

              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Controller
                    name="ownerContactId"
                    control={control}
                    render={({ field }) => (
                      <FormControl fullWidth>
                        <InputLabel>Owner Contact</InputLabel>
                        <Select {...field} label="Owner Contact" required>
                          <MenuItem value={1}>Default Contact</MenuItem>
                          {/* TODO: Load actual contacts */}
                        </Select>
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
                        sx={{ mt: 2 }}
                      />
                    )}
                  />
                </Grid>
                <Grid item xs={12}>
                  <Alert severity="info">
                    <Typography variant="body2">
                      Review all settings above and click "Create Indicator" to save your configuration.
                    </Typography>
                  </Alert>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        );

      default:
        return null;
    }
  };

  return (
    <Box sx={{ maxWidth: 1000, mx: 'auto', p: 3 }}>
      <Paper elevation={1} sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {isEdit ? 'Edit Indicator' : 'Create New Indicator'}
        </Typography>
        
        <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        <form onSubmit={handleSubmit(handleFormSubmit)}>
          {renderStepContent(activeStep)}

          <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 3 }}>
            <Button
              onClick={onCancel}
              startIcon={<Cancel />}
              disabled={isSubmitting || loading}
            >
              Cancel
            </Button>

            <Box sx={{ display: 'flex', gap: 1 }}>
              {activeStep > 0 && (
                <Button onClick={handleBack} disabled={isSubmitting || loading}>
                  Back
                </Button>
              )}
              
              {activeStep < steps.length - 1 ? (
                <Button variant="contained" onClick={handleNext}>
                  Next
                </Button>
              ) : (
                <Button
                  type="submit"
                  variant="contained"
                  startIcon={<Save />}
                  disabled={isSubmitting || loading}
                >
                  {isSubmitting || loading ? 'Saving...' : isEdit ? 'Update Indicator' : 'Create Indicator'}
                </Button>
              )}
            </Box>
          </Box>
        </form>
      </Paper>
    </Box>
  );
};

export default EnhancedIndicatorForm;

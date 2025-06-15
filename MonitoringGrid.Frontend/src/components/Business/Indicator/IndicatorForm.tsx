import React from 'react';
import {
  Grid,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Typography,
  Box,
  Chip,
} from '@mui/material';
import { useFormContext, Controller } from 'react-hook-form';
import { Assessment as IndicatorIcon } from '@mui/icons-material';
import * as yup from 'yup';

import { CreateFormDialog, EditFormDialog } from '../../UI/GenericFormDialog';
import { CollectorSelector } from '../../CollectorSelector';
import { SchedulerSelector } from './SchedulerSelector';
import { DomainValidators, BaseValidators } from '../../../utils/validationSchemas';
import { useContacts } from '../../../hooks/useContacts';
import { CreateIndicatorRequest, UpdateIndicatorRequest } from '../../../services/indicatorService';

// Form data interface
interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorItemName: string;
  schedulerID?: number;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
}

// Validation schema using the new validation library
const indicatorValidationSchema = yup.object({
  indicatorName: DomainValidators.indicatorName(),
  indicatorCode: DomainValidators.indicatorCode(),
  indicatorDesc: DomainValidators.description(false),
  collectorID: BaseValidators.optionalNumber(),
  collectorItemName: BaseValidators.requiredString('Collector Item Name'),
  schedulerID: BaseValidators.optionalNumber(),
  lastMinutes: DomainValidators.lastMinutes(),
  thresholdType: BaseValidators.requiredString('Threshold Type'),
  thresholdField: BaseValidators.requiredString('Threshold Field'),
  thresholdComparison: BaseValidators.requiredString('Threshold Comparison'),
  thresholdValue: DomainValidators.thresholdValue(),
  priority: DomainValidators.priority(),
  ownerContactId: BaseValidators.requiredNumber('Owner Contact', 1),
  averageLastDays: BaseValidators.optionalNumber(1, 365),
  isActive: BaseValidators.requiredBoolean('Active Status'),
});

// Threshold types and comparisons
const thresholdTypes = [
  { value: 'count', label: 'Count' },
  { value: 'average', label: 'Average' },
  { value: 'sum', label: 'Sum' },
  { value: 'min', label: 'Minimum' },
  { value: 'max', label: 'Maximum' },
];

const thresholdComparisons = [
  { value: 'gt', label: 'Greater Than (>)' },
  { value: 'gte', label: 'Greater Than or Equal (>=)' },
  { value: 'lt', label: 'Less Than (<)' },
  { value: 'lte', label: 'Less Than or Equal (<=)' },
  { value: 'eq', label: 'Equal To (=)' },
  { value: 'ne', label: 'Not Equal To (!=)' },
];

const priorities = [
  { value: 1, label: '1 - Critical', color: 'error' as const },
  { value: 2, label: '2 - High', color: 'warning' as const },
  { value: 3, label: '3 - Medium', color: 'info' as const },
  { value: 4, label: '4 - Low', color: 'success' as const },
];

// Form fields component
const IndicatorFormFields: React.FC = () => {
  const { control, watch, setValue } = useFormContext<IndicatorFormData>();
  const { data: contacts = [] } = useContacts();

  const watchedCollectorId = watch('collectorID');
  const watchedSchedulerId = watch('schedulerID');
  // const watchedPriority = watch('priority'); // Reserved for future priority-based logic

  return (
    <>
      {/* Basic Information */}
      <Grid item xs={12}>
        <Typography
          variant="h6"
          gutterBottom
          sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
        >
          <IndicatorIcon color="primary" />
          Basic Information
        </Typography>
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="indicatorName"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Indicator Name"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="indicatorCode"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Indicator Code"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>

      <Grid item xs={12}>
        <Controller
          name="indicatorDesc"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Description"
              multiline
              rows={3}
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>

      {/* Data Source Configuration */}
      <Grid item xs={12}>
        <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
          Data Source Configuration
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <CollectorSelector
          selectedCollectorId={watchedCollectorId}
          selectedItemName={watch('collectorItemName')}
          onCollectorChange={collectorId => setValue('collectorID', collectorId)}
          onItemNameChange={itemName => setValue('collectorItemName', itemName)}
          required
          variant="detailed"
          showRefreshButton
          showCollectorInfo
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="lastMinutes"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Data Window (Minutes)"
              type="number"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'How far back to look for data'}
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="averageLastDays"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Average Last Days"
              type="number"
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'Days to calculate average (optional)'}
            />
          )}
        />
      </Grid>

      {/* Scheduling */}
      <Grid item xs={12}>
        <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
          Scheduling
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <SchedulerSelector
          selectedSchedulerId={watchedSchedulerId}
          onSchedulerChange={schedulerId => setValue('schedulerID', schedulerId)}
          variant="detailed"
          showCreateButton
          showRefreshButton
          showSchedulerInfo
        />
      </Grid>

      {/* Threshold Configuration */}
      <Grid item xs={12}>
        <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
          Threshold Configuration
        </Typography>
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdType"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Threshold Type *</InputLabel>
              <Select {...field} label="Threshold Type *">
                {thresholdTypes.map(type => (
                  <MenuItem key={type.value} value={type.value}>
                    {type.label}
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdField"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Threshold Field"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message || 'Field name to evaluate'}
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdComparison"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Comparison *</InputLabel>
              <Select {...field} label="Comparison *">
                {thresholdComparisons.map(comp => (
                  <MenuItem key={comp.value} value={comp.value}>
                    {comp.label}
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="thresholdValue"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              fullWidth
              label="Threshold Value"
              type="number"
              required
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>

      {/* Priority and Owner */}
      <Grid item xs={12}>
        <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
          Priority and Ownership
        </Typography>
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="priority"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Priority *</InputLabel>
              <Select {...field} label="Priority *">
                {priorities.map(priority => (
                  <MenuItem key={priority.value} value={priority.value}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Chip
                        label={priority.value}
                        color={priority.color}
                        size="small"
                        variant="outlined"
                      />
                      {priority.label}
                    </Box>
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="ownerContactId"
          control={control}
          render={({ field, fieldState }) => (
            <FormControl fullWidth error={!!fieldState.error}>
              <InputLabel>Owner Contact *</InputLabel>
              <Select {...field} label="Owner Contact *">
                {contacts.map(contact => (
                  <MenuItem key={contact.contactID} value={contact.contactID}>
                    {contact.name}
                  </MenuItem>
                ))}
              </Select>
              {fieldState.error && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1 }}>
                  {fieldState.error.message}
                </Typography>
              )}
            </FormControl>
          )}
        />
      </Grid>

      {/* Status */}
      <Grid item xs={12}>
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
    </>
  );
};

// Create Indicator Dialog
interface CreateIndicatorDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateIndicatorRequest) => void | Promise<void>;
  loading?: boolean;
}

export const CreateIndicatorDialog: React.FC<CreateIndicatorDialogProps> = ({
  open,
  onClose,
  onSubmit,
  loading = false,
}) => {
  const defaultValues: IndicatorFormData = {
    indicatorName: '',
    indicatorCode: '',
    indicatorDesc: '',
    collectorID: undefined,
    collectorItemName: '',
    schedulerID: undefined,
    lastMinutes: 60,
    thresholdType: 'count',
    thresholdField: '',
    thresholdComparison: 'gt',
    thresholdValue: 0,
    priority: 3,
    ownerContactId: 0,
    averageLastDays: undefined,
    isActive: true,
  };

  return (
    <CreateFormDialog
      entityName="Indicator"
      open={open}
      onClose={onClose}
      onSubmit={onSubmit}
      loading={loading}
      validationSchema={indicatorValidationSchema}
      defaultValues={defaultValues}
      maxWidth="lg"
      icon={<IndicatorIcon />}
      subtitle="Configure a new monitoring indicator"
    >
      <IndicatorFormFields />
    </CreateFormDialog>
  );
};

// Edit Indicator Dialog
interface EditIndicatorDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: UpdateIndicatorRequest) => void | Promise<void>;
  loading?: boolean;
  initialData: IndicatorFormData;
}

export const EditIndicatorDialog: React.FC<EditIndicatorDialogProps> = ({
  open,
  onClose,
  onSubmit,
  loading = false,
  initialData,
}) => {
  return (
    <EditFormDialog
      entityName="Indicator"
      open={open}
      onClose={onClose}
      onSubmit={onSubmit}
      loading={loading}
      validationSchema={indicatorValidationSchema}
      defaultValues={initialData}
      maxWidth="lg"
      icon={<IndicatorIcon />}
      subtitle="Modify indicator configuration"
    >
      <IndicatorFormFields />
    </EditFormDialog>
  );
};

// Export IndicatorForm as an alias for IndicatorFormFields for compatibility
export const IndicatorForm = IndicatorFormFields;

export default {
  CreateIndicatorDialog,
  EditIndicatorDialog,
  IndicatorFormFields,
  IndicatorForm,
};

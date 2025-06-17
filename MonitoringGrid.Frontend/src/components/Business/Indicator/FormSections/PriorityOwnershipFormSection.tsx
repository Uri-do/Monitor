import React from 'react';
import {
  Grid,
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
import { Person as PersonIcon } from '@mui/icons-material';
import { useContacts } from '../../../../hooks/useContacts';

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

const priorities = [
  { value: 1, label: '1 - Critical', color: 'error' as const },
  { value: 2, label: '2 - High', color: 'warning' as const },
  { value: 3, label: '3 - Medium', color: 'info' as const },
  { value: 4, label: '4 - Low', color: 'success' as const },
];

export const PriorityOwnershipFormSection: React.FC = () => {
  const { control } = useFormContext<IndicatorFormData>();
  const { data: contacts = [] } = useContacts();

  return (
    <>
      {/* Priority and Owner */}
      <Grid item xs={12}>
        <Typography 
          variant="h6" 
          gutterBottom 
          sx={{ 
            mt: 2,
            display: 'flex', 
            alignItems: 'center', 
            gap: 1 
          }}
        >
          <PersonIcon color="primary" />
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

export default PriorityOwnershipFormSection;

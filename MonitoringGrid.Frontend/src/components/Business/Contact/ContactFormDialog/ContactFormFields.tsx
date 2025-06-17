import React from 'react';
import {
  Grid,
  Typography,
  InputAdornment,
  FormControlLabel,
  Switch,
  Alert,
} from '@mui/material';
import {
  Email as EmailIcon,
  Phone as PhoneIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { Controller, Control, FieldErrors } from 'react-hook-form';
import { InputField } from '@/components';
import { ContactFormData } from './types';

interface ContactFormFieldsProps {
  control: Control<ContactFormData>;
  errors: FieldErrors<ContactFormData>;
}

export const ContactFormFields: React.FC<ContactFormFieldsProps> = ({
  control,
  errors,
}) => {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Typography variant="h6" gutterBottom>
          Contact Information
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <Controller
          name="name"
          control={control}
          render={({ field }) => (
            <InputField
              {...field}
              label="Full Name"
              fullWidth
              error={!!errors.name}
              helperText={errors.name?.message}
              placeholder="e.g., John Smith"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <PersonIcon color="action" />
                  </InputAdornment>
                ),
              }}
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="email"
          control={control}
          render={({ field }) => (
            <InputField
              {...field}
              label="Email Address"
              type="email"
              fullWidth
              error={!!errors.email}
              helperText={errors.email?.message || 'Primary notification method'}
              placeholder="e.g., john.smith@company.com"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <EmailIcon color="action" />
                  </InputAdornment>
                ),
              }}
            />
          )}
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Controller
          name="phone"
          control={control}
          render={({ field }) => (
            <InputField
              {...field}
              label="Phone Number"
              fullWidth
              error={!!errors.phone}
              helperText={errors.phone?.message || 'Secondary notification method'}
              placeholder="e.g., +1 (555) 123-4567"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <PhoneIcon color="action" />
                  </InputAdornment>
                ),
              }}
            />
          )}
        />
      </Grid>

      <Grid item xs={12}>
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
        <Typography variant="caption" color="textSecondary" display="block">
          Inactive contacts will not receive notifications
        </Typography>
      </Grid>

      <Grid item xs={12}>
        <Alert severity="info">
          <Typography variant="body2">
            <strong>Note:</strong> At least one contact method (email or phone) is required. Email
            is recommended as the primary notification method.
          </Typography>
        </Alert>
      </Grid>
    </Grid>
  );
};

export default ContactFormFields;

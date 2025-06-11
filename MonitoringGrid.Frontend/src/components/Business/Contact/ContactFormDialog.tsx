import React from 'react';
import { FormControlLabel, Switch, Grid, Typography, Alert, InputAdornment } from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Person as PersonIcon,
  ContactMail as ContactIcon,
} from '@mui/icons-material';
import { Dialog, InputField, Button } from '@/components';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CreateContactRequest, UpdateContactRequest } from '@/types/api';

// Validation schema
const contactSchema = yup
  .object({
    name: yup.string().required('Contact name is required'),
    email: yup.string().email('Invalid email format').nullable(),
    phone: yup.string().nullable(),
    isActive: yup.boolean().required(),
  })
  .test(
    'contact-method',
    'At least one contact method (email or phone) is required',
    function (value) {
      return !!(value.email || value.phone);
    }
  );

export type ContactFormData = yup.InferType<typeof contactSchema>;

interface ContactFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateContactRequest | UpdateContactRequest) => void;
  initialData?: Partial<ContactFormData>;
  isEdit?: boolean;
  loading?: boolean;
}

export const ContactFormDialog: React.FC<ContactFormDialogProps> = ({
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
  } = useForm<ContactFormData>({
    resolver: yupResolver(contactSchema),
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      isActive: true,
      ...initialData,
    },
  });

  React.useEffect(() => {
    if (open && initialData) {
      reset(initialData);
    }
  }, [open, initialData, reset]);

  const handleFormSubmit = (data: ContactFormData) => {
    onSubmit(data as CreateContactRequest | UpdateContactRequest);
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
      title={isEdit ? 'Edit Contact' : 'Create New Contact'}
      subtitle={
        isEdit ? 'Update contact information and settings' : 'Add a new contact for notifications'
      }
      icon={<ContactIcon />}
      gradient="secondary"
      maxWidth="sm"
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
            gradient="secondary"
            startIcon={<SaveIcon />}
            disabled={isSubmitting}
            onClick={handleSubmit(handleFormSubmit)}
          >
            {isSubmitting ? 'Saving...' : isEdit ? 'Update Contact' : 'Create Contact'}
          </Button>
        </>
      }
    >
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
    </Dialog>
  );
};

export default ContactFormDialog;

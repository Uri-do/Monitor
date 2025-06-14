import React, { useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  TextField,
  FormControlLabel,
  Switch,
  Button,
  Typography,
  Alert,
  Stack,
  InputAdornment,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Person as PersonIcon,
  People as ContactIcon,
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { CreateContactRequest, UpdateContactRequest } from '@/types/api';
import { useContact } from '@/hooks/useContacts';
import { useCreateContact, useUpdateContact } from '@/hooks/mutations';
import toast from 'react-hot-toast';
import { PageHeader, LoadingSpinner, FormLayout, FormSection, FormActions } from '@/components';

// Validation schema
const contactSchema = yup
  .object({
    name: yup
      .string()
      .required('Name is required')
      .max(100, 'Name must be less than 100 characters'),
    email: yup
      .string()
      .email('Invalid email format')
      .max(255, 'Email must be less than 255 characters')
      .nullable(),
    phone: yup.string().max(20, 'Phone must be less than 20 characters').nullable(),
    isActive: yup.boolean(),
  })
  .test(
    'contact-method',
    'At least one contact method (email or phone) is required',
    function (value) {
      return !!(value.email || value.phone);
    }
  );

type ContactFormData = yup.InferType<typeof contactSchema>;

const ContactCreate: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(id);
  const contactId = parseInt(id || '0');

  // Use our enhanced hooks
  const { data: contact, isLoading: contactLoading } = useContact(contactId);
  const createContactMutation = useCreateContact();
  const updateContactMutation = useUpdateContact();

  // Form setup
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
    },
  });

  // Update form when contact data is loaded
  useEffect(() => {
    if (contact && isEdit) {
      reset({
        name: contact.name,
        email: contact.email || '',
        phone: contact.phone || '',
        isActive: contact.isActive,
      });
    }
  }, [contact, isEdit, reset]);

  // Create wrapper mutations with navigation
  const createMutation = {
    ...createContactMutation,
    mutate: (data: CreateContactRequest) => {
      createContactMutation.mutate(data, {
        onSuccess: () => navigate('/contacts'),
      });
    },
  };

  const updateMutation = {
    ...updateContactMutation,
    mutate: (data: UpdateContactRequest) => {
      updateContactMutation.mutate(data, {
        onSuccess: () => navigate(`/contacts/${contactId}`),
      });
    },
  };

  const onSubmit = (data: ContactFormData) => {
    const formData = {
      name: data.name,
      email: data.email || undefined,
      phone: data.phone || undefined,
      isActive: data.isActive,
    };

    if (isEdit) {
      updateMutation.mutate({
        ...formData,
        contactID: contactId,
        isActive: formData.isActive ?? true,
      });
    } else {
      createMutation.mutate({
        ...formData,
        isActive: formData.isActive ?? true,
      });
    }
  };

  if (contactLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title={isEdit ? 'Edit Contact' : 'Create Contact'}
        subtitle={isEdit ? `Editing: ${contact?.name}` : 'Create a new notification contact'}
        icon={<ContactIcon />}
        backAction={{
          label: isEdit ? 'Back to Contact' : 'Back to Contacts',
          icon: <BackIcon />,
          onClick: () => navigate(isEdit ? `/contacts/${contactId}` : '/contacts'),
        }}
      />

      <form onSubmit={handleSubmit(onSubmit)}>
        <FormLayout fullWidth spacing={3}>
          {/* Contact Information */}
          <Grid item xs={12}>
            <FormSection
              title="Contact Information"
              subtitle="Enter contact details for notifications"
              icon={<PersonIcon />}
            >
              <Grid item xs={12}>
                <Controller
                  name="name"
                  control={control}
                  render={({ field }) => (
                    <TextField
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
                    <TextField
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
                    <TextField
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
                    <strong>Note:</strong> At least one contact method (email or phone) is required.
                    Email is recommended as the primary notification method.
                  </Typography>
                </Alert>
              </Grid>
            </FormSection>
          </Grid>

          {/* Form Actions */}
          <Grid item xs={12}>
            <FormActions
              secondaryActions={[
                {
                  label: 'Cancel',
                  variant: 'outlined',
                  startIcon: <CancelIcon />,
                  onClick: () => navigate('/contacts'),
                  disabled: isSubmitting,
                },
              ]}
              primaryAction={{
                label: isSubmitting ? 'Saving...' : isEdit ? 'Update Contact' : 'Create Contact',
                type: 'submit',
                startIcon: <SaveIcon />,
                disabled: isSubmitting,
                loading: isSubmitting,
              }}
            />
          </Grid>
        </FormLayout>
      </form>
    </Box>
  );
};

export default ContactCreate;

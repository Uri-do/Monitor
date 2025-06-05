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
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { contactApi } from '@/services/api';
import { CreateContactRequest, UpdateContactRequest } from '@/types/api';
import toast from 'react-hot-toast';
import {
  PageHeader,
  LoadingSpinner,
} from '@/components/Common';

// Validation schema
const contactSchema = yup.object({
  name: yup.string().required('Name is required').max(100, 'Name must be less than 100 characters'),
  email: yup.string().email('Invalid email format').max(255, 'Email must be less than 255 characters').nullable(),
  phone: yup.string().max(20, 'Phone must be less than 20 characters').nullable(),
  isActive: yup.boolean(),
}).test('contact-method', 'At least one contact method (email or phone) is required', function(value) {
  return !!(value.email || value.phone);
});

type ContactFormData = yup.InferType<typeof contactSchema>;

const ContactCreate: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = Boolean(id);
  const contactId = parseInt(id || '0');

  // Fetch contact for editing
  const { data: contact, isLoading: contactLoading } = useQuery({
    queryKey: ['contact', contactId],
    queryFn: () => contactApi.getContact(contactId),
    enabled: isEdit && !!contactId,
  });

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

  // Create/Update mutations
  const createMutation = useMutation({
    mutationFn: (data: CreateContactRequest) => contactApi.createContact(data),
    onSuccess: () => {
      toast.success('Contact created successfully');
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      navigate('/contacts');
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create contact');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateContactRequest) => contactApi.updateContact(data),
    onSuccess: () => {
      toast.success('Contact updated successfully');
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      queryClient.invalidateQueries({ queryKey: ['contact', contactId] });
      navigate(`/contacts/${contactId}`);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update contact');
    },
  });

  const onSubmit = (data: ContactFormData) => {
    const formData = {
      name: data.name,
      email: data.email || undefined,
      phone: data.phone || undefined,
      isActive: data.isActive,
    };

    if (isEdit) {
      updateMutation.mutate({ ...formData, contactId });
    } else {
      createMutation.mutate(formData);
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
        breadcrumbs={[
          { label: 'Contacts', href: '/contacts' },
          { label: isEdit ? 'Edit' : 'Create' },
        ]}
      />

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3} maxWidth="md">
          {/* Basic Information */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Contact Information
                </Typography>
                <Grid container spacing={2}>
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
                </Grid>

                {/* Validation Info */}
                <Alert severity="info" sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>Note:</strong> At least one contact method (email or phone) is required.
                    Email is recommended as the primary notification method.
                  </Typography>
                </Alert>
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
                    onClick={() => navigate('/contacts')}
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
                    {isSubmitting ? 'Saving...' : (isEdit ? 'Update Contact' : 'Create Contact')}
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

export default ContactCreate;

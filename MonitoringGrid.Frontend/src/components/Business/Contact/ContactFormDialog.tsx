import React from 'react';
import {
  ContactMail as ContactIcon,
} from '@mui/icons-material';
import { Dialog } from '@/components';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import { CreateContactRequest, UpdateContactRequest } from '@/types/api';
import { ContactFormFields } from './ContactFormDialog/ContactFormFields';
import { ContactFormActions } from './ContactFormDialog/ContactFormActions';
import { contactSchema, type ContactFormData } from './ContactFormDialog/types';

// Re-export types for backward compatibility
export type { ContactFormData };

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
        <ContactFormActions
          isEdit={isEdit}
          isSubmitting={isSubmitting}
          onCancel={handleClose}
          onSubmit={handleSubmit(handleFormSubmit)}
        />
      }
    >
      <ContactFormFields
        control={control}
        errors={errors}
      />
    </Dialog>
  );
};

export default ContactFormDialog;

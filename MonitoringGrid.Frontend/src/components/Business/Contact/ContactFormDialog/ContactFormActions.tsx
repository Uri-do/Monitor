import React from 'react';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
} from '@mui/icons-material';
import { Button } from '@/components';

interface ContactFormActionsProps {
  isEdit: boolean;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: () => void;
}

export const ContactFormActions: React.FC<ContactFormActionsProps> = ({
  isEdit,
  isSubmitting,
  onCancel,
  onSubmit,
}) => {
  return (
    <>
      <Button
        variant="outlined"
        gradient="secondary"
        startIcon={<CancelIcon />}
        onClick={onCancel}
        disabled={isSubmitting}
      >
        Cancel
      </Button>
      <Button
        type="submit"
        gradient="secondary"
        startIcon={<SaveIcon />}
        disabled={isSubmitting}
        onClick={onSubmit}
      >
        {isSubmitting ? 'Saving...' : isEdit ? 'Update Contact' : 'Create Contact'}
      </Button>
    </>
  );
};

export default ContactFormActions;

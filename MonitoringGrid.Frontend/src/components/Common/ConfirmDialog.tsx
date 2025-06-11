import React from 'react';
import { Typography, Alert } from '@mui/material';
import {
  Warning as WarningIcon,
  Delete as DeleteIcon,
  Check as CheckIcon,
  Cancel as CancelIcon,
} from '@mui/icons-material';
import { Dialog, Button } from '@/components/UI';

interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  severity?: 'warning' | 'error' | 'info';
  loading?: boolean;
  details?: string;
}

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  open,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  severity = 'warning',
  loading = false,
  details,
}) => {
  const getIcon = () => {
    switch (severity) {
      case 'error':
        return <DeleteIcon color="error" sx={{ fontSize: 48 }} />;
      case 'warning':
        return <WarningIcon color="warning" sx={{ fontSize: 48 }} />;
      case 'info':
        return <CheckIcon color="info" sx={{ fontSize: 48 }} />;
      default:
        return <WarningIcon color="warning" sx={{ fontSize: 48 }} />;
    }
  };

  const getGradient = () => {
    switch (severity) {
      case 'error':
        return 'error';
      case 'warning':
        return 'warning';
      case 'info':
        return 'info';
      default:
        return 'warning';
    }
  };

  return (
    <Dialog
      open={open}
      onClose={loading ? undefined : onClose}
      title={title}
      icon={getIcon()}
      gradient={getGradient()}
      maxWidth="sm"
      actions={
        <>
          <Button
            variant="outlined"
            gradient="secondary"
            startIcon={<CancelIcon />}
            onClick={onClose}
            disabled={loading}
          >
            {cancelText}
          </Button>
          <Button
            gradient={getGradient()}
            startIcon={severity === 'error' ? <DeleteIcon /> : <CheckIcon />}
            onClick={onConfirm}
            disabled={loading}
          >
            {loading ? 'Processing...' : confirmText}
          </Button>
        </>
      }
    >
      <Typography variant="body1" gutterBottom>
        {message}
      </Typography>

      {details && (
        <Alert severity={severity} sx={{ mt: 2 }}>
          <Typography variant="body2">{details}</Typography>
        </Alert>
      )}
    </Dialog>
  );
};

export default ConfirmDialog;

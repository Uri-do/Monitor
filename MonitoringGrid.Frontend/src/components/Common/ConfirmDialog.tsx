import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Alert,
} from '@mui/material';
import {
  Warning as WarningIcon,
  Delete as DeleteIcon,
  Check as CheckIcon,
  Cancel as CancelIcon,
} from '@mui/icons-material';

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

  const getColor = () => {
    switch (severity) {
      case 'error':
        return 'error';
      case 'warning':
        return 'warning';
      case 'info':
        return 'primary';
      default:
        return 'warning';
    }
  };

  return (
    <Dialog
      open={open}
      onClose={loading ? undefined : onClose}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={2}>
          {getIcon()}
          <Typography variant="h6">{title}</Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Typography variant="body1" gutterBottom>
          {message}
        </Typography>
        
        {details && (
          <Alert severity={severity} sx={{ mt: 2 }}>
            <Typography variant="body2">{details}</Typography>
          </Alert>
        )}
      </DialogContent>

      <DialogActions>
        <Button
          onClick={onClose}
          disabled={loading}
          startIcon={<CancelIcon />}
        >
          {cancelText}
        </Button>
        <Button
          onClick={onConfirm}
          variant="contained"
          color={getColor()}
          disabled={loading}
          startIcon={severity === 'error' ? <DeleteIcon /> : <CheckIcon />}
        >
          {loading ? 'Processing...' : confirmText}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ConfirmDialog;

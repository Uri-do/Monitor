import React from 'react';
import { Snackbar as MuiSnackbar, Alert } from '@mui/material';

interface SnackbarProps {
  open: boolean;
  message: string;
  severity?: 'success' | 'error' | 'warning' | 'info';
  autoHideDuration?: number;
  onClose: () => void;
  position?: {
    vertical: 'top' | 'bottom';
    horizontal: 'left' | 'center' | 'right';
  };
}

const gradientMap = {
  success: 'linear-gradient(135deg, #4caf50 0%, #8bc34a 100%)',
  error: 'linear-gradient(135deg, #f44336 0%, #ff5722 100%)',
  warning: 'linear-gradient(135deg, #ff9800 0%, #ffc107 100%)',
  info: 'linear-gradient(135deg, #2196f3 0%, #03a9f4 100%)',
};

const shadowColorMap = {
  success: 'rgba(76, 175, 80, 0.3)',
  error: 'rgba(244, 67, 54, 0.3)',
  warning: 'rgba(255, 152, 0, 0.3)',
  info: 'rgba(33, 150, 243, 0.3)',
};

export const Snackbar: React.FC<SnackbarProps> = ({
  open,
  message,
  severity = 'success',
  autoHideDuration = 4000,
  onClose,
  position = { vertical: 'top', horizontal: 'right' },
}) => {
  return (
    <MuiSnackbar
      open={open}
      autoHideDuration={autoHideDuration}
      onClose={onClose}
      anchorOrigin={position}
    >
      <Alert
        onClose={onClose}
        severity={severity}
        variant="filled"
        sx={{
          background: gradientMap[severity],
          boxShadow: `0 8px 25px ${shadowColorMap[severity]}`,
          borderRadius: 1,
          '& .MuiAlert-icon': {
            color: 'white',
          },
          '& .MuiAlert-message': {
            color: 'white',
            fontWeight: 500,
          },
          '& .MuiAlert-action': {
            '& .MuiIconButton-root': {
              color: 'white',
            },
          },
        }}
      >
        {message}
      </Alert>
    </MuiSnackbar>
  );
};

export default Snackbar;

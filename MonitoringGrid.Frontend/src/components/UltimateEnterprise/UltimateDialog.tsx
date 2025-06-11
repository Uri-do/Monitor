import React from 'react';
import {
  Dialog,
  DialogProps,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  IconButton,
} from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';

interface UltimateDialogProps extends Omit<DialogProps, 'title'> {
  children: React.ReactNode;
  title: string;
  subtitle?: string;
  icon?: React.ReactElement;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  actions?: React.ReactNode;
  onClose?: () => void;
  showCloseButton?: boolean;
}

const gradientMap = {
  primary: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
  secondary: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
  success: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
  warning: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
  error: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
  info: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
};

const borderColorMap = {
  primary: 'rgba(102, 126, 234, 0.2)',
  secondary: 'rgba(240, 147, 251, 0.2)',
  success: 'rgba(67, 233, 123, 0.2)',
  warning: 'rgba(250, 112, 154, 0.2)',
  error: 'rgba(255, 107, 107, 0.2)',
  info: 'rgba(79, 172, 254, 0.2)',
};

const shadowColorMap = {
  primary: 'rgba(102, 126, 234, 0.15)',
  secondary: 'rgba(240, 147, 251, 0.15)',
  success: 'rgba(67, 233, 123, 0.15)',
  warning: 'rgba(250, 112, 154, 0.15)',
  error: 'rgba(255, 107, 107, 0.15)',
  info: 'rgba(79, 172, 254, 0.15)',
};

const backgroundGradientMap = {
  primary: 'linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)',
  secondary: 'linear-gradient(135deg, rgba(240, 147, 251, 0.05) 0%, rgba(245, 87, 108, 0.05) 100%)',
  success: 'linear-gradient(135deg, rgba(67, 233, 123, 0.05) 0%, rgba(56, 249, 215, 0.05) 100%)',
  warning: 'linear-gradient(135deg, rgba(250, 112, 154, 0.05) 0%, rgba(254, 225, 64, 0.05) 100%)',
  error: 'linear-gradient(135deg, rgba(255, 107, 107, 0.05) 0%, rgba(238, 90, 36, 0.05) 100%)',
  info: 'linear-gradient(135deg, rgba(79, 172, 254, 0.05) 0%, rgba(0, 242, 254, 0.05) 100%)',
};

export const UltimateDialog: React.FC<UltimateDialogProps> = ({
  children,
  title,
  subtitle,
  icon,
  gradient = 'primary',
  actions,
  onClose,
  showCloseButton = true,
  maxWidth = 'md',
  fullWidth = true,
  ...props
}) => {
  return (
    <Dialog
      maxWidth={maxWidth}
      fullWidth={fullWidth}
      onClose={onClose}
      PaperProps={{
        sx: {
          background: 'linear-gradient(145deg, #ffffff 0%, #f8f9fa 100%)',
          border: `1px solid ${borderColorMap[gradient]}`,
          borderRadius: 3,
          boxShadow: `0 20px 40px ${shadowColorMap[gradient]}`,
        }
      }}
      {...props}
    >
      <DialogTitle sx={{ 
        background: gradientMap[gradient],
        color: 'white',
        textAlign: 'center',
        py: 3,
        position: 'relative',
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          right: 0,
          width: '100px',
          height: '100px',
          background: 'rgba(255, 255, 255, 0.1)',
          borderRadius: '50%',
          transform: 'translate(30px, -30px)',
        }
      }}>
        {showCloseButton && onClose && (
          <IconButton
            onClick={onClose}
            sx={{
              position: 'absolute',
              right: 8,
              top: 8,
              color: 'white',
              backgroundColor: 'rgba(255, 255, 255, 0.1)',
              '&:hover': {
                backgroundColor: 'rgba(255, 255, 255, 0.2)',
              }
            }}
          >
            <CloseIcon />
          </IconButton>
        )}
        
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2, position: 'relative', zIndex: 1 }}>
          {icon && <Box sx={{ fontSize: 32 }}>{icon}</Box>}
          <Typography variant="h5" fontWeight="bold">
            {title}
          </Typography>
        </Box>
        {subtitle && (
          <Typography variant="body2" sx={{ opacity: 0.9, mt: 1, position: 'relative', zIndex: 1 }}>
            {subtitle}
          </Typography>
        )}
      </DialogTitle>
      
      <DialogContent sx={{ p: 4 }}>
        {children}
      </DialogContent>
      
      {actions && (
        <DialogActions sx={{ 
          p: 3, 
          background: backgroundGradientMap[gradient],
          gap: 2,
        }}>
          {actions}
        </DialogActions>
      )}
    </Dialog>
  );
};

export default UltimateDialog;

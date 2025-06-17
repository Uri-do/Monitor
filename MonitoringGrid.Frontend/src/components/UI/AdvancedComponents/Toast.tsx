import React, { useState, useEffect } from 'react';
import {
  Snackbar,
  Alert,
  AlertTitle,
  Button,
  IconButton,
  Box,
  Slide,
  Fade,
  Grow,
  Collapse,
} from '@mui/material';
import {
  Close as CloseIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { TransitionProps } from '@mui/material/transitions';

/**
 * Advanced Toast Notification System
 * Enhanced notifications with actions, positioning, and animations
 */

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';
export type ToastPosition = 'top-left' | 'top-center' | 'top-right' | 'bottom-left' | 'bottom-center' | 'bottom-right';
export type ToastTransition = 'slide' | 'fade' | 'grow' | 'collapse';

export interface ToastAction {
  label: string;
  onClick: () => void;
  variant?: 'text' | 'outlined' | 'contained';
  color?: 'inherit' | 'primary' | 'secondary';
}

export interface ToastProps {
  open: boolean;
  onClose: () => void;
  variant?: ToastVariant;
  title?: string;
  message: string;
  duration?: number;
  position?: ToastPosition;
  transition?: ToastTransition;
  actions?: ToastAction[];
  persistent?: boolean;
  showIcon?: boolean;
  closable?: boolean;
  elevation?: number;
}

// Transition components
const SlideTransition = (props: TransitionProps) => <Slide {...props} direction="up" />;
const FadeTransition = (props: TransitionProps) => <Fade {...props} />;
const GrowTransition = (props: TransitionProps) => <Grow {...props} />;
const CollapseTransition = (props: TransitionProps) => <Collapse {...props} />;

const getTransitionComponent = (transition: ToastTransition) => {
  switch (transition) {
    case 'slide': return SlideTransition;
    case 'fade': return FadeTransition;
    case 'grow': return GrowTransition;
    case 'collapse': return CollapseTransition;
    default: return SlideTransition;
  }
};

const getAnchorOrigin = (position: ToastPosition) => {
  const [vertical, horizontal] = position.split('-');
  return {
    vertical: vertical as 'top' | 'bottom',
    horizontal: horizontal === 'center' ? 'center' as const : horizontal as 'left' | 'right',
  };
};

const getIcon = (variant: ToastVariant) => {
  switch (variant) {
    case 'success': return <SuccessIcon />;
    case 'error': return <ErrorIcon />;
    case 'warning': return <WarningIcon />;
    case 'info': return <InfoIcon />;
    default: return <InfoIcon />;
  }
};

export const Toast: React.FC<ToastProps> = ({
  open,
  onClose,
  variant = 'info',
  title,
  message,
  duration = 6000,
  position = 'bottom-center',
  transition = 'slide',
  actions = [],
  persistent = false,
  showIcon = true,
  closable = true,
  elevation = 6,
}) => {
  const [internalOpen, setInternalOpen] = useState(open);

  useEffect(() => {
    setInternalOpen(open);
  }, [open]);

  const handleClose = (event?: React.SyntheticEvent | Event, reason?: string) => {
    if (reason === 'clickaway' && persistent) {
      return;
    }
    setInternalOpen(false);
    onClose();
  };

  const TransitionComponent = getTransitionComponent(transition);
  const anchorOrigin = getAnchorOrigin(position);

  return (
    <Snackbar
      open={internalOpen}
      autoHideDuration={persistent ? null : duration}
      onClose={handleClose}
      anchorOrigin={anchorOrigin}
      TransitionComponent={TransitionComponent}
      sx={{
        '& .MuiSnackbarContent-root': {
          padding: 0,
        },
      }}
    >
      <Alert
        severity={variant}
        elevation={elevation}
        variant="filled"
        icon={showIcon ? getIcon(variant) : false}
        action={
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {actions.map((action, index) => (
              <Button
                key={index}
                size="small"
                variant={action.variant || 'text'}
                color={action.color || 'inherit'}
                onClick={action.onClick}
                sx={{
                  color: 'inherit',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  },
                }}
              >
                {action.label}
              </Button>
            ))}
            {closable && (
              <IconButton
                size="small"
                onClick={handleClose}
                sx={{
                  color: 'inherit',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  },
                }}
              >
                <CloseIcon fontSize="small" />
              </IconButton>
            )}
          </Box>
        }
        sx={{
          width: '100%',
          minWidth: 300,
          maxWidth: 500,
          '& .MuiAlert-message': {
            width: '100%',
          },
        }}
      >
        {title && <AlertTitle>{title}</AlertTitle>}
        {message}
      </Alert>
    </Snackbar>
  );
};

// Toast Manager for programmatic usage
export class ToastManager {
  private static toasts: Map<string, ToastProps> = new Map();
  private static listeners: Set<() => void> = new Set();

  static show(props: Omit<ToastProps, 'open' | 'onClose'> & { id?: string }) {
    const id = props.id || `toast-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    const toastProps: ToastProps = {
      ...props,
      open: true,
      onClose: () => this.hide(id),
    };

    this.toasts.set(id, toastProps);
    this.notifyListeners();
    
    return id;
  }

  static hide(id: string) {
    this.toasts.delete(id);
    this.notifyListeners();
  }

  static hideAll() {
    this.toasts.clear();
    this.notifyListeners();
  }

  static getToasts(): ToastProps[] {
    return Array.from(this.toasts.values());
  }

  static subscribe(listener: () => void) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private static notifyListeners() {
    this.listeners.forEach(listener => listener());
  }

  // Convenience methods
  static success(message: string, options?: Partial<ToastProps>) {
    return this.show({ ...options, variant: 'success', message });
  }

  static error(message: string, options?: Partial<ToastProps>) {
    return this.show({ ...options, variant: 'error', message, persistent: true });
  }

  static warning(message: string, options?: Partial<ToastProps>) {
    return this.show({ ...options, variant: 'warning', message });
  }

  static info(message: string, options?: Partial<ToastProps>) {
    return this.show({ ...options, variant: 'info', message });
  }
}

// Toast Container Component
export const ToastContainer: React.FC = () => {
  const [toasts, setToasts] = useState<ToastProps[]>([]);

  useEffect(() => {
    const unsubscribe = ToastManager.subscribe(() => {
      setToasts(ToastManager.getToasts());
    });

    return unsubscribe;
  }, []);

  return (
    <>
      {toasts.map((toast, index) => (
        <Toast key={index} {...toast} />
      ))}
    </>
  );
};

// Hook for using toasts
export const useToast = () => {
  return {
    show: ToastManager.show,
    hide: ToastManager.hide,
    hideAll: ToastManager.hideAll,
    success: ToastManager.success,
    error: ToastManager.error,
    warning: ToastManager.warning,
    info: ToastManager.info,
  };
};

export default Toast;

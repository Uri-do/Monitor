import React, { useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  IconButton,
  Button,
  Grid,
  useTheme,
  CircularProgress,
} from '@mui/material';
import { Close as CloseIcon, Save as SaveIcon, Cancel as CancelIcon } from '@mui/icons-material';
import { useForm, FormProvider, FieldValues, DefaultValues, Resolver } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';

// Generic form dialog props
export interface GenericFormDialogProps<T extends FieldValues> {
  // Dialog state
  open: boolean;
  onClose: () => void;

  // Form configuration
  title: string;
  subtitle?: string;
  icon?: React.ReactElement;
  validationSchema?: yup.ObjectSchema<T>;
  defaultValues: DefaultValues<T>;

  // Form submission
  onSubmit: (data: T) => void | Promise<void>;
  loading?: boolean;

  // Form content
  children: React.ReactNode;

  // Customization
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  fullWidth?: boolean;
  showCloseButton?: boolean;
  submitText?: string;
  cancelText?: string;
  submitIcon?: React.ReactElement;
  cancelIcon?: React.ReactElement;

  // Advanced options
  resetOnClose?: boolean;
  resetOnSubmit?: boolean;
  validateOnChange?: boolean;
  validateOnBlur?: boolean;

  // Custom actions
  additionalActions?: React.ReactNode[];

  // Styling
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
}

// Color mappings for gradients
const borderColorMap = {
  primary: 'rgba(25, 118, 210, 0.3)',
  secondary: 'rgba(156, 39, 176, 0.3)',
  success: 'rgba(46, 125, 50, 0.3)',
  warning: 'rgba(237, 108, 2, 0.3)',
  error: 'rgba(211, 47, 47, 0.3)',
  info: 'rgba(2, 136, 209, 0.3)',
};

const shadowColorMap = {
  primary: 'rgba(25, 118, 210, 0.15)',
  secondary: 'rgba(156, 39, 176, 0.15)',
  success: 'rgba(46, 125, 50, 0.15)',
  warning: 'rgba(237, 108, 2, 0.15)',
  error: 'rgba(211, 47, 47, 0.15)',
  info: 'rgba(2, 136, 209, 0.15)',
};

export function GenericFormDialog<T extends FieldValues>({
  open,
  onClose,
  title,
  subtitle,
  icon,
  validationSchema,
  defaultValues,
  onSubmit,
  loading = false,
  children,
  maxWidth = 'md',
  fullWidth = true,
  showCloseButton = true,
  submitText = 'Save',
  cancelText = 'Cancel',
  submitIcon = <SaveIcon />,
  cancelIcon = <CancelIcon />,
  resetOnClose = true,
  resetOnSubmit = false,
  validateOnChange = true,
  validateOnBlur = true,
  additionalActions = [],
  gradient = 'primary',
}: GenericFormDialogProps<T>) {
  const theme = useTheme();

  // Setup form with validation
  const methods = useForm<T>({
    resolver: validationSchema ? (yupResolver(validationSchema) as Resolver<T>) : undefined,
    defaultValues,
    mode: validateOnChange ? 'onChange' : validateOnBlur ? 'onBlur' : 'onSubmit',
  });

  const {
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty, isValid },
  } = methods;

  // Reset form when dialog opens/closes
  useEffect(() => {
    if (open) {
      reset(defaultValues);
    }
  }, [open, defaultValues, reset]);

  // Handle form submission
  const handleFormSubmit = async (data: T) => {
    try {
      await onSubmit(data);

      if (resetOnSubmit) {
        reset();
      }
    } catch (error) {
      // Error handling is done by the parent component
      console.error('Form submission error:', error);
    }
  };

  // Handle dialog close
  const handleClose = () => {
    if (!isSubmitting && !loading) {
      if (resetOnClose) {
        reset();
      }
      onClose();
    }
  };

  // Check if form can be submitted
  const canSubmit = !isSubmitting && !loading && (!validationSchema || isValid);

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth={maxWidth}
      fullWidth={fullWidth}
      PaperProps={{
        sx: {
          backgroundColor: theme.palette.background.paper,
          border: `1px solid ${borderColorMap[gradient]}`,
          borderRadius: 1,
          boxShadow: `0 20px 40px ${shadowColorMap[gradient]}`,
        },
      }}
    >
      {/* Dialog Title */}
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {icon && (
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: 40,
                  height: 40,
                  borderRadius: 1,
                  backgroundColor: `${gradient}.main`,
                  color: 'white',
                }}
              >
                {icon}
              </Box>
            )}
            <Box>
              <Typography variant="h6" component="div" fontWeight="bold">
                {title}
              </Typography>
              {subtitle && (
                <Typography variant="body2" color="text.secondary">
                  {subtitle}
                </Typography>
              )}
            </Box>
          </Box>

          {showCloseButton && (
            <IconButton
              onClick={handleClose}
              disabled={isSubmitting || loading}
              size="small"
              sx={{ color: 'text.secondary' }}
            >
              <CloseIcon />
            </IconButton>
          )}
        </Box>
      </DialogTitle>

      {/* Dialog Content */}
      <DialogContent dividers>
        <FormProvider {...methods}>
          <form onSubmit={handleSubmit(handleFormSubmit)} id="generic-form">
            <Grid container spacing={3}>
              {children}
            </Grid>
          </form>
        </FormProvider>
      </DialogContent>

      {/* Dialog Actions */}
      <DialogActions sx={{ p: 2, gap: 1 }}>
        {/* Additional Actions */}
        {additionalActions.map((action, index) => (
          <React.Fragment key={index}>{action}</React.Fragment>
        ))}

        {/* Spacer */}
        <Box sx={{ flexGrow: 1 }} />

        {/* Cancel Button */}
        <Button
          onClick={handleClose}
          disabled={isSubmitting || loading}
          startIcon={cancelIcon}
          variant="outlined"
          color="inherit"
        >
          {cancelText}
        </Button>

        {/* Submit Button */}
        <Button
          type="submit"
          form="generic-form"
          disabled={!canSubmit}
          startIcon={isSubmitting || loading ? <CircularProgress size={16} /> : submitIcon}
          variant="contained"
          color={gradient}
        >
          {isSubmitting || loading ? 'Saving...' : submitText}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// Specialized form dialogs for common use cases
export interface CreateFormDialogProps<T extends FieldValues>
  extends Omit<GenericFormDialogProps<T>, 'title' | 'submitText'> {
  entityName: string;
}

export function CreateFormDialog<T extends FieldValues>({
  entityName,
  ...props
}: CreateFormDialogProps<T>) {
  return (
    <GenericFormDialog
      {...props}
      title={`Create New ${entityName}`}
      submitText={`Create ${entityName}`}
      gradient="success"
    />
  );
}

export interface EditFormDialogProps<T extends FieldValues>
  extends Omit<GenericFormDialogProps<T>, 'title' | 'submitText'> {
  entityName: string;
}

export function EditFormDialog<T extends FieldValues>({
  entityName,
  ...props
}: EditFormDialogProps<T>) {
  return (
    <GenericFormDialog
      {...props}
      title={`Edit ${entityName}`}
      submitText={`Update ${entityName}`}
      gradient="primary"
    />
  );
}

export interface ViewFormDialogProps<T extends FieldValues>
  extends Omit<GenericFormDialogProps<T>, 'onSubmit' | 'submitText' | 'cancelText'> {
  entityName: string;
}

export function ViewFormDialog<T extends FieldValues>({
  entityName,
  onClose,
  ...props
}: ViewFormDialogProps<T>) {
  return (
    <GenericFormDialog
      {...props}
      title={`View ${entityName}`}
      onSubmit={() => {}} // No-op for view mode
      submitText="Close"
      cancelText=""
      onClose={onClose}
      gradient="info"
    />
  );
}

export default GenericFormDialog;

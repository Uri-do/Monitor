import React from 'react';
import { Card, CardContent, Stack, Button as MuiButton, ButtonProps } from '@mui/material';
import Button from './Button';

export interface FormAction {
  label: string;
  onClick?: () => void;
  variant?: 'contained' | 'outlined' | 'text';
  color?: 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success';
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  startIcon?: React.ReactElement;
  endIcon?: React.ReactElement;
  disabled?: boolean;
  loading?: boolean;
  type?: 'button' | 'submit' | 'reset';
  size?: 'small' | 'medium' | 'large';
}

export interface FormActionsProps {
  actions?: FormAction[];
  primaryAction?: FormAction;
  secondaryActions?: FormAction[];
  alignment?: 'left' | 'center' | 'right' | 'space-between';
  spacing?: number;
  cardProps?: React.ComponentProps<typeof Card>;
  contentProps?: React.ComponentProps<typeof CardContent>;
  stackProps?: React.ComponentProps<typeof Stack>;
}

/**
 * FormActions - Standardized action buttons component for forms
 * Provides consistent button layout and styling across all form pages
 */
const FormActions: React.FC<FormActionsProps> = ({
  actions = [],
  primaryAction,
  secondaryActions = [],
  alignment = 'right',
  spacing = 2,
  cardProps,
  contentProps,
  stackProps,
}) => {
  const allActions = [...secondaryActions, ...actions];
  if (primaryAction) {
    allActions.push(primaryAction);
  }

  if (allActions.length === 0) {
    return null;
  }

  const getJustifyContent = () => {
    switch (alignment) {
      case 'left':
        return 'flex-start';
      case 'center':
        return 'center';
      case 'space-between':
        return 'space-between';
      case 'right':
      default:
        return 'flex-end';
    }
  };

  return (
    <Card {...cardProps}>
      <CardContent sx={{ p: 3, ...contentProps?.sx }} {...contentProps}>
        <Stack
          direction="row"
          spacing={spacing}
          justifyContent={getJustifyContent()}
          alignItems="center"
          {...stackProps}
        >
          {allActions.map((action, index) => {
            const isCustomButton = action.gradient;
            
            if (isCustomButton) {
              return (
                <Button
                  key={index}
                  variant={action.variant || 'contained'}
                  gradient={action.gradient}
                  startIcon={action.startIcon}
                  endIcon={action.endIcon}
                  onClick={action.onClick}
                  disabled={action.disabled || action.loading}
                  type={action.type || 'button'}
                  size={action.size}
                >
                  {action.loading ? 'Loading...' : action.label}
                </Button>
              );
            }

            return (
              <MuiButton
                key={index}
                variant={action.variant || 'contained'}
                color={action.color || 'primary'}
                startIcon={action.startIcon}
                endIcon={action.endIcon}
                onClick={action.onClick}
                disabled={action.disabled || action.loading}
                type={action.type || 'button'}
                size={action.size}
              >
                {action.loading ? 'Loading...' : action.label}
              </MuiButton>
            );
          })}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default FormActions;

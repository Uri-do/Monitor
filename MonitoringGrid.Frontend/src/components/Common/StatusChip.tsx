import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import {
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Schedule as PendingIcon,
  Block as InactiveIcon,
} from '@mui/icons-material';

export type StatusType =
  | 'success'
  | 'error'
  | 'warning'
  | 'info'
  | 'pending'
  | 'inactive'
  | 'active'
  | 'running'
  | 'due'
  | 'critical'
  | 'high'
  | 'medium'
  | 'low'
  | 'resolved'
  | 'unresolved';

interface StatusChipProps extends Omit<ChipProps, 'color'> {
  status: StatusType | string;
  customColors?: Record<
    string,
    {
      color: ChipProps['color'];
      icon?: React.ReactElement;
    }
  >;
}

const StatusChip: React.FC<StatusChipProps> = ({ status, customColors = {}, ...chipProps }) => {
  const getStatusConfig = (status: string) => {
    // Check custom colors first
    if (customColors[status.toLowerCase()]) {
      return customColors[status.toLowerCase()];
    }

    // Default status configurations
    const statusConfigs: Record<string, { color: ChipProps['color']; icon?: React.ReactElement }> =
      {
        // General statuses
        success: { color: 'success', icon: <SuccessIcon /> },
        error: { color: 'error', icon: <ErrorIcon /> },
        warning: { color: 'warning', icon: <WarningIcon /> },
        info: { color: 'info', icon: <InfoIcon /> },
        pending: { color: 'warning', icon: <PendingIcon /> },
        inactive: { color: 'default', icon: <InactiveIcon /> },

        // KPI statuses
        active: { color: 'success', icon: <SuccessIcon /> },
        running: { color: 'success', icon: <SuccessIcon /> },
        due: { color: 'warning', icon: <WarningIcon /> },
        'due soon': { color: 'warning', icon: <WarningIcon /> },
        'never run': { color: 'error', icon: <ErrorIcon /> },

        // Alert severities
        critical: { color: 'error', icon: <ErrorIcon /> },
        high: { color: 'warning', icon: <WarningIcon /> },
        medium: { color: 'info', icon: <InfoIcon /> },
        low: { color: 'success', icon: <SuccessIcon /> },

        // Alert statuses
        resolved: { color: 'success', icon: <SuccessIcon /> },
        unresolved: { color: 'error', icon: <ErrorIcon /> },

        // Priority levels
        '1': { color: 'error', icon: <ErrorIcon /> },
        '2': { color: 'warning', icon: <WarningIcon /> },
        '3': { color: 'info', icon: <InfoIcon /> },
        '4': { color: 'success', icon: <SuccessIcon /> },
      };

    return statusConfigs[status.toLowerCase()] || { color: 'default' as ChipProps['color'] };
  };

  const config = getStatusConfig(status);

  return (
    <Chip
      label={status}
      color={config.color}
      icon={config.icon}
      size="small"
      variant="filled"
      {...chipProps}
    />
  );
};

export default StatusChip;

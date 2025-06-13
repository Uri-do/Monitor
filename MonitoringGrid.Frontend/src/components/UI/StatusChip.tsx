import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import {
  CheckCircle as ActiveIcon,
  Cancel as InactiveIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  PlayArrow as RunningIcon,
  Schedule as DueIcon,
  Info as InfoIcon,
} from '@mui/icons-material';

interface StatusChipProps extends Omit<ChipProps, 'color'> {
  status: string;
  animated?: boolean;
}

const statusConfig = {
  active: {
    label: 'Active',
    color: '#43e97b',
    backgroundColor: 'rgba(67, 233, 123, 0.1)',
    icon: <ActiveIcon />,
  },
  inactive: {
    label: 'Inactive',
    color: '#9e9e9e',
    backgroundColor: 'rgba(158, 158, 158, 0.1)',
    icon: <InactiveIcon />,
  },
  running: {
    label: 'Running',
    color: '#4facfe',
    backgroundColor: 'rgba(79, 172, 254, 0.1)',
    icon: <RunningIcon />,
    animated: true,
  },
  due: {
    label: 'Due',
    color: '#fa709a',
    backgroundColor: 'rgba(250, 112, 154, 0.1)',
    icon: <DueIcon />,
    animated: true,
  },
  error: {
    label: 'Error',
    color: '#ff6b6b',
    backgroundColor: 'rgba(255, 107, 107, 0.1)',
    icon: <ErrorIcon />,
  },
  warning: {
    label: 'Warning',
    color: '#fee140',
    backgroundColor: 'rgba(254, 225, 64, 0.1)',
    icon: <WarningIcon />,
  },
  success: {
    label: 'Success',
    color: '#43e97b',
    backgroundColor: 'rgba(67, 233, 123, 0.1)',
    icon: <ActiveIcon />,
  },
  info: {
    label: 'Info',
    color: '#4facfe',
    backgroundColor: 'rgba(79, 172, 254, 0.1)',
    icon: <InfoIcon />,
  },
  // Priority levels
  'Critical (SMS + Email)': {
    label: 'Critical',
    color: '#ff6b6b',
    backgroundColor: 'rgba(255, 107, 107, 0.1)',
    icon: <ErrorIcon />,
  },
  'High (Email Only)': {
    label: 'High',
    color: '#fa709a',
    backgroundColor: 'rgba(250, 112, 154, 0.1)',
    icon: <WarningIcon />,
  },
  Medium: {
    label: 'Medium',
    color: '#fee140',
    backgroundColor: 'rgba(254, 225, 64, 0.1)',
    icon: <InfoIcon />,
  },
  Low: {
    label: 'Low',
    color: '#43e97b',
    backgroundColor: 'rgba(67, 233, 123, 0.1)',
    icon: <InfoIcon />,
  },
  // Schedule types
  interval: {
    label: 'Interval',
    color: '#4facfe',
    backgroundColor: 'rgba(79, 172, 254, 0.1)',
    icon: <InfoIcon />,
  },
  cron: {
    label: 'Cron',
    color: '#9c27b0',
    backgroundColor: 'rgba(156, 39, 176, 0.1)',
    icon: <InfoIcon />,
  },
  onetime: {
    label: 'One Time',
    color: '#ff9800',
    backgroundColor: 'rgba(255, 152, 0, 0.1)',
    icon: <InfoIcon />,
  },
};

export const StatusChip: React.FC<StatusChipProps> = ({
  status,
  animated = true,
  sx,
  ...props
}) => {
  const config =
    statusConfig[status.toLowerCase() as keyof typeof statusConfig] ||
    statusConfig[status as keyof typeof statusConfig] ||
    statusConfig.info;

  const shouldAnimate = animated && (config as any).animated;

  return (
    <Chip
      label={config.label}
      icon={config.icon}
      size="small"
      sx={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        border: `1px solid ${config.color}30`,
        fontWeight: 600,
        fontSize: '0.75rem',
        height: 28,
        '& .MuiChip-icon': {
          color: config.color,
          fontSize: '16px',
          ...(shouldAnimate && {
            animation: 'pulse 2s infinite',
            '@keyframes pulse': {
              '0%': { opacity: 1 },
              '50%': { opacity: 0.6 },
              '100%': { opacity: 1 },
            },
          }),
        },
        '& .MuiChip-label': {
          paddingLeft: 1,
          paddingRight: 1.5,
        },
        transition: 'all 0.2s ease-in-out',
        '&:hover': {
          backgroundColor: `${config.color}20`,
          transform: 'translateY(-1px)',
          boxShadow: `0 4px 8px ${config.color}30`,
        },
        ...sx,
      }}
      {...props}
    />
  );
};

export default StatusChip;

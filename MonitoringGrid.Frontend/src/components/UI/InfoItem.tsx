import React from 'react';
import { Box, Typography } from '@mui/material';

interface InfoItemProps {
  label: string;
  value: React.ReactNode;
  icon?: React.ReactNode;
  variant?: 'default' | 'compact' | 'prominent';
  labelColor?: string;
  valueColor?: string;
  direction?: 'column' | 'row';
}

/**
 * Reusable component for displaying label-value pairs consistently across the application
 */
export const InfoItem: React.FC<InfoItemProps> = ({
  label,
  value,
  icon,
  variant = 'default',
  labelColor = 'text.secondary',
  valueColor = 'text.primary',
  direction = 'column',
}) => {
  const getSpacing = () => {
    switch (variant) {
      case 'compact':
        return { mb: 0.5, gap: 0.5 };
      case 'prominent':
        return { mb: 2, gap: 1 };
      default:
        return { mb: 1, gap: 0.5 };
    }
  };

  const getLabelVariant = () => {
    switch (variant) {
      case 'compact':
        return 'caption';
      case 'prominent':
        return 'subtitle2';
      default:
        return 'body2';
    }
  };

  const getValueVariant = () => {
    switch (variant) {
      case 'compact':
        return 'body2';
      case 'prominent':
        return 'h6';
      default:
        return 'body1';
    }
  };

  const spacing = getSpacing();

  if (direction === 'row') {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: spacing.gap, ...spacing }}>
        {icon}
        <Typography variant={getLabelVariant()} color={labelColor} sx={{ minWidth: 120 }}>
          {label}:
        </Typography>
        <Typography variant={getValueVariant()} color={valueColor} sx={{ fontWeight: variant === 'prominent' ? 600 : 400 }}>
          {value}
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={spacing}>
      <Typography variant={getLabelVariant()} color={labelColor} gutterBottom>
        {label}
      </Typography>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: spacing.gap }}>
        {icon}
        <Typography 
          variant={getValueVariant()} 
          color={valueColor}
          sx={{ fontWeight: variant === 'prominent' ? 600 : 400 }}
        >
          {value}
        </Typography>
      </Box>
    </Box>
  );
};

export default InfoItem;

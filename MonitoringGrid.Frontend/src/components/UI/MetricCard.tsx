import React from 'react';
import { Card, CardContent, Typography, Box, Chip } from '@mui/material';

interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: React.ReactElement;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  chip?: {
    label: string;
    color?: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning';
  };
  action?: React.ReactElement;
  onClick?: () => void;
  size?: 'small' | 'medium' | 'large';
}

const gradientMap = {
  primary: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
  secondary: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
  success: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
  warning: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
  error: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
  info: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
};

const shadowColorMap = {
  primary: 'rgba(102, 126, 234, 0.4)',
  secondary: 'rgba(240, 147, 251, 0.4)',
  success: 'rgba(67, 233, 123, 0.4)',
  warning: 'rgba(250, 112, 154, 0.4)',
  error: 'rgba(255, 107, 107, 0.4)',
  info: 'rgba(79, 172, 254, 0.4)',
};

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  subtitle,
  icon,
  gradient = 'primary',
  chip,
  action,
  onClick,
  size = 'medium',
}) => {
  const getSizeStyles = () => {
    switch (size) {
      case 'small':
        return {
          padding: 2,
          titleVariant: 'subtitle1' as const,
          valueVariant: 'h4' as const,
          iconSize: 20,
        };
      case 'large':
        return {
          padding: 4,
          titleVariant: 'h5' as const,
          valueVariant: 'h2' as const,
          iconSize: 36,
        };
      default: // medium
        return {
          padding: 3,
          titleVariant: 'h6' as const,
          valueVariant: 'h3' as const,
          iconSize: 28,
        };
    }
  };

  const sizeStyles = getSizeStyles();
  return (
    <Card
      elevation={4}
      onClick={onClick}
      sx={{
        background: gradientMap[gradient],
        color: 'white',
        border: `1px solid ${gradientMap[gradient].replace('135deg', '135deg').replace('100%', '30%')}`,
        transition: 'all 0.3s ease-in-out',
        cursor: onClick ? 'pointer' : 'default',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: `0 12px 30px ${shadowColorMap[gradient]}`,
        },
      }}
    >
      <CardContent sx={{ p: sizeStyles.padding }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          {icon && <Box sx={{ fontSize: sizeStyles.iconSize }}>{icon}</Box>}
          <Typography variant={sizeStyles.titleVariant} fontWeight="bold">
            {title}
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
          <Typography variant={sizeStyles.valueVariant} fontWeight="bold">
            {value}
          </Typography>
          {chip && (
            <Chip
              label={chip.label}
              color={chip.color || 'default'}
              sx={{
                background: 'rgba(255, 255, 255, 0.2)',
                color: 'white',
                fontWeight: 'bold',
              }}
            />
          )}
        </Box>

        {subtitle && (
          <Typography variant="caption" sx={{ opacity: 0.9, display: 'block' }}>
            {subtitle}
          </Typography>
        )}

        {action && <Box sx={{ mt: 2 }}>{action}</Box>}
      </CardContent>
    </Card>
  );
};

export default MetricCard;

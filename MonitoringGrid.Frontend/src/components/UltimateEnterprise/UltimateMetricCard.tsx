import React from 'react';
import { Card, CardContent, Typography, Box, Chip } from '@mui/material';

interface UltimateMetricCardProps {
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

export const UltimateMetricCard: React.FC<UltimateMetricCardProps> = ({
  title,
  value,
  subtitle,
  icon,
  gradient = 'primary',
  chip,
  action,
  onClick,
}) => {
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
        }
      }}
    >
      <CardContent sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          {icon && <Box sx={{ fontSize: 28 }}>{icon}</Box>}
          <Typography variant="h6" fontWeight="bold">
            {title}
          </Typography>
        </Box>
        
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
          <Typography variant="h3" fontWeight="bold">
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
        
        {action && (
          <Box sx={{ mt: 2 }}>
            {action}
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default UltimateMetricCard;

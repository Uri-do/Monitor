import React from 'react';
import { Card, CardProps, Box } from '@mui/material';

interface UltimateCardProps extends CardProps {
  children: React.ReactNode;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  hoverEffect?: boolean;
  glowEffect?: boolean;
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
  primary: 'rgba(102, 126, 234, 0.3)',
  secondary: 'rgba(240, 147, 251, 0.3)',
  success: 'rgba(67, 233, 123, 0.3)',
  warning: 'rgba(250, 112, 154, 0.3)',
  error: 'rgba(255, 107, 107, 0.3)',
  info: 'rgba(79, 172, 254, 0.3)',
};

export const UltimateCard: React.FC<UltimateCardProps> = ({
  children,
  gradient = 'primary',
  hoverEffect = true,
  glowEffect = false,
  sx,
  ...props
}) => {
  return (
    <Card
      elevation={4}
      sx={{
        background: glowEffect
          ? gradientMap[gradient]
          : 'linear-gradient(145deg, #ffffff 0%, #f8f9fa 100%)',
        border: `1px solid ${borderColorMap[gradient]}`,
        borderRadius: 3,
        transition: 'all 0.3s ease-in-out',
        position: 'relative',
        overflow: 'hidden',
        ...(hoverEffect && {
          '&:hover': {
            transform: 'translateY(-4px)',
            boxShadow: `0 12px 30px ${shadowColorMap[gradient]}`,
            border: `1px solid ${borderColorMap[gradient].replace('0.2', '0.4')}`,
          },
        }),
        ...(glowEffect && {
          color: 'white',
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
            zIndex: 0,
          },
        }),
        ...sx,
      }}
      {...props}
    >
      <Box sx={{ position: 'relative', zIndex: 1 }}>{children}</Box>
    </Card>
  );
};

export default UltimateCard;

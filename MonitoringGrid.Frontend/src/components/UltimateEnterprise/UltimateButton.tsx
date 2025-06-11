import React from 'react';
import { Button, ButtonProps, Box } from '@mui/material';

interface UltimateButtonProps extends Omit<ButtonProps, 'color'> {
  children: React.ReactNode;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  icon?: React.ReactElement;
  loading?: boolean;
  loadingIcon?: React.ReactElement;
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

const hoverGradientMap = {
  primary: 'linear-gradient(135deg, #5a6fd8 0%, #6a4190 100%)',
  secondary: 'linear-gradient(135deg, #e081e9 0%, #e3455a 100%)',
  success: 'linear-gradient(135deg, #3dd169 0%, #30e7c5 100%)',
  warning: 'linear-gradient(135deg, #f85e88 0%, #fdd32e 100%)',
  error: 'linear-gradient(135deg, #ff5252 0%, #d84315 100%)',
  info: 'linear-gradient(135deg, #3d9aec 0%, #00e0ec 100%)',
};

const shadowColorMap = {
  primary: 'rgba(102, 126, 234, 0.4)',
  secondary: 'rgba(240, 147, 251, 0.4)',
  success: 'rgba(67, 233, 123, 0.4)',
  warning: 'rgba(250, 112, 154, 0.4)',
  error: 'rgba(255, 107, 107, 0.4)',
  info: 'rgba(79, 172, 254, 0.4)',
};

const outlineColorMap = {
  primary: '#667eea',
  secondary: '#f093fb',
  success: '#43e97b',
  warning: '#fa709a',
  error: '#ff6b6b',
  info: '#4facfe',
};

export const UltimateButton: React.FC<UltimateButtonProps> = ({
  children,
  gradient = 'primary',
  icon,
  loading = false,
  loadingIcon,
  glowEffect = true,
  variant = 'contained',
  disabled,
  sx,
  ...props
}) => {
  const isContained = variant === 'contained';
  const isOutlined = variant === 'outlined';

  return (
    <Button
      variant={variant}
      disabled={disabled || loading}
      sx={{
        ...(isContained && {
          background: gradientMap[gradient],
          color: 'white',
          fontWeight: 600,
          textTransform: 'none',
          ...(glowEffect && {
            boxShadow: `0 4px 15px ${shadowColorMap[gradient]}`,
          }),
          '&:hover': {
            background: hoverGradientMap[gradient],
            ...(glowEffect && {
              boxShadow: `0 6px 20px ${shadowColorMap[gradient].replace('0.4', '0.6')}`,
            }),
          },
          '&:disabled': {
            background: 'linear-gradient(135deg, #ccc 0%, #999 100%)',
            color: 'white',
          },
        }),
        ...(isOutlined && {
          borderColor: `${outlineColorMap[gradient]}40`,
          color: outlineColorMap[gradient],
          fontWeight: 600,
          textTransform: 'none',
          '&:hover': {
            borderColor: outlineColorMap[gradient],
            background: `${outlineColorMap[gradient]}10`,
            ...(glowEffect && {
              boxShadow: `0 4px 15px ${shadowColorMap[gradient]}`,
            }),
          },
        }),
        py: 1.5,
        px: 3,
        borderRadius: 2,
        ...sx,
      }}
      {...props}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        {loading && loadingIcon ? (
          <Box sx={{ animation: 'spin 1s linear infinite' }}>{loadingIcon}</Box>
        ) : icon ? (
          icon
        ) : null}
        {children}
      </Box>
    </Button>
  );
};

// Add CSS animation for loading spinner
const style = document.createElement('style');
style.textContent = `
  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }
`;
document.head.appendChild(style);

export default UltimateButton;

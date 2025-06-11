import React from 'react';
import { Box, CircularProgress, Typography, Stack } from '@mui/material';
import Card from './Card';

interface UltimateLoadingSpinnerProps {
  message?: string;
  size?: number;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  fullScreen?: boolean;
  overlay?: boolean;
}

const gradientColors = {
  primary: '#667eea',
  secondary: '#f093fb',
  success: '#43e97b',
  warning: '#fa709a',
  error: '#ff6b6b',
  info: '#4facfe',
};

export const UltimateLoadingSpinner: React.FC<UltimateLoadingSpinnerProps> = ({
  message = 'Loading...',
  size = 60,
  gradient = 'primary',
  fullScreen = false,
  overlay = false,
}) => {
  const color = gradientColors[gradient];

  const SpinnerContent = (
    <Stack alignItems="center" spacing={2}>
      <Box sx={{ position: 'relative' }}>
        {/* Outer ring */}
        <CircularProgress
          size={size}
          thickness={2}
          sx={{
            color: `${color}40`,
            position: 'absolute',
          }}
        />
        {/* Inner spinning ring */}
        <CircularProgress
          size={size}
          thickness={4}
          sx={{
            color,
            animationDuration: '1.2s',
            filter: `drop-shadow(0 0 8px ${color}60)`,
          }}
        />
        {/* Center dot */}
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            width: size * 0.2,
            height: size * 0.2,
            borderRadius: '50%',
            background: `linear-gradient(135deg, ${color} 0%, ${color}80 100%)`,
            animation: 'pulse 2s infinite',
            '@keyframes pulse': {
              '0%': { opacity: 1, transform: 'translate(-50%, -50%) scale(1)' },
              '50%': { opacity: 0.7, transform: 'translate(-50%, -50%) scale(1.2)' },
              '100%': { opacity: 1, transform: 'translate(-50%, -50%) scale(1)' },
            },
          }}
        />
      </Box>

      {message && (
        <Typography
          variant="body1"
          sx={{
            color: fullScreen || overlay ? 'white' : 'text.primary',
            fontWeight: 500,
            textAlign: 'center',
            animation: 'fadeInOut 2s infinite',
            '@keyframes fadeInOut': {
              '0%': { opacity: 0.7 },
              '50%': { opacity: 1 },
              '100%': { opacity: 0.7 },
            },
          }}
        >
          {message}
        </Typography>
      )}
    </Stack>
  );

  if (fullScreen) {
    return (
      <Box
        sx={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: `linear-gradient(135deg, ${color}20 0%, ${color}10 100%)`,
          backdropFilter: 'blur(10px)',
          zIndex: 9999,
        }}
      >
        {SpinnerContent}
      </Box>
    );
  }

  if (overlay) {
    return (
      <Box
        sx={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: `linear-gradient(135deg, ${color}15 0%, ${color}05 100%)`,
          backdropFilter: 'blur(5px)',
          borderRadius: 'inherit',
          zIndex: 10,
        }}
      >
        {SpinnerContent}
      </Box>
    );
  }

  return <Card sx={{ p: 4, textAlign: 'center' }}>{SpinnerContent}</Card>;
};

export default UltimateLoadingSpinner;

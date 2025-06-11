import React from 'react';
import { FormControl, InputLabel, Select, SelectProps, Box } from '@mui/material';

interface UltimateSelectProps extends Omit<SelectProps, 'color'> {
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  glowEffect?: boolean;
  label: string;
  children: React.ReactNode;
}

const gradientMap = {
  primary: 'linear-gradient(45deg, rgba(102, 126, 234, 0.05) 30%, rgba(118, 75, 162, 0.05) 90%)',
  secondary: 'linear-gradient(45deg, rgba(240, 147, 251, 0.05) 30%, rgba(245, 87, 108, 0.05) 90%)',
  success: 'linear-gradient(45deg, rgba(67, 233, 123, 0.05) 30%, rgba(56, 249, 215, 0.05) 90%)',
  warning: 'linear-gradient(45deg, rgba(250, 112, 154, 0.05) 30%, rgba(254, 225, 64, 0.05) 90%)',
  error: 'linear-gradient(45deg, rgba(255, 107, 107, 0.05) 30%, rgba(238, 90, 36, 0.05) 90%)',
  info: 'linear-gradient(45deg, rgba(79, 172, 254, 0.05) 30%, rgba(0, 242, 254, 0.05) 90%)',
};

const borderColorMap = {
  primary: 'rgba(102, 126, 234, 0.1)',
  secondary: 'rgba(240, 147, 251, 0.1)',
  success: 'rgba(67, 233, 123, 0.1)',
  warning: 'rgba(250, 112, 154, 0.1)',
  error: 'rgba(255, 107, 107, 0.1)',
  info: 'rgba(79, 172, 254, 0.1)',
};

const focusColorMap = {
  primary: '#667eea',
  secondary: '#f093fb',
  success: '#43e97b',
  warning: '#fa709a',
  error: '#ff6b6b',
  info: '#4facfe',
};

export const UltimateSelect: React.FC<UltimateSelectProps> = ({
  gradient = 'primary',
  glowEffect = true,
  label,
  children,
  sx,
  ...props
}) => {
  return (
    <Box sx={{ 
      p: 3, 
      borderRadius: 2, 
      background: gradientMap[gradient],
      border: `1px solid ${borderColorMap[gradient]}`,
      height: '100%',
      transition: 'all 0.3s ease-in-out',
      ...(glowEffect && {
        '&:hover': {
          boxShadow: `0 4px 15px ${borderColorMap[gradient]}`,
        }
      })
    }}>
      <FormControl fullWidth>
        <InputLabel 
          sx={{ 
            fontWeight: 600,
            '&.Mui-focused': {
              color: focusColorMap[gradient],
            }
          }}
        >
          {label}
        </InputLabel>
        <Select
          label={label}
          sx={{
            background: 'rgba(255, 255, 255, 0.8)',
            borderRadius: 2,
            transition: 'all 0.3s ease-in-out',
            '&:hover': {
              background: 'rgba(255, 255, 255, 0.9)',
            },
            '&.Mui-focused': {
              background: 'white',
              '& .MuiOutlinedInput-notchedOutline': {
                borderColor: focusColorMap[gradient],
                borderWidth: 2,
              }
            },
            ...sx,
          }}
          {...props}
        >
          {children}
        </Select>
      </FormControl>
    </Box>
  );
};

export default UltimateSelect;

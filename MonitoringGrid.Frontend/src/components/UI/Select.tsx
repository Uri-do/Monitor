import React from 'react';
import {
  FormControl,
  InputLabel,
  Select,
  SelectProps,
  Box,
  MenuItem,
  useTheme,
} from '@mui/material';

interface SelectOption {
  value: any;
  label: string;
}

interface CustomSelectProps extends Omit<SelectProps, 'color'> {
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  glowEffect?: boolean;
  label: string;
  children?: React.ReactNode;
  options?: SelectOption[];
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

export type { CustomSelectProps };

export const CustomSelect: React.FC<CustomSelectProps> = ({
  gradient = 'primary',
  glowEffect = true,
  label,
  children,
  options,
  sx,
  ...props
}) => {
  const theme = useTheme();

  return (
    <Box
      sx={{
        p: 3,
        borderRadius: 1,
        background: gradientMap[gradient],
        border: `1px solid ${borderColorMap[gradient]}`,
        height: '100%',
        transition: 'all 0.3s ease-in-out',
        ...(glowEffect && {
          '&:hover': {
            boxShadow: `0 4px 15px ${borderColorMap[gradient]}`,
          },
        }),
      }}
    >
      <FormControl fullWidth>
        <InputLabel
          sx={{
            fontWeight: 600,
            '&.Mui-focused': {
              color: focusColorMap[gradient],
            },
          }}
        >
          {label}
        </InputLabel>
        <Select
          label={label}
          sx={{
            background:
              theme.palette.mode === 'light'
                ? 'rgba(255, 255, 255, 0.8)'
                : 'rgba(255, 255, 255, 0.05)',
            borderRadius: 1,
            transition: 'all 0.3s ease-in-out',
            '&:hover': {
              background:
                theme.palette.mode === 'light'
                  ? 'rgba(255, 255, 255, 0.9)'
                  : 'rgba(255, 255, 255, 0.08)',
            },
            '&.Mui-focused': {
              background: theme.palette.mode === 'light' ? 'white' : 'rgba(255, 255, 255, 0.1)',
              '& .MuiOutlinedInput-notchedOutline': {
                borderColor: focusColorMap[gradient],
                borderWidth: 2,
              },
            },
            ...sx,
          }}
          {...props}
        >
          {options
            ? options.map(option => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))
            : children}
        </Select>
      </FormControl>
    </Box>
  );
};

export default CustomSelect;

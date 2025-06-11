import React from 'react';
import { TextField, TextFieldProps } from '@mui/material';

interface CustomInputFieldProps extends Omit<TextFieldProps, 'color'> {
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  glowEffect?: boolean;
}

// Gradient map for future use with enhanced input styling
// const gradientMap = {
//   primary: 'linear-gradient(45deg, rgba(102, 126, 234, 0.05) 30%, rgba(118, 75, 162, 0.05) 90%)',
//   secondary: 'linear-gradient(45deg, rgba(240, 147, 251, 0.05) 30%, rgba(245, 87, 108, 0.05) 90%)',
//   success: 'linear-gradient(45deg, rgba(67, 233, 123, 0.05) 30%, rgba(56, 249, 215, 0.05) 90%)',
//   warning: 'linear-gradient(45deg, rgba(250, 112, 154, 0.05) 30%, rgba(254, 225, 64, 0.05) 90%)',
//   error: 'linear-gradient(45deg, rgba(255, 107, 107, 0.05) 30%, rgba(238, 90, 36, 0.05) 90%)',
//   info: 'linear-gradient(45deg, rgba(79, 172, 254, 0.05) 30%, rgba(0, 242, 254, 0.05) 90%)',
// };

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

export type { CustomInputFieldProps };

export const CustomInputField = React.forwardRef<HTMLDivElement, CustomInputFieldProps>(
  ({ gradient = 'primary', glowEffect = true, sx, ...props }, ref) => {
    // const theme = useTheme(); // Available for future theming enhancements

    return (
      <TextField
        ref={ref}
        variant="outlined"
        fullWidth
        sx={{
          '& .MuiOutlinedInput-root': {
            borderRadius: 2, // More rounded corners
            transition: 'all 0.3s ease-in-out',
            '&.Mui-focused': {
              '& .MuiOutlinedInput-notchedOutline': {
                borderColor: focusColorMap[gradient],
                borderWidth: 2,
              },
            },
            ...(glowEffect && {
              '&:hover': {
                boxShadow: `0 2px 8px ${borderColorMap[gradient]}`,
              },
            }),
          },
          '& .MuiInputLabel-root': {
            fontWeight: 500,
            '&.Mui-focused': {
              color: focusColorMap[gradient],
            },
          },
          ...sx,
        }}
        {...props}
      />
    );
  }
);

CustomInputField.displayName = 'CustomInputField';

export default CustomInputField;

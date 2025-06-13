import { createTheme, Theme } from '@mui/material/styles';
import type { PaletteMode } from '@mui/material';
import { designTokens } from './designTokens';

export const createAppTheme = (mode: PaletteMode): Theme => {
  return createTheme({
    palette: {
      mode,
      primary: {
        main: designTokens.colors.primary[700],
        light: designTokens.colors.primary[400],
        dark: designTokens.colors.primary[800],
      },
      secondary: {
        main: designTokens.colors.secondary[600],
        light: designTokens.colors.secondary[400],
        dark: designTokens.colors.secondary[800],
      },
      background: {
        default: mode === 'light' ? '#f8fafc' : '#0a0e1a',
        paper: mode === 'light' ? '#ffffff' : '#1a1d29',
      },
      error: {
        main: designTokens.colors.error[500],
        light: designTokens.colors.error[400],
        dark: designTokens.colors.error[700],
      },
      warning: {
        main: designTokens.colors.warning[500],
        light: designTokens.colors.warning[400],
        dark: designTokens.colors.warning[700],
      },
      success: {
        main: designTokens.colors.success[500],
        light: designTokens.colors.success[400],
        dark: designTokens.colors.success[700],
      },
      info: {
        main: designTokens.colors.info[500],
        light: designTokens.colors.info[400],
        dark: designTokens.colors.info[700],
      },
      grey: designTokens.colors.neutral,
      ...(mode === 'dark' && {
        text: {
          primary: '#ffffff',
          secondary: 'rgba(255, 255, 255, 0.7)',
        },
        divider: 'rgba(255, 255, 255, 0.12)',
      }),
    },
    typography: {
      fontFamily: designTokens.typography.fontFamily.primary,
      h1: {
        fontWeight: designTokens.typography.fontWeight.bold,
        fontSize: designTokens.typography.fontSize['4xl'],
        lineHeight: designTokens.typography.lineHeight.tight,
      },
      h2: {
        fontWeight: designTokens.typography.fontWeight.bold,
        fontSize: designTokens.typography.fontSize['3xl'],
        lineHeight: designTokens.typography.lineHeight.tight,
      },
      h3: {
        fontWeight: designTokens.typography.fontWeight.semibold,
        fontSize: designTokens.typography.fontSize['2xl'],
        lineHeight: designTokens.typography.lineHeight.normal,
      },
      h4: {
        fontWeight: designTokens.typography.fontWeight.semibold,
        fontSize: designTokens.typography.fontSize.xl,
        lineHeight: designTokens.typography.lineHeight.normal,
      },
      h5: {
        fontWeight: designTokens.typography.fontWeight.semibold,
        fontSize: designTokens.typography.fontSize.lg,
        lineHeight: designTokens.typography.lineHeight.normal,
      },
      h6: {
        fontWeight: designTokens.typography.fontWeight.semibold,
        fontSize: designTokens.typography.fontSize.base,
        lineHeight: designTokens.typography.lineHeight.normal,
      },
      body1: {
        fontSize: designTokens.typography.fontSize.base,
        lineHeight: designTokens.typography.lineHeight.relaxed,
      },
      body2: {
        fontSize: designTokens.typography.fontSize.sm,
        lineHeight: designTokens.typography.lineHeight.relaxed,
      },
      button: {
        fontWeight: designTokens.typography.fontWeight.semibold,
        fontSize: designTokens.typography.fontSize.sm,
        textTransform: 'none',
      },
    },
    shape: {
      borderRadius: parseInt(designTokens.borderRadius.base.replace('rem', '')) * 16, // Convert rem to px
    },
    spacing: 8,
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          '*': {
            // Global scrollbar styling
            '&::-webkit-scrollbar': {
              width: '8px',
              height: '8px',
            },
            '&::-webkit-scrollbar-track': {
              backgroundColor: mode === 'dark'
                ? 'rgba(255, 255, 255, 0.05)'
                : 'rgba(0, 0, 0, 0.05)',
              borderRadius: '4px',
            },
            '&::-webkit-scrollbar-thumb': {
              backgroundColor: mode === 'dark'
                ? 'rgba(255, 255, 255, 0.2)'
                : 'rgba(0, 0, 0, 0.2)',
              borderRadius: '4px',
              '&:hover': {
                backgroundColor: mode === 'dark'
                  ? 'rgba(255, 255, 255, 0.3)'
                  : 'rgba(0, 0, 0, 0.3)',
              },
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: designTokens.borderRadius.base,
            boxShadow: designTokens.shadows.md,
            border:
              mode === 'light'
                ? '1px solid rgba(0, 0, 0, 0.04)'
                : '1px solid rgba(255, 255, 255, 0.08)',
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: designTokens.borderRadius.base,
            textTransform: 'none',
            fontWeight: designTokens.typography.fontWeight.semibold,
            boxShadow: 'none',
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            fontWeight: designTokens.typography.fontWeight.medium,
            borderRadius: designTokens.borderRadius.base,
          },
        },
      },
      MuiTableContainer: {
        styleOverrides: {
          root: {
            backgroundColor: mode === 'light' ? '#ffffff' : '#1a1d29',
            border:
              mode === 'light'
                ? '1px solid rgba(0, 0, 0, 0.1)'
                : '1px solid rgba(255, 255, 255, 0.08)',
          },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: {
            backgroundColor: mode === 'light' ? 'rgba(0, 0, 0, 0.02)' : 'rgba(255, 255, 255, 0.02)',
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: {
            '&:nth-of-type(even)': {
              backgroundColor:
                mode === 'light' ? 'rgba(0, 0, 0, 0.01)' : 'rgba(255, 255, 255, 0.01)',
            },
            '&:hover': {
              backgroundColor:
                mode === 'light' ? 'rgba(0, 0, 0, 0.04)' : 'rgba(255, 255, 255, 0.04)',
            },
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            borderColor: mode === 'light' ? 'rgba(0, 0, 0, 0.08)' : 'rgba(255, 255, 255, 0.08)',
          },
          head: {
            fontWeight: designTokens.typography.fontWeight.semibold,
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            '& .MuiOutlinedInput-root': {
              backgroundColor: mode === 'light' ? '#ffffff' : 'rgba(255, 255, 255, 0.02)',
              '& fieldset': {
                borderColor: mode === 'light' ? 'rgba(0, 0, 0, 0.12)' : 'rgba(255, 255, 255, 0.12)',
              },
            },
          },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: {
            backgroundColor: mode === 'light' ? '#ffffff' : '#1a1d29',
            backgroundImage: 'none',
          },
        },
      },
    },
  });
};

// Default theme (light mode)
export const theme = createAppTheme('light');

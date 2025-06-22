import type { ThemeConfig } from 'antd';
import { designTokens } from './designTokens';

export type ThemeMode = 'light' | 'dark';

export const createAppTheme = (mode: ThemeMode): ThemeConfig => {
  return {
    token: {
      // Primary colors
      colorPrimary: designTokens.colors.primary[700],
      colorSuccess: designTokens.colors.success[500],
      colorWarning: designTokens.colors.warning[500],
      colorError: designTokens.colors.error[500],
      colorInfo: designTokens.colors.info[500],

      // Background colors
      colorBgBase: mode === 'light' ? '#ffffff' : '#1a1d29',
      colorBgContainer: mode === 'light' ? '#ffffff' : '#1a1d29',
      colorBgLayout: mode === 'light' ? '#f8fafc' : '#0a0e1a',

      // Text colors
      colorText: mode === 'light' ? 'rgba(0, 0, 0, 0.88)' : '#ffffff',
      colorTextSecondary: mode === 'light' ? 'rgba(0, 0, 0, 0.65)' : 'rgba(255, 255, 255, 0.7)',

      // Border
      colorBorder: mode === 'light' ? 'rgba(0, 0, 0, 0.12)' : 'rgba(255, 255, 255, 0.12)',

      // Typography
      fontFamily: designTokens.typography.fontFamily.primary,
      fontSize: 14,

      // Border radius
      borderRadius: 8,

      // Spacing
      padding: 16,
      margin: 16,
    },
    components: {
      Card: {
        borderRadius: 8,
        boxShadow: designTokens.shadows.md,
      },
      Button: {
        borderRadius: 8,
        fontWeight: 600,
      },
      Table: {
        borderRadius: 8,
      },
    },
  };
};
// Default themes
export const lightTheme = createAppTheme('light');
export const darkTheme = createAppTheme('dark');

// Default theme (light mode)
export const theme = lightTheme;

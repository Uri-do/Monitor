import React, { createContext, useContext, useState, ReactNode } from 'react';
import type { ThemeConfig } from 'antd';
import { createAppTheme, type ThemeMode } from '@/theme/theme';

interface ThemeContextType {
  mode: ThemeMode;
  toggleTheme: () => void;
  theme: ThemeConfig;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

interface CustomThemeProviderProps {
  children: ReactNode;
}

export function CustomThemeProvider({ children }: CustomThemeProviderProps) {
  const [mode, setMode] = useState<ThemeMode>(() => {
    const savedMode = localStorage.getItem('themeMode');
    return (savedMode as ThemeMode) || 'light';
  });

  const theme = React.useMemo(() => createAppTheme(mode), [mode]);

  const toggleTheme = () => {
    const newMode = mode === 'light' ? 'dark' : 'light';
    setMode(newMode);
    localStorage.setItem('themeMode', newMode);
  };

  const value = {
    mode,
    toggleTheme,
    theme,
  };

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextType {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within a CustomThemeProvider');
  }
  return context;
}

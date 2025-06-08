import type { Preview } from '@storybook/react-vite';
import React from 'react';
import { CssBaseline } from '@mui/material';
import { CustomThemeProvider } from '../src/hooks/useTheme';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Create a client for Storybook
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: 0,
    },
  },
});

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    docs: {
      toc: true,
    },
    backgrounds: {
      default: 'light',
      values: [
        {
          name: 'light',
          value: '#f8fafc',
        },
        {
          name: 'dark',
          value: '#0a0e1a',
        },
      ],
    },
  },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <CustomThemeProvider>
          <CssBaseline />
          <div style={{ padding: '1rem' }}>
            <Story />
          </div>
        </CustomThemeProvider>
      </QueryClientProvider>
    ),
  ],
};

export default preview;
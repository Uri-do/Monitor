import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Toaster } from 'react-hot-toast'
import { HelmetProvider } from 'react-helmet-async'
import { ErrorBoundary } from 'react-error-boundary'

import { AppRoutes } from '@/routes'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { LoadingProvider } from '@/providers/LoadingProvider'
import { ThemeProvider } from '@/providers/ThemeProvider'
<!--#if (enableAuth)-->
import { AuthProvider } from '@/providers/AuthProvider'
<!--#endif-->
<!--#if (enableRealtime)-->
import { SignalRProvider } from '@/providers/SignalRProvider'
<!--#endif-->

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
      retry: (failureCount, error: any) => {
        // Don't retry on 4xx errors except 408, 429
        if (error?.response?.status >= 400 && error?.response?.status < 500) {
          if (error?.response?.status === 408 || error?.response?.status === 429) {
            return failureCount < 2
          }
          return false
        }
        // Retry up to 3 times for other errors
        return failureCount < 3
      },
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      retry: (failureCount, error: any) => {
        // Don't retry mutations on client errors
        if (error?.response?.status >= 400 && error?.response?.status < 500) {
          return false
        }
        // Retry up to 2 times for server errors
        return failureCount < 2
      },
    },
  },
})

function App() {
  return (
    <ErrorBoundary
      FallbackComponent={ErrorFallback}
      onError={(error, errorInfo) => {
        console.error('Application error:', error, errorInfo)
        // Here you could send error to monitoring service
      }}
    >
      <HelmetProvider>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <ThemeProvider>
              <LoadingProvider>
<!--#if (enableAuth)-->
                <AuthProvider>
<!--#endif-->
<!--#if (enableRealtime)-->
                  <SignalRProvider>
<!--#endif-->
                    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
                      <AppRoutes />
                    </div>
<!--#if (enableRealtime)-->
                  </SignalRProvider>
<!--#endif-->
<!--#if (enableAuth)-->
                </AuthProvider>
<!--#endif-->
              </LoadingProvider>
            </ThemeProvider>
          </BrowserRouter>

          {/* Toast notifications */}
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 4000,
              style: {
                background: '#363636',
                color: '#fff',
              },
              success: {
                duration: 3000,
                iconTheme: {
                  primary: '#22c55e',
                  secondary: '#fff',
                },
              },
              error: {
                duration: 5000,
                iconTheme: {
                  primary: '#ef4444',
                  secondary: '#fff',
                },
              },
            }}
          />

          {/* React Query DevTools */}
          {import.meta.env.DEV && (
            <ReactQueryDevtools
              initialIsOpen={false}
              position="bottom-right"
              buttonPosition="bottom-right"
            />
          )}
        </QueryClientProvider>
      </HelmetProvider>
    </ErrorBoundary>
  )
}

export default App

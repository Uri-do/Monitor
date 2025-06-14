import { useQuery, UseQueryOptions, UseQueryResult } from '@tanstack/react-query';
import { NotFoundError } from '@/services/BaseApiService';

// Standard query configuration presets
export const QueryPresets = {
  // Real-time data (alerts, dashboard)
  realtime: {
    staleTime: 30 * 1000, // 30 seconds
    refetchInterval: 60 * 1000, // 1 minute
    refetchOnWindowFocus: true,
    retry: 2,
  },

  // Frequently changing data (indicators, execution history)
  dynamic: {
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true,
    retry: 2,
  },

  // Stable reference data (users, roles, contacts)
  stable: {
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
    retry: 3,
  },

  // Static configuration data (rarely changes)
  static: {
    staleTime: 15 * 60 * 1000, // 15 minutes
    refetchInterval: false,
    refetchOnWindowFocus: false,
    retry: 3,
  },

  // One-time fetch (details, analytics)
  oneTime: {
    staleTime: 10 * 60 * 1000, // 10 minutes
    refetchInterval: false,
    refetchOnWindowFocus: false,
    retry: 1,
  },
} as const;

export type QueryPreset = keyof typeof QueryPresets;

// Query options with presets and fallbacks
export interface GenericQueryOptions<T> extends Omit<UseQueryOptions<T>, 'queryKey' | 'queryFn'> {
  // Preset configuration
  preset?: QueryPreset;

  // Fallback handling
  fallbackValue?: T;
  graceful404?: boolean;

  // Error handling
  logErrors?: boolean;
  errorContext?: string;

  // Conditional fetching
  dependencies?: (string | number | boolean | undefined | null)[];

  // Custom retry logic
  retryCondition?: (error: any, failureCount: number) => boolean;
}

// Generic query hook with comprehensive features
export function useGenericQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  options: GenericQueryOptions<T> = {}
): UseQueryResult<T> {
  const {
    preset = 'dynamic',
    fallbackValue,
    graceful404 = false,
    logErrors = true,
    errorContext,
    dependencies = [],
    retryCondition,
    ...customOptions
  } = options;

  // Get preset configuration
  const presetConfig = QueryPresets[preset];

  // Check if all dependencies are satisfied
  const enabled =
    dependencies.length === 0 ||
    dependencies.every(dep => dep !== undefined && dep !== null && dep !== '');

  // Query function with error handling
  const queryFn = async (): Promise<T> => {
    try {
      return await queryFn();
    } catch (error) {
      // Handle 404 errors gracefully if requested
      if (graceful404 && error instanceof NotFoundError) {
        if (logErrors) {
          console.warn(
            `404 error for query ${queryKey.join('.')}${errorContext ? ` (${errorContext})` : ''}, returning fallback value`
          );
        }
        return fallbackValue as T;
      }

      // Log errors if enabled
      if (logErrors) {
        console.error(
          `Query error for ${queryKey.join('.')}${errorContext ? ` (${errorContext})` : ''}:`,
          error
        );
      }

      throw error;
    }
  };

  // Retry logic
  const retryFn = (failureCount: number, error: any) => {
    // Use custom retry condition if provided
    if (retryCondition) {
      return retryCondition(error, failureCount);
    }

    // Don't retry 404 errors if graceful404 is enabled
    if (graceful404 && error instanceof NotFoundError) {
      return false;
    }

    // Don't retry client errors (4xx) except 408 (timeout)
    if (error?.status >= 400 && error?.status < 500 && error?.status !== 408) {
      return false;
    }

    // Use preset retry configuration
    const maxRetries = presetConfig.retry || 2;
    return failureCount < maxRetries;
  };

  // Merge configurations: preset -> custom options
  const finalOptions: UseQueryOptions<T> = {
    ...presetConfig,
    ...customOptions,
    queryKey: queryKey.filter(key => key !== undefined && key !== null),
    queryFn: queryFn,
    enabled: enabled && customOptions.enabled !== false,
    retry: retryFn,
    placeholderData: (previousData: T | undefined) => {
      // Use custom placeholder data if provided
      if (customOptions.placeholderData) {
        if (typeof customOptions.placeholderData === 'function') {
          return (customOptions.placeholderData as any)(previousData);
        }
        return customOptions.placeholderData;
      }

      // Default: keep previous data to prevent UI flickering
      return previousData;
    },
  };

  return useQuery(finalOptions);
}

// Specialized hooks for common patterns
export function useRealtimeQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  options: Omit<GenericQueryOptions<T>, 'preset'> = {}
) {
  return useGenericQuery(queryKey, queryFn, { ...options, preset: 'realtime' });
}

export function useStableQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  options: Omit<GenericQueryOptions<T>, 'preset'> = {}
) {
  return useGenericQuery(queryKey, queryFn, { ...options, preset: 'stable' });
}

export function useStaticQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  options: Omit<GenericQueryOptions<T>, 'preset'> = {}
) {
  return useGenericQuery(queryKey, queryFn, { ...options, preset: 'static' });
}

export function useOneTimeQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  options: Omit<GenericQueryOptions<T>, 'preset'> = {}
) {
  return useGenericQuery(queryKey, queryFn, { ...options, preset: 'oneTime' });
}

// Hook for queries that might return 404 (graceful fallback)
export function useOptionalQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  fallbackValue: T,
  options: Omit<GenericQueryOptions<T>, 'graceful404' | 'fallbackValue'> = {}
) {
  return useGenericQuery(queryKey, queryFn, {
    ...options,
    graceful404: true,
    fallbackValue,
  });
}

// Hook for conditional queries (only fetch when dependencies are met)
export function useConditionalQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  dependencies: (string | number | boolean | undefined | null)[],
  options: Omit<GenericQueryOptions<T>, 'dependencies'> = {}
) {
  return useGenericQuery(queryKey, queryFn, {
    ...options,
    dependencies,
  });
}

// Hook for paginated queries
export function usePaginatedQuery<T>(
  queryKey: (string | number | boolean | undefined | null)[],
  queryFn: () => Promise<T>,
  page: number,
  pageSize: number,
  options: GenericQueryOptions<T> = {}
) {
  return useGenericQuery([...queryKey, 'page', page, 'pageSize', pageSize], queryFn, {
    ...options,
    dependencies: [page, pageSize],
    // Keep previous data during pagination
    placeholderData: previousData => previousData,
  });
}

export default useGenericQuery;

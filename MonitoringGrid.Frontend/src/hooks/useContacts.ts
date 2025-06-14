import { useQuery } from '@tanstack/react-query';
import { contactApi } from '@/services/api';
// ContactDto available for future use
// import { ContactDto } from '@/types/api';
import { queryKeys } from '@/utils/queryKeys';

/**
 * useContacts hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useContacts = (filters?: { isActive?: boolean; search?: string }) => {
  return useQuery({
    queryKey: queryKeys.contacts.list(filters || {}),
    queryFn: async () => {
      try {
        return await contactApi.getContacts(filters);
      } catch (error: any) {
        // Handle 404 gracefully - API endpoints not implemented yet
        if (error?.response?.status === 404) {
          console.warn('Contact endpoints not yet implemented, returning empty array');
          return [];
        }
        throw error;
      }
    },
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 2 * 60 * 1000, // Consider data fresh for 2 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
    retry: (failureCount, error: any) => {
      // Don't retry 404 errors
      if (error?.response?.status === 404) {
        return false;
      }
      return failureCount < 3;
    },
  });
};

/**
 * Hook to fetch a single contact by ID
 */
export const useContact = (id: number) => {
  return useQuery({
    queryKey: queryKeys.contacts.detail(id),
    queryFn: () => contactApi.getContact(id),
    enabled: !!id && id > 0,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Hook to fetch active contacts (commonly used for dropdowns)
 */
export const useActiveContacts = () => {
  return useQuery({
    queryKey: queryKeys.contacts.list({ isActive: true }),
    queryFn: async () => {
      try {
        return await contactApi.getContacts({ isActive: true });
      } catch (error: any) {
        // Handle 404 gracefully - API endpoints not implemented yet
        if (error?.response?.status === 404) {
          console.warn('Contact endpoints not yet implemented, returning empty array');
          return [];
        }
        throw error;
      }
    },
    staleTime: 5 * 60 * 1000, // Active contacts change less frequently
    refetchInterval: 10 * 60 * 1000, // Refetch every 10 minutes
    retry: (failureCount, error: any) => {
      // Don't retry 404 errors
      if (error?.response?.status === 404) {
        return false;
      }
      return failureCount < 3;
    },
  });
};

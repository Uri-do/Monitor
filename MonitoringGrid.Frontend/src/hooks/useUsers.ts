import { useQuery } from '@tanstack/react-query';
import { userService, roleService } from '@/services';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useUsers hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useUsers = (isActive?: boolean) => {
  return useQuery({
    queryKey: queryKeys.users.list({ isActive }),
    queryFn: () => userService.getUsers(isActive),
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 2 * 60 * 1000, // Consider data fresh for 2 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
  });
};

/**
 * Hook to fetch a single user by ID
 */
export const useUser = (id: string) => {
  return useQuery({
    queryKey: queryKeys.users.detail(id),
    queryFn: () => userService.getUser(id),
    enabled: !!id,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Hook to fetch current user profile
 */
export const useUserProfile = () => {
  return useQuery({
    queryKey: queryKeys.users.profile(),
    queryFn: () => userService.getCurrentUser(),
    staleTime: 5 * 60 * 1000, // Profile data changes less frequently
    refetchInterval: 10 * 60 * 1000, // Refetch every 10 minutes
  });
};

/**
 * Hook to fetch all roles
 */
export const useRoles = () => {
  return useQuery({
    queryKey: queryKeys.users.roles(),
    queryFn: () => roleService.getRoles(),
    staleTime: 10 * 60 * 1000, // Roles change very infrequently
    refetchInterval: 30 * 60 * 1000, // Refetch every 30 minutes
  });
};

/**
 * Hook to fetch a single role by ID
 */
export const useRole = (id: string) => {
  return useQuery({
    queryKey: ['role', id],
    queryFn: () => roleService.getRole(id),
    enabled: !!id,
    staleTime: 10 * 60 * 1000,
  });
};

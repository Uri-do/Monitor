import { useQuery } from '@tanstack/react-query';
import { roleService, userService } from '@/services';
import { queryKeys } from '@/utils/queryKeys';

/**
 * Enhanced useRoles hook using TanStack Query
 * Provides automatic caching, background refetching, and error handling
 */
export const useRoles = () => {
  return useQuery({
    queryKey: queryKeys.roles.list(),
    queryFn: () => roleService.getRoles(),
    placeholderData: previousData => previousData, // Prevents UI flickering during refetch
    staleTime: 5 * 60 * 1000, // Consider data fresh for 5 minutes
    refetchInterval: 10 * 60 * 1000, // Auto-refetch every 10 minutes
  });
};

/**
 * Enhanced useRole hook for single role details
 */
export const useRole = (roleId: string) => {
  return useQuery({
    queryKey: queryKeys.roles.detail(roleId),
    queryFn: () => roleService.getRoleById(roleId),
    enabled: !!roleId,
    staleTime: 5 * 60 * 1000,
    refetchInterval: 10 * 60 * 1000,
  });
};

/**
 * Enhanced usePermissions hook for all permissions
 */
export const usePermissions = () => {
  return useQuery({
    queryKey: queryKeys.permissions.list(),
    queryFn: () => roleService.getAllPermissions(),
    placeholderData: previousData => previousData,
    staleTime: 10 * 60 * 1000, // Permissions change rarely, cache for 10 minutes
    refetchInterval: 30 * 60 * 1000, // Auto-refetch every 30 minutes
  });
};

/**
 * Enhanced useUserRoles hook for user's roles
 */
export const useUserRoles = (userId: string) => {
  return useQuery({
    queryKey: queryKeys.users.roles(userId),
    queryFn: () => userService.getUserRoles(userId),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
    refetchInterval: 10 * 60 * 1000,
  });
};

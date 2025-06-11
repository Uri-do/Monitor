import { useMutation, useQueryClient } from '@tanstack/react-query';
import { roleService } from '@/services';
import { queryKeys } from '@/utils/queryKeys';
import { CreateRoleRequest, UpdateRoleRequest } from '@/types/api';
import toast from 'react-hot-toast';

/**
 * Hook for creating a new role
 */
export const useCreateRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRoleRequest) => roleService.createRole(data),
    onSuccess: newRole => {
      toast.success('Role created successfully');

      // Invalidate roles list to show the new role
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.lists() });

      // Optionally set the new role in cache
      queryClient.setQueryData(queryKeys.roles.detail(newRole.roleId), newRole);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create role');
    },
  });
};

/**
 * Hook for updating an existing role
 */
export const useUpdateRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ roleId, data }: { roleId: string; data: UpdateRoleRequest }) =>
      roleService.updateRole(roleId, data),
    onSuccess: (updatedRole, { roleId }) => {
      toast.success('Role updated successfully');

      // Update the specific role in cache
      queryClient.setQueryData(queryKeys.roles.detail(roleId), updatedRole);

      // Invalidate roles list to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update role');
    },
  });
};

/**
 * Hook for deleting a role
 */
export const useDeleteRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roleId: string) => roleService.deleteRole(roleId),
    onSuccess: (_, roleId) => {
      toast.success('Role deleted successfully');

      // Remove the role from cache
      queryClient.removeQueries({ queryKey: queryKeys.roles.detail(roleId) });

      // Invalidate roles list to reflect deletion
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to delete role');
    },
  });
};

/**
 * Hook for assigning roles to a user
 */
export const useAssignUserRoles = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, roleIds }: { userId: string; roleIds: string[] }) =>
      roleService.assignUserRoles?.(userId, roleIds) || Promise.resolve(),
    onSuccess: (_, { userId }) => {
      toast.success('User roles updated successfully');

      // Invalidate user roles cache
      queryClient.invalidateQueries({ queryKey: queryKeys.users.roles(userId) });

      // Invalidate users list to reflect role changes
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update user roles');
    },
  });
};

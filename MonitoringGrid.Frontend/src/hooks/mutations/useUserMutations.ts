import { useMutation, useQueryClient } from '@tanstack/react-query';
import { userService, roleService } from '@/services';
import { queryKeys } from '@/utils/queryKeys';
import toast from 'react-hot-toast';

/**
 * Hook for creating a new user
 */
export const useCreateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: userService.createUser,
    onSuccess: (newUser) => {
      toast.success('User created successfully');
      
      // Invalidate user lists to show the new user
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
      
      // Optionally set the new user in cache
      queryClient.setQueryData(queryKeys.users.detail(newUser.id), newUser);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create user');
    },
  });
};

/**
 * Hook for updating an existing user
 */
export const useUpdateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: userService.updateUser,
    onSuccess: (updatedUser, variables) => {
      toast.success('User updated successfully');
      
      // Update the specific user in cache
      queryClient.setQueryData(queryKeys.users.detail(variables.id), updatedUser);
      
      // Invalidate user lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
      
      // If this is the current user, update profile cache
      queryClient.invalidateQueries({ queryKey: queryKeys.users.profile() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update user');
    },
  });
};

/**
 * Hook for deleting a user
 */
export const useDeleteUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => userService.deleteUser(userId),
    onSuccess: (_, userId) => {
      toast.success('User deleted successfully');
      
      // Remove the user from cache
      queryClient.removeQueries({ queryKey: queryKeys.users.detail(userId) });
      
      // Invalidate user lists
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to delete user');
    },
  });
};

/**
 * Hook for updating user password
 */
export const useUpdateUserPassword = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: userService.updatePassword,
    onSuccess: () => {
      toast.success('Password updated successfully');
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update password');
    },
  });
};

/**
 * Hook for creating a new role
 */
export const useCreateRole = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: roleService.createRole,
    onSuccess: (newRole) => {
      toast.success('Role created successfully');
      
      // Invalidate roles to show the new role
      queryClient.invalidateQueries({ queryKey: queryKeys.users.roles() });
      
      // Optionally set the new role in cache
      queryClient.setQueryData(['role', newRole.id], newRole);
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
    mutationFn: roleService.updateRole,
    onSuccess: (updatedRole, variables) => {
      toast.success('Role updated successfully');
      
      // Update the specific role in cache
      queryClient.setQueryData(['role', variables.id], updatedRole);
      
      // Invalidate roles list to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.users.roles() });
      
      // Invalidate user lists as role changes affect user data
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
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
      queryClient.removeQueries({ queryKey: ['role', roleId] });
      
      // Invalidate roles list
      queryClient.invalidateQueries({ queryKey: queryKeys.users.roles() });
      
      // Invalidate user lists as role deletion affects user data
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to delete role');
    },
  });
};

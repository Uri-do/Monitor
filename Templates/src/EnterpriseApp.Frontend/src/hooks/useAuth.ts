import { useContext } from 'react'
import { useAuth as useAuthContext } from '@/providers/AuthProvider'

// Re-export the useAuth hook from the provider for convenience
export const useAuth = useAuthContext

// Additional auth-related hooks

/**
 * Hook to check if user has specific role
 */
export function useRole(role: string) {
  const { hasRole } = useAuth()
  return hasRole(role as any)
}

/**
 * Hook to check if user has specific permission
 */
export function usePermission(permission: string) {
  const { hasPermission } = useAuth()
  return hasPermission(permission as any)
}

/**
 * Hook to check if user has any of the specified roles
 */
export function useAnyRole(roles: string[]) {
  const { hasAnyRole } = useAuth()
  return hasAnyRole(roles as any)
}

/**
 * Hook to check if user has any of the specified permissions
 */
export function useAnyPermission(permissions: string[]) {
  const { hasAnyPermission } = useAuth()
  return hasAnyPermission(permissions as any)
}

/**
 * Hook to get user's display name
 */
export function useUserDisplayName() {
  const { user } = useAuth()
  
  if (!user) return null
  
  if (user.firstName && user.lastName) {
    return `${user.firstName} ${user.lastName}`
  }
  
  return user.name || user.email
}

/**
 * Hook to get user's initials
 */
export function useUserInitials() {
  const { user } = useAuth()
  
  if (!user) return null
  
  if (user.firstName && user.lastName) {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
  }
  
  if (user.name) {
    const nameParts = user.name.split(' ')
    if (nameParts.length >= 2) {
      return `${nameParts[0].charAt(0)}${nameParts[1].charAt(0)}`.toUpperCase()
    }
    return user.name.charAt(0).toUpperCase()
  }
  
  return user.email.charAt(0).toUpperCase()
}

/**
 * Hook to check if user's email is verified
 */
export function useEmailVerified() {
  const { user } = useAuth()
  return user?.isEmailVerified ?? false
}

/**
 * Hook to check if user is admin
 */
export function useIsAdmin() {
  return useRole('Admin')
}

/**
 * Hook to check if user is super admin
 */
export function useIsSuperAdmin() {
  return useRole('SuperAdmin')
}

/**
 * Hook to check if user can manage users
 */
export function useCanManageUsers() {
  return useAnyPermission(['user:write', 'admin:write'])
}

/**
 * Hook to check if user can manage domain entities
 */
export function useCanManageDomainEntities() {
  return useAnyPermission(['domain-entity:write'])
}

/**
 * Hook to check if user can access worker management
 */
export function useCanAccessWorker() {
  return useAnyRole(['Admin', 'SuperAdmin'])
}

/**
 * Hook to check if user can access system settings
 */
export function useCanAccessSystemSettings() {
  return useAnyPermission(['system:write', 'admin:write'])
}

export default useAuth

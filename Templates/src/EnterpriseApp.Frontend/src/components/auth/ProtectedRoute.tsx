import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/providers/AuthProvider'
import { ProtectedRouteProps } from '@/types/auth'
import { LoadingSpinner } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

export function ProtectedRoute({
  children,
  requiredRole,
  requiredPermission,
  requiredRoles = [],
  requiredPermissions = [],
  requireAll = false,
  fallback,
  redirectTo = '/login',
}: ProtectedRouteProps) {
  const { 
    isAuthenticated, 
    isLoading, 
    user, 
    hasRole, 
    hasPermission, 
    hasAnyRole, 
    hasAnyPermission 
  } = useAuth()
  const location = useLocation()

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return (
      <Navigate 
        to={redirectTo} 
        state={{ from: location }} 
        replace 
      />
    )
  }

  // Check role requirements
  const roleRequirements = [
    ...(requiredRole ? [requiredRole] : []),
    ...requiredRoles,
  ]

  // Check permission requirements
  const permissionRequirements = [
    ...(requiredPermission ? [requiredPermission] : []),
    ...requiredPermissions,
  ]

  // Determine if user meets requirements
  let hasRequiredAccess = true

  if (roleRequirements.length > 0) {
    if (requireAll) {
      // User must have ALL specified roles
      hasRequiredAccess = roleRequirements.every(role => hasRole(role))
    } else {
      // User must have ANY of the specified roles
      hasRequiredAccess = hasAnyRole(roleRequirements)
    }
  }

  if (hasRequiredAccess && permissionRequirements.length > 0) {
    if (requireAll) {
      // User must have ALL specified permissions
      hasRequiredAccess = permissionRequirements.every(permission => hasPermission(permission))
    } else {
      // User must have ANY of the specified permissions
      hasRequiredAccess = hasAnyPermission(permissionRequirements)
    }
  }

  // Show fallback or access denied if user doesn't meet requirements
  if (!hasRequiredAccess) {
    if (fallback) {
      return <>{fallback}</>
    }

    return (
      <div className="flex items-center justify-center min-h-screen p-4">
        <div className="max-w-md w-full">
          <Alert
            variant="error"
            title="Access Denied"
            description="You don't have permission to access this page. Please contact your administrator if you believe this is an error."
          />
        </div>
      </div>
    )
  }

  // User is authenticated and has required permissions
  return <>{children}</>
}

// Higher-order component for protecting routes
export function withAuth<P extends object>(
  Component: React.ComponentType<P>,
  options?: Omit<ProtectedRouteProps, 'children'>
) {
  return function AuthenticatedComponent(props: P) {
    return (
      <ProtectedRoute {...options}>
        <Component {...props} />
      </ProtectedRoute>
    )
  }
}

// Hook for conditional rendering based on permissions
export function usePermissions() {
  const { hasRole, hasPermission, hasAnyRole, hasAnyPermission } = useAuth()

  const canAccess = (requirements: {
    roles?: string[]
    permissions?: string[]
    requireAll?: boolean
  }) => {
    const { roles = [], permissions = [], requireAll = false } = requirements

    let hasRequiredAccess = true

    if (roles.length > 0) {
      if (requireAll) {
        hasRequiredAccess = roles.every(role => hasRole(role as any))
      } else {
        hasRequiredAccess = hasAnyRole(roles as any)
      }
    }

    if (hasRequiredAccess && permissions.length > 0) {
      if (requireAll) {
        hasRequiredAccess = permissions.every(permission => hasPermission(permission as any))
      } else {
        hasRequiredAccess = hasAnyPermission(permissions as any)
      }
    }

    return hasRequiredAccess
  }

  return {
    hasRole,
    hasPermission,
    hasAnyRole,
    hasAnyPermission,
    canAccess,
  }
}

// Component for conditional rendering
interface ConditionalRenderProps {
  children: React.ReactNode
  fallback?: React.ReactNode
  roles?: string[]
  permissions?: string[]
  requireAll?: boolean
}

export function ConditionalRender({
  children,
  fallback = null,
  roles = [],
  permissions = [],
  requireAll = false,
}: ConditionalRenderProps) {
  const { canAccess } = usePermissions()

  const hasAccess = canAccess({ roles, permissions, requireAll })

  return hasAccess ? <>{children}</> : <>{fallback}</>
}

// Email verification guard
export function EmailVerificationGuard({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()

  if (!user?.isEmailVerified) {
    return (
      <div className="flex items-center justify-center min-h-screen p-4">
        <div className="max-w-md w-full">
          <Alert
            variant="warning"
            title="Email Verification Required"
            description="Please verify your email address to continue. Check your inbox for a verification link."
          />
        </div>
      </div>
    )
  }

  return <>{children}</>
}

// Admin route guard
export function AdminRoute({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute requiredRole="Admin">
      {children}
    </ProtectedRoute>
  )
}

// Super admin route guard
export function SuperAdminRoute({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute requiredRole="SuperAdmin">
      {children}
    </ProtectedRoute>
  )
}

// Manager route guard
export function ManagerRoute({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Manager']}>
      {children}
    </ProtectedRoute>
  )
}

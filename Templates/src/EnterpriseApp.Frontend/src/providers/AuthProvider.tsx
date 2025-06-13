import React, { createContext, useContext, useEffect, useReducer, useCallback } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import toast from 'react-hot-toast'

import {
  AuthContextType,
  AuthState,
  User,
  AuthTokens,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  VerifyEmailRequest,
  VerifyEmailResponse,
  ChangePasswordRequest,
  ChangePasswordResponse,
  UpdateProfileRequest,
  UpdateProfileResponse,
  Role,
  Permission,
} from '@/types/auth'
import authService from '@/services/authService'

// Auth Actions
type AuthAction =
  | { type: 'AUTH_START' }
  | { type: 'AUTH_SUCCESS'; payload: { user: User; tokens: AuthTokens } }
  | { type: 'AUTH_FAILURE'; payload: string }
  | { type: 'AUTH_LOGOUT' }
  | { type: 'AUTH_CLEAR_ERROR' }
  | { type: 'AUTH_UPDATE_USER'; payload: User }
  | { type: 'AUTH_UPDATE_TOKENS'; payload: AuthTokens }

// Initial state
const initialState: AuthState = {
  user: null,
  tokens: null,
  isAuthenticated: false,
  isLoading: true, // Start with loading to check existing session
  error: null,
}

// Auth reducer
function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'AUTH_START':
      return {
        ...state,
        isLoading: true,
        error: null,
      }

    case 'AUTH_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        tokens: action.payload.tokens,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      }

    case 'AUTH_FAILURE':
      return {
        ...state,
        user: null,
        tokens: null,
        isAuthenticated: false,
        isLoading: false,
        error: action.payload,
      }

    case 'AUTH_LOGOUT':
      return {
        ...state,
        user: null,
        tokens: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      }

    case 'AUTH_CLEAR_ERROR':
      return {
        ...state,
        error: null,
      }

    case 'AUTH_UPDATE_USER':
      return {
        ...state,
        user: action.payload,
      }

    case 'AUTH_UPDATE_TOKENS':
      return {
        ...state,
        tokens: action.payload,
      }

    default:
      return state
  }
}

// Create context
const AuthContext = createContext<AuthContextType | undefined>(undefined)

// Auth Provider Props
interface AuthProviderProps {
  children: React.ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, dispatch] = useReducer(authReducer, initialState)
  const navigate = useNavigate()
  const location = useLocation()

  // Initialize auth state on mount
  useEffect(() => {
    const initializeAuth = async () => {
      try {
        if (authService.isAuthenticated()) {
          // Try to get current user to validate session
          const user = await authService.getCurrentUser()
          const tokens = authService.getStoredTokens()
          
          if (user && tokens) {
            dispatch({
              type: 'AUTH_SUCCESS',
              payload: { user, tokens },
            })
          } else {
            dispatch({ type: 'AUTH_LOGOUT' })
          }
        } else {
          dispatch({ type: 'AUTH_LOGOUT' })
        }
      } catch (error) {
        console.error('Auth initialization failed:', error)
        dispatch({ type: 'AUTH_LOGOUT' })
      }
    }

    initializeAuth()
  }, [])

  // Auto token refresh
  useEffect(() => {
    if (!state.isAuthenticated) return

    const interval = setInterval(async () => {
      try {
        if (authService.shouldRefreshToken()) {
          const tokens = await authService.refreshTokens()
          dispatch({ type: 'AUTH_UPDATE_TOKENS', payload: tokens })
        }
      } catch (error) {
        console.error('Token refresh failed:', error)
        await logout()
      }
    }, 60000) // Check every minute

    return () => clearInterval(interval)
  }, [state.isAuthenticated])

  // Login
  const login = useCallback(async (credentials: LoginRequest): Promise<LoginResponse> => {
    dispatch({ type: 'AUTH_START' })

    try {
      const response = await authService.login(credentials)
      
      dispatch({
        type: 'AUTH_SUCCESS',
        payload: {
          user: response.user,
          tokens: response.tokens,
        },
      })

      toast.success('Welcome back!')

      // Redirect to intended page or dashboard
      const from = (location.state as any)?.from?.pathname || '/'
      navigate(from, { replace: true })

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Login failed'
      dispatch({ type: 'AUTH_FAILURE', payload: errorMessage })
      toast.error(errorMessage)
      throw error
    }
  }, [navigate, location])

  // Register
  const register = useCallback(async (data: RegisterRequest): Promise<RegisterResponse> => {
    dispatch({ type: 'AUTH_START' })

    try {
      const response = await authService.register(data)

      if (response.tokens) {
        // Registration complete, user is logged in
        dispatch({
          type: 'AUTH_SUCCESS',
          payload: {
            user: response.user,
            tokens: response.tokens,
          },
        })

        toast.success('Account created successfully!')
        navigate('/', { replace: true })
      } else {
        // Registration successful but requires email verification
        dispatch({ type: 'AUTH_LOGOUT' })
        toast.success('Account created! Please check your email to verify your account.')
        navigate('/login', { replace: true })
      }

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Registration failed'
      dispatch({ type: 'AUTH_FAILURE', payload: errorMessage })
      toast.error(errorMessage)
      throw error
    }
  }, [navigate])

  // Logout
  const logout = useCallback(async (): Promise<void> => {
    try {
      await authService.logout()
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      dispatch({ type: 'AUTH_LOGOUT' })
      toast.success('Logged out successfully')
      navigate('/login', { replace: true })
    }
  }, [navigate])

  // Forgot Password
  const forgotPassword = useCallback(async (email: string): Promise<ForgotPasswordResponse> => {
    try {
      const response = await authService.forgotPassword(email)
      toast.success('Password reset instructions sent to your email')
      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to send reset email'
      toast.error(errorMessage)
      throw error
    }
  }, [])

  // Reset Password
  const resetPassword = useCallback(async (data: ResetPasswordRequest): Promise<ResetPasswordResponse> => {
    try {
      const response = await authService.resetPassword(data)
      
      if (response.success) {
        toast.success('Password reset successfully')
        navigate('/login', { replace: true })
      }

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Password reset failed'
      toast.error(errorMessage)
      throw error
    }
  }, [navigate])

  // Verify Email
  const verifyEmail = useCallback(async (token: string): Promise<VerifyEmailResponse> => {
    try {
      const response = await authService.verifyEmail(token)
      
      if (response.success) {
        toast.success('Email verified successfully')
        
        // Update user if currently authenticated
        if (state.user) {
          dispatch({
            type: 'AUTH_UPDATE_USER',
            payload: { ...state.user, isEmailVerified: true },
          })
        }
      }

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Email verification failed'
      toast.error(errorMessage)
      throw error
    }
  }, [state.user])

  // Change Password
  const changePassword = useCallback(async (data: ChangePasswordRequest): Promise<ChangePasswordResponse> => {
    try {
      const response = await authService.changePassword(data)
      
      if (response.success) {
        toast.success('Password changed successfully')
      }

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Password change failed'
      toast.error(errorMessage)
      throw error
    }
  }, [])

  // Update Profile
  const updateProfile = useCallback(async (data: UpdateProfileRequest): Promise<UpdateProfileResponse> => {
    try {
      const response = await authService.updateProfile(data)
      
      dispatch({ type: 'AUTH_UPDATE_USER', payload: response.user })
      toast.success('Profile updated successfully')

      return response
    } catch (error: any) {
      const errorMessage = error.message || 'Profile update failed'
      toast.error(errorMessage)
      throw error
    }
  }, [])

  // Refresh Tokens
  const refreshTokens = useCallback(async (): Promise<AuthTokens> => {
    try {
      const tokens = await authService.refreshTokens()
      dispatch({ type: 'AUTH_UPDATE_TOKENS', payload: tokens })
      return tokens
    } catch (error: any) {
      await logout()
      throw error
    }
  }, [logout])

  // Permission and Role Checks
  const hasRole = useCallback((role: Role): boolean => {
    return state.user?.roles.includes(role) ?? false
  }, [state.user])

  const hasPermission = useCallback((permission: Permission): boolean => {
    return state.user?.permissions.includes(permission) ?? false
  }, [state.user])

  const hasAnyRole = useCallback((roles: Role[]): boolean => {
    return roles.some(role => hasRole(role))
  }, [hasRole])

  const hasAnyPermission = useCallback((permissions: Permission[]): boolean => {
    return permissions.some(permission => hasPermission(permission))
  }, [hasPermission])

  // Clear Error
  const clearError = useCallback(() => {
    dispatch({ type: 'AUTH_CLEAR_ERROR' })
  }, [])

  // Context value
  const value: AuthContextType = {
    // State
    user: state.user,
    tokens: state.tokens,
    isAuthenticated: state.isAuthenticated,
    isLoading: state.isLoading,
    error: state.error,

    // Actions
    login,
    register,
    logout,
    forgotPassword,
    resetPassword,
    verifyEmail,
    changePassword,
    updateProfile,
    refreshTokens,

    // Utilities
    hasRole,
    hasPermission,
    hasAnyRole,
    hasAnyPermission,
    clearError,
  }

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}

// Custom hook to use auth context
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext)
  
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  
  return context
}

export default AuthProvider

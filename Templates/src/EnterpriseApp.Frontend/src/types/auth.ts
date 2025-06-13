// Authentication types and interfaces

export interface User {
  id: string
  email: string
  name: string
  firstName?: string
  lastName?: string
  avatar?: string
  roles: string[]
  permissions: string[]
  isEmailVerified: boolean
  lastLoginAt?: string
  createdAt: string
  updatedAt: string
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
  expiresAt: number
  tokenType: 'Bearer'
}

export interface AuthState {
  user: User | null
  tokens: AuthTokens | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}

// Login/Register DTOs
export interface LoginRequest {
  email: string
  password: string
  rememberMe?: boolean
}

export interface LoginResponse {
  user: User
  tokens: AuthTokens
  message?: string
}

export interface RegisterRequest {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  acceptTerms: boolean
}

export interface RegisterResponse {
  user: User
  tokens: AuthTokens
  message?: string
  requiresEmailVerification?: boolean
}

// Password Reset
export interface ForgotPasswordRequest {
  email: string
}

export interface ForgotPasswordResponse {
  message: string
  resetTokenSent: boolean
}

export interface ResetPasswordRequest {
  token: string
  password: string
  confirmPassword: string
}

export interface ResetPasswordResponse {
  message: string
  success: boolean
}

// Email Verification
export interface VerifyEmailRequest {
  token: string
}

export interface VerifyEmailResponse {
  message: string
  success: boolean
}

// Change Password
export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export interface ChangePasswordResponse {
  message: string
  success: boolean
}

// Profile Update
export interface UpdateProfileRequest {
  firstName?: string
  lastName?: string
  avatar?: string
}

export interface UpdateProfileResponse {
  user: User
  message?: string
}

// Auth Errors
export interface AuthError {
  code: string
  message: string
  field?: string
  details?: Record<string, any>
}

export interface ValidationError {
  field: string
  message: string
}

// Auth Events
export type AuthEvent = 
  | 'login'
  | 'logout'
  | 'register'
  | 'password-reset'
  | 'email-verified'
  | 'profile-updated'
  | 'token-refreshed'
  | 'session-expired'

// Permission and Role types
export type Permission = 
  | 'user:read'
  | 'user:write'
  | 'user:delete'
  | 'admin:read'
  | 'admin:write'
  | 'domain-entity:read'
  | 'domain-entity:write'
  | 'domain-entity:delete'
  | 'worker:read'
  | 'worker:write'
  | 'system:read'
  | 'system:write'

export type Role = 
  | 'User'
  | 'Admin'
  | 'SuperAdmin'
  | 'Manager'
  | 'Viewer'

// Auth Configuration
export interface AuthConfig {
  apiBaseUrl: string
  tokenStorageKey: string
  refreshTokenStorageKey: string
  autoRefreshTokens: boolean
  tokenRefreshThreshold: number // minutes before expiry
  sessionTimeout: number // minutes
  maxLoginAttempts: number
  lockoutDuration: number // minutes
  passwordMinLength: number
  passwordRequireUppercase: boolean
  passwordRequireLowercase: boolean
  passwordRequireNumbers: boolean
  passwordRequireSpecialChars: boolean
}

// Auth Context Types
export interface AuthContextType {
  // State
  user: User | null
  tokens: AuthTokens | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null

  // Actions
  login: (credentials: LoginRequest) => Promise<LoginResponse>
  register: (data: RegisterRequest) => Promise<RegisterResponse>
  logout: () => Promise<void>
  forgotPassword: (email: string) => Promise<ForgotPasswordResponse>
  resetPassword: (data: ResetPasswordRequest) => Promise<ResetPasswordResponse>
  verifyEmail: (token: string) => Promise<VerifyEmailResponse>
  changePassword: (data: ChangePasswordRequest) => Promise<ChangePasswordResponse>
  updateProfile: (data: UpdateProfileRequest) => Promise<UpdateProfileResponse>
  refreshTokens: () => Promise<AuthTokens>
  
  // Utilities
  hasRole: (role: Role) => boolean
  hasPermission: (permission: Permission) => boolean
  hasAnyRole: (roles: Role[]) => boolean
  hasAnyPermission: (permissions: Permission[]) => boolean
  clearError: () => void
}

// Route Protection
export interface ProtectedRouteProps {
  children: React.ReactNode
  requiredRole?: Role
  requiredPermission?: Permission
  requiredRoles?: Role[]
  requiredPermissions?: Permission[]
  requireAll?: boolean // true = AND logic, false = OR logic
  fallback?: React.ReactNode
  redirectTo?: string
}

// Auth Hook Options
export interface UseAuthOptions {
  redirectOnLogin?: string
  redirectOnLogout?: string
  autoRefresh?: boolean
  persistSession?: boolean
}

// Social Auth (if needed)
export interface SocialAuthProvider {
  id: string
  name: string
  icon: string
  color: string
}

export interface SocialAuthRequest {
  provider: string
  code: string
  state?: string
}

export interface SocialAuthResponse {
  user: User
  tokens: AuthTokens
  isNewUser: boolean
  message?: string
}

// Two-Factor Authentication (if needed)
export interface TwoFactorSetupRequest {
  password: string
}

export interface TwoFactorSetupResponse {
  qrCode: string
  secret: string
  backupCodes: string[]
}

export interface TwoFactorVerifyRequest {
  code: string
  password?: string
}

export interface TwoFactorVerifyResponse {
  success: boolean
  message: string
  backupCodes?: string[]
}

// Session Management
export interface SessionInfo {
  id: string
  deviceName: string
  ipAddress: string
  userAgent: string
  lastActivity: string
  isCurrent: boolean
}

export interface SessionsResponse {
  sessions: SessionInfo[]
  currentSessionId: string
}

// Auth Storage Interface
export interface AuthStorage {
  getTokens: () => AuthTokens | null
  setTokens: (tokens: AuthTokens) => void
  removeTokens: () => void
  getUser: () => User | null
  setUser: (user: User) => void
  removeUser: () => void
  clear: () => void
}

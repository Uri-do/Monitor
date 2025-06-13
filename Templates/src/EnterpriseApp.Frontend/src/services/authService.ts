import axios, { AxiosInstance, AxiosResponse } from 'axios'
import {
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
  AuthTokens,
  User,
  AuthConfig,
  SessionsResponse,
} from '@/types/auth'

class AuthService {
  private api: AxiosInstance
  private config: AuthConfig

  constructor(config: AuthConfig) {
    this.config = config
    this.api = axios.create({
      baseURL: config.apiBaseUrl,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Request interceptor to add auth token
    this.api.interceptors.request.use(
      (config) => {
        const tokens = this.getStoredTokens()
        if (tokens?.accessToken) {
          config.headers.Authorization = `${tokens.tokenType} ${tokens.accessToken}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    // Response interceptor for token refresh
    this.api.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true

          try {
            const tokens = await this.refreshTokens()
            originalRequest.headers.Authorization = `${tokens.tokenType} ${tokens.accessToken}`
            return this.api(originalRequest)
          } catch (refreshError) {
            // Refresh failed, redirect to login
            this.clearStoredAuth()
            window.location.href = '/login'
            return Promise.reject(refreshError)
          }
        }

        return Promise.reject(error)
      }
    )
  }

  // Authentication Methods
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    try {
      const response: AxiosResponse<LoginResponse> = await this.api.post('/auth/login', credentials)
      const { user, tokens } = response.data

      // Store tokens and user
      this.storeTokens(tokens)
      this.storeUser(user)

      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async register(data: RegisterRequest): Promise<RegisterResponse> {
    try {
      const response: AxiosResponse<RegisterResponse> = await this.api.post('/auth/register', data)
      const { user, tokens } = response.data

      // Store tokens and user if registration is complete
      if (tokens) {
        this.storeTokens(tokens)
        this.storeUser(user)
      }

      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async logout(): Promise<void> {
    try {
      const tokens = this.getStoredTokens()
      if (tokens?.refreshToken) {
        await this.api.post('/auth/logout', { refreshToken: tokens.refreshToken })
      }
    } catch (error) {
      console.warn('Logout request failed:', error)
    } finally {
      this.clearStoredAuth()
    }
  }

  async forgotPassword(email: string): Promise<ForgotPasswordResponse> {
    try {
      const response: AxiosResponse<ForgotPasswordResponse> = await this.api.post('/auth/forgot-password', { email })
      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async resetPassword(data: ResetPasswordRequest): Promise<ResetPasswordResponse> {
    try {
      const response: AxiosResponse<ResetPasswordResponse> = await this.api.post('/auth/reset-password', data)
      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async verifyEmail(token: string): Promise<VerifyEmailResponse> {
    try {
      const response: AxiosResponse<VerifyEmailResponse> = await this.api.post('/auth/verify-email', { token })
      
      // Update stored user if verification successful
      if (response.data.success) {
        const user = this.getStoredUser()
        if (user) {
          this.storeUser({ ...user, isEmailVerified: true })
        }
      }

      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async changePassword(data: ChangePasswordRequest): Promise<ChangePasswordResponse> {
    try {
      const response: AxiosResponse<ChangePasswordResponse> = await this.api.post('/auth/change-password', data)
      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async updateProfile(data: UpdateProfileRequest): Promise<UpdateProfileResponse> {
    try {
      const response: AxiosResponse<UpdateProfileResponse> = await this.api.put('/auth/profile', data)
      
      // Update stored user
      this.storeUser(response.data.user)

      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async refreshTokens(): Promise<AuthTokens> {
    try {
      const tokens = this.getStoredTokens()
      if (!tokens?.refreshToken) {
        throw new Error('No refresh token available')
      }

      const response: AxiosResponse<{ tokens: AuthTokens }> = await this.api.post('/auth/refresh', {
        refreshToken: tokens.refreshToken,
      })

      const newTokens = response.data.tokens
      this.storeTokens(newTokens)

      return newTokens
    } catch (error) {
      this.clearStoredAuth()
      throw this.handleAuthError(error)
    }
  }

  async getCurrentUser(): Promise<User> {
    try {
      const response: AxiosResponse<{ user: User }> = await this.api.get('/auth/me')
      
      // Update stored user
      this.storeUser(response.data.user)

      return response.data.user
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async getSessions(): Promise<SessionsResponse> {
    try {
      const response: AxiosResponse<SessionsResponse> = await this.api.get('/auth/sessions')
      return response.data
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  async revokeSession(sessionId: string): Promise<void> {
    try {
      await this.api.delete(`/auth/sessions/${sessionId}`)
    } catch (error) {
      throw this.handleAuthError(error)
    }
  }

  // Token Management
  private storeTokens(tokens: AuthTokens): void {
    localStorage.setItem(this.config.tokenStorageKey, JSON.stringify(tokens))
  }

  private getStoredTokens(): AuthTokens | null {
    try {
      const stored = localStorage.getItem(this.config.tokenStorageKey)
      return stored ? JSON.parse(stored) : null
    } catch {
      return null
    }
  }

  private storeUser(user: User): void {
    localStorage.setItem('auth_user', JSON.stringify(user))
  }

  private getStoredUser(): User | null {
    try {
      const stored = localStorage.getItem('auth_user')
      return stored ? JSON.parse(stored) : null
    } catch {
      return null
    }
  }

  private clearStoredAuth(): void {
    localStorage.removeItem(this.config.tokenStorageKey)
    localStorage.removeItem('auth_user')
  }

  // Utility Methods
  isTokenExpired(token?: AuthTokens): boolean {
    const tokens = token || this.getStoredTokens()
    if (!tokens) return true
    
    const now = Date.now()
    const expiresAt = tokens.expiresAt * 1000 // Convert to milliseconds
    const threshold = this.config.tokenRefreshThreshold * 60 * 1000 // Convert to milliseconds
    
    return now >= (expiresAt - threshold)
  }

  shouldRefreshToken(): boolean {
    const tokens = this.getStoredTokens()
    return tokens ? this.isTokenExpired(tokens) : false
  }

  isAuthenticated(): boolean {
    const tokens = this.getStoredTokens()
    const user = this.getStoredUser()
    return !!(tokens && user && !this.isTokenExpired(tokens))
  }

  getAuthHeader(): string | null {
    const tokens = this.getStoredTokens()
    return tokens ? `${tokens.tokenType} ${tokens.accessToken}` : null
  }

  // Error Handling
  private handleAuthError(error: any): Error {
    if (error.response?.data) {
      const { message, code, field, details } = error.response.data
      const authError = new Error(message || 'Authentication failed')
      ;(authError as any).code = code
      ;(authError as any).field = field
      ;(authError as any).details = details
      ;(authError as any).status = error.response.status
      return authError
    }

    if (error.request) {
      return new Error('Network error. Please check your connection.')
    }

    return new Error(error.message || 'An unexpected error occurred')
  }

  // Validation Helpers
  validatePassword(password: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = []

    if (password.length < this.config.passwordMinLength) {
      errors.push(`Password must be at least ${this.config.passwordMinLength} characters long`)
    }

    if (this.config.passwordRequireUppercase && !/[A-Z]/.test(password)) {
      errors.push('Password must contain at least one uppercase letter')
    }

    if (this.config.passwordRequireLowercase && !/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter')
    }

    if (this.config.passwordRequireNumbers && !/\d/.test(password)) {
      errors.push('Password must contain at least one number')
    }

    if (this.config.passwordRequireSpecialChars && !/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      errors.push('Password must contain at least one special character')
    }

    return {
      isValid: errors.length === 0,
      errors,
    }
  }

  validateEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }
}

// Default configuration
export const defaultAuthConfig: AuthConfig = {
  apiBaseUrl: '/api',
  tokenStorageKey: 'auth_tokens',
  refreshTokenStorageKey: 'auth_refresh_token',
  autoRefreshTokens: true,
  tokenRefreshThreshold: 5, // 5 minutes before expiry
  sessionTimeout: 480, // 8 hours
  maxLoginAttempts: 5,
  lockoutDuration: 15, // 15 minutes
  passwordMinLength: 8,
  passwordRequireUppercase: true,
  passwordRequireLowercase: true,
  passwordRequireNumbers: true,
  passwordRequireSpecialChars: true,
}

// Create singleton instance
export const authService = new AuthService(defaultAuthConfig)

export default authService

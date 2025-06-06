// Authentication and user management types

export interface User {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  title?: string;
  department?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  lastLogin?: string;
  createdDate: string;
  modifiedDate: string;
  roles: Role[];
  permissions: string[];
}

export interface Role {
  roleId: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  isActive: boolean;
  permissions: Permission[];
}

export interface Permission {
  permissionId: string;
  name: string;
  description: string;
  resource: string;
  action: string;
  isSystemPermission: boolean;
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  title?: string;
  department?: string;
  roles: string[];
  isActive: boolean;
  password: string;
}

export interface UpdateUserRequest {
  id: string;
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  title?: string;
  department?: string;
  roles: string[];
  isActive: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
  twoFactorCode?: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  department?: string;
  title?: string;
  password: string;
  confirmPassword: string;
}

export interface RegisterResponse {
  isSuccess: boolean;
  message?: string;
  data?: User;
  errors: string[];
}

export interface LoginResponse {
  isSuccess: boolean;
  errorMessage?: string;
  token?: JwtToken;
  user?: User;
  requiresTwoFactor: boolean;
  requiresPasswordChange: boolean;
}

export interface JwtToken {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  refreshExpiresAt: string;
  tokenType: string;
  scopes: string[];
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface SecurityConfig {
  passwordPolicy: {
    minimumLength: number;
    requireUppercase: boolean;
    requireLowercase: boolean;
    requireNumbers: boolean;
    requireSpecialChars: boolean;
    passwordExpirationDays: number;
    maxFailedAttempts: number;
    lockoutDurationMinutes: number;
  };
  sessionSettings: {
    sessionTimeoutMinutes: number;
    idleTimeoutMinutes: number;
    allowConcurrentSessions: boolean;
  };
  twoFactorSettings: {
    enabled: boolean;
    required: boolean;
    methods: string[];
  };
  rateLimitSettings: {
    enabled: boolean;
    maxRequestsPerMinute: number;
    maxRequestsPerHour: number;
  };
}

export interface SecurityEvent {
  id: string;
  userId: string;
  username: string;
  eventType: string;
  description: string;
  ipAddress: string;
  userAgent: string;
  timestamp: Date;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
}

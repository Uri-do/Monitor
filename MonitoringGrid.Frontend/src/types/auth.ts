// Authentication and user management types

export interface User {
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
  lastLogin?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
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

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresAt: Date;
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

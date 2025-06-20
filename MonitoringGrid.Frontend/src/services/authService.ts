import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  User,
  JwtToken,
} from '@/types/auth';

// Use relative URL to work with Vite proxy configuration
// The proxy in vite.config.ts forwards /api requests to http://localhost:5001
const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || '';

class AuthService {
  private baseUrl = API_BASE_URL ? `${API_BASE_URL}/api/security` : '/api/security';

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${this.baseUrl}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    let result;
    try {
      // Check if response has content before parsing JSON
      const text = await response.text();
      if (text) {
        result = JSON.parse(text);
      } else {
        result = {};
      }
    } catch (error) {
      console.error('Failed to parse response as JSON:', error);
      throw new Error('Invalid response from server');
    }

    if (!response.ok) {
      // Handle validation errors (400 Bad Request)
      if (response.status === 400 && result.errors) {
        const errorMessages = Object.values(result.errors).flat().join(', ');
        throw new Error(errorMessages || 'Validation failed');
      }

      throw new Error(result.errorMessage || result.message || result.title || 'Login failed');
    }

    // Handle wrapped API response (ApiResponse<T> structure)
    const loginData = result.data || result;

    if (!loginData.isSuccess) {
      throw new Error(loginData.errorMessage || 'Login failed');
    }

    return loginData;
  }

  async register(request: RegisterRequest): Promise<RegisterResponse> {
    const response = await fetch(`${this.baseUrl}/auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    const result = await response.json();

    if (!response.ok) {
      throw new Error(result.message || 'Registration failed');
    }

    if (!result.isSuccess) {
      throw new Error(result.message || result.errors?.join(', ') || 'Registration failed');
    }

    return result;
  }

  async logout(): Promise<void> {
    const token = this.getToken();
    if (token) {
      try {
        await fetch(`${this.baseUrl}/auth/logout`, {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });
      } catch (error) {
        console.warn('Logout request failed:', error);
      }
    }

    this.clearToken();
  }



  async refreshToken(): Promise<JwtToken> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await fetch(`${this.baseUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      // Clear tokens if refresh fails
      this.clearToken();
      throw new Error(`Token refresh failed: ${response.status}`);
    }

    const result = await response.json();

    // Backend returns JwtTokenDto directly
    const accessToken = result.accessToken;
    const newRefreshToken = result.refreshToken || refreshToken;

    if (!accessToken) {
      throw new Error('No access token in refresh response');
    }

    this.setToken(accessToken);
    this.setRefreshToken(newRefreshToken);

    return result;
  }

  shouldRefreshToken(): boolean {
    const token = this.getToken();
    if (!token || typeof token !== 'string' || token.trim() === '') {
      return false;
    }

    try {
      // Validate JWT format first
      const parts = token.split('.');
      if (parts.length !== 3) {
        return true; // Invalid format, should refresh
      }

      // Decode JWT token to check expiration
      const payload = JSON.parse(atob(parts[1]));
      const currentTime = Math.floor(Date.now() / 1000);

      // Refresh if token expires within the next 10 minutes (600 seconds)
      return payload.exp && payload.exp < currentTime + 600;
    } catch (error) {
      // If we can't decode the token, we should refresh
      return true;
    }
  }

  async getCurrentUser(token?: string): Promise<User> {
    const authToken = token || this.getToken();
    if (!authToken) {
      throw new Error('No authentication token');
    }

    // Check if token is expired before making the request (only if using stored token)
    if (!token && this.isTokenExpired(authToken)) {
      throw new Error('Authentication token expired');
    }

    const response = await fetch(`${this.baseUrl}/auth/profile`, {
      headers: {
        Authorization: `Bearer ${authToken}`,
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        // Token is invalid/expired
        throw new Error('Authentication token expired');
      }
      throw new Error(`Failed to get current user: ${response.status}`);
    }

    return response.json();
  }

  private isTokenExpired(token: string): boolean {
    if (!token || typeof token !== 'string' || token.trim() === '') {
      return true;
    }

    try {
      // Validate JWT format first
      const parts = token.split('.');
      if (parts.length !== 3) {
        return true; // Invalid format, consider expired
      }

      // Decode JWT token to check expiration
      const payload = JSON.parse(atob(parts[1]));
      const currentTime = Math.floor(Date.now() / 1000);

      // Check if token is actually expired (not just expiring soon)
      return payload.exp && payload.exp < currentTime;
    } catch (error) {
      // If we can't decode the token, consider it expired
      console.warn('Failed to decode JWT token:', error);
      return true;
    }
  }

  getToken(): string | null {
    return sessionStorage.getItem('auth_token');
  }

  setToken(token: string): void {
    sessionStorage.setItem('auth_token', token);
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refresh_token');
  }

  setRefreshToken(token: string): void {
    sessionStorage.setItem('refresh_token', token);
  }

  clearToken(): void {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('refresh_token');
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token || typeof token !== 'string' || token.trim() === '') {
      return false;
    }

    // Check if token is expired
    return !this.isTokenExpired(token);
  }
}

export const authService = new AuthService();

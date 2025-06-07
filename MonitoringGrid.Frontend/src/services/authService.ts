import { LoginRequest, LoginResponse, RegisterRequest, RegisterResponse, User } from '../types/auth';

const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || '';

class AuthService {
  private baseUrl = `${API_BASE_URL}/api/auth`;

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${this.baseUrl}/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    const result = await response.json();

    if (!response.ok) {
      throw new Error(result.errorMessage || result.message || 'Login failed');
    }

    if (!result.isSuccess) {
      throw new Error(result.errorMessage || 'Login failed');
    }

    return result;
  }

  async register(request: RegisterRequest): Promise<RegisterResponse> {
    const response = await fetch(`${this.baseUrl}/register`, {
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
        await fetch(`${this.baseUrl}/logout`, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        });
      } catch (error) {
        console.warn('Logout request failed:', error);
      }
    }

    this.clearToken();
  }

  async refreshToken(): Promise<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await fetch(`${this.baseUrl}/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    const result = await response.json();
    this.setToken(result.token);
    this.setRefreshToken(result.refreshToken);
    return result;
  }

  async getCurrentUser(): Promise<User> {
    const token = this.getToken();
    if (!token) {
      throw new Error('No authentication token');
    }

    const response = await fetch(`${this.baseUrl}/profile`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to get current user');
    }

    return response.json();
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  setToken(token: string): void {
    localStorage.setItem('auth_token', token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  setRefreshToken(token: string): void {
    localStorage.setItem('refresh_token', token);
  }

  clearToken(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}

export const authService = new AuthService();

import { SecurityConfig, SecurityEvent } from '../types/auth';
import { authService } from './authService';

// Use relative URL to work with Vite proxy configuration
// The proxy in vite.config.ts forwards /api requests to http://localhost:5001
const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || '';

class SecurityService {
  private baseUrl = API_BASE_URL ? `${API_BASE_URL}/api/security` : '/api/security';

  private getAuthHeaders() {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };
  }

  async getSecurityConfig(): Promise<SecurityConfig> {
    const response = await fetch(`${this.baseUrl}/config`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch security configuration');
    }

    return response.json();
  }

  async updateSecurityConfig(config: SecurityConfig): Promise<SecurityConfig> {
    const response = await fetch(`${this.baseUrl}/config`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(config),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to update security configuration');
    }

    return response.json();
  }

  async getSecurityEvents(limit: number = 100): Promise<SecurityEvent[]> {
    const response = await fetch(`${this.baseUrl}/events?limit=${limit}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch security events');
    }

    return response.json();
  }

  async getSecurityEventsByUser(userId: string, limit: number = 50): Promise<SecurityEvent[]> {
    const response = await fetch(`${this.baseUrl}/events/user/${userId}?limit=${limit}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user security events');
    }

    return response.json();
  }

  async logSecurityEvent(event: Omit<SecurityEvent, 'id' | 'timestamp'>): Promise<void> {
    const response = await fetch(`${this.baseUrl}/events`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(event),
    });

    if (!response.ok) {
      throw new Error('Failed to log security event');
    }
  }

  async validatePassword(password: string): Promise<{ isValid: boolean; errors: string[] }> {
    const response = await fetch(`${this.baseUrl}/validate-password`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ password }),
    });

    if (!response.ok) {
      throw new Error('Failed to validate password');
    }

    return response.json();
  }

  async checkAccountLockout(
    username: string
  ): Promise<{ isLocked: boolean; lockoutExpiresAt?: Date }> {
    const response = await fetch(`${this.baseUrl}/lockout-status/${username}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to check account lockout status');
    }

    return response.json();
  }

  async unlockAccount(username: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/unlock-account/${username}`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to unlock account');
    }
  }

  // API Key Management
  async getApiKeys(): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/api-keys`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      // Return empty array if endpoint doesn't exist yet
      if (response.status === 404) {
        return [];
      }
      throw new Error('Failed to fetch API keys');
    }

    return response.json();
  }

  async createApiKey(name: string, scopes: string[]): Promise<{ keyId: string; key: string }> {
    const response = await fetch(`${this.baseUrl}/api-keys`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ name, scopes }),
    });

    if (!response.ok) {
      throw new Error('Failed to create API key');
    }

    return response.json();
  }

  async revokeApiKey(keyId: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/api-keys/${keyId}`, {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to revoke API key');
    }
  }

  // User Management - Connect to actual User API
  async getUsers(): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/users`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch users');
    }

    return response.json();
  }

  async updateUserRoles(userId: string, roles: string[]): Promise<void> {
    const response = await fetch(`${this.baseUrl}/users/${userId}/roles`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ roles }),
    });

    if (!response.ok) {
      throw new Error('Failed to update user roles');
    }
  }

  async getRoles(): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/roles`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch roles');
    }

    return response.json();
  }

  async getPermissions(): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/permissions`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch permissions');
    }

    return response.json();
  }


}

export const securityService = new SecurityService();

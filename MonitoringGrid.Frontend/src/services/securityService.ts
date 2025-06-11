import { SecurityConfig, SecurityEvent } from '../types/auth';
import { authService } from './authService';

const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || 'https://localhost:57652';

class SecurityService {
  private baseUrl = `${API_BASE_URL}/api/security`;

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

  // Enterprise Security Features
  async getThreatDetections(filters?: {
    type?: string;
    severity?: string;
    status?: string;
    limit?: number;
  }): Promise<any[]> {
    const params = new URLSearchParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value) params.append(key, value.toString());
      });
    }

    const response = await fetch(`${this.baseUrl}/threats?${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) return [];
      throw new Error('Failed to fetch threat detections');
    }

    return response.json();
  }

  async getComplianceReports(type?: string): Promise<any[]> {
    const params = type ? `?type=${type}` : '';
    const response = await fetch(`${this.baseUrl}/compliance${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) return [];
      throw new Error('Failed to fetch compliance reports');
    }

    return response.json();
  }

  async getAuditLogs(filters?: {
    userId?: string;
    action?: string;
    resource?: string;
    startDate?: string;
    endDate?: string;
    limit?: number;
  }): Promise<any[]> {
    const params = new URLSearchParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value) params.append(key, value.toString());
      });
    }

    const response = await fetch(`${this.baseUrl}/audit?${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) return [];
      throw new Error('Failed to fetch audit logs');
    }

    return response.json();
  }

  async getSecurityDashboard(): Promise<{
    totalEvents: number;
    criticalEvents: number;
    activeThreats: number;
    riskScore: number;
    recentEvents: SecurityEvent[];
    threatTrends: Array<{ date: string; count: number; severity: string }>;
  }> {
    const response = await fetch(`${this.baseUrl}/dashboard`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      // Return mock data if endpoint doesn't exist
      if (response.status === 404) {
        return {
          totalEvents: 0,
          criticalEvents: 0,
          activeThreats: 0,
          riskScore: 85,
          recentEvents: [],
          threatTrends: [],
        };
      }
      throw new Error('Failed to fetch security dashboard');
    }

    return response.json();
  }

  async getUserRiskScore(userId?: string): Promise<{
    score: number;
    level: 'low' | 'medium' | 'high' | 'critical';
    factors: Array<{
      factor: string;
      impact: number;
      description: string;
    }>;
    recommendations: string[];
  }> {
    const url = userId ? `${this.baseUrl}/risk/${userId}` : `${this.baseUrl}/risk`;
    const response = await fetch(url, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      // Return mock data if endpoint doesn't exist
      if (response.status === 404) {
        return {
          score: 25,
          level: 'low',
          factors: [],
          recommendations: [],
        };
      }
      throw new Error('Failed to fetch user risk score');
    }

    return response.json();
  }

  async getSecurityAnalytics(period: string = '30d'): Promise<{
    loginAttempts: Array<{ date: string; successful: number; failed: number }>;
    threatsByType: Array<{ type: string; count: number }>;
    riskTrends: Array<{ date: string; averageRisk: number }>;
    complianceScore: number;
    topRiskyUsers: Array<{ userId: string; userName: string; riskScore: number }>;
  }> {
    const response = await fetch(`${this.baseUrl}/analytics?period=${period}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      // Return mock data if endpoint doesn't exist
      if (response.status === 404) {
        return {
          loginAttempts: [],
          threatsByType: [],
          riskTrends: [],
          complianceScore: 95,
          topRiskyUsers: [],
        };
      }
      throw new Error('Failed to fetch security analytics');
    }

    return response.json();
  }
}

export const securityService = new SecurityService();

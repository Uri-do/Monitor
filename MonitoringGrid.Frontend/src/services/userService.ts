import { User, Role, Permission } from '@/types/auth';
import { authService } from './authService';

const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || 'http://localhost:5000';

export interface CreateUserRequest {
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  department?: string;
  title?: string;
  password: string;
  roleIds: string[];
  isActive: boolean;
  emailConfirmed: boolean;
}

export interface UpdateUserRequest {
  email?: string;
  displayName?: string;
  firstName?: string;
  lastName?: string;
  department?: string;
  title?: string;
  roleIds?: string[];
  isActive?: boolean;
  emailConfirmed?: boolean;
}

export interface BulkUserOperation {
  userIds: string[];
  operation: 'activate' | 'deactivate' | 'delete' | 'assign-role' | 'remove-role';
  roleId?: string;
}

class UserService {
  private baseUrl = `${API_BASE_URL}/api/security/users`;
  private authBaseUrl = `${API_BASE_URL}/api/security`;

  private getAuthHeaders() {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      Authorization: token ? `Bearer ${token}` : '',
    };
  }

  // User CRUD operations
  async getUsers(isActive?: boolean): Promise<User[]> {
    const params = new URLSearchParams();
    if (isActive !== undefined) {
      params.append('isActive', isActive.toString());
    }

    const response = await fetch(`${this.baseUrl}?${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch users');
    }

    return response.json();
  }

  async getUserById(id: string): Promise<User> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user');
    }

    return response.json();
  }

  async getCurrentUser(): Promise<User> {
    const response = await fetch(`${this.baseUrl}/profile`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch current user');
    }

    return response.json();
  }

  async updatePassword(data: {
    userId: string;
    currentPassword: string;
    newPassword: string;
  }): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${data.userId}/password`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to update password');
    }
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    const response = await fetch(this.baseUrl, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create user');
    }

    return response.json();
  }

  async updateUser(id: string, request: UpdateUserRequest): Promise<User> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to update user');
    }

    return response.json();
  }

  async deleteUser(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to delete user');
    }
  }

  // User status operations
  async activateUser(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}/activate`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to activate user');
    }
  }

  async deactivateUser(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}/deactivate`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to deactivate user');
    }
  }

  // Role management
  async getUserRoles(id: string): Promise<Role[]> {
    const response = await fetch(`${this.baseUrl}/${id}/roles`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user roles');
    }

    return response.json();
  }

  async assignRoles(id: string, roleIds: string[]): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}/roles`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ userId: id, roleIds }),
    });

    if (!response.ok) {
      throw new Error('Failed to assign roles');
    }
  }

  async getUserPermissions(id: string): Promise<Permission[]> {
    const response = await fetch(`${this.baseUrl}/${id}/permissions`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user permissions');
    }

    return response.json();
  }

  // Bulk operations
  async bulkOperation(operation: BulkUserOperation): Promise<void> {
    const response = await fetch(`${this.baseUrl}/bulk`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(operation),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Bulk operation failed');
    }
  }

  // Validation
  async checkUsernameAvailability(username: string): Promise<boolean> {
    const response = await fetch(`${this.baseUrl}/check-username/${encodeURIComponent(username)}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to check username availability');
    }

    const result = await response.json();
    return result.isAvailable;
  }

  async checkEmailAvailability(email: string): Promise<boolean> {
    const response = await fetch(`${this.baseUrl}/check-email/${encodeURIComponent(email)}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to check email availability');
    }

    const result = await response.json();
    return result.isAvailable;
  }

  // Password management
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    const response = await fetch(`${this.authBaseUrl}/change-password`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({
        currentPassword,
        newPassword,
        confirmPassword: newPassword,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to change password');
    }
  }

  // Legacy method for compatibility
  async getRoles(): Promise<Role[]> {
    const response = await fetch(`${this.authBaseUrl}/roles`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch roles');
    }

    return response.json();
  }
}

export const userService = new UserService();

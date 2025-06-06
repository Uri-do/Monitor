import { Role, Permission } from '../types/auth';
import { authService } from './authService';

const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || 'http://localhost:5000';

export interface CreateRoleRequest {
  name: string;
  description: string;
  permissionIds: string[];
  isActive: boolean;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissionIds?: string[];
  isActive?: boolean;
}

class RoleService {
  private baseUrl = `${API_BASE_URL}/api/role`;

  private getAuthHeaders() {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : '',
    };
  }

  // Role CRUD operations
  async getRoles(isActive?: boolean): Promise<Role[]> {
    const params = new URLSearchParams();
    if (isActive !== undefined) {
      params.append('isActive', isActive.toString());
    }

    const response = await fetch(`${this.baseUrl}?${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch roles');
    }

    return response.json();
  }

  async getRoleById(id: string): Promise<Role> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch role');
    }

    return response.json();
  }

  async createRole(request: CreateRoleRequest): Promise<Role> {
    const response = await fetch(this.baseUrl, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create role');
    }

    return response.json();
  }

  async updateRole(id: string, request: UpdateRoleRequest): Promise<Role> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to update role');
    }

    return response.json();
  }

  async deleteRole(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to delete role');
    }
  }

  // Permission management
  async getRolePermissions(id: string): Promise<Permission[]> {
    const response = await fetch(`${this.baseUrl}/${id}/permissions`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch role permissions');
    }

    return response.json();
  }

  async assignPermissions(id: string, permissionIds: string[]): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}/permissions`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ roleId: id, permissionIds }),
    });

    if (!response.ok) {
      throw new Error('Failed to assign permissions');
    }
  }

  // Get all available permissions
  async getAllPermissions(): Promise<Permission[]> {
    const response = await fetch(`${API_BASE_URL}/api/permission`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch permissions');
    }

    return response.json();
  }

  // Role validation
  async checkRoleNameAvailability(name: string, excludeId?: string): Promise<boolean> {
    const params = new URLSearchParams();
    params.append('name', name);
    if (excludeId) {
      params.append('excludeId', excludeId);
    }

    const response = await fetch(`${this.baseUrl}/check-name?${params}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to check role name availability');
    }

    const result = await response.json();
    return result.isAvailable;
  }

  // Get users assigned to a role
  async getRoleUsers(id: string): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/${id}/users`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch role users');
    }

    return response.json();
  }
}

export const roleService = new RoleService();

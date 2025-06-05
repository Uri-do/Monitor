import { User, CreateUserRequest, UpdateUserRequest, Role } from '../types/auth';
import { authService } from './authService';

const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || 'https://localhost:7001';

class UserService {
  private baseUrl = `${API_BASE_URL}/api/users`;

  private getAuthHeaders() {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
    };
  }

  async getUsers(): Promise<User[]> {
    const response = await fetch(this.baseUrl, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch users');
    }

    return response.json();
  }

  async getUser(id: string): Promise<User> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user');
    }

    return response.json();
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    const response = await fetch(this.baseUrl, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to create user');
    }

    return response.json();
  }

  async updateUser(request: UpdateUserRequest): Promise<User> {
    const response = await fetch(`${this.baseUrl}/${request.id}`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to update user');
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

  async getRoles(): Promise<Role[]> {
    const response = await fetch(`${API_BASE_URL}/api/roles`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch roles');
    }

    return response.json();
  }

  async resetPassword(userId: string, newPassword: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${userId}/reset-password`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ newPassword }),
    });

    if (!response.ok) {
      throw new Error('Failed to reset password');
    }
  }

  async toggleUserStatus(userId: string, isActive: boolean): Promise<User> {
    const response = await fetch(`${this.baseUrl}/${userId}/status`, {
      method: 'PATCH',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ isActive }),
    });

    if (!response.ok) {
      throw new Error('Failed to update user status');
    }

    return response.json();
  }
}

export const userService = new UserService();

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { indicatorService, userService, alertService } from '../api';
import { createMockIndicator, createMockUser, createMockAlert } from '@/test/utils';

// Mock axios
vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
    })),
  },
}));

// Mock the base API service
const mockAxios = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
};

vi.mock('../BaseApiService', () => ({
  default: mockAxios,
}));

describe('API Services', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('indicatorService', () => {
    it('gets all indicators', async () => {
      const mockIndicators = [
        createMockIndicator({ id: 1, name: 'Indicator 1' }),
        createMockIndicator({ id: 2, name: 'Indicator 2' }),
      ];

      mockAxios.get.mockResolvedValue({ data: mockIndicators });

      const result = await indicatorService.getIndicators();

      expect(mockAxios.get).toHaveBeenCalledWith('/indicators');
      expect(result).toEqual(mockIndicators);
    });

    it('gets indicator by id', async () => {
      const mockIndicator = createMockIndicator({ id: 1, name: 'Test Indicator' });

      mockAxios.get.mockResolvedValue({ data: mockIndicator });

      const result = await indicatorService.getIndicator(1);

      expect(mockAxios.get).toHaveBeenCalledWith('/indicators/1');
      expect(result).toEqual(mockIndicator);
    });

    it('creates new indicator', async () => {
      const newIndicator = { name: 'New Indicator', description: 'Test', isActive: true };
      const createdIndicator = createMockIndicator({ id: 3, ...newIndicator });

      mockAxios.post.mockResolvedValue({ data: createdIndicator });

      const result = await indicatorService.createIndicator(newIndicator);

      expect(mockAxios.post).toHaveBeenCalledWith('/indicators', newIndicator);
      expect(result).toEqual(createdIndicator);
    });

    it('updates existing indicator', async () => {
      const updateData = { id: 1, name: 'Updated Indicator', isActive: false };
      const updatedIndicator = createMockIndicator(updateData);

      mockAxios.put.mockResolvedValue({ data: updatedIndicator });

      const result = await indicatorService.updateIndicator(updateData);

      expect(mockAxios.put).toHaveBeenCalledWith('/indicators/1', updateData);
      expect(result).toEqual(updatedIndicator);
    });

    it('deletes indicator', async () => {
      mockAxios.delete.mockResolvedValue({ data: null });

      await indicatorService.deleteIndicator(1);

      expect(mockAxios.delete).toHaveBeenCalledWith('/indicators/1');
    });

    it('handles API errors', async () => {
      const error = new Error('API Error');
      mockAxios.get.mockRejectedValue(error);

      await expect(indicatorService.getIndicators()).rejects.toThrow('API Error');
    });
  });

  describe('userService', () => {
    it('gets all users', async () => {
      const mockUsers = [
        createMockUser({ id: 1, username: 'user1' }),
        createMockUser({ id: 2, username: 'user2' }),
      ];

      mockAxios.get.mockResolvedValue({ data: mockUsers });

      const result = await userService.getUsers();

      expect(mockAxios.get).toHaveBeenCalledWith('/users');
      expect(result).toEqual(mockUsers);
    });

    it('gets user by id', async () => {
      const mockUser = createMockUser({ id: 1, username: 'testuser' });

      mockAxios.get.mockResolvedValue({ data: mockUser });

      const result = await userService.getUser(1);

      expect(mockAxios.get).toHaveBeenCalledWith('/users/1');
      expect(result).toEqual(mockUser);
    });

    it('creates new user', async () => {
      const newUser = { username: 'newuser', email: 'new@example.com', password: 'password' };
      const createdUser = createMockUser({ id: 3, ...newUser });

      mockAxios.post.mockResolvedValue({ data: createdUser });

      const result = await userService.createUser(newUser);

      expect(mockAxios.post).toHaveBeenCalledWith('/users', newUser);
      expect(result).toEqual(createdUser);
    });

    it('updates existing user', async () => {
      const updateData = { id: 1, username: 'updateduser', email: 'updated@example.com' };
      const updatedUser = createMockUser(updateData);

      mockAxios.put.mockResolvedValue({ data: updatedUser });

      const result = await userService.updateUser(updateData);

      expect(mockAxios.put).toHaveBeenCalledWith('/users/1', updateData);
      expect(result).toEqual(updatedUser);
    });

    it('deletes user', async () => {
      mockAxios.delete.mockResolvedValue({ data: null });

      await userService.deleteUser(1);

      expect(mockAxios.delete).toHaveBeenCalledWith('/users/1');
    });
  });

  describe('alertService', () => {
    it('gets all alerts', async () => {
      const mockAlerts = [
        createMockAlert({ id: 1, title: 'Alert 1' }),
        createMockAlert({ id: 2, title: 'Alert 2' }),
      ];

      mockAxios.get.mockResolvedValue({ data: mockAlerts });

      const result = await alertService.getAlerts();

      expect(mockAxios.get).toHaveBeenCalledWith('/alerts');
      expect(result).toEqual(mockAlerts);
    });

    it('gets alert by id', async () => {
      const mockAlert = createMockAlert({ id: 1, title: 'Test Alert' });

      mockAxios.get.mockResolvedValue({ data: mockAlert });

      const result = await alertService.getAlert(1);

      expect(mockAxios.get).toHaveBeenCalledWith('/alerts/1');
      expect(result).toEqual(mockAlert);
    });

    it('resolves alert', async () => {
      const resolvedAlert = createMockAlert({ id: 1, isResolved: true });

      mockAxios.put.mockResolvedValue({ data: resolvedAlert });

      const result = await alertService.resolveAlert(1);

      expect(mockAxios.put).toHaveBeenCalledWith('/alerts/1/resolve');
      expect(result).toEqual(resolvedAlert);
    });

    it('gets critical alerts', async () => {
      const criticalAlerts = [
        createMockAlert({ id: 1, severity: 'critical' }),
      ];

      mockAxios.get.mockResolvedValue({ data: criticalAlerts });

      const result = await alertService.getCriticalAlerts();

      expect(mockAxios.get).toHaveBeenCalledWith('/alerts/critical');
      expect(result).toEqual(criticalAlerts);
    });

    it('gets unresolved alerts', async () => {
      const unresolvedAlerts = [
        createMockAlert({ id: 1, isResolved: false }),
      ];

      mockAxios.get.mockResolvedValue({ data: unresolvedAlerts });

      const result = await alertService.getUnresolvedAlerts();

      expect(mockAxios.get).toHaveBeenCalledWith('/alerts/unresolved');
      expect(result).toEqual(unresolvedAlerts);
    });
  });
});

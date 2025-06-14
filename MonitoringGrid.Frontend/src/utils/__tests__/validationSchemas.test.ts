import { describe, it, expect } from 'vitest';
import {
  indicatorValidationSchema,
  userValidationSchema,
  loginValidationSchema,
  registerValidationSchema,
} from '../validationSchemas';

describe('Validation Schemas', () => {
  describe('indicatorValidationSchema', () => {
    it('validates valid indicator data', async () => {
      const validData = {
        name: 'Test Indicator',
        description: 'Test description',
        isActive: true,
        lastMinutes: 60,
      };

      const result = await indicatorValidationSchema.isValid(validData);
      expect(result).toBe(true);
    });

    it('rejects indicator with empty name', async () => {
      const invalidData = {
        name: '',
        description: 'Test description',
        isActive: true,
        lastMinutes: 60,
      };

      const result = await indicatorValidationSchema.isValid(invalidData);
      expect(result).toBe(false);

      try {
        await indicatorValidationSchema.validate(invalidData);
      } catch (error: any) {
        expect(error.message).toContain('name');
      }
    });

    it('rejects indicator with name too long', async () => {
      const invalidData = {
        name: 'a'.repeat(256), // Assuming max length is 255
        description: 'Test description',
        isActive: true,
        lastMinutes: 60,
      };

      const result = await indicatorValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });

    it('rejects indicator with invalid lastMinutes', async () => {
      const invalidData = {
        name: 'Test Indicator',
        description: 'Test description',
        isActive: true,
        lastMinutes: -1, // Should be positive
      };

      const result = await indicatorValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });

    it('accepts indicator without description', async () => {
      const validData = {
        name: 'Test Indicator',
        isActive: true,
        lastMinutes: 60,
      };

      const result = await indicatorValidationSchema.isValid(validData);
      expect(result).toBe(true);
    });
  });

  describe('userValidationSchema', () => {
    it('validates valid user data', async () => {
      const validData = {
        username: 'testuser',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        isActive: true,
      };

      const result = await userValidationSchema.isValid(validData);
      expect(result).toBe(true);
    });

    it('rejects user with invalid email', async () => {
      const invalidData = {
        username: 'testuser',
        email: 'invalid-email',
        firstName: 'Test',
        lastName: 'User',
        isActive: true,
      };

      const result = await userValidationSchema.isValid(invalidData);
      expect(result).toBe(false);

      try {
        await userValidationSchema.validate(invalidData);
      } catch (error: any) {
        expect(error.message).toContain('email');
      }
    });

    it('rejects user with empty username', async () => {
      const invalidData = {
        username: '',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        isActive: true,
      };

      const result = await userValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });

    it('rejects user with username too short', async () => {
      const invalidData = {
        username: 'ab', // Assuming min length is 3
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        isActive: true,
      };

      const result = await userValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });
  });

  describe('loginValidationSchema', () => {
    it('validates valid login data', async () => {
      const validData = {
        username: 'testuser',
        password: 'password123',
      };

      const result = await loginValidationSchema.isValid(validData);
      expect(result).toBe(true);
    });

    it('rejects login with empty username', async () => {
      const invalidData = {
        username: '',
        password: 'password123',
      };

      const result = await loginValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });

    it('rejects login with empty password', async () => {
      const invalidData = {
        username: 'testuser',
        password: '',
      };

      const result = await loginValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });
  });

  describe('registerValidationSchema', () => {
    it('validates valid registration data', async () => {
      const validData = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        firstName: 'Test',
        lastName: 'User',
      };

      const result = await registerValidationSchema.isValid(validData);
      expect(result).toBe(true);
    });

    it('rejects registration with mismatched passwords', async () => {
      const invalidData = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'different123',
        firstName: 'Test',
        lastName: 'User',
      };

      const result = await registerValidationSchema.isValid(invalidData);
      expect(result).toBe(false);

      try {
        await registerValidationSchema.validate(invalidData);
      } catch (error: any) {
        expect(error.message).toContain('password');
      }
    });

    it('rejects registration with weak password', async () => {
      const invalidData = {
        username: 'testuser',
        email: 'test@example.com',
        password: '123', // Too short
        confirmPassword: '123',
        firstName: 'Test',
        lastName: 'User',
      };

      const result = await registerValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });

    it('rejects registration with invalid email', async () => {
      const invalidData = {
        username: 'testuser',
        email: 'invalid-email',
        password: 'password123',
        confirmPassword: 'password123',
        firstName: 'Test',
        lastName: 'User',
      };

      const result = await registerValidationSchema.isValid(invalidData);
      expect(result).toBe(false);
    });
  });
});

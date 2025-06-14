import { authService } from './authService';

// Standard API response wrapper
export interface ApiResponse<T = any> {
  data?: T;
  isSuccess: boolean;
  errorMessage?: string;
  message?: string;
  errors?: Record<string, string[]> | string[];
}

// Request configuration interface
export interface RequestConfig {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  headers?: Record<string, string>;
  body?: any;
  params?: Record<string, string | number | boolean>;
}

// Error types for better error handling
export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
    public response?: any,
    public code?: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class ValidationError extends ApiError {
  constructor(
    message: string,
    public validationErrors: Record<string, string[]>
  ) {
    super(message, 400);
    this.name = 'ValidationError';
  }
}

export class AuthenticationError extends ApiError {
  constructor(message: string = 'Authentication required') {
    super(message, 401);
    this.name = 'AuthenticationError';
  }
}

export class AuthorizationError extends ApiError {
  constructor(message: string = 'Access denied') {
    super(message, 403);
    this.name = 'AuthorizationError';
  }
}

export class NotFoundError extends ApiError {
  constructor(message: string = 'Resource not found') {
    super(message, 404);
    this.name = 'NotFoundError';
  }
}

// Base API service class that all other services can extend
export abstract class BaseApiService {
  protected readonly baseUrl: string;
  protected readonly apiBaseUrl: string;

  constructor(endpoint: string) {
    this.apiBaseUrl = (import.meta as any).env.VITE_API_BASE_URL || '';
    this.baseUrl = `${this.apiBaseUrl}/api/${endpoint}`;
  }

  // Get standardized auth headers
  protected getAuthHeaders(): Record<string, string> {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    };
  }

  // Build query string from params
  protected buildQueryString(params?: Record<string, string | number | boolean>): string {
    if (!params || Object.keys(params).length === 0) {
      return '';
    }

    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        searchParams.append(key, value.toString());
      }
    });

    const queryString = searchParams.toString();
    return queryString ? `?${queryString}` : '';
  }

  // Enhanced error handling with specific error types
  protected handleError(error: any): never {
    // Handle fetch/network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      throw new ApiError('Network error - please check your connection', 0, error);
    }

    // Handle response errors
    if (error.response) {
      const { status, data } = error.response;

      // Extract error message from various response formats
      let message = 'An unexpected error occurred';

      if (data?.errorMessage) {
        message = data.errorMessage;
      } else if (data?.message) {
        message = data.message;
      } else if (data?.errors) {
        if (Array.isArray(data.errors)) {
          message = data.errors.join(', ');
        } else if (typeof data.errors === 'object') {
          message = Object.values(data.errors).flat().join(', ');
        }
      } else if (typeof data === 'string') {
        message = data;
      }

      // Create specific error types based on status
      switch (status) {
        case 400:
          if (data?.errors && typeof data.errors === 'object' && !Array.isArray(data.errors)) {
            throw new ValidationError(message, data.errors);
          }
          throw new ApiError(message, status, data, 'BAD_REQUEST');

        case 401:
          throw new AuthenticationError(message);

        case 403:
          throw new AuthorizationError(message);

        case 404:
          throw new NotFoundError(message);

        case 409:
          throw new ApiError(message, status, data, 'CONFLICT');

        case 422:
          throw new ValidationError(message, data?.errors || {});

        case 500:
          throw new ApiError('Internal server error', status, data, 'INTERNAL_ERROR');

        default:
          throw new ApiError(message, status, data);
      }
    }

    // Handle other error types
    if (error.message) {
      throw new ApiError(error.message);
    }

    throw new ApiError('An unexpected error occurred');
  }

  // Generic request method
  protected async request<T>(endpoint: string, config: RequestConfig = {}): Promise<T> {
    const { method = 'GET', headers = {}, body, params } = config;

    const url = `${this.baseUrl}${endpoint}${this.buildQueryString(params)}`;

    const requestHeaders = {
      ...this.getAuthHeaders(),
      ...headers,
    };

    const requestConfig: RequestInit = {
      method,
      headers: requestHeaders,
    };

    // Add body for non-GET requests
    if (body && method !== 'GET') {
      requestConfig.body = JSON.stringify(body);
    }

    try {
      const response = await fetch(url, requestConfig);

      // Handle empty responses (204 No Content, etc.)
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        return {} as T;
      }

      let data: any;
      const contentType = response.headers.get('content-type');

      if (contentType?.includes('application/json')) {
        data = await response.json();
      } else {
        data = await response.text();
      }

      if (!response.ok) {
        this.handleError({ response: { status: response.status, data } });
      }

      // Handle wrapped API responses
      if (data && typeof data === 'object' && 'isSuccess' in data) {
        if (!data.isSuccess) {
          this.handleError({
            response: {
              status: response.status,
              data: { errorMessage: data.errorMessage || data.message },
            },
          });
        }
        // Return the data property if it exists, otherwise return empty array for arrays or the data object
        if (data.data !== undefined) {
          return data.data;
        }
        // If no data property but isSuccess is true, return appropriate fallback
        return Array.isArray(data) ? data : (data.value || data);
      }

      return data;
    } catch (error) {
      this.handleError(error);
    }
  }

  // Convenience methods for common HTTP operations
  protected async get<T>(endpoint: string, params?: Record<string, any>): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET', params });
  }

  protected async post<T>(endpoint: string, body?: any): Promise<T> {
    return this.request<T>(endpoint, { method: 'POST', body });
  }

  protected async put<T>(endpoint: string, body?: any): Promise<T> {
    return this.request<T>(endpoint, { method: 'PUT', body });
  }

  protected async patch<T>(endpoint: string, body?: any): Promise<T> {
    return this.request<T>(endpoint, { method: 'PATCH', body });
  }

  protected async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }

  // Utility method for handling 404s gracefully (returns empty array)
  protected async getWithFallback<T>(
    endpoint: string,
    params?: Record<string, any>,
    fallbackValue: T = [] as any
  ): Promise<T> {
    try {
      return await this.get<T>(endpoint, params);
    } catch (error) {
      if (error instanceof NotFoundError) {
        console.warn(`Endpoint ${endpoint} not implemented, returning fallback value`);
        return fallbackValue;
      }
      throw error;
    }
  }

  // Bulk operations helper
  protected async bulkOperation<T>(
    endpoint: string,
    operation: string,
    ids: (string | number)[],
    additionalData?: any
  ): Promise<T> {
    return this.post<T>(endpoint, {
      operation,
      ids,
      ...additionalData,
    });
  }
}

export default BaseApiService;

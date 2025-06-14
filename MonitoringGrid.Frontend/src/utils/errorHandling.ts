import toast from 'react-hot-toast';
import {
  ApiError,
  ValidationError,
  AuthenticationError,
  AuthorizationError,
  NotFoundError,
} from '@/services/BaseApiService';

// Error severity levels
export enum ErrorSeverity {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high',
  CRITICAL = 'critical',
}

// Error categories for better handling
export enum ErrorCategory {
  NETWORK = 'network',
  VALIDATION = 'validation',
  AUTHENTICATION = 'authentication',
  AUTHORIZATION = 'authorization',
  NOT_FOUND = 'not_found',
  SERVER = 'server',
  CLIENT = 'client',
  UNKNOWN = 'unknown',
}

// Structured error information
export interface ErrorInfo {
  message: string;
  category: ErrorCategory;
  severity: ErrorSeverity;
  code?: string;
  details?: any;
  timestamp: Date;
  userFriendly: boolean;
  actionable: boolean;
  retryable: boolean;
}

// Error handling configuration
export interface ErrorHandlingConfig {
  showToast?: boolean;
  logToConsole?: boolean;
  logToService?: boolean;
  context?: string;
  fallbackMessage?: string;
  customHandler?: (error: ErrorInfo) => void;
}

// Default error messages for different categories
const DefaultErrorMessages = {
  [ErrorCategory.NETWORK]:
    'Network connection error. Please check your internet connection and try again.',
  [ErrorCategory.VALIDATION]: 'Please check your input and try again.',
  [ErrorCategory.AUTHENTICATION]: 'Authentication required. Please log in and try again.',
  [ErrorCategory.AUTHORIZATION]: 'You do not have permission to perform this action.',
  [ErrorCategory.NOT_FOUND]: 'The requested resource was not found.',
  [ErrorCategory.SERVER]: 'A server error occurred. Please try again later.',
  [ErrorCategory.CLIENT]: 'An error occurred while processing your request.',
  [ErrorCategory.UNKNOWN]: 'An unexpected error occurred. Please try again.',
} as const;

// User-friendly error messages
const UserFriendlyMessages = {
  'Failed to fetch': 'Unable to connect to the server. Please check your internet connection.',
  'Network request failed': 'Network error. Please try again.',
  Unauthorized: 'Please log in to continue.',
  Forbidden: 'You do not have permission to access this resource.',
  'Not Found': 'The requested item could not be found.',
  'Internal Server Error': 'A server error occurred. Our team has been notified.',
  'Bad Gateway': 'Service temporarily unavailable. Please try again in a few minutes.',
  'Service Unavailable': 'Service is currently unavailable. Please try again later.',
  'Gateway Timeout': 'Request timed out. Please try again.',
} as const;

// Extract error information from various error types
export function extractErrorInfo(error: any, context?: string): ErrorInfo {
  const timestamp = new Date();
  let message = 'An unexpected error occurred';
  let category = ErrorCategory.UNKNOWN;
  let severity = ErrorSeverity.MEDIUM;
  let code: string | undefined;
  let details: any;
  let userFriendly = false;
  let actionable = false;
  let retryable = false;

  // Handle specific error types
  if (error instanceof ValidationError) {
    category = ErrorCategory.VALIDATION;
    severity = ErrorSeverity.LOW;
    message = error.message;
    details = error.validationErrors;
    userFriendly = true;
    actionable = true;
    retryable = false;
  } else if (error instanceof AuthenticationError) {
    category = ErrorCategory.AUTHENTICATION;
    severity = ErrorSeverity.HIGH;
    message = error.message;
    userFriendly = true;
    actionable = true;
    retryable = false;
  } else if (error instanceof AuthorizationError) {
    category = ErrorCategory.AUTHORIZATION;
    severity = ErrorSeverity.MEDIUM;
    message = error.message;
    userFriendly = true;
    actionable = false;
    retryable = false;
  } else if (error instanceof NotFoundError) {
    category = ErrorCategory.NOT_FOUND;
    severity = ErrorSeverity.LOW;
    message = error.message;
    userFriendly = true;
    actionable = false;
    retryable = false;
  } else if (error instanceof ApiError) {
    category = error.status >= 500 ? ErrorCategory.SERVER : ErrorCategory.CLIENT;
    severity = error.status >= 500 ? ErrorSeverity.HIGH : ErrorSeverity.MEDIUM;
    message = error.message;
    code = error.code;
    details = error.response;
    userFriendly = true;
    actionable = error.status < 500;
    retryable = error.status >= 500 || error.status === 408 || error.status === 429;
  } else if (error?.name === 'TypeError' && error?.message?.includes('fetch')) {
    category = ErrorCategory.NETWORK;
    severity = ErrorSeverity.HIGH;
    message = UserFriendlyMessages['Failed to fetch'];
    userFriendly = true;
    actionable = true;
    retryable = true;
  } else if (error?.response) {
    // Axios-style error
    const status = error.response.status;
    const data = error.response.data;

    category = status >= 500 ? ErrorCategory.SERVER : ErrorCategory.CLIENT;
    severity = status >= 500 ? ErrorSeverity.HIGH : ErrorSeverity.MEDIUM;

    // Extract message from response
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
    } else {
      message = `HTTP ${status} Error`;
    }

    details = data;
    userFriendly = true;
    actionable = status < 500;
    retryable = status >= 500 || status === 408 || status === 429;
  } else if (error?.message) {
    message = error.message;

    // Check for known error patterns
    const knownMessage = Object.keys(UserFriendlyMessages).find(key => error.message.includes(key));

    if (knownMessage) {
      message = UserFriendlyMessages[knownMessage as keyof typeof UserFriendlyMessages];
      userFriendly = true;

      if (knownMessage.includes('Network') || knownMessage.includes('fetch')) {
        category = ErrorCategory.NETWORK;
        retryable = true;
      }
    }
  }

  // Add context to message if provided
  if (context) {
    message = `${context}: ${message}`;
  }

  return {
    message,
    category,
    severity,
    code,
    details,
    timestamp,
    userFriendly,
    actionable,
    retryable,
  };
}

// Handle errors with configurable behavior
export function handleError(error: any, config: ErrorHandlingConfig = {}): ErrorInfo {
  const {
    showToast = true,
    logToConsole = true,
    logToService = false,
    context,
    fallbackMessage,
    customHandler,
  } = config;

  const errorInfo = extractErrorInfo(error, context);

  // Use fallback message if provided and error is not user-friendly
  if (fallbackMessage && !errorInfo.userFriendly) {
    errorInfo.message = fallbackMessage;
    errorInfo.userFriendly = true;
  }

  // Log to console if enabled
  if (logToConsole) {
    const logLevel = errorInfo.severity === ErrorSeverity.CRITICAL ? 'error' : 'warn';
    console[logLevel]('Error handled:', {
      ...errorInfo,
      originalError: error,
    });
  }

  // Show toast notification if enabled
  if (showToast && errorInfo.userFriendly) {
    const toastOptions = {
      duration: errorInfo.severity === ErrorSeverity.LOW ? 3000 : 5000,
    };

    switch (errorInfo.severity) {
      case ErrorSeverity.LOW:
        toast(errorInfo.message, toastOptions);
        break;
      case ErrorSeverity.MEDIUM:
        toast.error(errorInfo.message, toastOptions);
        break;
      case ErrorSeverity.HIGH:
      case ErrorSeverity.CRITICAL:
        toast.error(errorInfo.message, { ...toastOptions, duration: 7000 });
        break;
    }
  }

  // Log to external service if enabled
  if (logToService) {
    // TODO: Implement external logging service
    // logToExternalService(errorInfo);
  }

  // Call custom handler if provided
  if (customHandler) {
    customHandler(errorInfo);
  }

  return errorInfo;
}

// Specialized error handlers for common scenarios
export const ErrorHandlers = {
  // For API mutations (show toast, log errors)
  mutation: (error: any, context?: string) =>
    handleError(error, {
      showToast: true,
      logToConsole: true,
      context,
    }),

  // For background queries (log only, no toast)
  query: (error: any, context?: string) =>
    handleError(error, {
      showToast: false,
      logToConsole: true,
      context,
    }),

  // For critical operations (show toast, log, and custom handling)
  critical: (error: any, context?: string, customHandler?: (error: ErrorInfo) => void) =>
    handleError(error, {
      showToast: true,
      logToConsole: true,
      logToService: true,
      context,
      customHandler,
    }),

  // For silent operations (log only)
  silent: (error: any, context?: string) =>
    handleError(error, {
      showToast: false,
      logToConsole: true,
      context,
    }),

  // For user-facing operations with custom fallback
  userFacing: (error: any, fallbackMessage: string, context?: string) =>
    handleError(error, {
      showToast: true,
      logToConsole: true,
      context,
      fallbackMessage,
    }),
} as const;

// Utility functions for error checking
export const ErrorUtils = {
  isRetryable: (error: any): boolean => {
    const errorInfo = extractErrorInfo(error);
    return errorInfo.retryable;
  },

  isUserError: (error: any): boolean => {
    const errorInfo = extractErrorInfo(error);
    return (
      errorInfo.category === ErrorCategory.VALIDATION ||
      errorInfo.category === ErrorCategory.AUTHENTICATION ||
      errorInfo.category === ErrorCategory.AUTHORIZATION
    );
  },

  isServerError: (error: any): boolean => {
    const errorInfo = extractErrorInfo(error);
    return errorInfo.category === ErrorCategory.SERVER;
  },

  isNetworkError: (error: any): boolean => {
    const errorInfo = extractErrorInfo(error);
    return errorInfo.category === ErrorCategory.NETWORK;
  },

  shouldShowToUser: (error: any): boolean => {
    const errorInfo = extractErrorInfo(error);
    return errorInfo.userFriendly;
  },
} as const;

export default {
  handleError,
  extractErrorInfo,
  ErrorHandlers,
  ErrorUtils,
  ErrorSeverity,
  ErrorCategory,
};

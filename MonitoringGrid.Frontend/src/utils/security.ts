/**
 * Security utilities and hardening functions
 */

// Content Security Policy configuration
export const CSP_DIRECTIVES = {
  'default-src': ["'self'"],
  'script-src': [
    "'self'",
    "'unsafe-inline'", // Required for React development
    "'unsafe-eval'", // Required for development tools
    'https://cdn.jsdelivr.net',
    'https://unpkg.com',
  ],
  'style-src': [
    "'self'",
    "'unsafe-inline'", // Required for styled-components and MUI
    'https://fonts.googleapis.com',
  ],
  'font-src': [
    "'self'",
    'data:',
    'https://fonts.gstatic.com',
  ],
  'img-src': [
    "'self'",
    'data:',
    'https:',
    'blob:',
  ],
  'connect-src': [
    "'self'",
    'https:',
    'wss:',
    'ws:',
  ],
  'media-src': ["'self'"],
  'object-src': ["'none'"],
  'base-uri': ["'self'"],
  'form-action': ["'self'"],
  'frame-ancestors': ["'none'"],
  'upgrade-insecure-requests': [],
};

// Generate CSP header string
export const generateCSPHeader = (): string => {
  return Object.entries(CSP_DIRECTIVES)
    .map(([directive, sources]) => {
      if (sources.length === 0) {
        return directive;
      }
      return `${directive} ${sources.join(' ')}`;
    })
    .join('; ');
};

// Security headers configuration
export const SECURITY_HEADERS = {
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'DENY',
  'X-XSS-Protection': '1; mode=block',
  'Referrer-Policy': 'strict-origin-when-cross-origin',
  'Permissions-Policy': 'camera=(), microphone=(), geolocation=(), payment=()',
  'Strict-Transport-Security': 'max-age=31536000; includeSubDomains; preload',
  'Content-Security-Policy': generateCSPHeader(),
};

// Input sanitization
export const sanitizeInput = (input: string): string => {
  if (typeof input !== 'string') {
    return '';
  }

  return input
    .replace(/[<>]/g, '') // Remove potential HTML tags
    .replace(/javascript:/gi, '') // Remove javascript: protocol
    .replace(/on\w+=/gi, '') // Remove event handlers
    .trim();
};

// HTML sanitization for rich content
export const sanitizeHTML = (html: string): string => {
  const div = document.createElement('div');
  div.textContent = html;
  return div.innerHTML;
};

// URL validation
export const isValidURL = (url: string): boolean => {
  try {
    const urlObj = new URL(url);
    return ['http:', 'https:'].includes(urlObj.protocol);
  } catch {
    return false;
  }
};

// Safe URL creation
export const createSafeURL = (url: string, baseURL?: string): string | null => {
  try {
    const urlObj = new URL(url, baseURL);
    if (['http:', 'https:'].includes(urlObj.protocol)) {
      return urlObj.toString();
    }
    return null;
  } catch {
    return null;
  }
};

// Token validation
export const isValidJWT = (token: string): boolean => {
  if (!token || typeof token !== 'string') {
    return false;
  }

  const parts = token.split('.');
  if (parts.length !== 3) {
    return false;
  }

  try {
    // Validate base64 encoding
    parts.forEach(part => {
      atob(part.replace(/-/g, '+').replace(/_/g, '/'));
    });
    return true;
  } catch {
    return false;
  }
};

// Secure token storage
export const secureStorage = {
  setItem: (key: string, value: string): void => {
    try {
      // Use sessionStorage for sensitive data instead of localStorage
      sessionStorage.setItem(key, value);
    } catch (error) {
      console.error('Failed to store secure item:', error);
    }
  },

  getItem: (key: string): string | null => {
    try {
      return sessionStorage.getItem(key);
    } catch (error) {
      console.error('Failed to retrieve secure item:', error);
      return null;
    }
  },

  removeItem: (key: string): void => {
    try {
      sessionStorage.removeItem(key);
    } catch (error) {
      console.error('Failed to remove secure item:', error);
    }
  },

  clear: (): void => {
    try {
      sessionStorage.clear();
    } catch (error) {
      console.error('Failed to clear secure storage:', error);
    }
  },
};

// Password strength validation
export const validatePasswordStrength = (password: string): {
  isValid: boolean;
  score: number;
  feedback: string[];
} => {
  const feedback: string[] = [];
  let score = 0;

  if (password.length < 8) {
    feedback.push('Password must be at least 8 characters long');
  } else {
    score += 1;
  }

  if (!/[a-z]/.test(password)) {
    feedback.push('Password must contain at least one lowercase letter');
  } else {
    score += 1;
  }

  if (!/[A-Z]/.test(password)) {
    feedback.push('Password must contain at least one uppercase letter');
  } else {
    score += 1;
  }

  if (!/\d/.test(password)) {
    feedback.push('Password must contain at least one number');
  } else {
    score += 1;
  }

  if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
    feedback.push('Password must contain at least one special character');
  } else {
    score += 1;
  }

  return {
    isValid: score >= 4,
    score,
    feedback,
  };
};

// Rate limiting utility
class RateLimiter {
  private attempts: Map<string, number[]> = new Map();

  public isAllowed(key: string, maxAttempts: number = 5, windowMs: number = 60000): boolean {
    const now = Date.now();
    const attempts = this.attempts.get(key) || [];
    
    // Remove old attempts outside the window
    const validAttempts = attempts.filter(timestamp => now - timestamp < windowMs);
    
    if (validAttempts.length >= maxAttempts) {
      return false;
    }

    validAttempts.push(now);
    this.attempts.set(key, validAttempts);
    return true;
  }

  public reset(key: string): void {
    this.attempts.delete(key);
  }

  public cleanup(): void {
    const now = Date.now();
    for (const [key, attempts] of this.attempts.entries()) {
      const validAttempts = attempts.filter(timestamp => now - timestamp < 300000); // 5 minutes
      if (validAttempts.length === 0) {
        this.attempts.delete(key);
      } else {
        this.attempts.set(key, validAttempts);
      }
    }
  }
}

export const rateLimiter = new RateLimiter();

// CSRF token management
export const csrfToken = {
  generate: (): string => {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
  },

  validate: (token: string): boolean => {
    return typeof token === 'string' && token.length === 64 && /^[a-f0-9]+$/.test(token);
  },
};

// Secure random string generation
export const generateSecureRandom = (length: number = 32): string => {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
};

// Environment validation
export const validateEnvironment = (): void => {
  // Check for development tools in production
  if (process.env.NODE_ENV === 'production') {
    // Disable console in production
    if (typeof console !== 'undefined') {
      console.log = () => {};
      console.warn = () => {};
      console.error = () => {};
    }

    // Check for debugging tools
    const devtools = /./;
    devtools.toString = function() {
      throw new Error('Developer tools detected');
    };

    // Disable right-click context menu
    document.addEventListener('contextmenu', (e) => {
      e.preventDefault();
    });

    // Disable F12, Ctrl+Shift+I, Ctrl+U
    document.addEventListener('keydown', (e) => {
      if (
        e.key === 'F12' ||
        (e.ctrlKey && e.shiftKey && e.key === 'I') ||
        (e.ctrlKey && e.key === 'U')
      ) {
        e.preventDefault();
      }
    });
  }
};

// Security audit function
export const performSecurityAudit = (): {
  passed: boolean;
  issues: string[];
} => {
  const issues: string[] = [];

  // Check HTTPS
  if (location.protocol !== 'https:' && location.hostname !== 'localhost') {
    issues.push('Application should be served over HTTPS');
  }

  // Check for mixed content
  if (location.protocol === 'https:') {
    const insecureResources = Array.from(document.querySelectorAll('script[src], link[href], img[src]'))
      .filter((element: any) => {
        const url = element.src || element.href;
        return url && url.startsWith('http:');
      });

    if (insecureResources.length > 0) {
      issues.push('Mixed content detected: insecure resources loaded over HTTPS');
    }
  }

  // Check for inline scripts (basic check)
  const inlineScripts = document.querySelectorAll('script:not([src])');
  if (inlineScripts.length > 0) {
    issues.push('Inline scripts detected - consider using CSP with nonce');
  }

  // Check localStorage usage for sensitive data
  try {
    const localStorageKeys = Object.keys(localStorage);
    const sensitiveKeys = localStorageKeys.filter(key => 
      key.toLowerCase().includes('token') || 
      key.toLowerCase().includes('password') ||
      key.toLowerCase().includes('secret')
    );
    
    if (sensitiveKeys.length > 0) {
      issues.push('Sensitive data found in localStorage - consider using sessionStorage');
    }
  } catch (error) {
    // localStorage not available
  }

  return {
    passed: issues.length === 0,
    issues,
  };
};

import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import {
  performSecurityAudit,
  validateEnvironment,
  rateLimiter,
  secureStorage,
  csrfToken,
} from '@/utils/security';

interface SecurityContextType {
  isSecure: boolean;
  securityIssues: string[];
  csrfToken: string;
  refreshSecurityAudit: () => void;
  checkRateLimit: (key: string, maxAttempts?: number, windowMs?: number) => boolean;
  resetRateLimit: (key: string) => void;
  secureStore: typeof secureStorage;
}

const SecurityContext = createContext<SecurityContextType | undefined>(undefined);

interface SecurityProviderProps {
  children: ReactNode;
  enableAutoAudit?: boolean;
  auditInterval?: number;
}

export const SecurityProvider: React.FC<SecurityProviderProps> = ({
  children,
  enableAutoAudit = true,
  auditInterval = 300000, // 5 minutes
}) => {
  const [isSecure, setIsSecure] = useState(true);
  const [securityIssues, setSecurityIssues] = useState<string[]>([]);
  const [currentCsrfToken, setCurrentCsrfToken] = useState('');

  const refreshSecurityAudit = () => {
    const audit = performSecurityAudit();
    setIsSecure(audit.passed);
    setSecurityIssues(audit.issues);

    if (!audit.passed) {
      console.warn('Security audit failed:', audit.issues);
    }
  };

  const checkRateLimit = (key: string, maxAttempts = 5, windowMs = 60000): boolean => {
    return rateLimiter.isAllowed(key, maxAttempts, windowMs);
  };

  const resetRateLimit = (key: string): void => {
    rateLimiter.reset(key);
  };

  // Initialize security measures
  useEffect(() => {
    // Validate environment
    validateEnvironment();

    // Generate initial CSRF token
    setCurrentCsrfToken(csrfToken.generate());

    // Perform initial security audit
    if (enableAutoAudit) {
      refreshSecurityAudit();
    }

    // Set up periodic security audits
    let auditIntervalId: NodeJS.Timeout;
    if (enableAutoAudit) {
      auditIntervalId = setInterval(refreshSecurityAudit, auditInterval);
    }

    // Set up rate limiter cleanup
    const cleanupInterval = setInterval(() => {
      rateLimiter.cleanup();
    }, 300000); // 5 minutes

    return () => {
      if (auditIntervalId) {
        clearInterval(auditIntervalId);
      }
      clearInterval(cleanupInterval);
    };
  }, [enableAutoAudit, auditInterval]);

  // Monitor for security violations
  useEffect(() => {
    if (!isSecure && securityIssues.length > 0) {
      // In production, send security alerts to monitoring service
      if (process.env.NODE_ENV === 'production') {
        // Example: securityMonitoring.alert('security_audit_failed', { issues: securityIssues });
      }
    }
  }, [isSecure, securityIssues]);

  const value: SecurityContextType = {
    isSecure,
    securityIssues,
    csrfToken: currentCsrfToken,
    refreshSecurityAudit,
    checkRateLimit,
    resetRateLimit,
    secureStore: secureStorage,
  };

  return (
    <SecurityContext.Provider value={value}>
      {children}
    </SecurityContext.Provider>
  );
};

export const useSecurity = (): SecurityContextType => {
  const context = useContext(SecurityContext);
  if (context === undefined) {
    throw new Error('useSecurity must be used within a SecurityProvider');
  }
  return context;
};

// Hook for secure API calls
export const useSecureApi = () => {
  const { csrfToken: token, checkRateLimit } = useSecurity();

  const secureRequest = async (
    url: string,
    options: RequestInit = {},
    rateLimitKey?: string
  ): Promise<Response> => {
    // Check rate limiting
    if (rateLimitKey && !checkRateLimit(rateLimitKey)) {
      throw new Error('Rate limit exceeded');
    }

    // Add security headers
    const secureHeaders = {
      'Content-Type': 'application/json',
      'X-CSRF-Token': token,
      'X-Requested-With': 'XMLHttpRequest',
      ...options.headers,
    };

    const secureOptions: RequestInit = {
      ...options,
      headers: secureHeaders,
      credentials: 'same-origin',
    };

    return fetch(url, secureOptions);
  };

  return { secureRequest };
};

// Hook for input validation
export const useInputValidation = () => {
  const validateInput = (input: string, type: 'text' | 'email' | 'url' = 'text'): {
    isValid: boolean;
    sanitized: string;
    errors: string[];
  } => {
    const errors: string[] = [];
    let sanitized = input.trim();

    // Basic sanitization
    sanitized = sanitized.replace(/[<>]/g, '');

    // Type-specific validation
    switch (type) {
      case 'email':
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(sanitized)) {
          errors.push('Invalid email format');
        }
        break;
      case 'url':
        try {
          new URL(sanitized);
        } catch {
          errors.push('Invalid URL format');
        }
        break;
    }

    // Check for potential XSS
    if (sanitized.includes('javascript:') || sanitized.includes('data:')) {
      errors.push('Potentially unsafe content detected');
    }

    return {
      isValid: errors.length === 0,
      sanitized,
      errors,
    };
  };

  return { validateInput };
};

// Security monitoring component
export const SecurityMonitor: React.FC = () => {
  const { isSecure, securityIssues } = useSecurity();

  if (process.env.NODE_ENV !== 'development') {
    return null;
  }

  return (
    <div
      style={{
        position: 'fixed',
        top: 10,
        left: 10,
        background: isSecure ? 'rgba(0, 128, 0, 0.8)' : 'rgba(255, 0, 0, 0.8)',
        color: 'white',
        padding: '10px',
        borderRadius: '5px',
        fontSize: '12px',
        fontFamily: 'monospace',
        zIndex: 9999,
        maxWidth: '300px',
      }}
    >
      <div>Security Status: {isSecure ? '🔒 Secure' : '⚠️ Issues Detected'}</div>
      {securityIssues.length > 0 && (
        <div style={{ marginTop: '5px' }}>
          <div>Issues:</div>
          <ul style={{ margin: '5px 0', paddingLeft: '15px' }}>
            {securityIssues.map((issue, index) => (
              <li key={index} style={{ fontSize: '10px' }}>
                {issue}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

// HOC for securing components
export const withSecurity = <P extends object>(
  Component: React.ComponentType<P>
): React.ComponentType<P> => {
  return (props: P) => {
    const { isSecure } = useSecurity();

    if (!isSecure && process.env.NODE_ENV === 'production') {
      return (
        <div style={{ padding: '20px', textAlign: 'center' }}>
          <h2>Security Check Failed</h2>
          <p>Please refresh the page and try again.</p>
        </div>
      );
    }

    return <Component {...props} />;
  };
};

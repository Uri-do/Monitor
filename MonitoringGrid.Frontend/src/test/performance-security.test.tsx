import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@/test/utils';
import { 
  initializePerformanceMonitoring, 
  getPerformanceMetrics,
  checkPerformanceBudget,
  cleanupPerformanceMonitoring 
} from '@/utils/performance';
import { 
  performSecurityAudit,
  sanitizeInput,
  isValidJWT,
  validatePasswordStrength,
  rateLimiter,
  csrfToken 
} from '@/utils/security';
import { PerformanceProvider, usePerformance } from '@/contexts/PerformanceContext';
import { SecurityProvider, useSecurity } from '@/contexts/SecurityContext';

// Mock performance API
Object.defineProperty(window, 'performance', {
  value: {
    now: vi.fn(() => Date.now()),
    getEntriesByType: vi.fn(() => []),
    memory: {
      usedJSHeapSize: 1024 * 1024 * 10, // 10MB
      totalJSHeapSize: 1024 * 1024 * 50, // 50MB
      jsHeapSizeLimit: 1024 * 1024 * 100, // 100MB
    },
  },
});

// Mock PerformanceObserver
global.PerformanceObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  disconnect: vi.fn(),
}));

describe('Performance Optimization', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Performance Monitoring', () => {
    it('initializes performance monitoring', () => {
      const monitor = initializePerformanceMonitoring();
      expect(monitor).toBeDefined();
    });

    it('gets performance metrics', () => {
      initializePerformanceMonitoring();
      const metrics = getPerformanceMetrics();
      expect(metrics).toBeDefined();
      expect(typeof metrics).toBe('object');
    });

    it('checks performance budget', () => {
      const metrics = {
        fcp: 2000, // Over budget (1800ms)
        lcp: 2000, // Under budget (2500ms)
        fid: 150,  // Over budget (100ms)
        cls: 0.05, // Under budget (0.1)
        ttfb: 500, // Under budget (600ms)
      };

      const violations = checkPerformanceBudget(metrics);
      expect(violations).toContain('FCP: 2000 > 1800');
      expect(violations).toContain('FID: 150 > 100');
      expect(violations).not.toContain('LCP');
      expect(violations).not.toContain('CLS');
      expect(violations).not.toContain('TTFB');
    });

    it('cleans up performance monitoring', () => {
      const monitor = initializePerformanceMonitoring();
      expect(() => cleanupPerformanceMonitoring()).not.toThrow();
    });
  });

  describe('Performance Context', () => {
    it('provides performance data', () => {
      const TestComponent = () => {
        const { metrics, isMonitoring } = usePerformance();
        return (
          <div>
            <span data-testid="monitoring">{isMonitoring ? 'active' : 'inactive'}</span>
            <span data-testid="metrics">{JSON.stringify(metrics)}</span>
          </div>
        );
      };

      render(
        <PerformanceProvider>
          <TestComponent />
        </PerformanceProvider>
      );

      expect(screen.getByTestId('monitoring')).toHaveTextContent('active');
      expect(screen.getByTestId('metrics')).toBeInTheDocument();
    });
  });
});

describe('Security Hardening', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Security Audit', () => {
    it('performs security audit', () => {
      const audit = performSecurityAudit();
      expect(audit).toHaveProperty('passed');
      expect(audit).toHaveProperty('issues');
      expect(Array.isArray(audit.issues)).toBe(true);
    });
  });

  describe('Input Sanitization', () => {
    it('sanitizes malicious input', () => {
      const maliciousInput = '<script>alert("xss")</script>Hello';
      const sanitized = sanitizeInput(maliciousInput);
      expect(sanitized).not.toContain('<script>');
      expect(sanitized).not.toContain('</script>');
      expect(sanitized).toContain('Hello');
    });

    it('removes javascript protocols', () => {
      const maliciousInput = 'javascript:alert("xss")';
      const sanitized = sanitizeInput(maliciousInput);
      expect(sanitized).not.toContain('javascript:');
    });

    it('removes event handlers', () => {
      const maliciousInput = 'onclick=alert("xss")';
      const sanitized = sanitizeInput(maliciousInput);
      expect(sanitized).not.toContain('onclick=');
    });
  });

  describe('JWT Validation', () => {
    it('validates correct JWT format', () => {
      const validJWT = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
      expect(isValidJWT(validJWT)).toBe(true);
    });

    it('rejects invalid JWT format', () => {
      expect(isValidJWT('invalid.jwt')).toBe(false);
      expect(isValidJWT('not.a.jwt.token')).toBe(false);
      expect(isValidJWT('')).toBe(false);
      expect(isValidJWT('null')).toBe(false);
    });
  });

  describe('Password Strength Validation', () => {
    it('validates strong password', () => {
      const result = validatePasswordStrength('StrongP@ssw0rd123');
      expect(result.isValid).toBe(true);
      expect(result.score).toBe(5);
      expect(result.feedback).toHaveLength(0);
    });

    it('rejects weak password', () => {
      const result = validatePasswordStrength('weak');
      expect(result.isValid).toBe(false);
      expect(result.score).toBeLessThan(4);
      expect(result.feedback.length).toBeGreaterThan(0);
    });

    it('provides specific feedback', () => {
      const result = validatePasswordStrength('password');
      expect(result.feedback).toContain('Password must contain at least one uppercase letter');
      expect(result.feedback).toContain('Password must contain at least one number');
      expect(result.feedback).toContain('Password must contain at least one special character');
    });
  });

  describe('Rate Limiting', () => {
    beforeEach(() => {
      // Reset rate limiter state
      rateLimiter.reset('test-key');
    });

    it('allows requests within limit', () => {
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(true);
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(true);
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(true);
    });

    it('blocks requests over limit', () => {
      // Make 5 requests (at limit)
      for (let i = 0; i < 5; i++) {
        expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(true);
      }
      
      // 6th request should be blocked
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(false);
    });

    it('resets rate limit', () => {
      // Fill up the limit
      for (let i = 0; i < 5; i++) {
        rateLimiter.isAllowed('test-key', 5, 60000);
      }
      
      // Should be blocked
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(false);
      
      // Reset and try again
      rateLimiter.reset('test-key');
      expect(rateLimiter.isAllowed('test-key', 5, 60000)).toBe(true);
    });
  });

  describe('CSRF Token', () => {
    it('generates valid CSRF token', () => {
      const token = csrfToken.generate();
      expect(token).toHaveLength(64);
      expect(/^[a-f0-9]+$/.test(token)).toBe(true);
    });

    it('validates CSRF token', () => {
      const validToken = 'a'.repeat(64);
      const invalidToken = 'invalid';
      
      expect(csrfToken.validate(validToken)).toBe(true);
      expect(csrfToken.validate(invalidToken)).toBe(false);
    });
  });

  describe('Security Context', () => {
    it('provides security status', () => {
      const TestComponent = () => {
        const { isSecure, securityIssues } = useSecurity();
        return (
          <div>
            <span data-testid="secure">{isSecure ? 'secure' : 'insecure'}</span>
            <span data-testid="issues">{securityIssues.length}</span>
          </div>
        );
      };

      render(
        <SecurityProvider>
          <TestComponent />
        </SecurityProvider>
      );

      expect(screen.getByTestId('secure')).toBeInTheDocument();
      expect(screen.getByTestId('issues')).toBeInTheDocument();
    });
  });
});

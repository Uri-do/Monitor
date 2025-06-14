/**
 * Advanced Security Implementation
 * Enterprise-grade security utilities and protection mechanisms
 */

// Security configuration interface
export interface SecurityConfig {
  csp: {
    enabled: boolean;
    directives: Record<string, string[]>;
    reportUri?: string;
    reportOnly?: boolean;
  };
  xss: {
    enabled: boolean;
    sanitizeInputs: boolean;
    allowedTags: string[];
    allowedAttributes: Record<string, string[]>;
  };
  csrf: {
    enabled: boolean;
    tokenName: string;
    cookieName: string;
    headerName: string;
  };
  rateLimit: {
    enabled: boolean;
    maxRequests: number;
    windowMs: number;
    blockDuration: number;
  };
  encryption: {
    algorithm: string;
    keySize: number;
    ivSize: number;
  };
}

// Default enterprise security configuration
export const ENTERPRISE_SECURITY_CONFIG: SecurityConfig = {
  csp: {
    enabled: true,
    directives: {
      'default-src': ["'self'"],
      'script-src': ["'self'", "'unsafe-inline'", "'unsafe-eval'"],
      'style-src': ["'self'", "'unsafe-inline'", 'https://fonts.googleapis.com'],
      'font-src': ["'self'", 'https://fonts.gstatic.com'],
      'img-src': ["'self'", 'data:', 'https:'],
      'connect-src': ["'self'", 'wss:', 'https:'],
      'frame-ancestors': ["'none'"],
      'base-uri': ["'self'"],
      'form-action': ["'self'"],
    },
    reportUri: '/api/csp-report',
    reportOnly: false,
  },
  xss: {
    enabled: true,
    sanitizeInputs: true,
    allowedTags: ['b', 'i', 'em', 'strong', 'a', 'p', 'br'],
    allowedAttributes: {
      'a': ['href', 'title'],
      '*': ['class', 'id'],
    },
  },
  csrf: {
    enabled: true,
    tokenName: 'csrfToken',
    cookieName: 'XSRF-TOKEN',
    headerName: 'X-XSRF-TOKEN',
  },
  rateLimit: {
    enabled: true,
    maxRequests: 100,
    windowMs: 15 * 60 * 1000, // 15 minutes
    blockDuration: 60 * 1000, // 1 minute
  },
  encryption: {
    algorithm: 'AES-GCM',
    keySize: 256,
    ivSize: 12,
  },
};

// Advanced security manager
export class AdvancedSecurityManager {
  private config: SecurityConfig;
  private rateLimitStore = new Map<string, { count: number; resetTime: number; blocked?: number }>();
  private csrfToken: string | null = null;

  constructor(config: SecurityConfig = ENTERPRISE_SECURITY_CONFIG) {
    this.config = config;
    this.initialize();
  }

  private initialize(): void {
    this.setupCSP();
    this.setupXSSProtection();
    this.setupCSRFProtection();
    this.setupSecurityHeaders();
    this.setupInputValidation();
  }

  // Content Security Policy setup
  private setupCSP(): void {
    if (!this.config.csp.enabled) return;

    const cspString = Object.entries(this.config.csp.directives)
      .map(([directive, sources]) => `${directive} ${sources.join(' ')}`)
      .join('; ');

    const metaTag = document.createElement('meta');
    metaTag.httpEquiv = this.config.csp.reportOnly ? 'Content-Security-Policy-Report-Only' : 'Content-Security-Policy';
    metaTag.content = cspString;
    document.head.appendChild(metaTag);

    // CSP violation reporting
    if (this.config.csp.reportUri) {
      document.addEventListener('securitypolicyviolation', (event) => {
        this.reportCSPViolation(event);
      });
    }
  }

  private reportCSPViolation(event: SecurityPolicyViolationEvent): void {
    const report = {
      documentUri: event.documentURI,
      referrer: event.referrer,
      violatedDirective: event.violatedDirective,
      effectiveDirective: event.effectiveDirective,
      originalPolicy: event.originalPolicy,
      blockedUri: event.blockedURI,
      statusCode: event.statusCode,
      timestamp: new Date().toISOString(),
    };

    // Send report to server
    fetch(this.config.csp.reportUri!, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(report),
    }).catch(console.error);
  }

  // XSS Protection setup
  private setupXSSProtection(): void {
    if (!this.config.xss.enabled) return;

    // Add XSS protection headers
    const metaTag = document.createElement('meta');
    metaTag.httpEquiv = 'X-XSS-Protection';
    metaTag.content = '1; mode=block';
    document.head.appendChild(metaTag);
  }

  // CSRF Protection setup
  private setupCSRFProtection(): void {
    if (!this.config.csrf.enabled) return;

    // Generate CSRF token
    this.csrfToken = this.generateSecureToken();
    
    // Set CSRF token in cookie
    document.cookie = `${this.config.csrf.cookieName}=${this.csrfToken}; Secure; SameSite=Strict`;
  }

  // Security headers setup
  private setupSecurityHeaders(): void {
    // These would typically be set by the server, but we can add meta tags for some
    const headers = [
      { name: 'X-Content-Type-Options', content: 'nosniff' },
      { name: 'X-Frame-Options', content: 'DENY' },
      { name: 'Referrer-Policy', content: 'strict-origin-when-cross-origin' },
    ];

    headers.forEach(header => {
      const metaTag = document.createElement('meta');
      metaTag.httpEquiv = header.name;
      metaTag.content = header.content;
      document.head.appendChild(metaTag);
    });
  }

  // Input validation and sanitization
  private setupInputValidation(): void {
    if (!this.config.xss.sanitizeInputs) return;

    // Monitor form inputs for potential XSS
    document.addEventListener('input', (event) => {
      const target = event.target as HTMLInputElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
        const sanitized = this.sanitizeInput(target.value);
        if (sanitized !== target.value) {
          console.warn('Potentially malicious input detected and sanitized');
          target.value = sanitized;
        }
      }
    });
  }

  // Input sanitization
  public sanitizeInput(input: string): string {
    if (!this.config.xss.sanitizeInputs) return input;

    // Basic XSS prevention - remove script tags and event handlers
    let sanitized = input
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
      .replace(/on\w+\s*=\s*["'][^"']*["']/gi, '')
      .replace(/javascript:/gi, '')
      .replace(/vbscript:/gi, '')
      .replace(/data:text\/html/gi, '');

    // Additional sanitization based on allowed tags
    const allowedTagsRegex = new RegExp(
      `<(?!\/?(?:${this.config.xss.allowedTags.join('|')})\s*\/?>)[^>]+>`,
      'gi'
    );
    sanitized = sanitized.replace(allowedTagsRegex, '');

    return sanitized;
  }

  // Rate limiting
  public checkRateLimit(identifier: string): boolean {
    if (!this.config.rateLimit.enabled) return true;

    const now = Date.now();
    const entry = this.rateLimitStore.get(identifier);

    // Check if currently blocked
    if (entry?.blocked && now < entry.blocked) {
      return false;
    }

    // Reset window if expired
    if (!entry || now > entry.resetTime) {
      this.rateLimitStore.set(identifier, {
        count: 1,
        resetTime: now + this.config.rateLimit.windowMs,
      });
      return true;
    }

    // Increment count
    entry.count++;

    // Check if limit exceeded
    if (entry.count > this.config.rateLimit.maxRequests) {
      entry.blocked = now + this.config.rateLimit.blockDuration;
      return false;
    }

    return true;
  }

  // Secure token generation
  public generateSecureToken(length: number = 32): string {
    const array = new Uint8Array(length);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
  }

  // Encryption utilities
  public async encryptData(data: string, key?: CryptoKey): Promise<{ encrypted: string; iv: string }> {
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);

    // Generate or use provided key
    const cryptoKey = key || await crypto.subtle.generateKey(
      { name: this.config.encryption.algorithm, length: this.config.encryption.keySize },
      false,
      ['encrypt', 'decrypt']
    );

    // Generate IV
    const iv = crypto.getRandomValues(new Uint8Array(this.config.encryption.ivSize));

    // Encrypt data
    const encrypted = await crypto.subtle.encrypt(
      { name: this.config.encryption.algorithm, iv },
      cryptoKey,
      dataBuffer
    );

    return {
      encrypted: this.arrayBufferToBase64(encrypted),
      iv: this.arrayBufferToBase64(iv),
    };
  }

  public async decryptData(encryptedData: string, iv: string, key: CryptoKey): Promise<string> {
    const encrypted = this.base64ToArrayBuffer(encryptedData);
    const ivBuffer = this.base64ToArrayBuffer(iv);

    const decrypted = await crypto.subtle.decrypt(
      { name: this.config.encryption.algorithm, iv: ivBuffer },
      key,
      encrypted
    );

    const decoder = new TextDecoder();
    return decoder.decode(decrypted);
  }

  // Utility methods
  private arrayBufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
  }

  // Security audit
  public performSecurityAudit(): SecurityAuditReport {
    const report: SecurityAuditReport = {
      timestamp: new Date().toISOString(),
      csp: {
        enabled: this.config.csp.enabled,
        violations: 0, // Would be tracked in real implementation
      },
      xss: {
        enabled: this.config.xss.enabled,
        inputsSanitized: this.config.xss.sanitizeInputs,
      },
      csrf: {
        enabled: this.config.csrf.enabled,
        tokenPresent: !!this.csrfToken,
      },
      rateLimit: {
        enabled: this.config.rateLimit.enabled,
        activeBlocks: Array.from(this.rateLimitStore.values()).filter(entry => 
          entry.blocked && Date.now() < entry.blocked
        ).length,
      },
      recommendations: this.generateSecurityRecommendations(),
    };

    return report;
  }

  private generateSecurityRecommendations(): string[] {
    const recommendations: string[] = [];

    if (!this.config.csp.enabled) {
      recommendations.push('Enable Content Security Policy for XSS protection');
    }

    if (!this.config.csrf.enabled) {
      recommendations.push('Enable CSRF protection for form submissions');
    }

    if (!this.config.rateLimit.enabled) {
      recommendations.push('Enable rate limiting to prevent abuse');
    }

    if (!this.config.xss.sanitizeInputs) {
      recommendations.push('Enable input sanitization to prevent XSS attacks');
    }

    return recommendations;
  }

  // Get CSRF token for requests
  public getCSRFToken(): string | null {
    return this.csrfToken;
  }

  // Update security configuration
  public updateConfig(newConfig: Partial<SecurityConfig>): void {
    this.config = { ...this.config, ...newConfig };
    this.initialize();
  }
}

// Security audit report interface
export interface SecurityAuditReport {
  timestamp: string;
  csp: {
    enabled: boolean;
    violations: number;
  };
  xss: {
    enabled: boolean;
    inputsSanitized: boolean;
  };
  csrf: {
    enabled: boolean;
    tokenPresent: boolean;
  };
  rateLimit: {
    enabled: boolean;
    activeBlocks: number;
  };
  recommendations: string[];
}

// Singleton instance
let securityManager: AdvancedSecurityManager | null = null;

export const initializeAdvancedSecurity = (config?: SecurityConfig): AdvancedSecurityManager => {
  if (!securityManager) {
    securityManager = new AdvancedSecurityManager(config);
  }
  return securityManager;
};

export const getSecurityManager = (): AdvancedSecurityManager | null => {
  return securityManager;
};

// React hook for security features
export const useSecurity = () => {
  const manager = getSecurityManager();
  
  return {
    sanitizeInput: (input: string) => manager?.sanitizeInput(input) || input,
    checkRateLimit: (identifier: string) => manager?.checkRateLimit(identifier) || true,
    generateToken: (length?: number) => manager?.generateSecureToken(length) || '',
    getCSRFToken: () => manager?.getCSRFToken(),
    performAudit: () => manager?.performSecurityAudit(),
  };
};

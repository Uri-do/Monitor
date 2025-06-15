import React, { useEffect } from 'react';
import { Helmet } from 'react-helmet-async';
import { SECURITY_HEADERS, generateCSPHeader } from '@/utils/security';

interface SecurityHeadersProps {
  title?: string;
  description?: string;
  keywords?: string;
  canonical?: string;
  noIndex?: boolean;
  customCSP?: Record<string, string[]>;
}

export const SecurityHeaders: React.FC<SecurityHeadersProps> = ({
  title = 'MonitoringGrid - Advanced Monitoring Dashboard',
  description = 'Comprehensive monitoring and analytics dashboard for system performance tracking',
  keywords = 'monitoring, dashboard, analytics, performance, system tracking',
  canonical,
  noIndex = false,
  customCSP,
}) => {
  // Generate CSP with custom directives if provided
  const cspHeader = generateCSPHeader();

  useEffect(() => {
    // Set security headers programmatically for SPA
    const metaTags = [
      { name: 'X-Content-Type-Options', content: 'nosniff' },
      { name: 'X-Frame-Options', content: 'DENY' },
      { name: 'X-XSS-Protection', content: '1; mode=block' },
      { name: 'Referrer-Policy', content: 'strict-origin-when-cross-origin' },
      { name: 'Permissions-Policy', content: 'camera=(), microphone=(), geolocation=(), payment=()' },
    ];

    metaTags.forEach(({ name, content }) => {
      let meta = document.querySelector(`meta[name="${name}"]`) as HTMLMetaElement;
      if (!meta) {
        meta = document.createElement('meta');
        meta.name = name;
        document.head.appendChild(meta);
      }
      meta.content = content;
    });

    // Set CSP meta tag
    let cspMeta = document.querySelector('meta[http-equiv="Content-Security-Policy"]') as HTMLMetaElement;
    if (!cspMeta) {
      cspMeta = document.createElement('meta');
      cspMeta.httpEquiv = 'Content-Security-Policy';
      document.head.appendChild(cspMeta);
    }
    cspMeta.content = cspHeader;

    return () => {
      // Cleanup is not necessary for meta tags as they should persist
    };
  }, [cspHeader]);

  return (
    <Helmet>
      {/* Basic Meta Tags */}
      <title>{title}</title>
      <meta name="description" content={description} />
      <meta name="keywords" content={keywords} />
      
      {/* Viewport and Mobile */}
      <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=5.0" />
      <meta name="format-detection" content="telephone=no" />
      
      {/* Security Headers */}
      <meta httpEquiv="X-Content-Type-Options" content="nosniff" />
      <meta httpEquiv="X-Frame-Options" content="DENY" />
      <meta httpEquiv="X-XSS-Protection" content="1; mode=block" />
      <meta name="referrer" content="strict-origin-when-cross-origin" />
      <meta httpEquiv="Permissions-Policy" content="camera=(), microphone=(), geolocation=(), payment=()" />
      
      {/* Content Security Policy */}
      <meta httpEquiv="Content-Security-Policy" content={cspHeader} />
      
      {/* HTTPS and Security */}
      {window.location.protocol === 'https:' && (
        <meta httpEquiv="Strict-Transport-Security" content="max-age=31536000; includeSubDomains; preload" />
      )}
      
      {/* SEO */}
      {canonical && <link rel="canonical" href={canonical} />}
      {noIndex && <meta name="robots" content="noindex, nofollow" />}
      
      {/* Preconnect to external domains */}
      <link rel="preconnect" href="https://fonts.googleapis.com" />
      <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
      
      {/* DNS Prefetch */}
      <link rel="dns-prefetch" href="//fonts.googleapis.com" />
      <link rel="dns-prefetch" href="//fonts.gstatic.com" />
      
      {/* Favicon and App Icons */}
      <link rel="icon" type="image/x-icon" href="/favicon.ico" />
      <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
      <link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
      <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png" />
      
      {/* Web App Manifest */}
      <link rel="manifest" href="/manifest.json" />
      <meta name="theme-color" content="#1976d2" />
      <meta name="apple-mobile-web-app-capable" content="yes" />
      <meta name="apple-mobile-web-app-status-bar-style" content="default" />
      <meta name="apple-mobile-web-app-title" content="MonitoringGrid" />
      
      {/* Open Graph */}
      <meta property="og:type" content="website" />
      <meta property="og:title" content={title} />
      <meta property="og:description" content={description} />
      <meta property="og:site_name" content="MonitoringGrid" />
      {canonical && <meta property="og:url" content={canonical} />}
      
      {/* Twitter Card */}
      <meta name="twitter:card" content="summary" />
      <meta name="twitter:title" content={title} />
      <meta name="twitter:description" content={description} />
      
      {/* Performance Hints */}
      <link rel="preload" href="/fonts/main.woff2" as="font" type="font/woff2" crossOrigin="anonymous" />
      
      {/* Resource Hints for Critical Resources */}
      <link rel="modulepreload" href="/src/main.tsx" />
    </Helmet>
  );
};

// Hook for dynamic security headers
export const useSecurityHeaders = () => {
  const updateCSP = (newDirectives: Record<string, string[]>) => {
    const cspHeader = Object.entries(newDirectives)
      .map(([directive, sources]) => `${directive} ${sources.join(' ')}`)
      .join('; ');
    
    let cspMeta = document.querySelector('meta[http-equiv="Content-Security-Policy"]') as HTMLMetaElement;
    if (cspMeta) {
      cspMeta.content = cspHeader;
    }
  };

  const addCSPSource = (directive: string, source: string) => {
    const cspMeta = document.querySelector('meta[http-equiv="Content-Security-Policy"]') as HTMLMetaElement;
    if (cspMeta) {
      const currentCSP = cspMeta.content;
      const directives = currentCSP.split(';').map(d => d.trim());
      
      const directiveIndex = directives.findIndex(d => d.startsWith(directive));
      if (directiveIndex !== -1) {
        const currentDirective = directives[directiveIndex];
        if (!currentDirective.includes(source)) {
          directives[directiveIndex] = `${currentDirective} ${source}`;
          cspMeta.content = directives.join('; ');
        }
      }
    }
  };

  return { updateCSP, addCSPSource };
};

// Component for reporting CSP violations
export const CSPViolationReporter: React.FC = () => {
  useEffect(() => {
    const handleCSPViolation = (event: SecurityPolicyViolationEvent) => {
      console.warn('CSP Violation:', {
        blockedURI: event.blockedURI,
        violatedDirective: event.violatedDirective,
        originalPolicy: event.originalPolicy,
        sourceFile: event.sourceFile,
        lineNumber: event.lineNumber,
        columnNumber: event.columnNumber,
      });

      // In production, send to monitoring service
      if (process.env.NODE_ENV === 'production') {
        // Example: analytics.track('csp_violation', { ... });
      }
    };

    document.addEventListener('securitypolicyviolation', handleCSPViolation);

    return () => {
      document.removeEventListener('securitypolicyviolation', handleCSPViolation);
    };
  }, []);

  return null;
};

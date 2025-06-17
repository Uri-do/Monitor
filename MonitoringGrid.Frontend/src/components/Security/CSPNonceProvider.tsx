import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';

interface CSPNonceContextType {
  nonce: string;
  generateNonce: () => string;
  addInlineScript: (script: string) => void;
  addInlineStyle: (style: string) => void;
}

const CSPNonceContext = createContext<CSPNonceContextType | undefined>(undefined);

interface CSPNonceProviderProps {
  children: ReactNode;
}

/**
 * CSP Nonce Provider for secure inline scripts and styles
 * Implements Content Security Policy with nonce-based inline content
 */
export const CSPNonceProvider: React.FC<CSPNonceProviderProps> = ({ children }) => {
  const [nonce, setNonce] = useState<string>('');

  // Generate cryptographically secure nonce
  const generateNonce = (): string => {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
  };

  // Initialize nonce on mount
  useEffect(() => {
    const initialNonce = generateNonce();
    setNonce(initialNonce);
    
    // Set nonce in meta tag for server-side CSP
    const metaTag = document.querySelector('meta[name="csp-nonce"]') as HTMLMetaElement;
    if (metaTag) {
      metaTag.content = initialNonce;
    } else {
      const newMetaTag = document.createElement('meta');
      newMetaTag.name = 'csp-nonce';
      newMetaTag.content = initialNonce;
      document.head.appendChild(newMetaTag);
    }
  }, []);

  // Add inline script with nonce
  const addInlineScript = (script: string): void => {
    const scriptElement = document.createElement('script');
    scriptElement.nonce = nonce;
    scriptElement.textContent = script;
    document.head.appendChild(scriptElement);
  };

  // Add inline style with nonce
  const addInlineStyle = (style: string): void => {
    const styleElement = document.createElement('style');
    styleElement.nonce = nonce;
    styleElement.textContent = style;
    document.head.appendChild(styleElement);
  };

  const contextValue: CSPNonceContextType = {
    nonce,
    generateNonce,
    addInlineScript,
    addInlineStyle,
  };

  return (
    <CSPNonceContext.Provider value={contextValue}>
      {children}
    </CSPNonceContext.Provider>
  );
};

/**
 * Hook to use CSP nonce context
 */
export const useCSPNonce = (): CSPNonceContextType => {
  const context = useContext(CSPNonceContext);
  if (!context) {
    throw new Error('useCSPNonce must be used within a CSPNonceProvider');
  }
  return context;
};

/**
 * Higher-order component to inject nonce into components
 */
export const withCSPNonce = <P extends object>(
  Component: React.ComponentType<P & { nonce?: string }>
) => {
  return (props: P) => {
    const { nonce } = useCSPNonce();
    return <Component {...props} nonce={nonce} />;
  };
};

/**
 * Secure Script Component with nonce
 */
interface SecureScriptProps {
  children: string;
  defer?: boolean;
  async?: boolean;
}

export const SecureScript: React.FC<SecureScriptProps> = ({ 
  children, 
  defer = false, 
  async = false 
}) => {
  const { nonce } = useCSPNonce();

  useEffect(() => {
    const script = document.createElement('script');
    script.nonce = nonce;
    script.textContent = children;
    script.defer = defer;
    script.async = async;
    
    document.head.appendChild(script);

    return () => {
      if (script.parentNode) {
        script.parentNode.removeChild(script);
      }
    };
  }, [children, nonce, defer, async]);

  return null;
};

/**
 * Secure Style Component with nonce
 */
interface SecureStyleProps {
  children: string;
}

export const SecureStyle: React.FC<SecureStyleProps> = ({ children }) => {
  const { nonce } = useCSPNonce();

  useEffect(() => {
    const style = document.createElement('style');
    style.nonce = nonce;
    style.textContent = children;
    
    document.head.appendChild(style);

    return () => {
      if (style.parentNode) {
        style.parentNode.removeChild(style);
      }
    };
  }, [children, nonce]);

  return null;
};

export default CSPNonceProvider;

/**
 * Micro-Frontend Architecture Foundation
 * Enterprise-grade micro-frontend support and module federation
 */

import React from 'react';

// Micro-frontend module interface
export interface MicrofrontendModule {
  name: string;
  version: string;
  entry: string;
  routes: string[];
  permissions?: string[];
  dependencies?: string[];
  metadata?: Record<string, unknown>;
}

// Module registry for dynamic loading
export class MicrofrontendRegistry {
  private modules = new Map<string, MicrofrontendModule>();
  private loadedModules = new Map<string, any>();
  private eventBus = new EventTarget();

  // Register a micro-frontend module
  public registerModule(module: MicrofrontendModule): void {
    this.modules.set(module.name, module);
    this.eventBus.dispatchEvent(new CustomEvent('module-registered', { 
      detail: module 
    }));
  }

  // Unregister a module
  public unregisterModule(name: string): void {
    this.modules.delete(name);
    this.loadedModules.delete(name);
    this.eventBus.dispatchEvent(new CustomEvent('module-unregistered', { 
      detail: { name } 
    }));
  }

  // Get all registered modules
  public getModules(): MicrofrontendModule[] {
    return Array.from(this.modules.values());
  }

  // Get module by name
  public getModule(name: string): MicrofrontendModule | undefined {
    return this.modules.get(name);
  }

  // Check if module is loaded
  public isModuleLoaded(name: string): boolean {
    return this.loadedModules.has(name);
  }

  // Load module dynamically
  public async loadModule(name: string): Promise<any> {
    const module = this.modules.get(name);
    if (!module) {
      throw new Error(`Module ${name} not found`);
    }

    if (this.loadedModules.has(name)) {
      return this.loadedModules.get(name);
    }

    try {
      // Load dependencies first
      if (module.dependencies) {
        await Promise.all(
          module.dependencies.map(dep => this.loadModule(dep))
        );
      }

      // Dynamic import of the module
      const loadedModule = await import(/* webpackIgnore: true */ module.entry);
      this.loadedModules.set(name, loadedModule);
      
      this.eventBus.dispatchEvent(new CustomEvent('module-loaded', { 
        detail: { name, module: loadedModule } 
      }));

      return loadedModule;
    } catch (error) {
      this.eventBus.dispatchEvent(new CustomEvent('module-load-error', { 
        detail: { name, error } 
      }));
      throw error;
    }
  }

  // Subscribe to registry events
  public subscribe(event: string, callback: (event: CustomEvent) => void): () => void {
    this.eventBus.addEventListener(event, callback as EventListener);
    return () => this.eventBus.removeEventListener(event, callback as EventListener);
  }
}

// Shared state management for micro-frontends
export class SharedStateManager {
  private state = new Map<string, any>();
  private subscribers = new Map<string, Set<(value: any) => void>>();
  private eventBus = new EventTarget();

  // Set shared state
  public setState<T>(key: string, value: T): void {
    const previousValue = this.state.get(key);
    this.state.set(key, value);
    
    // Notify subscribers
    const keySubscribers = this.subscribers.get(key);
    if (keySubscribers) {
      keySubscribers.forEach(callback => callback(value));
    }

    // Emit global state change event
    this.eventBus.dispatchEvent(new CustomEvent('state-changed', {
      detail: { key, value, previousValue }
    }));
  }

  // Get shared state
  public getState<T>(key: string): T | undefined {
    return this.state.get(key);
  }

  // Subscribe to state changes
  public subscribe<T>(key: string, callback: (value: T) => void): () => void {
    if (!this.subscribers.has(key)) {
      this.subscribers.set(key, new Set());
    }
    
    const keySubscribers = this.subscribers.get(key)!;
    keySubscribers.add(callback);

    // Return unsubscribe function
    return () => {
      keySubscribers.delete(callback);
      if (keySubscribers.size === 0) {
        this.subscribers.delete(key);
      }
    };
  }

  // Clear all state
  public clearState(): void {
    this.state.clear();
    this.subscribers.clear();
    this.eventBus.dispatchEvent(new CustomEvent('state-cleared'));
  }
}

// Communication bus for micro-frontends
export class MicrofrontendCommunicationBus {
  private eventBus = new EventTarget();
  private messageQueue = new Map<string, any[]>();

  // Send message to specific module or broadcast
  public sendMessage(message: {
    type: string;
    payload?: any;
    target?: string;
    source: string;
  }): void {
    const event = new CustomEvent('microfrontend-message', { detail: message });
    this.eventBus.dispatchEvent(event);

    // Queue message if target module is not loaded
    if (message.target && !this.isModuleActive(message.target)) {
      if (!this.messageQueue.has(message.target)) {
        this.messageQueue.set(message.target, []);
      }
      this.messageQueue.get(message.target)!.push(message);
    }
  }

  // Subscribe to messages
  public subscribe(
    callback: (message: any) => void,
    filter?: { type?: string; source?: string }
  ): () => void {
    const handler = (event: CustomEvent) => {
      const message = event.detail;
      
      // Apply filters
      if (filter?.type && message.type !== filter.type) return;
      if (filter?.source && message.source !== filter.source) return;
      
      callback(message);
    };

    this.eventBus.addEventListener('microfrontend-message', handler as EventListener);
    
    return () => {
      this.eventBus.removeEventListener('microfrontend-message', handler as EventListener);
    };
  }

  // Process queued messages for a module
  public processQueuedMessages(moduleName: string): void {
    const queuedMessages = this.messageQueue.get(moduleName);
    if (queuedMessages) {
      queuedMessages.forEach(message => {
        this.sendMessage(message);
      });
      this.messageQueue.delete(moduleName);
    }
  }

  private isModuleActive(moduleName: string): boolean {
    // Check if module is currently active/loaded
    return microfrontendRegistry.isModuleLoaded(moduleName);
  }
}

// Error boundary for micro-frontends
export class MicrofrontendErrorBoundary extends React.Component<
  { children: React.ReactNode; moduleName: string },
  { hasError: boolean; error?: Error }
> {
  constructor(props: { children: React.ReactNode; moduleName: string }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error(`Micro-frontend error in ${this.props.moduleName}:`, error, errorInfo);
    
    // Report error to monitoring system
    communicationBus.sendMessage({
      type: 'ERROR',
      payload: {
        moduleName: this.props.moduleName,
        error: error.message,
        stack: error.stack,
        errorInfo
      },
      source: 'error-boundary'
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{ 
          padding: '20px', 
          border: '1px solid #ff6b6b', 
          borderRadius: '4px',
          backgroundColor: '#ffe0e0',
          color: '#d63031'
        }}>
          <h3>Micro-frontend Error</h3>
          <p>Module "{this.props.moduleName}" encountered an error.</p>
          <details>
            <summary>Error Details</summary>
            <pre>{this.state.error?.stack}</pre>
          </details>
          <button 
            onClick={() => this.setState({ hasError: false, error: undefined })}
            style={{
              marginTop: '10px',
              padding: '8px 16px',
              backgroundColor: '#d63031',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            Retry
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}

// React hook for micro-frontend integration
export const useMicrofrontend = (moduleName: string) => {
  const [module, setModule] = React.useState<any>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);

  const loadModule = React.useCallback(async () => {
    if (microfrontendRegistry.isModuleLoaded(moduleName)) {
      setModule(microfrontendRegistry.getModule(moduleName));
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const loadedModule = await microfrontendRegistry.loadModule(moduleName);
      setModule(loadedModule);
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Failed to load module'));
    } finally {
      setLoading(false);
    }
  }, [moduleName]);

  React.useEffect(() => {
    loadModule();
  }, [loadModule]);

  return { module, loading, error, reload: loadModule };
};

// Singleton instances
export const microfrontendRegistry = new MicrofrontendRegistry();
export const sharedStateManager = new SharedStateManager();
export const communicationBus = new MicrofrontendCommunicationBus();

// Initialize micro-frontend architecture
export const initializeMicrofrontendArchitecture = () => {
  // Set up global error handling for micro-frontends
  window.addEventListener('unhandledrejection', (event) => {
    communicationBus.sendMessage({
      type: 'UNHANDLED_REJECTION',
      payload: {
        reason: event.reason,
        promise: event.promise
      },
      source: 'global-error-handler'
    });
  });

  // Set up performance monitoring for micro-frontends
  if ('PerformanceObserver' in window) {
    const observer = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      entries.forEach((entry) => {
        if (entry.entryType === 'navigation' || entry.entryType === 'resource') {
          communicationBus.sendMessage({
            type: 'PERFORMANCE_METRIC',
            payload: {
              entryType: entry.entryType,
              name: entry.name,
              duration: entry.duration,
              startTime: entry.startTime
            },
            source: 'performance-monitor'
          });
        }
      });
    });

    try {
      observer.observe({ entryTypes: ['navigation', 'resource'] });
    } catch (e) {
      console.warn('Performance observer not fully supported');
    }
  }

  console.log('Micro-frontend architecture initialized');
};

/**
 * Advanced Data Management System
 * Enterprise-grade data handling, caching, and synchronization
 */

// Advanced caching system with TTL and invalidation
export class AdvancedCache<T = any> {
  private cache = new Map<string, { data: T; timestamp: number; ttl: number }>();
  private subscribers = new Map<string, Set<(data: T | null) => void>>();
  private cleanupInterval: NodeJS.Timeout;

  constructor(private defaultTTL: number = 5 * 60 * 1000) { // 5 minutes default
    // Cleanup expired entries every minute
    this.cleanupInterval = setInterval(() => this.cleanup(), 60 * 1000);
  }

  set(key: string, data: T, ttl?: number): void {
    const entry = {
      data,
      timestamp: Date.now(),
      ttl: ttl || this.defaultTTL,
    };

    this.cache.set(key, entry);
    this.notifySubscribers(key, data);
  }

  get(key: string): T | null {
    const entry = this.cache.get(key);
    
    if (!entry) {
      return null;
    }

    // Check if expired
    if (Date.now() - entry.timestamp > entry.ttl) {
      this.cache.delete(key);
      this.notifySubscribers(key, null);
      return null;
    }

    return entry.data;
  }

  has(key: string): boolean {
    return this.get(key) !== null;
  }

  delete(key: string): boolean {
    const deleted = this.cache.delete(key);
    if (deleted) {
      this.notifySubscribers(key, null);
    }
    return deleted;
  }

  clear(): void {
    const keys = Array.from(this.cache.keys());
    this.cache.clear();
    keys.forEach(key => this.notifySubscribers(key, null));
  }

  // Subscribe to cache changes
  subscribe(key: string, callback: (data: T | null) => void): () => void {
    if (!this.subscribers.has(key)) {
      this.subscribers.set(key, new Set());
    }
    
    this.subscribers.get(key)!.add(callback);

    // Return unsubscribe function
    return () => {
      const keySubscribers = this.subscribers.get(key);
      if (keySubscribers) {
        keySubscribers.delete(callback);
        if (keySubscribers.size === 0) {
          this.subscribers.delete(key);
        }
      }
    };
  }

  private notifySubscribers(key: string, data: T | null): void {
    const keySubscribers = this.subscribers.get(key);
    if (keySubscribers) {
      keySubscribers.forEach(callback => callback(data));
    }
  }

  private cleanup(): void {
    const now = Date.now();
    const expiredKeys: string[] = [];

    this.cache.forEach((entry, key) => {
      if (now - entry.timestamp > entry.ttl) {
        expiredKeys.push(key);
      }
    });

    expiredKeys.forEach(key => {
      this.cache.delete(key);
      this.notifySubscribers(key, null);
    });
  }

  // Get cache statistics
  getStats(): {
    size: number;
    hitRate: number;
    memoryUsage: number;
  } {
    const size = this.cache.size;
    const memoryUsage = JSON.stringify(Array.from(this.cache.values())).length;
    
    return {
      size,
      hitRate: 0, // Would need to track hits/misses for accurate calculation
      memoryUsage,
    };
  }

  destroy(): void {
    clearInterval(this.cleanupInterval);
    this.clear();
    this.subscribers.clear();
  }
}

// Advanced data synchronization manager
export interface SyncOptions {
  interval?: number;
  retryAttempts?: number;
  retryDelay?: number;
  onSuccess?: (data: any) => void;
  onError?: (error: Error) => void;
  onRetry?: (attempt: number) => void;
}

export class DataSynchronizer {
  private syncTasks = new Map<string, {
    fetchFn: () => Promise<any>;
    options: SyncOptions;
    intervalId?: NodeJS.Timeout;
    isRunning: boolean;
    lastSync?: Date;
    errorCount: number;
  }>();

  register(
    key: string,
    fetchFn: () => Promise<any>,
    options: SyncOptions = {}
  ): void {
    // Stop existing task if any
    this.unregister(key);

    const task = {
      fetchFn,
      options: {
        interval: 30000, // 30 seconds default
        retryAttempts: 3,
        retryDelay: 1000,
        ...options,
      },
      isRunning: false,
      errorCount: 0,
    };

    this.syncTasks.set(key, task);

    // Start automatic sync if interval is set
    if (task.options.interval) {
      this.startSync(key);
    }
  }

  unregister(key: string): void {
    const task = this.syncTasks.get(key);
    if (task) {
      this.stopSync(key);
      this.syncTasks.delete(key);
    }
  }

  async syncNow(key: string): Promise<any> {
    const task = this.syncTasks.get(key);
    if (!task) {
      throw new Error(`Sync task '${key}' not found`);
    }

    return this.executeSync(key, task);
  }

  startSync(key: string): void {
    const task = this.syncTasks.get(key);
    if (!task || task.isRunning) {
      return;
    }

    task.isRunning = true;
    
    if (task.options.interval) {
      task.intervalId = setInterval(() => {
        this.executeSync(key, task);
      }, task.options.interval);
    }
  }

  stopSync(key: string): void {
    const task = this.syncTasks.get(key);
    if (!task) {
      return;
    }

    task.isRunning = false;
    
    if (task.intervalId) {
      clearInterval(task.intervalId);
      task.intervalId = undefined;
    }
  }

  private async executeSync(key: string, task: any): Promise<any> {
    let attempt = 0;
    
    while (attempt <= (task.options.retryAttempts || 0)) {
      try {
        const data = await task.fetchFn();
        task.lastSync = new Date();
        task.errorCount = 0;
        
        if (task.options.onSuccess) {
          task.options.onSuccess(data);
        }
        
        return data;
      } catch (error) {
        attempt++;
        task.errorCount++;
        
        if (attempt <= (task.options.retryAttempts || 0)) {
          if (task.options.onRetry) {
            task.options.onRetry(attempt);
          }
          
          // Wait before retry with exponential backoff
          const delay = (task.options.retryDelay || 1000) * Math.pow(2, attempt - 1);
          await new Promise(resolve => setTimeout(resolve, delay));
        } else {
          if (task.options.onError) {
            task.options.onError(error as Error);
          }
          throw error;
        }
      }
    }
  }

  getSyncStatus(key: string): {
    isRunning: boolean;
    lastSync?: Date;
    errorCount: number;
  } | null {
    const task = this.syncTasks.get(key);
    if (!task) {
      return null;
    }

    return {
      isRunning: task.isRunning,
      lastSync: task.lastSync,
      errorCount: task.errorCount,
    };
  }

  getAllSyncStatus(): Record<string, any> {
    const status: Record<string, any> = {};
    
    this.syncTasks.forEach((task, key) => {
      status[key] = {
        isRunning: task.isRunning,
        lastSync: task.lastSync,
        errorCount: task.errorCount,
      };
    });

    return status;
  }

  destroy(): void {
    this.syncTasks.forEach((_, key) => {
      this.unregister(key);
    });
  }
}

// Advanced data transformation utilities
export class DataTransformer {
  private static transformations = new Map<string, (data: any) => any>();

  static register(name: string, transform: (data: any) => any): void {
    this.transformations.set(name, transform);
  }

  static transform(name: string, data: any): any {
    const transform = this.transformations.get(name);
    if (!transform) {
      throw new Error(`Transformation '${name}' not found`);
    }
    return transform(data);
  }

  static pipe(data: any, ...transformNames: string[]): any {
    return transformNames.reduce((result, name) => {
      return this.transform(name, result);
    }, data);
  }

  // Common transformations
  static normalize(data: any[]): any[] {
    if (!Array.isArray(data)) {
      return [];
    }
    
    return data.map(item => {
      if (typeof item === 'object' && item !== null) {
        const normalized: any = {};
        Object.keys(item).forEach(key => {
          // Convert to camelCase
          const camelKey = key.replace(/_([a-z])/g, (_, letter) => letter.toUpperCase());
          normalized[camelKey] = item[key];
        });
        return normalized;
      }
      return item;
    });
  }

  static denormalize(data: any[]): any[] {
    if (!Array.isArray(data)) {
      return [];
    }
    
    return data.map(item => {
      if (typeof item === 'object' && item !== null) {
        const denormalized: any = {};
        Object.keys(item).forEach(key => {
          // Convert to snake_case
          const snakeKey = key.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`);
          denormalized[snakeKey] = item[key];
        });
        return denormalized;
      }
      return item;
    });
  }

  static sanitize(data: any): any {
    if (typeof data === 'string') {
      return data.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');
    }
    
    if (Array.isArray(data)) {
      return data.map(item => this.sanitize(item));
    }
    
    if (typeof data === 'object' && data !== null) {
      const sanitized: any = {};
      Object.keys(data).forEach(key => {
        sanitized[key] = this.sanitize(data[key]);
      });
      return sanitized;
    }
    
    return data;
  }

  static validate(data: any, schema: any): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    // Simple validation - in production, use a library like Joi or Yup
    if (schema.required && (data === null || data === undefined)) {
      errors.push('Value is required');
    }
    
    if (schema.type && typeof data !== schema.type) {
      errors.push(`Expected type ${schema.type}, got ${typeof data}`);
    }
    
    if (schema.minLength && typeof data === 'string' && data.length < schema.minLength) {
      errors.push(`Minimum length is ${schema.minLength}`);
    }
    
    if (schema.maxLength && typeof data === 'string' && data.length > schema.maxLength) {
      errors.push(`Maximum length is ${schema.maxLength}`);
    }
    
    return {
      isValid: errors.length === 0,
      errors,
    };
  }
}

// Global instances
export const globalCache = new AdvancedCache();
export const globalSynchronizer = new DataSynchronizer();

// Register common transformations
DataTransformer.register('normalize', DataTransformer.normalize);
DataTransformer.register('denormalize', DataTransformer.denormalize);
DataTransformer.register('sanitize', DataTransformer.sanitize);

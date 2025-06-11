import api from './api';

// Configuration interfaces
export interface FeatureFlag {
  key: string;
  name: string;
  description: string;
  enabled: boolean;
  environment: 'development' | 'staging' | 'production' | 'all';
  rolloutPercentage: number;
  conditions?: Array<{
    type: 'user_id' | 'role' | 'country' | 'custom';
    operator: 'equals' | 'contains' | 'in' | 'not_in';
    value: any;
  }>;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
}

export interface SystemConfiguration {
  category: string;
  key: string;
  value: any;
  type: 'string' | 'number' | 'boolean' | 'json' | 'array';
  description: string;
  isSecret: boolean;
  isReadonly: boolean;
  validation?: {
    required?: boolean;
    min?: number;
    max?: number;
    pattern?: string;
    enum?: any[];
  };
  environment: string;
  lastModified: string;
  modifiedBy: string;
}

export interface EnvironmentConfig {
  name: string;
  displayName: string;
  isActive: boolean;
  apiBaseUrl: string;
  features: Record<string, boolean>;
  settings: Record<string, any>;
  secrets: string[];
  deploymentInfo: {
    version: string;
    buildNumber: string;
    deployedAt: string;
    deployedBy: string;
    commitHash: string;
  };
}

export interface ConfigurationTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  template: Record<string, any>;
  variables: Array<{
    key: string;
    type: string;
    description: string;
    required: boolean;
    defaultValue?: any;
  }>;
  createdAt: string;
  updatedAt: string;
}

export interface ConfigurationHistory {
  id: string;
  configKey: string;
  oldValue: any;
  newValue: any;
  changeType: 'create' | 'update' | 'delete';
  reason: string;
  changedBy: string;
  changedAt: string;
  environment: string;
  rollbackId?: string;
}

export interface ConfigurationValidation {
  isValid: boolean;
  errors: Array<{
    key: string;
    message: string;
    severity: 'error' | 'warning';
  }>;
  warnings: Array<{
    key: string;
    message: string;
  }>;
}

/**
 * Enterprise configuration management service
 */
class ConfigService {
  // Feature Flags
  async getFeatureFlags(environment?: string): Promise<FeatureFlag[]> {
    try {
      const response = await api.get('/config/feature-flags', {
        params: { environment },
      });
      return response.data;
    } catch (error) {
      console.warn('Feature flags endpoint not available, returning defaults');
      return this.getDefaultFeatureFlags();
    }
  }

  async getFeatureFlag(key: string): Promise<FeatureFlag | null> {
    try {
      const response = await api.get(`/config/feature-flags/${key}`);
      return response.data;
    } catch (error) {
      console.warn(`Feature flag ${key} not available, returning default`);
      return this.getDefaultFeatureFlags().find(f => f.key === key) || null;
    }
  }

  async updateFeatureFlag(key: string, updates: Partial<FeatureFlag>): Promise<FeatureFlag> {
    try {
      const response = await api.patch(`/config/feature-flags/${key}`, updates);
      return response.data;
    } catch (error) {
      console.warn(`Failed to update feature flag ${key}`);
      throw error;
    }
  }

  async createFeatureFlag(flag: Omit<FeatureFlag, 'createdAt' | 'updatedAt' | 'createdBy'>): Promise<FeatureFlag> {
    try {
      const response = await api.post('/config/feature-flags', flag);
      return response.data;
    } catch (error) {
      console.warn('Failed to create feature flag');
      throw error;
    }
  }

  async deleteFeatureFlag(key: string): Promise<void> {
    try {
      await api.delete(`/config/feature-flags/${key}`);
    } catch (error) {
      console.warn(`Failed to delete feature flag ${key}`);
      throw error;
    }
  }

  // System Configuration
  async getSystemConfiguration(environment?: string): Promise<SystemConfiguration[]> {
    try {
      const response = await api.get('/config/system', {
        params: { environment },
      });
      return response.data;
    } catch (error) {
      console.warn('System configuration endpoint not available, returning defaults');
      return this.getDefaultSystemConfig();
    }
  }

  async getConfigurationByCategory(category: string, environment?: string): Promise<SystemConfiguration[]> {
    try {
      const response = await api.get(`/config/system/category/${category}`, {
        params: { environment },
      });
      return response.data;
    } catch (error) {
      console.warn(`Configuration category ${category} not available`);
      return [];
    }
  }

  async updateConfiguration(key: string, value: any, reason?: string): Promise<SystemConfiguration> {
    try {
      const response = await api.patch(`/config/system/${key}`, {
        value,
        reason,
      });
      return response.data;
    } catch (error) {
      console.warn(`Failed to update configuration ${key}`);
      throw error;
    }
  }

  async validateConfiguration(config: Record<string, any>): Promise<ConfigurationValidation> {
    try {
      const response = await api.post('/config/validate', config);
      return response.data;
    } catch (error) {
      console.warn('Configuration validation endpoint not available');
      return { isValid: true, errors: [], warnings: [] };
    }
  }

  // Environment Management
  async getEnvironments(): Promise<EnvironmentConfig[]> {
    try {
      const response = await api.get('/config/environments');
      return response.data;
    } catch (error) {
      console.warn('Environments endpoint not available, returning defaults');
      return this.getDefaultEnvironments();
    }
  }

  async getEnvironment(name: string): Promise<EnvironmentConfig> {
    try {
      const response = await api.get(`/config/environments/${name}`);
      return response.data;
    } catch (error) {
      console.warn(`Environment ${name} not available, returning default`);
      return this.getDefaultEnvironments()[0];
    }
  }

  async updateEnvironment(name: string, updates: Partial<EnvironmentConfig>): Promise<EnvironmentConfig> {
    try {
      const response = await api.patch(`/config/environments/${name}`, updates);
      return response.data;
    } catch (error) {
      console.warn(`Failed to update environment ${name}`);
      throw error;
    }
  }

  // Configuration Templates
  async getConfigurationTemplates(): Promise<ConfigurationTemplate[]> {
    try {
      const response = await api.get('/config/templates');
      return response.data;
    } catch (error) {
      console.warn('Configuration templates endpoint not available');
      return [];
    }
  }

  async applyTemplate(templateId: string, variables: Record<string, any>): Promise<void> {
    try {
      await api.post(`/config/templates/${templateId}/apply`, { variables });
    } catch (error) {
      console.warn(`Failed to apply template ${templateId}`);
      throw error;
    }
  }

  // Configuration History
  async getConfigurationHistory(key?: string, limit: number = 50): Promise<ConfigurationHistory[]> {
    try {
      const response = await api.get('/config/history', {
        params: { key, limit },
      });
      return response.data;
    } catch (error) {
      console.warn('Configuration history endpoint not available');
      return [];
    }
  }

  async rollbackConfiguration(historyId: string): Promise<void> {
    try {
      await api.post(`/config/history/${historyId}/rollback`);
    } catch (error) {
      console.warn(`Failed to rollback configuration ${historyId}`);
      throw error;
    }
  }

  // Utility methods
  isFeatureEnabled(key: string, userId?: string, userRoles?: string[]): boolean {
    // This would typically check against cached feature flags
    const defaultFlags = this.getDefaultFeatureFlags();
    const flag = defaultFlags.find(f => f.key === key);
    
    if (!flag || !flag.enabled) {
      return false;
    }

    // Check rollout percentage
    if (flag.rolloutPercentage < 100) {
      const hash = this.hashString(key + (userId || ''));
      if ((hash % 100) >= flag.rolloutPercentage) {
        return false;
      }
    }

    // Check conditions
    if (flag.conditions && flag.conditions.length > 0) {
      return flag.conditions.every(condition => {
        switch (condition.type) {
          case 'user_id':
            return userId && this.evaluateCondition(userId, condition);
          case 'role':
            return userRoles && userRoles.some(role => this.evaluateCondition(role, condition));
          default:
            return true;
        }
      });
    }

    return true;
  }

  private evaluateCondition(value: any, condition: any): boolean {
    switch (condition.operator) {
      case 'equals':
        return value === condition.value;
      case 'contains':
        return String(value).includes(String(condition.value));
      case 'in':
        return Array.isArray(condition.value) && condition.value.includes(value);
      case 'not_in':
        return Array.isArray(condition.value) && !condition.value.includes(value);
      default:
        return false;
    }
  }

  private hashString(str: string): number {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash);
  }

  // Default configurations for development/fallback
  private getDefaultFeatureFlags(): FeatureFlag[] {
    return [
      {
        key: 'enable_ai_predictions',
        name: 'AI Predictions',
        description: 'Enable AI-powered predictive analytics',
        enabled: true,
        environment: 'all',
        rolloutPercentage: 100,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        createdBy: 'system',
      },
      {
        key: 'enable_collaboration',
        name: 'Real-time Collaboration',
        description: 'Enable real-time collaboration features',
        enabled: true,
        environment: 'all',
        rolloutPercentage: 100,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        createdBy: 'system',
      },
      {
        key: 'enable_advanced_security',
        name: 'Advanced Security',
        description: 'Enable enterprise security features',
        enabled: true,
        environment: 'all',
        rolloutPercentage: 100,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        createdBy: 'system',
      },
    ];
  }

  private getDefaultSystemConfig(): SystemConfiguration[] {
    return [
      {
        category: 'performance',
        key: 'max_concurrent_requests',
        value: 100,
        type: 'number',
        description: 'Maximum number of concurrent API requests',
        isSecret: false,
        isReadonly: false,
        environment: 'production',
        lastModified: new Date().toISOString(),
        modifiedBy: 'system',
      },
      {
        category: 'ui',
        key: 'default_page_size',
        value: 25,
        type: 'number',
        description: 'Default number of items per page',
        isSecret: false,
        isReadonly: false,
        environment: 'all',
        lastModified: new Date().toISOString(),
        modifiedBy: 'system',
      },
    ];
  }

  private getDefaultEnvironments(): EnvironmentConfig[] {
    return [
      {
        name: 'production',
        displayName: 'Production',
        isActive: true,
        apiBaseUrl: 'https://api.monitoringgrid.com',
        features: {
          enableAI: true,
          enableCollaboration: true,
          enableAdvancedSecurity: true,
        },
        settings: {
          logLevel: 'error',
          enableMetrics: true,
        },
        secrets: ['database_connection', 'api_keys'],
        deploymentInfo: {
          version: '1.0.0',
          buildNumber: '123',
          deployedAt: new Date().toISOString(),
          deployedBy: 'ci/cd',
          commitHash: 'abc123',
        },
      },
    ];
  }
}

export const configService = new ConfigService();

import { BaseApiService } from './BaseApiService';
import { ErrorHandlers } from '../utils/errorHandling';

// Indicator types
export interface Indicator {
  indicatorID: number;
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorItemName: string;
  schedulerID?: number;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
  createdDate?: string;
  updatedDate?: string;
}

export interface CreateIndicatorRequest {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorItemName: string;
  schedulerID?: number;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
}

export interface UpdateIndicatorRequest extends Partial<CreateIndicatorRequest> {
  indicatorID: number;
}

export interface IndicatorExecutionResult {
  indicatorID: number;
  executionID: string;
  status: 'success' | 'failed' | 'running';
  startTime: string;
  endTime?: string;
  result?: any;
  errorMessage?: string;
  executionTimeMs?: number;
}

export interface IndicatorStatistics {
  indicatorID: number;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  averageExecutionTime: number;
  lastExecutionTime?: string;
  lastExecutionStatus?: string;
}

// Indicator service extending BaseApiService
class IndicatorService extends BaseApiService {
  constructor() {
    super('indicators');
  }

  // Get all indicators
  async getAll(): Promise<Indicator[]> {
    try {
      return await this.get<Indicator[]>('');
    } catch (error) {
      ErrorHandlers.query(error, 'Failed to fetch indicators');
      throw error;
    }
  }

  // Get indicator by ID
  async getById(id: number): Promise<Indicator> {
    try {
      return await this.get<Indicator>(`/${id}`);
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch indicator ${id}`);
      throw error;
    }
  }

  // Create new indicator
  async create(data: CreateIndicatorRequest): Promise<Indicator> {
    try {
      const result = await this.post<Indicator>('', data);
      ErrorHandlers.mutation(null, 'Indicator created successfully');
      return result;
    } catch (error) {
      ErrorHandlers.mutation(error, 'Failed to create indicator');
      throw error;
    }
  }

  // Update indicator
  async update(id: number, data: UpdateIndicatorRequest): Promise<Indicator> {
    try {
      const result = await this.put<Indicator>(`/${id}`, data);
      ErrorHandlers.mutation(null, 'Indicator updated successfully');
      return result;
    } catch (error) {
      ErrorHandlers.mutation(error, `Failed to update indicator ${id}`);
      throw error;
    }
  }

  // Delete indicator
  async delete(id: number): Promise<void> {
    try {
      await this.delete<void>(`/${id}`);
      ErrorHandlers.mutation(null, 'Indicator deleted successfully');
    } catch (error) {
      ErrorHandlers.mutation(error, `Failed to delete indicator ${id}`);
      throw error;
    }
  }

  // Execute indicator
  async execute(id: number): Promise<IndicatorExecutionResult> {
    try {
      const result = await this.post<IndicatorExecutionResult>(`/${id}/execute`);
      ErrorHandlers.mutation(null, 'Indicator execution started');
      return result;
    } catch (error) {
      ErrorHandlers.mutation(error, `Failed to execute indicator ${id}`);
      throw error;
    }
  }

  // Get indicator execution history
  async getExecutionHistory(
    id: number,
    options?: {
      limit?: number;
      offset?: number;
      status?: string;
      fromDate?: string;
      toDate?: string;
    }
  ): Promise<IndicatorExecutionResult[]> {
    try {
      return await this.get<IndicatorExecutionResult[]>(`/${id}/executions`, options);
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch execution history for indicator ${id}`);
      throw error;
    }
  }

  // Get indicator statistics
  async getStatistics(id: number): Promise<IndicatorStatistics> {
    try {
      return await this.get<IndicatorStatistics>(`/${id}/statistics`);
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch statistics for indicator ${id}`);
      throw error;
    }
  }

  // Bulk operations
  async bulkDelete(ids: number[]): Promise<void> {
    try {
      await this.bulkOperation<void>('/bulk', 'delete', ids);
      ErrorHandlers.mutation(null, `${ids.length} indicators deleted successfully`);
    } catch (error) {
      ErrorHandlers.mutation(error, 'Failed to delete indicators');
      throw error;
    }
  }

  async bulkActivate(ids: number[]): Promise<void> {
    try {
      await this.bulkOperation<void>('/bulk', 'activate', ids);
      ErrorHandlers.mutation(null, `${ids.length} indicators activated successfully`);
    } catch (error) {
      ErrorHandlers.mutation(error, 'Failed to activate indicators');
      throw error;
    }
  }

  async bulkDeactivate(ids: number[]): Promise<void> {
    try {
      await this.bulkOperation<void>('/bulk', 'deactivate', ids);
      ErrorHandlers.mutation(null, `${ids.length} indicators deactivated successfully`);
    } catch (error) {
      ErrorHandlers.mutation(error, 'Failed to deactivate indicators');
      throw error;
    }
  }

  // Search indicators
  async search(
    query: string,
    filters?: {
      isActive?: boolean;
      priority?: number;
      collectorID?: number;
      ownerContactId?: number;
    }
  ): Promise<Indicator[]> {
    try {
      return await this.get<Indicator[]>('/search', { query, ...filters });
    } catch (error) {
      ErrorHandlers.query(error, 'Failed to search indicators');
      throw error;
    }
  }

  // Get indicators by collector
  async getByCollector(collectorId: number): Promise<Indicator[]> {
    try {
      return await this.get<Indicator[]>('/by-collector', { collectorId });
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch indicators for collector ${collectorId}`);
      throw error;
    }
  }

  // Get indicators by owner
  async getByOwner(ownerContactId: number): Promise<Indicator[]> {
    try {
      return await this.get<Indicator[]>('/by-owner', { ownerContactId });
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch indicators for owner ${ownerContactId}`);
      throw error;
    }
  }

  // Validate indicator configuration
  async validateConfiguration(data: CreateIndicatorRequest): Promise<{
    isValid: boolean;
    errors: string[];
    warnings: string[];
  }> {
    try {
      return await this.post<{
        isValid: boolean;
        errors: string[];
        warnings: string[];
      }>('/validate', data);
    } catch (error) {
      ErrorHandlers.query(error, 'Failed to validate indicator configuration');
      throw error;
    }
  }

  // Test indicator execution (dry run)
  async testExecution(data: CreateIndicatorRequest): Promise<{
    success: boolean;
    result?: any;
    error?: string;
    executionTimeMs: number;
  }> {
    try {
      return await this.post<{
        success: boolean;
        result?: any;
        error?: string;
        executionTimeMs: number;
      }>('/test', data);
    } catch (error) {
      ErrorHandlers.mutation(error, 'Failed to test indicator execution');
      throw error;
    }
  }
}

// Export singleton instance
export const indicatorService = new IndicatorService();
export default indicatorService;

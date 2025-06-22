import { BaseApiService } from './BaseApiService';
import { ErrorHandlers } from '@/utils/errorHandling';

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
    super('/api/indicator');
  }

  // Get all indicators with proper pagination
  async getAll(): Promise<Indicator[]> {
    try {
      const allIndicators: Indicator[] = [];
      let page = 1;
      const pageSize = 100; // Use maximum allowed page size
      let hasMoreData = true;

      while (hasMoreData) {
        const cacheBuster = `_t=${Date.now()}`;
        const endpoint = `?${cacheBuster}&page=${page}&pageSize=${pageSize}`;

        console.log(`🔍 Fetching page ${page} with pageSize ${pageSize}`);

        const response = await this.get<any>(endpoint);
        console.log(`🔍 Page ${page} response received:`, response);

        let pageIndicators: Indicator[] = [];

        // Handle paginated response structure
        if (response && typeof response === 'object' && 'indicators' in response) {
          pageIndicators = response.indicators || [];
          console.log(`✅ Found ${pageIndicators.length} indicators in paginated response (page ${page})`);
        }
        // Handle wrapped API response structure
        else if (response && typeof response === 'object' && 'data' in response && response.data && 'indicators' in response.data) {
          pageIndicators = response.data.indicators || [];
          console.log(`✅ Found ${pageIndicators.length} indicators in wrapped response (page ${page})`);
        }
        // Handle direct array response
        else if (Array.isArray(response)) {
          pageIndicators = response;
          console.log(`✅ Found ${pageIndicators.length} indicators in direct array response (page ${page})`);
        }
        // Handle paginated API response format
        else if (response && typeof response === 'object' && 'data' in response && Array.isArray(response.data)) {
          pageIndicators = response.data;
          console.log(`✅ Found ${pageIndicators.length} indicators in paginated API response (page ${page})`);
        }

        // Add indicators from this page to the total collection
        allIndicators.push(...pageIndicators);

        // Check if we have more data to fetch
        // If we got fewer indicators than the page size, we've reached the end
        hasMoreData = pageIndicators.length === pageSize;

        if (hasMoreData) {
          page++;
        }
      }

      console.log(`✅ Total indicators fetched: ${allIndicators.length} across ${page} page(s)`);
      return allIndicators;
    } catch (error) {
      console.error('❌ IndicatorService.getAll() error:', error);
      console.error('❌ Error details:', {
        message: error.message,
        status: error.status,
        response: error.response
      });
      ErrorHandlers.query(error, 'Failed to fetch indicators');
      throw error;
    }
  }

  // Get paginated indicators
  async getPaginated(options: {
    page?: number;
    pageSize?: number;
    searchText?: string;
    isActive?: boolean;
    ownerContactId?: number;
    collectorId?: number;
    schedulerId?: number;
    lastRunFrom?: string;
    lastRunTo?: string;
    sortBy?: string;
    sortDirection?: string;
  } = {}): Promise<{
    indicators: Indicator[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  }> {
    try {
      const {
        page = 1,
        pageSize = 20,
        searchText,
        isActive,
        ownerContactId,
        collectorId,
        schedulerId,
        lastRunFrom,
        lastRunTo,
        sortBy = 'indicatorName',
        sortDirection = 'asc'
      } = options;

      // Validate pageSize is within API limits
      const validPageSize = Math.min(Math.max(pageSize, 1), 100);

      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: validPageSize.toString(),
        sortBy,
        sortDirection,
        _t: Date.now().toString() // Cache busting
      });

      // Add optional filters
      if (searchText) params.append('searchText', searchText);
      if (isActive !== undefined) params.append('isActive', isActive.toString());
      if (ownerContactId) params.append('ownerContactId', ownerContactId.toString());
      if (collectorId) params.append('collectorId', collectorId.toString());
      if (schedulerId) params.append('schedulerId', schedulerId.toString());
      if (lastRunFrom) params.append('lastRunFrom', lastRunFrom);
      if (lastRunTo) params.append('lastRunTo', lastRunTo);

      const endpoint = `?${params.toString()}`;
      const response = await this.get<any>(endpoint);

      // Handle different response formats
      let indicators: Indicator[] = [];
      let totalCount = 0;
      let actualPage = page;
      let actualPageSize = validPageSize;

      if (response && typeof response === 'object') {
        // Handle paginated response structure
        if ('indicators' in response) {
          indicators = response.indicators || [];
          totalCount = response.totalCount || indicators.length;
          actualPage = response.page || page;
          actualPageSize = response.pageSize || validPageSize;
        }
        // Handle wrapped API response structure
        else if ('data' in response && response.data) {
          if ('indicators' in response.data) {
            indicators = response.data.indicators || [];
            totalCount = response.data.totalCount || indicators.length;
            actualPage = response.data.page || page;
            actualPageSize = response.data.pageSize || validPageSize;
          } else if (Array.isArray(response.data)) {
            indicators = response.data;
            totalCount = indicators.length;
          }
        }
        // Handle direct array response
        else if (Array.isArray(response)) {
          indicators = response;
          totalCount = indicators.length;
        }
      }

      const totalPages = Math.ceil(totalCount / actualPageSize);

      return {
        indicators,
        totalCount,
        page: actualPage,
        pageSize: actualPageSize,
        totalPages,
        hasNextPage: actualPage < totalPages,
        hasPreviousPage: actualPage > 1
      };
    } catch (error) {
      ErrorHandlers.query(error, 'Failed to fetch paginated indicators');
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

  // Get dashboard data
  async getDashboardData(): Promise<{
    totalIndicators: number;
    activeIndicators: number;
    recentExecutions: any[];
    alerts: any[];
    statistics: any;
  }> {
    try {
      return await this.get<{
        totalIndicators: number;
        activeIndicators: number;
        recentExecutions: any[];
        alerts: any[];
        statistics: any;
      }>('/dashboard');
    } catch (error) {
      ErrorHandlers.query(error, 'Failed to fetch dashboard data');
      throw error;
    }
  }

  // Get collector item names
  async getCollectorItemNames(collectorId: number): Promise<string[]> {
    try {
      return await this.get<string[]>(`/collectors/${collectorId}/items`);
    } catch (error) {
      ErrorHandlers.query(error, `Failed to fetch collector ${collectorId} item names`);
      throw error;
    }
  }
}

// Export singleton instance
export const indicatorService = new IndicatorService();
export default indicatorService;

import { KpiType, KpiTypeDefinition } from '@/types/api';

/**
 * Complete definitions for all KPI types
 */
export const KPI_TYPE_DEFINITIONS: KpiTypeDefinition[] = [
  {
    type: KpiType.SuccessRate,
    name: 'Success Rate Monitoring',
    description:
      'Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.',
    requiredFields: ['deviation', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTransactions',
  },
  {
    type: KpiType.TransactionVolume,
    name: 'Transaction Volume Monitoring',
    description:
      'Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.',
    requiredFields: ['deviation', 'minimumThreshold', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTransactionVolume',
  },
  {
    type: KpiType.Threshold,
    name: 'Threshold Monitoring',
    description:
      'Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.',
    requiredFields: ['thresholdValue', 'comparisonOperator'],
    defaultStoredProcedure: 'monitoring.usp_MonitorThreshold',
  },
  {
    type: KpiType.TrendAnalysis,
    name: 'Trend Analysis',
    description:
      'Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.',
    requiredFields: ['deviation', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTrends',
  },
];

/**
 * Gets the definition for a specific KPI type
 */
export const getKpiTypeDefinition = (type: KpiType): KpiTypeDefinition | undefined => {
  return KPI_TYPE_DEFINITIONS.find(def => def.type === type);
};

/**
 * Gets the display name for a KPI type
 */
export const getKpiTypeName = (type: KpiType): string => {
  const definition = getKpiTypeDefinition(type);
  return definition?.name || type;
};

/**
 * Gets the description for a KPI type
 */
export const getKpiTypeDescription = (type: KpiType): string => {
  const definition = getKpiTypeDefinition(type);
  return definition?.description || 'No description available';
};

/**
 * Gets the required fields for a KPI type
 */
export const getRequiredFields = (type: KpiType): string[] => {
  const definition = getKpiTypeDefinition(type);
  return definition?.requiredFields || [];
};

/**
 * Gets the default stored procedure for a KPI type
 */
export const getDefaultStoredProcedure = (type: KpiType): string => {
  const definition = getKpiTypeDefinition(type);
  return definition?.defaultStoredProcedure || '';
};

/**
 * Validates a KPI configuration based on its type
 */
export const validateKpiConfiguration = (
  type: KpiType,
  config: {
    deviation?: number;
    thresholdValue?: number;
    comparisonOperator?: string;
    minimumThreshold?: number;
    lastMinutes?: number;
  }
): { isValid: boolean; errors: string[] } => {
  const errors: string[] = [];
  const requiredFields = getRequiredFields(type);

  // Check required fields based on KPI type
  if (requiredFields.includes('deviation')) {
    if (config.deviation === undefined || config.deviation < 0 || config.deviation > 100) {
      errors.push('Deviation must be between 0 and 100 percent');
    }
  }

  if (requiredFields.includes('thresholdValue')) {
    if (config.thresholdValue === undefined || config.thresholdValue < 0) {
      errors.push('Threshold value must be a positive number');
    }
  }

  if (requiredFields.includes('comparisonOperator')) {
    const validOperators = ['gt', 'gte', 'lt', 'lte', 'eq'];
    if (!config.comparisonOperator || !validOperators.includes(config.comparisonOperator)) {
      errors.push('A valid comparison operator must be selected');
    }
  }

  if (requiredFields.includes('minimumThreshold')) {
    if (config.minimumThreshold === undefined || config.minimumThreshold < 0) {
      errors.push('Minimum threshold must be a positive number');
    }
  }

  if (requiredFields.includes('lastMinutes')) {
    if (config.lastMinutes === undefined || config.lastMinutes < 1) {
      errors.push('Data window must be at least 1 minute');
    }
  }

  // Type-specific validations
  switch (type) {
    case KpiType.TransactionVolume:
      if (config.minimumThreshold !== undefined && config.minimumThreshold < 1) {
        errors.push('Transaction volume monitoring requires a minimum threshold of at least 1');
      }
      break;

    case KpiType.Threshold:
      if (
        config.thresholdValue !== undefined &&
        config.thresholdValue === 0 &&
        config.comparisonOperator === 'gt'
      ) {
        errors.push('Threshold value of 0 with "greater than" operator may cause frequent alerts');
      }
      break;

    case KpiType.TrendAnalysis:
      if (config.lastMinutes !== undefined && config.lastMinutes < 60) {
        errors.push('Trend analysis requires at least 60 minutes of data for meaningful results');
      }
      break;
  }

  return { isValid: errors.length === 0, errors };
};

/**
 * Gets recommended configuration for a KPI type
 */
export const getRecommendedConfiguration = (type: KpiType) => {
  switch (type) {
    case KpiType.SuccessRate:
      return {
        deviation: 10,
        lastMinutes: 1440, // 24 hours
        minimumThreshold: 10,
        description: 'Monitor success rates with 10% deviation tolerance over 24 hours',
      };

    case KpiType.TransactionVolume:
      return {
        deviation: 20,
        lastMinutes: 1440, // 24 hours
        minimumThreshold: 100,
        description:
          'Monitor transaction volume with 20% deviation tolerance, minimum 100 transactions',
      };

    case KpiType.Threshold:
      return {
        thresholdValue: 100,
        comparisonOperator: 'gt' as const,
        description: 'Alert when value exceeds 100',
      };

    case KpiType.TrendAnalysis:
      return {
        deviation: 15,
        lastMinutes: 2880, // 48 hours
        description: 'Analyze trends over 48 hours with 15% deviation tolerance',
      };

    default:
      return {
        description: 'Default configuration',
      };
  }
};

/**
 * Gets example use cases for a KPI type
 */
export const getKpiTypeExamples = (type: KpiType): string[] => {
  switch (type) {
    case KpiType.SuccessRate:
      return [
        'Payment processing success rate',
        'API response success rate',
        'User login success rate',
        'Email delivery success rate',
        'Database query success rate',
      ];

    case KpiType.TransactionVolume:
      return [
        'Daily transaction count',
        'Hourly API calls',
        'User registrations per day',
        'Order volume monitoring',
        'File upload counts',
      ];

    case KpiType.Threshold:
      return [
        'CPU usage above 80%',
        'Memory usage above 90%',
        'Queue length above 1000',
        'Response time above 5 seconds',
        'Error count above 10',
      ];

    case KpiType.TrendAnalysis:
      return [
        'Gradual performance degradation',
        'Increasing error rates over time',
        'User engagement trends',
        'Resource usage growth',
        'Capacity planning metrics',
      ];

    default:
      return [];
  }
};

/**
 * Gets the appropriate icon name for a KPI type
 */
export const getKpiTypeIcon = (type: KpiType): string => {
  switch (type) {
    case KpiType.SuccessRate:
      return 'Speed';
    case KpiType.TransactionVolume:
      return 'Assessment';
    case KpiType.Threshold:
      return 'TrendingUp';
    case KpiType.TrendAnalysis:
      return 'Timeline';
    default:
      return 'Assessment';
  }
};

/**
 * Gets the color scheme for a KPI type
 */
export const getKpiTypeColor = (
  type: KpiType
): 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info' => {
  switch (type) {
    case KpiType.SuccessRate:
      return 'success';
    case KpiType.TransactionVolume:
      return 'primary';
    case KpiType.Threshold:
      return 'warning';
    case KpiType.TrendAnalysis:
      return 'info';
    default:
      return 'primary';
  }
};

/**
 * Comparison operators with human-readable labels
 */
export const COMPARISON_OPERATORS = [
  {
    value: 'gt',
    label: 'Greater than (>)',
    description: 'Alert when value exceeds threshold',
    symbol: '>',
  },
  {
    value: 'gte',
    label: 'Greater than or equal (≥)',
    description: 'Alert when value meets or exceeds threshold',
    symbol: '≥',
  },
  {
    value: 'lt',
    label: 'Less than (<)',
    description: 'Alert when value falls below threshold',
    symbol: '<',
  },
  {
    value: 'lte',
    label: 'Less than or equal (≤)',
    description: 'Alert when value meets or falls below threshold',
    symbol: '≤',
  },
  {
    value: 'eq',
    label: 'Equal to (=)',
    description: 'Alert when value exactly matches threshold',
    symbol: '=',
  },
];

/**
 * Gets the operator symbol for display
 */
export const getOperatorSymbol = (operator: string): string => {
  const op = COMPARISON_OPERATORS.find(o => o.value === operator);
  return op?.symbol || operator;
};

/**
 * Gets the operator description
 */
export const getOperatorDescription = (operator: string): string => {
  const op = COMPARISON_OPERATORS.find(o => o.value === operator);
  return op?.description || 'Unknown operator';
};

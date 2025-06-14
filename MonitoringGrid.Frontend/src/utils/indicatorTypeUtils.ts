import { IndicatorType, IndicatorTypeDefinition } from '@/types/api';

/**
 * Complete definitions for all Indicator types
 */
export const INDICATOR_TYPE_DEFINITIONS: IndicatorTypeDefinition[] = [
  {
    type: IndicatorType.SuccessRate,
    name: 'Success Rate Monitoring',
    description:
      'Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.',
    requiredFields: ['deviation', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTransactions',
  },
  {
    type: IndicatorType.TransactionVolume,
    name: 'Transaction Volume Monitoring',
    description:
      'Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.',
    requiredFields: ['deviation', 'minimumThreshold', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTransactionVolume',
  },
  {
    type: IndicatorType.Threshold,
    name: 'Threshold Monitoring',
    description:
      'Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.',
    requiredFields: ['thresholdValue', 'comparisonOperator'],
    defaultStoredProcedure: 'monitoring.usp_MonitorThreshold',
  },
  {
    type: IndicatorType.TrendAnalysis,
    name: 'Trend Analysis',
    description:
      'Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.',
    requiredFields: ['deviation', 'lastMinutes'],
    defaultStoredProcedure: 'monitoring.usp_MonitorTrends',
  },
];

/**
 * Gets the definition for a specific Indicator type
 */
export const getIndicatorTypeDefinition = (
  type: IndicatorType
): IndicatorTypeDefinition | undefined => {
  return INDICATOR_TYPE_DEFINITIONS.find(def => def.type === type);
};

/**
 * Gets the display name for an Indicator type
 */
export const getIndicatorTypeName = (type: IndicatorType): string => {
  const definition = getIndicatorTypeDefinition(type);
  return definition?.name || type;
};

/**
 * Gets the description for an Indicator type
 */
export const getIndicatorTypeDescription = (type: IndicatorType): string => {
  const definition = getIndicatorTypeDefinition(type);
  return definition?.description || 'No description available';
};

/**
 * Gets the required fields for an Indicator type
 */
export const getRequiredFields = (type: IndicatorType): string[] => {
  const definition = getIndicatorTypeDefinition(type);
  return definition?.requiredFields || [];
};

/**
 * Gets the default stored procedure for an Indicator type
 */
export const getDefaultStoredProcedure = (type: IndicatorType): string => {
  const definition = getIndicatorTypeDefinition(type);
  return definition?.defaultStoredProcedure || '';
};

/**
 * Validates an Indicator configuration based on its type
 */
export const validateIndicatorConfiguration = (
  type: IndicatorType,
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

  // Check required fields based on Indicator type
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
    case IndicatorType.TransactionVolume:
      if (config.minimumThreshold !== undefined && config.minimumThreshold < 1) {
        errors.push('Transaction volume monitoring requires a minimum threshold of at least 1');
      }
      break;

    case IndicatorType.Threshold:
      if (
        config.thresholdValue !== undefined &&
        config.thresholdValue === 0 &&
        config.comparisonOperator === 'gt'
      ) {
        errors.push('Threshold value of 0 with "greater than" operator may cause frequent alerts');
      }
      break;

    case IndicatorType.TrendAnalysis:
      if (config.lastMinutes !== undefined && config.lastMinutes < 60) {
        errors.push('Trend analysis requires at least 60 minutes of data for meaningful results');
      }
      break;
  }

  return { isValid: errors.length === 0, errors };
};

/**
 * Gets recommended configuration for an Indicator type
 */
export const getRecommendedConfiguration = (type: IndicatorType) => {
  switch (type) {
    case IndicatorType.SuccessRate:
      return {
        deviation: 10,
        lastMinutes: 1440, // 24 hours
        minimumThreshold: 10,
        description: 'Monitor success rates with 10% deviation tolerance over 24 hours',
      };

    case IndicatorType.TransactionVolume:
      return {
        deviation: 20,
        lastMinutes: 1440, // 24 hours
        minimumThreshold: 100,
        description:
          'Monitor transaction volume with 20% deviation tolerance, minimum 100 transactions',
      };

    case IndicatorType.Threshold:
      return {
        thresholdValue: 100,
        comparisonOperator: 'gt' as const,
        description: 'Alert when value exceeds 100',
      };

    case IndicatorType.TrendAnalysis:
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
 * Gets example use cases for an Indicator type
 */
export const getIndicatorTypeExamples = (type: IndicatorType): string[] => {
  switch (type) {
    case IndicatorType.SuccessRate:
      return [
        'Payment processing success rate',
        'API response success rate',
        'User login success rate',
        'Email delivery success rate',
        'Database query success rate',
      ];

    case IndicatorType.TransactionVolume:
      return [
        'Daily transaction count',
        'Hourly API calls',
        'User registrations per day',
        'Order volume monitoring',
        'File upload counts',
      ];

    case IndicatorType.Threshold:
      return [
        'CPU usage above 80%',
        'Memory usage above 90%',
        'Queue length above 1000',
        'Response time above 5 seconds',
        'Error count above 10',
      ];

    case IndicatorType.TrendAnalysis:
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
 * Gets the appropriate icon name for an Indicator type
 */
export const getIndicatorTypeIcon = (type: IndicatorType): string => {
  switch (type) {
    case IndicatorType.SuccessRate:
      return 'Speed';
    case IndicatorType.TransactionVolume:
      return 'Assessment';
    case IndicatorType.Threshold:
      return 'TrendingUp';
    case IndicatorType.TrendAnalysis:
      return 'Timeline';
    default:
      return 'Assessment';
  }
};

/**
 * Gets the color scheme for an Indicator type
 */
export const getIndicatorTypeColor = (
  type: IndicatorType
): 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info' => {
  switch (type) {
    case IndicatorType.SuccessRate:
      return 'success';
    case IndicatorType.TransactionVolume:
      return 'primary';
    case IndicatorType.Threshold:
      return 'warning';
    case IndicatorType.TrendAnalysis:
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

// Legacy exports for backward compatibility (will be removed in future versions)
export const KPI_TYPE_DEFINITIONS = INDICATOR_TYPE_DEFINITIONS;
export const getKpiTypeDefinition = getIndicatorTypeDefinition;
export const getKpiTypeName = getIndicatorTypeName;
export const getKpiTypeDescription = getIndicatorTypeDescription;
export const validateKpiConfiguration = validateIndicatorConfiguration;
export const getKpiTypeExamples = getIndicatorTypeExamples;
export const getKpiTypeIcon = getIndicatorTypeIcon;
export const getKpiTypeColor = getIndicatorTypeColor;

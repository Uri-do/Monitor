import React from 'react';
import {
  Box,
  CardContent,
  Typography,
  Grid,
  Alert,
  Chip,
  Tooltip,
  IconButton,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  Speed as SpeedIcon,
  Timeline as TimelineIcon,
  Assessment as AssessmentIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { IndicatorType } from '@/types/api';
import { INDICATOR_TYPE_DEFINITIONS, COMPARISON_OPERATORS } from '@/utils/indicatorTypeUtils';
import { Card, InputField, Select } from '@/components/UI';

interface IndicatorTypeSelectorProps {
  selectedType: IndicatorType;
  onTypeChange: (type: IndicatorType) => void;
  thresholdValue?: number;
  onThresholdChange?: (value: number) => void;
  comparisonOperator?: 'gt' | 'lt' | 'eq' | 'gte' | 'lte';
  onOperatorChange?: (operator: 'gt' | 'lt' | 'eq' | 'gte' | 'lte') => void;
  disabled?: boolean;
}

// Icon component function for Indicator types
const getIndicatorTypeIconComponent = (type: IndicatorType) => {
  switch (type) {
    case IndicatorType.SuccessRate:
      return <SpeedIcon color="primary" />;
    case IndicatorType.TransactionVolume:
      return <AssessmentIcon color="primary" />;
    case IndicatorType.Threshold:
      return <TrendingUpIcon color="primary" />;
    case IndicatorType.TrendAnalysis:
      return <TimelineIcon color="primary" />;
    default:
      return <AssessmentIcon color="primary" />;
  }
};

export const IndicatorTypeSelector: React.FC<IndicatorTypeSelectorProps> = ({
  selectedType,
  onTypeChange,
  thresholdValue,
  onThresholdChange,
  comparisonOperator = 'gt',
  onOperatorChange,
  disabled = false,
}) => {
  const selectedDefinition = INDICATOR_TYPE_DEFINITIONS.find(def => def.type === selectedType);
  const requiresThreshold = selectedDefinition?.requiredFields.includes('thresholdValue');
  const requiresOperator = selectedDefinition?.requiredFields.includes('comparisonOperator');

  return (
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          {getIndicatorTypeIconComponent(selectedType)}
          <Typography variant="h6">Indicator Type Configuration</Typography>
          <Tooltip title="Choose the type of monitoring this Indicator will perform">
            <IconButton size="small">
              <InfoIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>

        <Grid container spacing={3}>
          {/* Indicator Type Selection */}
          <Grid item xs={12}>
            <Select
              fullWidth
              label="Indicator Type"
              value={selectedType}
              onChange={e => onTypeChange(e.target.value as IndicatorType)}
              disabled={disabled}
              options={INDICATOR_TYPE_DEFINITIONS.map(definition => ({
                value: definition.type,
                label: definition.name,
                description: definition.description,
                icon: getIndicatorTypeIconComponent(definition.type),
              }))}
            />
          </Grid>

          {/* Type Description */}
          {selectedDefinition && (
            <Grid item xs={12}>
              <Alert severity="info" sx={{ mb: 2 }}>
                <Typography variant="body2">
                  <strong>{selectedDefinition.name}:</strong> {selectedDefinition.description}
                </Typography>
                <Box mt={1}>
                  <Typography variant="caption" color="text.secondary">
                    Default Stored Procedure: {selectedDefinition.defaultStoredProcedure}
                  </Typography>
                </Box>
                <Box mt={1} display="flex" flexWrap="wrap" gap={0.5}>
                  {selectedDefinition.requiredFields.map(field => (
                    <Chip
                      key={field}
                      label={field}
                      size="small"
                      variant="outlined"
                      color="primary"
                    />
                  ))}
                </Box>
              </Alert>
            </Grid>
          )}

          {/* Threshold Configuration */}
          {requiresThreshold && (
            <>
              <Grid item xs={12} md={6}>
                <InputField
                  fullWidth
                  label="Threshold Value"
                  type="number"
                  value={thresholdValue || ''}
                  onChange={e => onThresholdChange?.(parseFloat(e.target.value) || 0)}
                  disabled={disabled}
                  helperText="The value to compare against"
                  inputProps={{ step: 0.01 }}
                />
              </Grid>

              {requiresOperator && (
                <Grid item xs={12} md={6}>
                  <Select
                    fullWidth
                    label="Comparison Operator"
                    value={comparisonOperator}
                    onChange={e => onOperatorChange?.(e.target.value as any)}
                    disabled={disabled}
                    options={COMPARISON_OPERATORS.map(op => ({
                      value: op.value,
                      label: op.label,
                      description: op.description,
                    }))}
                  />
                </Grid>
              )}
            </>
          )}

          {/* Type-specific guidance */}
          <Grid item xs={12}>
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Configuration Tips:
              </Typography>
              {selectedType === IndicatorType.SuccessRate && (
                <Typography variant="body2" color="text.secondary">
                  • Set deviation percentage to define acceptable variance from historical average •
                  Use minimum threshold to avoid alerts on low-volume periods • Recommended for: API
                  success rates, payment processing, user login success
                </Typography>
              )}
              {selectedType === IndicatorType.TransactionVolume && (
                <Typography variant="body2" color="text.secondary">
                  • Monitor transaction counts and compare to historical patterns • Set minimum
                  threshold to filter out noise during low-activity periods • Recommended for: Daily
                  transactions, API calls, user registrations
                </Typography>
              )}
              {selectedType === IndicatorType.Threshold && (
                <Typography variant="body2" color="text.secondary">
                  • Simple threshold checking - alerts when value crosses the specified limit •
                  Choose appropriate comparison operator based on your monitoring needs •
                  Recommended for: System resources, queue lengths, error counts
                </Typography>
              )}
              {selectedType === IndicatorType.TrendAnalysis && (
                <Typography variant="body2" color="text.secondary">
                  • Analyzes trends over the specified time window • Detects gradual changes that
                  might indicate emerging issues • Recommended for: Performance degradation,
                  capacity planning, user behavior changes
                </Typography>
              )}
            </Box>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default IndicatorTypeSelector;

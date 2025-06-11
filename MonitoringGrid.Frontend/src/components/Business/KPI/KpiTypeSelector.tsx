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
import { KpiType } from '@/types/api';
import { KPI_TYPE_DEFINITIONS, COMPARISON_OPERATORS } from '@/utils/kpiTypeUtils';
import { UltimateCard, UltimateInputField, UltimateSelect } from '@/components/UltimateEnterprise';

interface KpiTypeSelectorProps {
  selectedType: KpiType;
  onTypeChange: (type: KpiType) => void;
  thresholdValue?: number;
  onThresholdChange?: (value: number) => void;
  comparisonOperator?: 'gt' | 'lt' | 'eq' | 'gte' | 'lte';
  onOperatorChange?: (operator: 'gt' | 'lt' | 'eq' | 'gte' | 'lte') => void;
  disabled?: boolean;
}

// Icon component function for KPI types
const getKpiTypeIconComponent = (type: KpiType) => {
  switch (type) {
    case KpiType.SuccessRate:
      return <SpeedIcon color="primary" />;
    case KpiType.TransactionVolume:
      return <AssessmentIcon color="primary" />;
    case KpiType.Threshold:
      return <TrendingUpIcon color="primary" />;
    case KpiType.TrendAnalysis:
      return <TimelineIcon color="primary" />;
    default:
      return <AssessmentIcon color="primary" />;
  }
};

export const KpiTypeSelector: React.FC<KpiTypeSelectorProps> = ({
  selectedType,
  onTypeChange,
  thresholdValue,
  onThresholdChange,
  comparisonOperator = 'gt',
  onOperatorChange,
  disabled = false,
}) => {
  const selectedDefinition = KPI_TYPE_DEFINITIONS.find(def => def.type === selectedType);
  const requiresThreshold = selectedDefinition?.requiredFields.includes('thresholdValue');
  const requiresOperator = selectedDefinition?.requiredFields.includes('comparisonOperator');

  return (
    <UltimateCard>
      <CardContent>
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          {getKpiTypeIconComponent(selectedType)}
          <Typography variant="h6">KPI Type Configuration</Typography>
          <Tooltip title="Choose the type of monitoring this KPI will perform">
            <IconButton size="small">
              <InfoIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>

        <Grid container spacing={3}>
          {/* KPI Type Selection */}
          <Grid item xs={12}>
            <UltimateSelect
              fullWidth
              label="KPI Type"
              value={selectedType}
              onChange={e => onTypeChange(e.target.value as KpiType)}
              disabled={disabled}
              options={KPI_TYPE_DEFINITIONS.map(definition => ({
                value: definition.type,
                label: definition.name,
                description: definition.description,
                icon: getKpiTypeIconComponent(definition.type),
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
                <UltimateInputField
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
                  <UltimateSelect
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
              {selectedType === KpiType.SuccessRate && (
                <Typography variant="body2" color="text.secondary">
                  • Set deviation percentage to define acceptable variance from historical average •
                  Use minimum threshold to avoid alerts on low-volume periods • Recommended for: API
                  success rates, payment processing, user login success
                </Typography>
              )}
              {selectedType === KpiType.TransactionVolume && (
                <Typography variant="body2" color="text.secondary">
                  • Monitor transaction counts and compare to historical patterns • Set minimum
                  threshold to filter out noise during low-activity periods • Recommended for: Daily
                  transactions, API calls, user registrations
                </Typography>
              )}
              {selectedType === KpiType.Threshold && (
                <Typography variant="body2" color="text.secondary">
                  • Simple threshold checking - alerts when value crosses the specified limit •
                  Choose appropriate comparison operator based on your monitoring needs •
                  Recommended for: System resources, queue lengths, error counts
                </Typography>
              )}
              {selectedType === KpiType.TrendAnalysis && (
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
    </UltimateCard>
  );
};

export default KpiTypeSelector;

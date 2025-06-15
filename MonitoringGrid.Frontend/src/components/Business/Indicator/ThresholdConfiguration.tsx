import React from 'react';
import {
  Box,
  Grid,
  Typography,
  CardContent,
} from '@mui/material';
import {
  Warning as ThresholdIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import { Card } from '@/components/UI';
import { IndicatorDto } from '@/types/api';

interface ThresholdConfigurationProps {
  indicator: IndicatorDto;
}

/**
 * Component to display threshold configuration details
 */
export const ThresholdConfiguration: React.FC<ThresholdConfigurationProps> = ({ indicator }) => {
  const getThresholdStatusIcon = () => {
    if (
      !indicator?.thresholdType ||
      !indicator?.thresholdField ||
      !indicator?.thresholdComparison ||
      indicator?.thresholdValue === undefined
    ) {
      return <ErrorIcon sx={{ color: 'warning.main' }} />;
    }
    return <CheckIcon sx={{ color: 'success.main' }} />;
  };

  const getThresholdDescription = () => {
    if (
      !indicator?.thresholdType ||
      !indicator?.thresholdField ||
      !indicator?.thresholdComparison ||
      indicator?.thresholdValue === undefined
    ) {
      return 'Threshold configuration is incomplete. Please configure all threshold settings.';
    }

    const field = indicator.thresholdField;
    const comparison = getComparisonText(indicator.thresholdComparison);
    const value = indicator.thresholdValue;
    const type = formatThresholdType(indicator.thresholdType);

    return `Alert when ${field} is ${comparison} ${value} (${type})`;
  };

  const getComparisonSymbol = (comparison?: string) => {
    switch (comparison) {
      case 'gt':
        return '>';
      case 'gte':
        return '≥';
      case 'lt':
        return '<';
      case 'lte':
        return '≤';
      case 'eq':
        return '=';
      default:
        return comparison || 'Not Set';
    }
  };

  const getComparisonText = (comparison?: string) => {
    switch (comparison) {
      case 'gt':
        return 'greater than';
      case 'gte':
        return 'greater than or equal to';
      case 'lt':
        return 'less than';
      case 'lte':
        return 'less than or equal to';
      case 'eq':
        return 'equal to';
      default:
        return 'compared to';
    }
  };

  const formatThresholdType = (type?: string) => {
    switch (type) {
      case 'volume_average':
        return 'Volume Average';
      case 'threshold_value':
        return 'Threshold Value';
      case 'percentage':
        return 'Percentage';
      default:
        return type || 'Not Set';
    }
  };

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box display="flex" alignItems="center" gap={2} mb={3}>
          <ThresholdIcon color="primary" />
          <Box flex={1}>
            <Typography variant="h6" sx={{ fontWeight: 600, mb: 0.5 }}>
              Alert Threshold Configuration
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {getThresholdDescription()}
            </Typography>
          </Box>
          {getThresholdStatusIcon()}
        </Box>

        <Grid container spacing={2}>
          <Grid item xs={6} md={3}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Field
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 500 }}>
              {indicator.thresholdField || 'Not Set'}
            </Typography>
          </Grid>
          <Grid item xs={6} md={3}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Comparison
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 500 }}>
              {getComparisonSymbol(indicator.thresholdComparison)}
            </Typography>
          </Grid>
          <Grid item xs={6} md={3}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Threshold Value
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 500 }}>
              {indicator.thresholdValue !== undefined ? indicator.thresholdValue : 'Not Set'}
            </Typography>
          </Grid>
          <Grid item xs={6} md={3}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Type
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 500 }}>
              {formatThresholdType(indicator.thresholdType)}
            </Typography>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default ThresholdConfiguration;

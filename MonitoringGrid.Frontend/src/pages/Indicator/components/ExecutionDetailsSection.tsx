import React from 'react';
import {
  Grid,
  Typography,
  CardContent,
  Stack,
  Divider,
} from '@mui/material';
import {
  Settings as SettingsIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { Card, InfoItem } from '@/components';
import { IndicatorDto } from '@/types/api';

interface ExecutionDetailsSectionProps {
  indicator: IndicatorDto;
}

export const ExecutionDetailsSection: React.FC<ExecutionDetailsSectionProps> = ({
  indicator,
}) => {
  return (
    <Grid item xs={12} md={6}>
      <Card>
        <CardContent>
          <Typography
            variant="h6"
            gutterBottom
            sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
          >
            <SettingsIcon color="primary" />
            Execution Details
          </Typography>
          <Stack spacing={2}>
            <InfoItem label="Data Window" value={`${indicator.lastMinutes} minutes`} />
            {indicator.averageLastDays && (
              <InfoItem label="Average Last Days" value={indicator.averageLastDays} />
            )}
            <Divider />
            <InfoItem
              label="Created"
              value={format(new Date(indicator.createdDate), 'MMM dd, yyyy HH:mm')}
            />
            <InfoItem
              label="Last Updated"
              value={format(new Date(indicator.updatedDate), 'MMM dd, yyyy HH:mm')}
            />
            {indicator.lastRunResult && (
              <InfoItem label="Last Run Result" value={indicator.lastRunResult} />
            )}
            {indicator.indicatorDesc && (
              <>
                <Divider />
                <InfoItem label="Description" value={indicator.indicatorDesc} />
              </>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default ExecutionDetailsSection;

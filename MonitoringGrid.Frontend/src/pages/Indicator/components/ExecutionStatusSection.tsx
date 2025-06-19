import React from 'react';
import {
  Grid,
  Typography,
  CardContent,
  Stack,
  Divider,
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
} from '@mui/icons-material';
import { Card, InfoItem } from '@/components';
import { SchedulerDetails } from '@/components/Business';
import { IndicatorDto } from '@/types/api';
import { safeFormatDate, safeDateDuration, formatDuration } from '@/utils/dateUtils';

interface ExecutionStatusSectionProps {
  indicator: IndicatorDto;
}

export const ExecutionStatusSection: React.FC<ExecutionStatusSectionProps> = ({
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
            <ScheduleIcon color="primary" />
            Scheduler & Execution
          </Typography>
          <Stack spacing={2}>
            {indicator.schedulerID ? (
              <SchedulerDetails schedulerId={indicator.schedulerID} />
            ) : (
              <Typography variant="body2" color="text.secondary">
                No scheduler assigned - manual execution only
              </Typography>
            )}
            <Divider />
            <InfoItem
              label="Currently Running"
              value={indicator.isCurrentlyRunning ? 'Yes' : 'No'}
            />
            {indicator.executionStartTime && (
              <InfoItem
                label="Execution Started"
                value={safeFormatDate(indicator.executionStartTime, 'MMM dd, yyyy HH:mm:ss')}
              />
            )}
            {indicator.executionContext && (
              <InfoItem label="Execution Context" value={indicator.executionContext} />
            )}
            {indicator.isCurrentlyRunning && indicator.executionStartTime && (
              <InfoItem
                label="Running Duration"
                value={formatDuration(safeDateDuration(indicator.executionStartTime))}
              />
            )}
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default ExecutionStatusSection;

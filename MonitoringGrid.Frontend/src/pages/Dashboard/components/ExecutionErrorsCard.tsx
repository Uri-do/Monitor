import React, { useState } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Alert,
  Collapse,
} from '@mui/material';
import ExecutionErrorDialog, { ExecutionErrorDetails } from '@/components/Business/Indicator/ExecutionErrorDialog';
import { ErrorHeader } from './ExecutionErrorsCard/ErrorHeader';
import { ErrorList } from './ExecutionErrorsCard/ErrorList';
import { useExecutionErrors, type ExecutionError } from './ExecutionErrorsCard/useExecutionErrors';

interface ExecutionErrorsCardProps {
  maxErrors?: number;
}

const ExecutionErrorsCard: React.FC<ExecutionErrorsCardProps> = ({
  maxErrors = 5,
}) => {
  const [selectedError, setSelectedError] = useState<ExecutionErrorDetails | null>(null);
  const [showErrorDialog, setShowErrorDialog] = useState(false);

  const {
    executionErrors,
    isExpanded,
    handleClearErrors,
    toggleExpanded,
  } = useExecutionErrors({ maxErrors });

  const handleViewError = (error: ExecutionError) => {
    const errorDetails: ExecutionErrorDetails = {
      indicatorID: error.indicatorID,
      indicator: error.indicator,
      owner: error.owner,
      errorMessage: error.errorMessage,
      duration: error.duration,
      completedAt: error.completedAt,
      collectorID: error.collectorID,
      collectorItemName: error.collectorItemName,
      lastMinutes: error.lastMinutes,
      executionContext: error.executionContext,
    };

    setSelectedError(errorDetails);
    setShowErrorDialog(true);
  };

  return (
    <>
      <Grid item xs={12} md={6}>
        <Card sx={{ height: '100%' }}>
          <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <ErrorHeader
              errorCount={executionErrors.length}
              isExpanded={isExpanded}
              onToggleExpanded={toggleExpanded}
              onClearErrors={handleClearErrors}
            />

            <Collapse in={isExpanded}>
              <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
                <ErrorList
                  errors={executionErrors}
                  onViewError={handleViewError}
                />
              </Box>
            </Collapse>

            {executionErrors.length > 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
                <Typography variant="body2">
                  Showing {executionErrors.length} most recent execution errors.
                  Click "Details" to view full error analysis and troubleshooting steps.
                </Typography>
              </Alert>
            )}
          </CardContent>
        </Card>
      </Grid>

      <ExecutionErrorDialog
        open={showErrorDialog}
        onClose={() => setShowErrorDialog(false)}
        errorDetails={selectedError}
      />
    </>
  );
};

export default ExecutionErrorsCard;

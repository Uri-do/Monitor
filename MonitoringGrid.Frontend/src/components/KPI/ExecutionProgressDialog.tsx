import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  LinearProgress,
  Stepper,
  Step,
  StepLabel,
  StepContent,
  Paper,
  Chip,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Divider,
} from '@mui/material';
import {
  PlayArrow as ExecuteIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Schedule as ScheduleIcon,
  Storage as DatabaseIcon,
  Code as CodeIcon,
  ExpandMore as ExpandMoreIcon,
  Timer as TimerIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';

interface ExecutionStep {
  id: string;
  label: string;
  status: 'pending' | 'active' | 'completed' | 'error';
  startTime?: Date;
  endTime?: Date;
  details?: string;
  error?: string;
}

interface ExecutionProgressDialogProps {
  open: boolean;
  onClose: () => void;
  kpiName: string;
  onExecute: () => Promise<any>;
}

const ExecutionProgressDialog: React.FC<ExecutionProgressDialogProps> = ({
  open,
  onClose,
  kpiName,
  onExecute,
}) => {
  const [isExecuting, setIsExecuting] = useState(false);
  const [activeStep, setActiveStep] = useState(0);
  const [executionResult, setExecutionResult] = useState<any>(null);
  const [executionError, setExecutionError] = useState<string | null>(null);
  const [startTime, setStartTime] = useState<Date | null>(null);
  const [endTime, setEndTime] = useState<Date | null>(null);

  const [steps, setSteps] = useState<ExecutionStep[]>([
    {
      id: 'initialize',
      label: 'Initializing Execution',
      status: 'pending',
    },
    {
      id: 'connect',
      label: 'Connecting to Database',
      status: 'pending',
    },
    {
      id: 'execute',
      label: 'Executing Stored Procedure',
      status: 'pending',
    },
    {
      id: 'process',
      label: 'Processing Results',
      status: 'pending',
    },
    {
      id: 'store',
      label: 'Storing Historical Data',
      status: 'pending',
    },
    {
      id: 'complete',
      label: 'Execution Complete',
      status: 'pending',
    },
  ]);

  const updateStep = (stepId: string, updates: Partial<ExecutionStep>) => {
    setSteps(prev => prev.map(step => 
      step.id === stepId ? { ...step, ...updates } : step
    ));
  };

  const simulateExecution = async () => {
    setIsExecuting(true);
    setStartTime(new Date());
    setExecutionResult(null);
    setExecutionError(null);
    setActiveStep(0);

    try {
      // Step 1: Initialize
      updateStep('initialize', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 500));
      updateStep('initialize', { 
        status: 'completed', 
        endTime: new Date(),
        details: 'Execution context prepared'
      });
      setActiveStep(1);

      // Step 2: Connect to Database
      updateStep('connect', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 800));
      updateStep('connect', { 
        status: 'completed', 
        endTime: new Date(),
        details: 'Connected to ProgressPlayDBTest database'
      });
      setActiveStep(2);

      // Step 3: Execute Stored Procedure
      updateStep('execute', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 1200));
      
      // Actually execute the KPI
      const result = await onExecute();
      
      updateStep('execute', { 
        status: 'completed', 
        endTime: new Date(),
        details: `Stored procedure executed successfully in ${result.executionTimeMs || 'N/A'}ms`
      });
      setActiveStep(3);

      // Step 4: Process Results
      updateStep('process', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 600));
      updateStep('process', { 
        status: 'completed', 
        endTime: new Date(),
        details: `Current value: ${result.currentValue}, Deviation: ${result.deviationPercent?.toFixed(2) || 'N/A'}%`
      });
      setActiveStep(4);

      // Step 5: Store Historical Data
      updateStep('store', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 400));
      updateStep('store', { 
        status: 'completed', 
        endTime: new Date(),
        details: 'Audit data stored with comprehensive metadata'
      });
      setActiveStep(5);

      // Step 6: Complete
      updateStep('complete', { status: 'active', startTime: new Date() });
      await new Promise(resolve => setTimeout(resolve, 200));
      updateStep('complete', { 
        status: 'completed', 
        endTime: new Date(),
        details: result.isSuccessful ? 'Execution completed successfully' : 'Execution completed with errors'
      });

      setExecutionResult(result);
      setEndTime(new Date());

    } catch (error: any) {
      const currentStepId = steps[activeStep]?.id;
      if (currentStepId) {
        updateStep(currentStepId, { 
          status: 'error', 
          endTime: new Date(),
          error: error.message || 'Unknown error occurred'
        });
      }
      setExecutionError(error.message || 'Execution failed');
      setEndTime(new Date());
    } finally {
      setIsExecuting(false);
    }
  };

  const handleClose = () => {
    if (!isExecuting) {
      onClose();
      // Reset state
      setActiveStep(0);
      setExecutionResult(null);
      setExecutionError(null);
      setStartTime(null);
      setEndTime(null);
      setSteps(prev => prev.map(step => ({ ...step, status: 'pending' as const })));
    }
  };

  const getTotalDuration = () => {
    if (startTime && endTime) {
      return endTime.getTime() - startTime.getTime();
    }
    return null;
  };

  const getStepIcon = (step: ExecutionStep) => {
    switch (step.status) {
      case 'completed':
        return <SuccessIcon color="success" />;
      case 'error':
        return <ErrorIcon color="error" />;
      case 'active':
        return <ScheduleIcon color="primary" />;
      default:
        return <ScheduleIcon color="disabled" />;
    }
  };

  return (
    <Dialog 
      open={open} 
      onClose={handleClose}
      maxWidth="md"
      fullWidth
      disableEscapeKeyDown={isExecuting}
    >
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          <ExecuteIcon />
          KPI Execution Progress
        </Box>
        <Typography variant="subtitle2" color="text.secondary">
          {kpiName}
        </Typography>
      </DialogTitle>

      <DialogContent>
        <Box sx={{ mb: 3 }}>
          {/* Overall Progress */}
          <Box sx={{ mb: 2 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Overall Progress
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {Math.round((activeStep / (steps.length - 1)) * 100)}%
              </Typography>
            </Box>
            <LinearProgress 
              variant="determinate" 
              value={(activeStep / (steps.length - 1)) * 100}
              sx={{ height: 8, borderRadius: 4 }}
            />
          </Box>

          {/* Execution Summary */}
          {(startTime || endTime) && (
            <Paper sx={{ p: 2, mb: 2, backgroundColor: 'grey.50' }}>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography variant="body2" color="text.secondary">
                    Started: {startTime ? format(startTime, 'HH:mm:ss.SSS') : 'N/A'}
                  </Typography>
                  {endTime && (
                    <Typography variant="body2" color="text.secondary">
                      Completed: {format(endTime, 'HH:mm:ss.SSS')}
                    </Typography>
                  )}
                </Box>
                {getTotalDuration() && (
                  <Chip 
                    icon={<TimerIcon />}
                    label={`${getTotalDuration()}ms`}
                    color="primary"
                    variant="outlined"
                  />
                )}
              </Box>
            </Paper>
          )}

          {/* Execution Result */}
          {executionResult && (
            <Alert 
              severity={executionResult.isSuccessful ? 'success' : 'error'}
              sx={{ mb: 2 }}
            >
              <Typography variant="body2">
                <strong>Execution {executionResult.isSuccessful ? 'Successful' : 'Failed'}</strong>
              </Typography>
              <Typography variant="body2">
                Current Value: {executionResult.currentValue}, 
                Historical: {executionResult.historicalValue || 'N/A'}, 
                Deviation: {executionResult.deviationPercent?.toFixed(2) || 'N/A'}%
              </Typography>
              {executionResult.errorMessage && (
                <Typography variant="body2" color="error">
                  Error: {executionResult.errorMessage}
                </Typography>
              )}
            </Alert>
          )}

          {/* Error Display */}
          {executionError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              <Typography variant="body2">
                <strong>Execution Failed</strong>
              </Typography>
              <Typography variant="body2">
                {executionError}
              </Typography>
            </Alert>
          )}
        </Box>

        {/* Step Details */}
        <Stepper activeStep={activeStep} orientation="vertical">
          {steps.map((step, index) => (
            <Step key={step.id}>
              <StepLabel 
                icon={getStepIcon(step)}
                error={step.status === 'error'}
              >
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body2">
                    {step.label}
                  </Typography>
                  {step.startTime && step.endTime && (
                    <Typography variant="caption" color="text.secondary">
                      {step.endTime.getTime() - step.startTime.getTime()}ms
                    </Typography>
                  )}
                </Box>
              </StepLabel>
              <StepContent>
                {step.details && (
                  <Typography variant="body2" color="text.secondary">
                    {step.details}
                  </Typography>
                )}
                {step.error && (
                  <Typography variant="body2" color="error">
                    Error: {step.error}
                  </Typography>
                )}
              </StepContent>
            </Step>
          ))}
        </Stepper>

        {/* Detailed Results */}
        {executionResult && (
          <Box sx={{ mt: 3 }}>
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">
                  <CodeIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Detailed Results
                </Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Box component="pre" sx={{ 
                  backgroundColor: 'grey.100', 
                  p: 2, 
                  borderRadius: 1, 
                  overflow: 'auto',
                  fontSize: '0.875rem',
                  fontFamily: 'monospace',
                  maxHeight: 300
                }}>
                  {JSON.stringify(executionResult, null, 2)}
                </Box>
              </AccordionDetails>
            </Accordion>
          </Box>
        )}
      </DialogContent>

      <DialogActions>
        {!isExecuting && !executionResult && (
          <Button 
            onClick={simulateExecution}
            variant="contained"
            startIcon={<ExecuteIcon />}
          >
            Execute KPI
          </Button>
        )}
        <Button 
          onClick={handleClose}
          disabled={isExecuting}
          variant={executionResult ? 'contained' : 'outlined'}
        >
          {executionResult ? 'Done' : 'Cancel'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ExecutionProgressDialog;

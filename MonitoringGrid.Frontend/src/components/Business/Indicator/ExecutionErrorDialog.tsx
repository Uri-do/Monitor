import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Chip,
  Divider,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Close as CloseIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  ExpandMore as ExpandMoreIcon,
  Storage as StorageIcon,
  Settings as SettingsIcon,
  Timeline as TimelineIcon,
  ContentCopy as CopyIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';

export interface ExecutionErrorDetails {
  indicatorID: number;
  indicator: string;
  owner: string;
  errorMessage: string;
  duration: number;
  completedAt: string;
  collectorID?: number;
  collectorItemName?: string;
  lastMinutes?: number;
  executionContext?: string;
  availableItems?: string[];
  expectedItem?: string;
  collectorData?: any[];
  configurationIssues?: string[];
}

interface ExecutionErrorDialogProps {
  open: boolean;
  onClose: () => void;
  errorDetails: ExecutionErrorDetails | null;
}

const ExecutionErrorDialog: React.FC<ExecutionErrorDialogProps> = ({
  open,
  onClose,
  errorDetails,
}) => {
  if (!errorDetails) return null;

  const handleCopyError = () => {
    const errorText = `
Indicator: ${errorDetails.indicator} (ID: ${errorDetails.indicatorID})
Owner: ${errorDetails.owner}
Error: ${errorDetails.errorMessage}
Duration: ${errorDetails.duration}ms
Completed At: ${errorDetails.completedAt}
Collector ID: ${errorDetails.collectorID}
Collector Item Name: ${errorDetails.collectorItemName}
Last Minutes: ${errorDetails.lastMinutes}
Available Items: ${errorDetails.availableItems?.join(', ') || 'None'}
    `.trim();
    
    navigator.clipboard.writeText(errorText);
  };

  const parseAvailableItems = (errorMessage: string): string[] => {
    const match = errorMessage.match(/Available items: \[(.*?)\]/);
    if (match && match[1]) {
      return match[1].split(', ').map(item => item.replace(/'/g, ''));
    }
    return [];
  };

  const availableItems = errorDetails.availableItems || parseAvailableItems(errorDetails.errorMessage);
  const isItemNotFoundError = errorDetails.errorMessage.includes('not found in collector results');

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box display="flex" alignItems="center" gap={1}>
            <ErrorIcon color="error" />
            <Typography variant="h6">Indicator Execution Error</Typography>
          </Box>
          <Box display="flex" alignItems="center" gap={1}>
            <Tooltip title="Copy error details">
              <IconButton onClick={handleCopyError} size="small">
                <CopyIcon />
              </IconButton>
            </Tooltip>
            <IconButton onClick={onClose} size="small">
              <CloseIcon />
            </IconButton>
          </Box>
        </Box>
      </DialogTitle>

      <DialogContent>
        {/* Basic Information */}
        <Box mb={3}>
          <Typography variant="h6" gutterBottom>
            <InfoIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Indicator Information
          </Typography>
          <Box display="flex" flexWrap="wrap" gap={1} mb={2}>
            <Chip label={`ID: ${errorDetails.indicatorID}`} variant="outlined" />
            <Chip label={`Owner: ${errorDetails.owner}`} variant="outlined" />
            <Chip 
              label={`Duration: ${errorDetails.duration}ms`} 
              color={errorDetails.duration > 5000 ? 'warning' : 'default'}
              variant="outlined" 
            />
            <Chip 
              label={errorDetails.executionContext || 'Scheduled'} 
              color="primary" 
              variant="outlined" 
            />
          </Box>
          <Typography variant="body1" fontWeight="medium">
            {errorDetails.indicator}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Completed at: {format(new Date(errorDetails.completedAt), 'PPpp')}
          </Typography>
        </Box>

        <Divider sx={{ my: 2 }} />

        {/* Error Message */}
        <Alert severity="error" sx={{ mb: 3 }}>
          <Typography variant="body1" fontWeight="medium">
            {errorDetails.errorMessage}
          </Typography>
        </Alert>

        {/* Collector Configuration */}
        {errorDetails.collectorID && (
          <Accordion sx={{ mb: 2 }}>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="h6">
                <SettingsIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Collector Configuration
              </Typography>
            </AccordionSummary>
            <AccordionDetails>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableBody>
                    <TableRow>
                      <TableCell><strong>Collector ID</strong></TableCell>
                      <TableCell>{errorDetails.collectorID}</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><strong>Expected Item Name</strong></TableCell>
                      <TableCell>
                        <Chip 
                          label={errorDetails.collectorItemName || 'Not specified'} 
                          color={errorDetails.collectorItemName ? 'primary' : 'error'}
                          size="small"
                        />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><strong>Time Window</strong></TableCell>
                      <TableCell>{errorDetails.lastMinutes} minutes</TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </AccordionDetails>
          </Accordion>
        )}

        {/* Data Analysis for Item Not Found Errors */}
        {isItemNotFoundError && (
          <Accordion sx={{ mb: 2 }}>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="h6">
                <StorageIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Data Analysis
              </Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Box mb={2}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  The indicator is looking for a specific item that doesn't exist in the collector results.
                </Typography>
              </Box>
              
              <Box display="flex" gap={2} mb={2}>
                <Box flex={1}>
                  <Typography variant="subtitle2" color="error" gutterBottom>
                    Expected Item:
                  </Typography>
                  <Chip 
                    label={errorDetails.collectorItemName || 'Not specified'} 
                    color="error" 
                    size="small"
                  />
                </Box>
                <Box flex={1}>
                  <Typography variant="subtitle2" color="success.main" gutterBottom>
                    Available Items ({availableItems.length}):
                  </Typography>
                  <Box display="flex" flexWrap="wrap" gap={0.5}>
                    {availableItems.length > 0 ? (
                      availableItems.map((item, index) => (
                        <Chip 
                          key={index}
                          label={item || 'Empty'} 
                          color="success" 
                          size="small" 
                          variant="outlined"
                        />
                      ))
                    ) : (
                      <Chip label="No data returned" color="warning" size="small" />
                    )}
                  </Box>
                </Box>
              </Box>

              {availableItems.length > 0 && (
                <Alert severity="info" sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>Suggestion:</strong> Update the indicator's CollectorItemName to match one of the available items, 
                    or check if the collector stored procedure is returning the expected data structure.
                  </Typography>
                </Alert>
              )}

              {availableItems.length === 0 && (
                <Alert severity="warning" sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>Issue:</strong> The collector stored procedure is not returning any data. 
                    Check the stored procedure parameters, data availability, or time window settings.
                  </Typography>
                </Alert>
              )}
            </AccordionDetails>
          </Accordion>
        )}

        {/* Troubleshooting Steps */}
        <Accordion>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6">
              <TimelineIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
              Troubleshooting Steps
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box component="ol" sx={{ pl: 2 }}>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                Verify the collector stored procedure is working correctly
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                Check if the expected data exists in the specified time window
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                Validate the CollectorItemName configuration matches available data
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                Review the stored procedure parameters and data types
              </Typography>
              <Typography component="li" variant="body2">
                Consider adjusting the LastMinutes time window if data is sparse
              </Typography>
            </Box>
          </AccordionDetails>
        </Accordion>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} variant="contained">
          Close
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ExecutionErrorDialog;

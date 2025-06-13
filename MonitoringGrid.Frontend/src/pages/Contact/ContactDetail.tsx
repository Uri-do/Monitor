import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  Stack,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Edit as EditIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Assignment as AssignIcon,
  Visibility as ViewIcon,
  PlayArrow as ExecuteIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useContact } from '@/hooks/useContacts';
import { useIndicators } from '@/hooks/useIndicators';
import { useExecuteIndicator } from '@/hooks/useIndicatorMutations';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import { PageHeader, StatusChip, LoadingSpinner } from '@/components';

const ContactDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);

  const contactId = parseInt(id || '0');

  // Use enhanced hooks for data fetching
  const { data: contact, isLoading: contactLoading } = useContact(contactId);
  const { data: allIndicators = [], isLoading: indicatorsLoading } = useIndicators();

  // Use mutation hook for Indicator execution
  const executeIndicatorMutation = useExecuteIndicator();

  if (contactLoading) {
    return <LoadingSpinner />;
  }

  if (!contact) {
    return (
      <Box>
        <Alert severity="error">Contact not found or you don't have permission to view it.</Alert>
      </Box>
    );
  }

  const handleIndicatorView = (indicatorId: number) => {
    navigate(`/indicators/${indicatorId}`);
  };

  const handleIndicatorExecute = (indicatorId: number) => {
    executeIndicatorMutation.mutate({ indicatorID: indicatorId });
  };

  return (
    <Box>
      <PageHeader
        title={contact.name}
        subtitle={`Contact Information and Indicator Assignments`}
        breadcrumbs={[{ label: 'Contacts', href: '/contacts' }, { label: contact.name }]}
        primaryAction={{
          label: 'Edit Contact',
          icon: <EditIcon />,
          onClick: () => navigate(`/contacts/${contactId}/edit`),
        }}
        actions={[
          {
            label: 'Assign Indicators',
            icon: <AssignIcon />,
            onClick: () => setAssignDialogOpen(true),
            variant: 'outlined',
          },
        ]}
      />

      <Grid container spacing={3}>
        {/* Contact Information */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Contact Information
              </Typography>
              <Stack spacing={2}>
                <Box>
                  <Typography variant="body2" color="textSecondary">
                    Status
                  </Typography>
                  <StatusChip status={contact.isActive ? 'active' : 'inactive'} sx={{ mt: 0.5 }} />
                </Box>

                {contact.email && (
                  <Box display="flex" alignItems="center" gap={1}>
                    <EmailIcon color="action" fontSize="small" />
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Email
                      </Typography>
                      <Typography variant="body1">{contact.email}</Typography>
                    </Box>
                  </Box>
                )}

                {contact.phone && (
                  <Box display="flex" alignItems="center" gap={1}>
                    <PhoneIcon color="action" fontSize="small" />
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Phone
                      </Typography>
                      <Typography variant="body1">{contact.phone}</Typography>
                    </Box>
                  </Box>
                )}
              </Stack>
            </CardContent>
          </Card>

          {/* Audit Information */}
          <Card sx={{ mt: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Audit Information
              </Typography>
              <Stack spacing={1}>
                <Box>
                  <Typography variant="body2" color="textSecondary">
                    Created Date
                  </Typography>
                  <Typography variant="body1">
                    {format(new Date(contact.createdDate), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="body2" color="textSecondary">
                    Last Modified
                  </Typography>
                  <Typography variant="body1">
                    {format(new Date(contact.modifiedDate), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Assigned Indicators */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">Assigned Indicators ({contact.assignedKpis.length})</Typography>
                <Button
                  variant="outlined"
                  startIcon={<AssignIcon />}
                  onClick={() => setAssignDialogOpen(true)}
                  size="small"
                >
                  Manage Assignments
                </Button>
              </Box>

              {contact.assignedKpis.length > 0 ? (
                <TableContainer component={Paper} variant="outlined">
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Indicator</TableCell>
                        <TableCell>Owner</TableCell>
                        <TableCell>Priority</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell align="right">Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {contact.assignedKpis.map(indicator => (
                        <TableRow key={indicator.kpiId}>
                          <TableCell>
                            <Typography variant="body2" fontWeight="medium">
                              {indicator.indicator}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2">{indicator.owner}</Typography>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={`Priority ${indicator.priority}`}
                              color={
                                indicator.priority <= 2
                                  ? 'error'
                                  : indicator.priority === 3
                                    ? 'warning'
                                    : 'success'
                              }
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <StatusChip status={indicator.isActive ? 'active' : 'inactive'} />
                          </TableCell>
                          <TableCell align="right">
                            <Stack direction="row" spacing={1}>
                              <Tooltip title="View Indicator">
                                <IconButton size="small" onClick={() => handleIndicatorView(indicator.kpiId)}>
                                  <ViewIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Execute Indicator">
                                <IconButton
                                  size="small"
                                  onClick={() => handleIndicatorExecute(indicator.kpiId)}
                                  disabled={!indicator.isActive}
                                >
                                  <ExecuteIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            </Stack>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              ) : (
                <Alert severity="info">
                  No Indicators assigned to this contact. Click "Manage Assignments" to assign Indicators.
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Indicator Assignment Dialog */}
      <Dialog
        open={assignDialogOpen}
        onClose={() => setAssignDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Manage Indicator Assignments</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Select Indicators to assign to {contact.name}. This feature will be fully implemented soon.
          </Typography>
          {/* TODO: Implement Indicator assignment interface */}
          <Alert severity="info" sx={{ mt: 2 }}>
            Indicator assignment interface is under development. For now, you can manage assignments
            through the Indicator edit page.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ContactDetail;

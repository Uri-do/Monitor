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
import { useKpis } from '@/hooks/useKpis';
import { useExecuteKpi } from '@/hooks/mutations';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  PageHeader,
  StatusChip,
  LoadingSpinner,
} from '@/components';

const ContactDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);

  const contactId = parseInt(id || '0');

  // Use enhanced hooks for data fetching
  const { data: contact, isLoading: contactLoading } = useContact(contactId);
  const { data: allKpis = [], isLoading: kpisLoading } = useKpis();

  // Use mutation hook for KPI execution
  const executeKpiMutation = useExecuteKpi();

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

  const handleKpiView = (kpiId: number) => {
    navigate(`/kpis/${kpiId}`);
  };

  const handleKpiExecute = (kpiId: number) => {
    executeKpiMutation.mutate({ kpiId });
  };

  return (
    <Box>
      <PageHeader
        title={contact.name}
        subtitle={`Contact Information and KPI Assignments`}
        breadcrumbs={[{ label: 'Contacts', href: '/contacts' }, { label: contact.name }]}
        primaryAction={{
          label: 'Edit Contact',
          icon: <EditIcon />,
          onClick: () => navigate(`/contacts/${contactId}/edit`),
        }}
        actions={[
          {
            label: 'Assign KPIs',
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

        {/* Assigned KPIs */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">Assigned KPIs ({contact.assignedKpis.length})</Typography>
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
                      {contact.assignedKpis.map(kpi => (
                        <TableRow key={kpi.kpiId}>
                          <TableCell>
                            <Typography variant="body2" fontWeight="medium">
                              {kpi.indicator}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2">{kpi.owner}</Typography>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={`Priority ${kpi.priority}`}
                              color={
                                kpi.priority <= 2
                                  ? 'error'
                                  : kpi.priority === 3
                                    ? 'warning'
                                    : 'success'
                              }
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <StatusChip status={kpi.isActive ? 'active' : 'inactive'} />
                          </TableCell>
                          <TableCell align="right">
                            <Stack direction="row" spacing={1}>
                              <Tooltip title="View KPI">
                                <IconButton size="small" onClick={() => handleKpiView(kpi.kpiId)}>
                                  <ViewIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Execute KPI">
                                <IconButton
                                  size="small"
                                  onClick={() => handleKpiExecute(kpi.kpiId)}
                                  disabled={!kpi.isActive}
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
                  No KPIs assigned to this contact. Click "Manage Assignments" to assign KPIs.
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* KPI Assignment Dialog */}
      <Dialog
        open={assignDialogOpen}
        onClose={() => setAssignDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Manage KPI Assignments</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Select KPIs to assign to {contact.name}. This feature will be fully implemented soon.
          </Typography>
          {/* TODO: Implement KPI assignment interface */}
          <Alert severity="info" sx={{ mt: 2 }}>
            KPI assignment interface is under development. For now, you can manage assignments
            through the KPI edit page.
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

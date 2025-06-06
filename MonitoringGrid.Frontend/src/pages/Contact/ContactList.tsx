import React, { useState, useMemo } from 'react';
import { Box, Chip, Stack } from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Assignment as AssignIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { contactApi } from '@/services/api';
import { ContactDto } from '@/types/api';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  DataTable,
  DataTableColumn,
  PageHeader,
  FilterPanel,
  StatusChip,
  LoadingSpinner,
} from '@/components/Common';

const ContactList: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState({
    isActive: '',
    search: '',
  });
  const [selectedRows, setSelectedRows] = useState<ContactDto[]>([]);

  // Fetch Contacts
  const { data: contacts = [], isLoading, refetch } = useQuery({
    queryKey: ['contacts', filters],
    queryFn: () => contactApi.getContacts({
      isActive: filters.isActive ? filters.isActive === 'true' : undefined,
      search: filters.search || undefined,
    }),
  });

  // Delete Contact mutation
  const deleteMutation = useMutation({
    mutationFn: contactApi.deleteContact,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Contact deleted successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete contact');
    },
  });

  const handleDelete = (contact: ContactDto) => {
    if (window.confirm(`Are you sure you want to delete "${contact.name}"?`)) {
      deleteMutation.mutate(contact.contactId);
    }
  };

  // Filter data based on search
  const filteredContacts = useMemo(() => {
    if (!filters.search) return contacts;

    const searchLower = filters.search.toLowerCase();
    return contacts.filter(contact =>
      contact.name.toLowerCase().includes(searchLower) ||
      contact.email?.toLowerCase().includes(searchLower) ||
      contact.phone?.toLowerCase().includes(searchLower)
    );
  }, [contacts, filters.search]);

  // Define table columns
  const columns: DataTableColumn<ContactDto>[] = [
    {
      id: 'name',
      label: 'Contact Name',
      sortable: true,
      minWidth: 180,
      render: (value, row) => (
        <Box>
          <Box sx={{ fontWeight: 'medium', mb: 0.5 }}>{value}</Box>
          <Stack direction="row" spacing={1}>
            {row.email && (
              <Chip
                icon={<EmailIcon />}
                label={row.email}
                size="small"
                variant="outlined"
                sx={{ fontSize: '0.7rem', height: 20 }}
              />
            )}
            {row.phone && (
              <Chip
                icon={<PhoneIcon />}
                label={row.phone}
                size="small"
                variant="outlined"
                sx={{ fontSize: '0.7rem', height: 20 }}
              />
            )}
          </Stack>
        </Box>
      ),
    },
    {
      id: 'isActive',
      label: 'Status',
      sortable: true,
      minWidth: 100,
      render: (value) => (
        <StatusChip status={value ? 'active' : 'inactive'} />
      ),
    },
    {
      id: 'assignedKpis',
      label: 'Assigned KPIs',
      minWidth: 120,
      align: 'center',
      render: (value) => (
        <Chip
          label={value.length}
          color={value.length > 0 ? 'primary' : 'default'}
          size="small"
        />
      ),
    },
    {
      id: 'createdDate',
      label: 'Created',
      sortable: true,
      minWidth: 120,
      render: (value) => format(new Date(value), 'MMM dd, yyyy'),
    },
    {
      id: 'modifiedDate',
      label: 'Last Modified',
      sortable: true,
      minWidth: 140,
      render: (value) => format(new Date(value), 'MMM dd, HH:mm'),
    },
  ];

  if (isLoading) {
    return <LoadingSpinner message="Loading contacts..." />;
  }

  return (
    <Box>
      <PageHeader
        title="Contact Management"
        subtitle={`Manage notification contacts and their KPI assignments (${filteredContacts.length} total)`}
        primaryAction={{
          label: 'Add Contact',
          icon: <AddIcon />,
          onClick: () => navigate('/contacts/create'),
        }}
        actions={[
          {
            label: 'Bulk Assign',
            icon: <AssignIcon />,
            onClick: () => {
              // TODO: Implement bulk assignment
              toast.info('Bulk assignment feature coming soon');
            },
            disabled: selectedRows.length === 0,
          },
        ]}
        onRefresh={refetch}
        refreshing={isLoading}
      />

      <FilterPanel
        fields={[
          {
            name: 'isActive',
            label: 'Status',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'true', label: 'Active' },
              { value: 'false', label: 'Inactive' },
            ],
          },
        ]}
        onFilterChange={(newFilters) => setFilters({ ...filters, ...newFilters })}
        onClear={() => setFilters({ isActive: '', search: '' })}
        onSearch={(searchTerm) => setFilters({ ...filters, search: searchTerm })}
        searchPlaceholder="Search contacts by name, email, or phone..."
        defaultExpanded={false}
      />

      <DataTable
        columns={columns}
        data={filteredContacts}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        defaultActions={{
          view: (contact) => navigate(`/contacts/${contact.contactId}`),
          edit: (contact) => navigate(`/contacts/${contact.contactId}/edit`),
          delete: handleDelete,
        }}
        actions={[
          {
            label: 'Assign KPIs',
            icon: <AssignIcon />,
            onClick: (contact) => {
              // TODO: Implement KPI assignment
              toast.info('KPI assignment feature coming soon');
            },
            color: 'primary',
          },
        ]}
        emptyMessage="No contacts found. Add your first contact to get started."
        rowKey="contactId"
      />
    </Box>
  );
};

export default ContactList;

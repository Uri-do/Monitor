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
import { useNavigate } from 'react-router-dom';
import { ContactDto } from '@/types/api';
import { useContacts } from '@/hooks/useContacts';
import { useDeleteContact } from '@/hooks/mutations';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  DataTable,
  DataTableColumn,
  PageHeader,
  FilterPanel,
  StatusChip,
  LoadingSpinner,
} from '@/components';

const ContactList: React.FC = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState({
    isActive: '',
    search: '',
  });
  const [selectedRows, setSelectedRows] = useState<ContactDto[]>([]);

  // Use our enhanced hooks
  const {
    data: contacts = [],
    isLoading,
    refetch,
  } = useContacts({
    isActive: filters.isActive ? filters.isActive === 'true' : undefined,
    search: filters.search || undefined,
  });

  const deleteContactMutation = useDeleteContact();

  const handleDelete = (contact: ContactDto) => {
    if (window.confirm(`Are you sure you want to delete "${contact.name}"?`)) {
      deleteContactMutation.mutate(contact.contactID);
    }
  };

  // Filter data based on search
  const filteredContacts = useMemo(() => {
    if (!filters.search) return contacts;

    const searchLower = filters.search.toLowerCase();
    return contacts.filter(
      contact =>
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
      width: 180,
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
      width: 100,
      render: value => <StatusChip status={value ? 'active' : 'inactive'} />,
    },
    {
      id: 'assignedIndicators',
      label: 'Assigned Indicators',
      width: 120,
      align: 'center',
      render: value => (
        <Chip label={value.length} color={value.length > 0 ? 'primary' : 'default'} size="small" />
      ),
    },
    {
      id: 'createdDate',
      label: 'Created',
      sortable: true,
      width: 120,
      render: value => format(new Date(value), 'MMM dd, yyyy'),
    },
    {
      id: 'modifiedDate',
      label: 'Last Modified',
      sortable: true,
      width: 140,
      render: value => format(new Date(value), 'MMM dd, HH:mm'),
    },
  ];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title="Contact Management"
        subtitle={`Manage notification contacts and their Indicator assignments (${filteredContacts.length} total)`}
        primaryAction={{
          label: 'Add Contact',
          icon: <AddIcon />,
          onClick: () => navigate('/contacts/create'),
        }}
        secondaryActions={[
          {
            label: 'Bulk Assign',
            icon: <AssignIcon />,
            onClick: () => {
              if (selectedRows.length === 0) {
                toast.error('Please select contacts to assign');
                return;
              }
              toast(
                `Bulk assignment for ${selectedRows.length} contacts - Feature in development`,
                { icon: 'ℹ️' }
              );
            },
            gradient: 'info',
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
        onFilterChange={newFilters => setFilters({ ...filters, ...newFilters })}
        onClear={() => setFilters({ isActive: '', search: '' })}
        onSearch={searchTerm => setFilters({ ...filters, search: searchTerm })}
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
          view: contact => navigate(`/contacts/${contact.contactID}`),
          edit: contact => navigate(`/contacts/${contact.contactID}/edit`),
          delete: handleDelete,
        }}
        actions={[
          {
            label: 'Assign Indicators',
            icon: <AssignIcon />,
            onClick: contact => {
              // TODO: Implement Indicator assignment
              toast('Indicator assignment feature coming soon', { icon: 'ℹ️' });
            },
          },
        ]}
        emptyMessage="No contacts found. Add your first contact to get started."
        rowKey="contactID"
      />
    </Box>
  );
};

export default ContactList;

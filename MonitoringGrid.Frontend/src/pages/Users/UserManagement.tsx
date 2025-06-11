import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  Tooltip,
  Alert,
  Switch,
  FormControlLabel,
  Menu,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Edit,
  Delete,
  Security,
  PersonAdd,
  Block,
  CheckCircle,
  MoreVert,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { UltimatePageHeader } from '@/components/UltimateEnterprise';
import {
  userService,
  CreateUserRequest,
  UpdateUserRequest,
  BulkUserOperation,
} from '../../services/userService';
import { User, Role } from '../../types/auth';
import { useUsers, useRoles } from '@/hooks/useUsers';
import { useCreateUser, useUpdateUser, useDeleteUser } from '@/hooks/mutations';
import toast from 'react-hot-toast';

const userSchema = yup.object({
  username: yup
    .string()
    .required('Username is required')
    .min(3, 'Username must be at least 3 characters'),
  email: yup.string().email('Invalid email').required('Email is required'),
  displayName: yup.string().required('Display name is required'),
  firstName: yup.string(),
  lastName: yup.string(),
  department: yup.string(),
  title: yup.string(),
  roles: yup.array().of(yup.string().required()).required().min(1, 'At least one role is required'),
  isActive: yup.boolean().required(),
});

interface UserFormData {
  username: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  department?: string;
  title?: string;
  roles: string[];
  isActive: boolean;
}

export const UserManagement: React.FC = () => {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [bulkMenuAnchor, setBulkMenuAnchor] = useState<null | HTMLElement>(null);

  // Use our enhanced hooks
  const { data: users = [], isLoading: usersLoading, refetch: refetchUsers } = useUsers();
  const { data: roles = [], isLoading: rolesLoading } = useRoles();
  const createUserMutation = useCreateUser();
  const updateUserMutation = useUpdateUser();
  const deleteUserMutation = useDeleteUser();

  const loading = usersLoading || rolesLoading;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UserFormData>({
    resolver: yupResolver(userSchema),
    defaultValues: {
      isActive: true,
      roles: [],
    },
  });

  // Data loading is now handled by our hooks automatically

  const handleCreateUser = () => {
    setEditingUser(null);
    reset({
      username: '',
      email: '',
      displayName: '',
      firstName: '',
      lastName: '',
      department: '',
      title: '',
      roles: [],
      isActive: true,
    });
    setDialogOpen(true);
  };

  const handleEditUser = (user: User) => {
    setEditingUser(user);
    reset({
      username: user.username,
      email: user.email,
      displayName: user.displayName,
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      department: user.department || '',
      title: user.title || '',
      roles: user.roles.map(role => role.roleId),
      isActive: user.isActive,
    });
    setDialogOpen(true);
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return;
    deleteUserMutation.mutate(userId);
  };

  const handleToggleUserStatus = async (userId: string, isActive: boolean) => {
    try {
      if (isActive) {
        await userService.activateUser(userId);
        toast.success('User activated successfully');
        setSuccess('User activated successfully');
      } else {
        await userService.deactivateUser(userId);
        toast.success('User deactivated successfully');
        setSuccess('User deactivated successfully');
      }
      refetchUsers(); // Use our hook's refetch instead of loadData
    } catch (err) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : `Failed to ${isActive ? 'activate' : 'deactivate'} user`;
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error toggling user status:', err);
    }
  };

  const onSubmit = async (data: UserFormData) => {
    if (editingUser) {
      const updateRequest: UpdateUserRequest = {
        id: editingUser.userId,
        displayName: data.displayName,
        firstName: data.firstName,
        lastName: data.lastName,
        department: data.department,
        title: data.title,
        roleIds: data.roles,
        isActive: data.isActive,
      };
      updateUserMutation.mutate(updateRequest, {
        onSuccess: () => {
          setDialogOpen(false);
          setSuccess('User updated successfully');
        },
        onError: (err: any) => {
          setError(err.message || 'Failed to update user');
        },
      });
    } else {
      const createRequest: CreateUserRequest = {
        username: data.username,
        email: data.email,
        displayName: data.displayName,
        firstName: data.firstName,
        lastName: data.lastName,
        department: data.department,
        title: data.title,
        password: 'TempPassword123!', // Temporary password - should be changed on first login
        roleIds: data.roles,
        isActive: data.isActive,
        emailConfirmed: false,
      };
      createUserMutation.mutate(createRequest, {
        onSuccess: () => {
          setDialogOpen(false);
          setSuccess('User created successfully');
        },
        onError: (err: any) => {
          setError(err.message || 'Failed to create user');
        },
      });
    }
  };

  const handleBulkOperation = async (operation: string) => {
    if (selectedUsers.length === 0) {
      toast.error('Please select users first');
      return;
    }

    const confirmMessage = `Are you sure you want to ${operation} ${selectedUsers.length} user(s)?`;
    if (!confirm(confirmMessage)) return;

    try {
      const bulkOperation: BulkUserOperation = {
        userIds: selectedUsers,
        operation: operation as any,
      };

      await userService.bulkOperation(bulkOperation);
      toast.success(`${operation} operation completed successfully`);
      setSelectedUsers([]);
      refetchUsers(); // Use our hook's refetch instead of loadData
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Failed to ${operation} users`;
      toast.error(errorMessage);
      console.error('Error in bulk operation:', err);
    }
    setBulkMenuAnchor(null);
  };

  const columns: GridColDef[] = [
    {
      field: 'username',
      headerName: 'Username',
      width: 150,
      renderCell: params => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="body2">{params.value}</Typography>
          {!params.row.isActive && (
            <Tooltip title="Inactive User">
              <Block color="error" fontSize="small" />
            </Tooltip>
          )}
        </Box>
      ),
    },
    {
      field: 'displayName',
      headerName: 'Display Name',
      width: 200,
    },
    {
      field: 'email',
      headerName: 'Email',
      width: 250,
    },
    {
      field: 'department',
      headerName: 'Department',
      width: 150,
    },
    {
      field: 'roles',
      headerName: 'Roles',
      width: 200,
      renderCell: params => (
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
          {params.value.map((role: Role) => (
            <Chip
              key={role.roleId}
              label={role.name}
              size="small"
              color={role.name === 'Admin' ? 'error' : 'primary'}
              variant="outlined"
            />
          ))}
        </Box>
      ),
    },
    {
      field: 'lastLogin',
      headerName: 'Last Login',
      width: 180,
      renderCell: params => (
        <Typography variant="body2">
          {params.value ? new Date(params.value).toLocaleString() : 'Never'}
        </Typography>
      ),
    },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 100,
      renderCell: params => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'error'}
          size="small"
        />
      ),
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 150,
      getActions: params => [
        <GridActionsCellItem
          icon={<Edit />}
          label="Edit"
          onClick={() => handleEditUser(params.row)}
        />,
        <GridActionsCellItem
          icon={params.row.isActive ? <Block /> : <CheckCircle />}
          label={params.row.isActive ? 'Deactivate' : 'Activate'}
          onClick={() => handleToggleUserStatus(params.row.userId, !params.row.isActive)}
        />,
        <GridActionsCellItem
          icon={<Delete />}
          label="Delete"
          onClick={() => handleDeleteUser(params.row.userId)}
          showInMenu
        />,
      ],
    },
  ];

  return (
    <Box>
      <UltimatePageHeader
        title="User Management"
        subtitle="Manage system users and their permissions"
        primaryAction={{
          label: 'Add User',
          icon: <PersonAdd />,
          onClick: handleCreateUser,
        }}
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      {selectedUsers.length > 0 && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Typography variant="h6">{selectedUsers.length} user(s) selected</Typography>
              <Button
                variant="outlined"
                startIcon={<MoreVert />}
                onClick={e => setBulkMenuAnchor(e.currentTarget)}
              >
                Bulk Actions
              </Button>
            </Box>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent>
          <DataGrid
            rows={users}
            columns={columns}
            loading={loading}
            getRowId={row => row.userId}
            pageSizeOptions={[10, 25, 50]}
            initialState={{
              pagination: { paginationModel: { pageSize: 25 } },
            }}
            checkboxSelection
            rowSelectionModel={selectedUsers}
            onRowSelectionModelChange={newSelection => {
              setSelectedUsers(newSelection as string[]);
            }}
            sx={{ height: 600 }}
          />
        </CardContent>
      </Card>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="md" fullWidth>
        <form onSubmit={handleSubmit(onSubmit as any)}>
          <DialogTitle>{editingUser ? 'Edit User' : 'Create New User'}</DialogTitle>
          <DialogContent>
            <Box sx={{ display: 'grid', gap: 2, mt: 1 }}>
              <Controller
                name="username"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Username"
                    error={!!errors.username}
                    helperText={errors.username?.message}
                    disabled={!!editingUser}
                    fullWidth
                  />
                )}
              />

              <Controller
                name="email"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Email"
                    type="email"
                    error={!!errors.email}
                    helperText={errors.email?.message}
                    fullWidth
                  />
                )}
              />

              <Controller
                name="displayName"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Display Name"
                    error={!!errors.displayName}
                    helperText={errors.displayName?.message}
                    fullWidth
                  />
                )}
              />

              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <Controller
                  name="firstName"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="First Name"
                      error={!!errors.firstName}
                      helperText={errors.firstName?.message}
                    />
                  )}
                />

                <Controller
                  name="lastName"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Last Name"
                      error={!!errors.lastName}
                      helperText={errors.lastName?.message}
                    />
                  )}
                />
              </Box>

              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <Controller
                  name="department"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Department"
                      error={!!errors.department}
                      helperText={errors.department?.message}
                    />
                  )}
                />

                <Controller
                  name="title"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Job Title"
                      error={!!errors.title}
                      helperText={errors.title?.message}
                    />
                  )}
                />
              </Box>

              <Controller
                name="roles"
                control={control}
                render={({ field }) => (
                  <FormControl error={!!errors.roles}>
                    <InputLabel>Roles</InputLabel>
                    <Select
                      {...field}
                      multiple
                      label="Roles"
                      renderValue={selected => (
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                          {(selected as string[]).map(value => (
                            <Chip key={value} label={value} size="small" />
                          ))}
                        </Box>
                      )}
                    >
                      {roles.map(role => (
                        <MenuItem key={role.roleId} value={role.name}>
                          {role.name}
                        </MenuItem>
                      ))}
                    </Select>
                    {errors.roles && (
                      <Typography variant="caption" color="error">
                        {errors.roles.message}
                      </Typography>
                    )}
                  </FormControl>
                )}
              />

              <Controller
                name="isActive"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    control={<Switch {...field} checked={field.value} />}
                    label="Active User"
                  />
                )}
              />
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">
              {editingUser ? 'Update' : 'Create'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Bulk Operations Menu */}
      <Menu
        anchorEl={bulkMenuAnchor}
        open={Boolean(bulkMenuAnchor)}
        onClose={() => setBulkMenuAnchor(null)}
      >
        <MenuItem onClick={() => handleBulkOperation('activate')}>
          <ListItemIcon>
            <CheckCircle fontSize="small" />
          </ListItemIcon>
          <ListItemText>Activate Users</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => handleBulkOperation('deactivate')}>
          <ListItemIcon>
            <Block fontSize="small" />
          </ListItemIcon>
          <ListItemText>Deactivate Users</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => handleBulkOperation('delete')}>
          <ListItemIcon>
            <Delete fontSize="small" />
          </ListItemIcon>
          <ListItemText>Delete Users</ListItemText>
        </MenuItem>
      </Menu>
    </Box>
  );
};

export default UserManagement;

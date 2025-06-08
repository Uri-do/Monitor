import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,

  Chip,
  Alert,
  Switch,
  FormControlLabel,
  Typography,

  FormGroup,
  Checkbox,
  Accordion,
  AccordionSummary,
  AccordionDetails
} from '@mui/material';
import {
  Add,
  Edit,
  Delete,
  Security,
  ExpandMore,
  Group
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { Role, Permission } from '../../types/auth';
import { roleService, CreateRoleRequest, UpdateRoleRequest } from '../../services/roleService';
import { PageHeader } from '../../components/Common';
import toast from 'react-hot-toast';

const roleSchema = yup.object({
  name: yup.string().required('Role name is required').min(2, 'Role name must be at least 2 characters'),
  description: yup.string().required('Description is required'),
  permissionIds: yup.array().of(yup.string().required()).required().min(1, 'At least one permission is required'),
  isActive: yup.boolean().required()
});

interface RoleFormData {
  name: string;
  description: string;
  permissionIds: string[];
  isActive: boolean;
}

const RoleManagement: React.FC = () => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors }
  } = useForm<RoleFormData>({
    resolver: yupResolver(roleSchema),
    defaultValues: {
      isActive: true,
      permissionIds: []
    }
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [rolesData, permissionsData] = await Promise.all([
        roleService.getRoles(),
        roleService.getAllPermissions()
      ]);
      setRoles(rolesData);
      setPermissions(permissionsData);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load role data';
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRole = () => {
    setEditingRole(null);
    reset({
      name: '',
      description: '',
      permissionIds: [],
      isActive: true
    });
    setDialogOpen(true);
  };

  const handleEditRole = (role: Role) => {
    setEditingRole(role);
    reset({
      name: role.name,
      description: role.description,
      permissionIds: role.permissions.map(p => p.permissionId),
      isActive: role.isActive
    });
    setDialogOpen(true);
  };

  const handleDeleteRole = async (roleId: string) => {
    if (!confirm('Are you sure you want to delete this role?')) return;

    try {
      await roleService.deleteRole(roleId);
      toast.success('Role deleted successfully');
      setSuccess('Role deleted successfully');
      loadData();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete role';
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error deleting role:', err);
    }
  };

  const onSubmit = async (data: RoleFormData) => {
    try {
      if (editingRole) {
        const updateRequest: UpdateRoleRequest = {
          name: data.name,
          description: data.description,
          permissionIds: data.permissionIds,
          isActive: data.isActive
        };
        await roleService.updateRole(editingRole.roleId, updateRequest);
        toast.success('Role updated successfully');
        setSuccess('Role updated successfully');
      } else {
        const createRequest: CreateRoleRequest = {
          name: data.name,
          description: data.description,
          permissionIds: data.permissionIds,
          isActive: data.isActive
        };
        await roleService.createRole(createRequest);
        toast.success('Role created successfully');
        setSuccess('Role created successfully');
      }
      setDialogOpen(false);
      loadData();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : `Failed to ${editingRole ? 'update' : 'create'} role`;
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error saving role:', err);
    }
  };

  const groupPermissionsByResource = (permissions: Permission[]) => {
    const grouped = permissions.reduce((acc, permission) => {
      if (!acc[permission.resource]) {
        acc[permission.resource] = [];
      }
      acc[permission.resource].push(permission);
      return acc;
    }, {} as Record<string, Permission[]>);
    return grouped;
  };

  const columns: GridColDef[] = [
    {
      field: 'name',
      headerName: 'Role Name',
      width: 200,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Security color={params.row.isSystemRole ? 'error' : 'primary'} />
          <Typography variant="body2">{params.value}</Typography>
        </Box>
      )
    },
    {
      field: 'description',
      headerName: 'Description',
      width: 300,
      flex: 1
    },
    {
      field: 'permissions',
      headerName: 'Permissions',
      width: 150,
      renderCell: (params) => (
        <Chip
          label={`${params.value.length} permissions`}
          size="small"
          color="primary"
          variant="outlined"
        />
      )
    },
    {
      field: 'isSystemRole',
      headerName: 'Type',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'System' : 'Custom'}
          color={params.value ? 'error' : 'default'}
          size="small"
        />
      )
    },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'error'}
          size="small"
        />
      )
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 120,
      getActions: (params) => [
        <GridActionsCellItem
          icon={<Edit />}
          label="Edit"
          onClick={() => handleEditRole(params.row)}
          disabled={params.row.isSystemRole}
        />,
        <GridActionsCellItem
          icon={<Delete />}
          label="Delete"
          onClick={() => handleDeleteRole(params.row.roleId)}
          disabled={params.row.isSystemRole}
          showInMenu
        />
      ]
    }
  ];

  const groupedPermissions = groupPermissionsByResource(permissions);

  return (
    <Box>
      <PageHeader
        title="Role Management"
        subtitle="Manage system roles and permissions"
        icon={<Group />}
        actions={[
          {
            label: 'Add Role',
            icon: <Add />,
            onClick: handleCreateRole,
            variant: 'contained' as const
          }
        ]}
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

      <Card>
        <CardContent>
          <DataGrid
            rows={roles}
            columns={columns}
            loading={loading}
            getRowId={(row) => row.roleId}
            pageSizeOptions={[10, 25, 50]}
            initialState={{
              pagination: { paginationModel: { pageSize: 25 } }
            }}
            disableRowSelectionOnClick
            sx={{ height: 600 }}
          />
        </CardContent>
      </Card>

      {/* Role Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <form onSubmit={handleSubmit(onSubmit as any)}>
          <DialogTitle>
            {editingRole ? 'Edit Role' : 'Create New Role'}
          </DialogTitle>
          <DialogContent>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
              <Controller
                name="name"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Role Name"
                    error={!!errors.name}
                    helperText={errors.name?.message}
                    fullWidth
                  />
                )}
              />

              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Description"
                    multiline
                    rows={3}
                    error={!!errors.description}
                    helperText={errors.description?.message}
                    fullWidth
                  />
                )}
              />

              <Controller
                name="isActive"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    control={<Switch {...field} checked={field.value} />}
                    label="Active Role"
                  />
                )}
              />

              <Typography variant="h6" sx={{ mt: 2 }}>
                Permissions
              </Typography>

              <Controller
                name="permissionIds"
                control={control}
                render={({ field }) => (
                  <Box>
                    {Object.entries(groupedPermissions).map(([resource, resourcePermissions]) => (
                      <Accordion key={resource}>
                        <AccordionSummary expandIcon={<ExpandMore />}>
                          <Typography variant="subtitle1">{resource}</Typography>
                        </AccordionSummary>
                        <AccordionDetails>
                          <FormGroup>
                            {resourcePermissions.map((permission) => (
                              <FormControlLabel
                                key={permission.permissionId}
                                control={
                                  <Checkbox
                                    checked={field.value.includes(permission.permissionId)}
                                    onChange={(e) => {
                                      if (e.target.checked) {
                                        field.onChange([...field.value, permission.permissionId]);
                                      } else {
                                        field.onChange(field.value.filter(id => id !== permission.permissionId));
                                      }
                                    }}
                                  />
                                }
                                label={
                                  <Box>
                                    <Typography variant="body2">{permission.name}</Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      {permission.description}
                                    </Typography>
                                  </Box>
                                }
                              />
                            ))}
                          </FormGroup>
                        </AccordionDetails>
                      </Accordion>
                    ))}
                    {errors.permissionIds && (
                      <Typography variant="caption" color="error">
                        {errors.permissionIds.message}
                      </Typography>
                    )}
                  </Box>
                )}
              />
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">
              {editingRole ? 'Update' : 'Create'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
};

export default RoleManagement;

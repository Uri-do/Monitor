import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Chip,
  LinearProgress,
  IconButton,
  Tooltip,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import {
  People,
  Security,
  VpnKey,
  TrendingUp,
  Warning,
  CheckCircle,
  Block,
  Refresh,
  AdminPanelSettings,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { PageHeader } from '../../components/Common';
import { userService } from '../../services/userService';
import { roleService } from '../../services/roleService';
import { User, Role } from '../../types/auth';

interface SystemStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  totalRoles: number;
  systemRoles: number;
  customRoles: number;
  unverifiedUsers: number;
  recentLogins: number;
}

const AdminDashboard: React.FC = () => {
  const [stats, setStats] = useState<SystemStats>({
    totalUsers: 0,
    activeUsers: 0,
    inactiveUsers: 0,
    totalRoles: 0,
    systemRoles: 0,
    customRoles: 0,
    unverifiedUsers: 0,
    recentLogins: 0,
  });

  const {
    data: users = [],
    isLoading: usersLoading,
    refetch: refetchUsers,
  } = useQuery({
    queryKey: ['admin-users'],
    queryFn: () => userService.getUsers(),
  });

  const {
    data: roles = [],
    isLoading: rolesLoading,
    refetch: refetchRoles,
  } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => roleService.getRoles(),
  });

  useEffect(() => {
    if (users.length > 0 && roles.length > 0) {
      calculateStats(users, roles);
    }
  }, [users, roles]);

  const calculateStats = (users: User[], roles: Role[]) => {
    const activeUsers = users.filter(u => u.isActive).length;
    const unverifiedUsers = users.filter(u => !u.emailConfirmed).length;
    const systemRoles = roles.filter(r => r.isSystemRole).length;
    const recentLogins = users.filter(u => {
      if (!u.lastLogin) return false;
      const lastLogin = new Date(u.lastLogin);
      const dayAgo = new Date();
      dayAgo.setDate(dayAgo.getDate() - 1);
      return lastLogin > dayAgo;
    }).length;

    setStats({
      totalUsers: users.length,
      activeUsers,
      inactiveUsers: users.length - activeUsers,
      totalRoles: roles.length,
      systemRoles,
      customRoles: roles.length - systemRoles,
      unverifiedUsers,
      recentLogins,
    });
  };

  const handleRefresh = () => {
    refetchUsers();
    refetchRoles();
  };

  const getInitials = (displayName: string) => {
    return displayName
      .split(' ')
      .map(name => name.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const recentUsers = users
    .sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime())
    .slice(0, 5);

  const inactiveUsers = users.filter(u => !u.isActive).slice(0, 5);

  return (
    <Box>
      <PageHeader
        title="Admin Dashboard"
        subtitle="System overview and user management"
        icon={<AdminPanelSettings />}
        actions={[
          {
            label: 'Refresh',
            icon: <Refresh />,
            onClick: handleRefresh,
            variant: 'outlined' as const,
          },
        ]}
      />

      {/* Statistics Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                  <People />
                </Avatar>
                <Box>
                  <Typography variant="h4">{stats.totalUsers}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Users
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="success.main">
                  {stats.activeUsers} active
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={stats.totalUsers > 0 ? (stats.activeUsers / stats.totalUsers) * 100 : 0}
                  sx={{ mt: 1 }}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Avatar sx={{ bgcolor: 'secondary.main', mr: 2 }}>
                  <Security />
                </Avatar>
                <Box>
                  <Typography variant="h4">{stats.totalRoles}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Roles
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="info.main">
                  {stats.systemRoles} system, {stats.customRoles} custom
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Avatar sx={{ bgcolor: 'warning.main', mr: 2 }}>
                  <Warning />
                </Avatar>
                <Box>
                  <Typography variant="h4">{stats.unverifiedUsers}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Unverified Users
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="warning.main">
                  Require email verification
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Avatar sx={{ bgcolor: 'success.main', mr: 2 }}>
                  <TrendingUp />
                </Avatar>
                <Box>
                  <Typography variant="h4">{stats.recentLogins}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Recent Logins
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="success.main">
                  Last 24 hours
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        {/* Recent Users */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Users
              </Typography>
              {usersLoading ? (
                <LinearProgress />
              ) : (
                <List>
                  {recentUsers.map(user => (
                    <ListItem key={user.userId}>
                      <ListItemAvatar>
                        <Avatar sx={{ bgcolor: 'primary.main' }}>
                          {getInitials(user.displayName)}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={user.displayName}
                        secondary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                              {user.email}
                            </Typography>
                            {user.isActive ? (
                              <CheckCircle color="success" fontSize="small" />
                            ) : (
                              <Block color="error" fontSize="small" />
                            )}
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Inactive Users */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Inactive Users
              </Typography>
              {usersLoading ? (
                <LinearProgress />
              ) : (
                <List>
                  {inactiveUsers.map(user => (
                    <ListItem key={user.userId}>
                      <ListItemAvatar>
                        <Avatar sx={{ bgcolor: 'error.main' }}>
                          {getInitials(user.displayName)}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={user.displayName}
                        secondary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                              {user.email}
                            </Typography>
                            <Chip label="Inactive" color="error" size="small" />
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Roles Overview */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Roles Overview
              </Typography>
              {rolesLoading ? (
                <LinearProgress />
              ) : (
                <TableContainer component={Paper} variant="outlined">
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Role Name</TableCell>
                        <TableCell>Description</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Permissions</TableCell>
                        <TableCell>Status</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {roles.map(role => (
                        <TableRow key={role.roleId}>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <VpnKey color={role.isSystemRole ? 'error' : 'primary'} />
                              {role.name}
                            </Box>
                          </TableCell>
                          <TableCell>{role.description}</TableCell>
                          <TableCell>
                            <Chip
                              label={role.isSystemRole ? 'System' : 'Custom'}
                              color={role.isSystemRole ? 'error' : 'default'}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={`${role.permissions.length} permissions`}
                              size="small"
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={role.isActive ? 'Active' : 'Inactive'}
                              color={role.isActive ? 'success' : 'error'}
                              size="small"
                            />
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default AdminDashboard;

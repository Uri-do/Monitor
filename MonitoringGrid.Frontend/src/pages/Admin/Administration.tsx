import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Tabs,
  Tab,
  Paper,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemButton,
  Divider,
  Chip,
  Alert,
} from '@mui/material';
import {
  Security as SecurityIcon,
  People as PeopleIcon,
  AdminPanelSettings as AdminIcon,
  Settings as SettingsIcon,
  VpnKey as ApiKeyIcon,
  History as AuditIcon,
  Shield as ShieldIcon,
  Lock as LockIcon,
  Key as KeyIcon,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { PageHeader } from '@/components/UI';
import { SecuritySettings } from '../Settings/SecuritySettings';
import ApiKeyManagement from './ApiKeyManagement';
import { useAuth } from '@/hooks/useAuth';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`admin-tabpanel-${index}`}
      aria-labelledby={`admin-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

interface AdminSection {
  id: string;
  title: string;
  description: string;
  icon: React.ReactElement;
  path: string;
  requiredPermissions: string[];
  status?: 'active' | 'warning' | 'error';
  count?: number;
}

const Administration: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();

  const hasPermission = (permissions: string[]) => {
    // In demo mode or if no user, allow access to all sections
    if (!user) return true;
    if (!user.permissions || user.permissions.length === 0) return true;
    return permissions.some(permission => user.permissions.includes(permission));
  };

  const adminSections: AdminSection[] = [
    {
      id: 'users',
      title: 'User Management',
      description: 'Manage user accounts, roles, and permissions',
      icon: <PeopleIcon />,
      path: '/admin/users',
      requiredPermissions: ['User:Read'],
      status: 'active',
      count: 12, // This would come from API
    },
    {
      id: 'roles',
      title: 'Role Management',
      description: 'Configure roles and permission assignments',
      icon: <AdminIcon />,
      path: '/admin/roles',
      requiredPermissions: ['Role:Read'],
      status: 'active',
      count: 4,
    },
    {
      id: 'security',
      title: 'Security Settings',
      description: 'Password policies, session settings, and authentication',
      icon: <SecurityIcon />,
      path: '/admin/security',
      requiredPermissions: ['System:Admin'],
      status: 'active',
    },
    {
      id: 'api-keys',
      title: 'API Key Management',
      description: 'Create and manage API keys for system integration',
      icon: <ApiKeyIcon />,
      path: '/admin/api-keys',
      requiredPermissions: ['System:Admin'],
      status: 'active',
      count: 3,
    },
    {
      id: 'audit',
      title: 'Security Audit Log',
      description: 'View security events and authentication logs',
      icon: <AuditIcon />,
      path: '/admin/audit',
      requiredPermissions: ['System:Admin'],
      status: 'active',
    },
    {
      id: 'system',
      title: 'System Settings',
      description: 'Configure system-wide settings and parameters',
      icon: <SettingsIcon />,
      path: '/admin/settings',
      requiredPermissions: ['System:Admin'],
      status: 'active',
    },
  ];

  const filteredSections = adminSections.filter(section =>
    hasPermission(section.requiredPermissions)
  );

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleSectionClick = (path: string) => {
    navigate(path);
  };

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'warning':
        return 'warning';
      case 'error':
        return 'error';
      default:
        return 'secondary';
    }
  };

  // Check which sub-page we're on
  const isSecurityPage = location.pathname === '/admin/security';
  const isApiKeysPage = location.pathname === '/admin/api-keys';
  const isAuditPage = location.pathname === '/admin/audit';

  if (isSecurityPage) {
    return (
      <Box>
        <PageHeader
          title="Security Settings"
          subtitle="Configure authentication, authorization, and security policies"
        />
        <SecuritySettings />
      </Box>
    );
  }

  if (isApiKeysPage) {
    return <ApiKeyManagement />;
  }

  if (isAuditPage) {
    return (
      <Box>
        <PageHeader
          title="Security Audit Log"
          subtitle="View security events and authentication logs"
        />
        <Alert severity="info">
          Security audit log functionality will be implemented in the next phase.
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Administration"
        subtitle="Manage users, security, and system configuration"
      />

      <Paper sx={{ width: '100%' }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="administration tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<ShieldIcon />} label="Security & Access" />
          <Tab icon={<SettingsIcon />} label="System Management" />
          <Tab icon={<AuditIcon />} label="Monitoring & Logs" />
        </Tabs>

        {/* Security & Access Tab */}
        <TabPanel value={activeTab} index={0}>
          <Grid container spacing={3}>
            {filteredSections
              .filter(section => ['users', 'roles', 'security', 'api-keys'].includes(section.id))
              .map(section => (
                <Grid item xs={12} md={6} key={section.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <ListItemButton
                        onClick={() => handleSectionClick(section.path)}
                        sx={{
                          borderRadius: 1,
                          '&:hover': {
                            backgroundColor: 'action.hover',
                          },
                        }}
                      >
                        <ListItemIcon>{section.icon}</ListItemIcon>
                        <ListItemText
                          primary={
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="h6">{section.title}</Typography>
                              {section.count && (
                                <Chip label={section.count} size="small" color="primary" />
                              )}
                              <Chip
                                label={section.status || 'active'}
                                size="small"
                                color={getStatusColor(section.status)}
                              />
                            </Box>
                          }
                          secondary={section.description}
                        />
                      </ListItemButton>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            {filteredSections.filter(section => ['users', 'roles', 'security', 'api-keys'].includes(section.id)).length === 0 && (
              <Grid item xs={12}>
                <Alert severity="info">
                  Security and access management features are available with proper authentication.
                  Please log in with administrative privileges to access user management, role configuration,
                  security settings, and API key management.
                </Alert>
              </Grid>
            )}
          </Grid>
        </TabPanel>

        {/* System Management Tab */}
        <TabPanel value={activeTab} index={1}>
          <Grid container spacing={3}>
            {filteredSections
              .filter(section => ['system'].includes(section.id))
              .map(section => (
                <Grid item xs={12} md={6} key={section.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <ListItemButton
                        onClick={() => handleSectionClick(section.path)}
                        sx={{
                          borderRadius: 1,
                          '&:hover': {
                            backgroundColor: 'action.hover',
                          },
                        }}
                      >
                        <ListItemIcon>{section.icon}</ListItemIcon>
                        <ListItemText
                          primary={
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="h6">{section.title}</Typography>
                              {section.count && (
                                <Chip label={section.count} size="small" color="primary" />
                              )}
                              <Chip
                                label={section.status || 'active'}
                                size="small"
                                color={getStatusColor(section.status)}
                              />
                            </Box>
                          }
                          secondary={section.description}
                        />
                      </ListItemButton>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            {filteredSections.filter(section => ['system'].includes(section.id)).length === 0 && (
              <Grid item xs={12}>
                <Alert severity="info">
                  System management features include configuration of system-wide settings,
                  performance parameters, and operational controls. Administrative access required.
                </Alert>
              </Grid>
            )}
          </Grid>
        </TabPanel>

        {/* Monitoring & Logs Tab */}
        <TabPanel value={activeTab} index={2}>
          <Grid container spacing={3}>
            {filteredSections
              .filter(section => ['audit'].includes(section.id))
              .map(section => (
                <Grid item xs={12} md={6} key={section.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <ListItemButton
                        onClick={() => handleSectionClick(section.path)}
                        sx={{
                          borderRadius: 1,
                          '&:hover': {
                            backgroundColor: 'action.hover',
                          },
                        }}
                      >
                        <ListItemIcon>{section.icon}</ListItemIcon>
                        <ListItemText
                          primary={
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="h6">{section.title}</Typography>
                              {section.count && (
                                <Chip label={section.count} size="small" color="primary" />
                              )}
                              <Chip
                                label={section.status || 'active'}
                                size="small"
                                color={getStatusColor(section.status)}
                              />
                            </Box>
                          }
                          secondary={section.description}
                        />
                      </ListItemButton>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
          </Grid>
        </TabPanel>
      </Paper>
    </Box>
  );
};

export default Administration;

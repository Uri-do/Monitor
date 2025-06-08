import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  AppBar,
  Box,
  CssBaseline,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  useTheme,
  useMediaQuery,
  Badge,
  Chip,
  Tooltip,
  Divider,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Assessment as KpiIcon,
  People as ContactIcon,
  Notifications as AlertIcon,
  Analytics as AnalyticsIcon,
  Settings as SettingsIcon,
  Refresh as RefreshIcon,
  AdminPanelSettings,
  Group,
  Security,
  History as HistoryIcon,
  ChevronLeft as ChevronLeftIcon,
  ChevronRight as ChevronRightIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { systemApi, alertApi } from '@/services/api';
import UserMenu from '@/components/Auth/UserMenu';
import { useAuth } from '@/hooks/useAuth';
import mgLogo from '@/assets/images/mglogo.png';

const drawerWidth = 240;
const collapsedDrawerWidth = 64;

interface LayoutProps {
  children: React.ReactNode;
}

interface NavItem {
  text: string;
  icon: React.ReactElement;
  path: string;
  badge?: number;
  requiredPermissions?: string[];
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();

  // Get system health
  const { data: healthData } = useQuery({
    queryKey: ['health'],
    queryFn: systemApi.getHealth,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Get alert dashboard for badge counts
  const { data: alertDashboard } = useQuery({
    queryKey: ['alert-dashboard'],
    queryFn: alertApi.getDashboard,
    refetchInterval: 60000, // Refresh every minute
  });

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleSidebarToggle = () => {
    setSidebarCollapsed(!sidebarCollapsed);
  };

  const handleNavigation = (path: string) => {
    navigate(path);
    if (isMobile) {
      setMobileOpen(false);
    }
  };

  const hasPermission = (permissions: string[]) => {
    if (!user) return false;
    return permissions.some(permission => user.permissions.includes(permission));
  };

  const allNavItems: NavItem[] = [
    {
      text: 'Dashboard',
      icon: <DashboardIcon />,
      path: '/dashboard',
    },
    {
      text: 'KPIs',
      icon: <KpiIcon />,
      path: '/kpis',
    },
    {
      text: 'Contacts',
      icon: <ContactIcon />,
      path: '/contacts',
    },
    {
      text: 'Alerts',
      icon: <AlertIcon />,
      path: '/alerts',
      badge: alertDashboard?.unresolvedAlerts || 0,
    },
    {
      text: 'Analytics',
      icon: <AnalyticsIcon />,
      path: '/analytics',
    },
    {
      text: 'Execution History',
      icon: <HistoryIcon />,
      path: '/execution-history',
    },
    {
      text: 'Administration',
      icon: <AdminPanelSettings />,
      path: '/administration',
      // Temporarily remove permissions for testing
      // requiredPermissions: ['System:Admin', 'User:Read', 'Role:Read'],
    },
    {
      text: 'User Settings',
      icon: <SettingsIcon />,
      path: '/settings',
    },
  ];

  const navItems = allNavItems.filter(item =>
    !item.requiredPermissions || hasPermission(item.requiredPermissions)
  );

  const currentDrawerWidth = sidebarCollapsed ? collapsedDrawerWidth : drawerWidth;

  const drawer = (
    <div>
      {/* Logo and Header */}
      <Box
        sx={{
          height: 64,
          display: 'flex',
          alignItems: 'center',
          justifyContent: sidebarCollapsed ? 'center' : 'space-between',
          px: sidebarCollapsed ? 1 : 2,
          position: 'relative',
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          color: 'white',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '60px',
            height: '60px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(20px, -20px)',
          }
        }}
      >
        {/* Logo */}
        <Box
          component="img"
          src={mgLogo}
          alt="MonitoringGrid Logo"
          sx={{
            height: sidebarCollapsed ? 28 : 40,
            width: 'auto',
            filter: 'brightness(0) invert(1)', // Make logo white
            position: 'relative',
            zIndex: 1,
          }}
        />

        {/* Collapse button - only show when not collapsed */}
        {!isMobile && !sidebarCollapsed && (
          <Tooltip title="Collapse sidebar">
            <IconButton
              onClick={handleSidebarToggle}
              size="small"
              sx={{
                color: 'white',
                backgroundColor: 'rgba(255, 255, 255, 0.15)',
                border: '1px solid rgba(255, 255, 255, 0.2)',
                '&:hover': {
                  backgroundColor: 'rgba(255, 255, 255, 0.25)',
                },
                position: 'relative',
                zIndex: 2,
                minWidth: '32px',
                height: '32px',
              }}
            >
              <ChevronLeftIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}

        {/* Expand button overlay for collapsed state */}
        {!isMobile && sidebarCollapsed && (
          <Tooltip title="Expand sidebar">
            <IconButton
              onClick={handleSidebarToggle}
              size="small"
              sx={{
                color: 'white',
                backgroundColor: 'rgba(255, 255, 255, 0.15)',
                border: '1px solid rgba(255, 255, 255, 0.2)',
                '&:hover': {
                  backgroundColor: 'rgba(255, 255, 255, 0.25)',
                },
                position: 'absolute',
                top: '50%',
                right: 8,
                transform: 'translateY(-50%)',
                zIndex: 2,
                minWidth: '32px',
                height: '32px',
              }}
            >
              <ChevronRightIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
      </Box>

      <Divider />

      <List sx={{ pt: 1 }}>
        {navItems.map((item) => (
          <ListItem key={item.text} disablePadding>
            <Tooltip
              title={sidebarCollapsed ? item.text : ""}
              placement="right"
              arrow
            >
              <ListItemButton
                selected={location.pathname === item.path || location.pathname.startsWith(item.path + '/')}
                onClick={() => handleNavigation(item.path)}
                sx={{
                  minHeight: 48,
                  justifyContent: sidebarCollapsed ? 'center' : 'initial',
                  px: sidebarCollapsed ? 1.5 : 2.5,
                  '&.Mui-selected': {
                    backgroundColor: theme.palette.primary.main + '20',
                    borderRight: `3px solid ${theme.palette.primary.main}`,
                    '& .MuiListItemIcon-root': {
                      color: theme.palette.primary.main,
                    },
                    '& .MuiListItemText-primary': {
                      color: theme.palette.primary.main,
                      fontWeight: 600,
                    },
                  },
                  '&:hover': {
                    backgroundColor: theme.palette.action.hover,
                  },
                }}
              >
                <ListItemIcon
                  sx={{
                    minWidth: 0,
                    mr: sidebarCollapsed ? 0 : 3,
                    justifyContent: 'center',
                  }}
                >
                  {item.badge && item.badge > 0 ? (
                    <Badge badgeContent={item.badge} color="error">
                      {item.icon}
                    </Badge>
                  ) : (
                    item.icon
                  )}
                </ListItemIcon>
                {!sidebarCollapsed && <ListItemText primary={item.text} />}
              </ListItemButton>
            </Tooltip>
          </ListItem>
        ))}
      </List>
    </div>
  );

  const getSystemStatusColor = () => {
    if (!healthData) return 'default';
    return healthData.status === 'Healthy' ? 'success' : 'error';
  };

  const getSystemStatusText = () => {
    if (!healthData) return 'Unknown';
    return healthData.status;
  };

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />
      
      {/* App Bar */}
      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${currentDrawerWidth}px)` },
          ml: { md: `${currentDrawerWidth}px` },
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          boxShadow: '0px 4px 20px rgba(102, 126, 234, 0.3)',
        }}
      >
        <Toolbar sx={{ minHeight: '64px !important' }}>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{
              mr: 2,
              display: { md: 'none' },
              backgroundColor: 'rgba(255, 255, 255, 0.1)',
              '&:hover': {
                backgroundColor: 'rgba(255, 255, 255, 0.2)',
              },
            }}
          >
            <MenuIcon />
          </IconButton>

          <Box sx={{ flexGrow: 1 }}>
            <Typography
              variant="h5"
              noWrap
              component="div"
              sx={{
                fontWeight: 700,
                letterSpacing: '0.5px',
                textShadow: '0 2px 4px rgba(0,0,0,0.1)',
              }}
            >
              {navItems.find(item =>
                location.pathname === item.path ||
                location.pathname.startsWith(item.path + '/')
              )?.text || 'Monitoring Grid'}
            </Typography>
            <Typography
              variant="caption"
              sx={{
                opacity: 0.8,
                display: 'block',
                lineHeight: 1,
              }}
            >
              Real-time monitoring dashboard
            </Typography>
          </Box>

          {/* System Status */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Chip
              label={`System: ${getSystemStatusText()}`}
              color={getSystemStatusColor()}
              size="small"
              variant="outlined"
              sx={{
                color: 'white',
                borderColor: 'rgba(255, 255, 255, 0.5)',
                backgroundColor: 'rgba(255, 255, 255, 0.1)',
                fontWeight: 600,
              }}
            />

            {alertDashboard && alertDashboard.unresolvedAlerts > 0 && (
              <Chip
                label={`${alertDashboard.unresolvedAlerts} Unresolved`}
                color="error"
                size="small"
                onClick={() => navigate('/alerts')}
                sx={{
                  cursor: 'pointer',
                  backgroundColor: 'rgba(244, 67, 54, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                  '&:hover': {
                    backgroundColor: 'rgba(244, 67, 54, 0.3)',
                  },
                }}
              />
            )}

            <Tooltip title="Refresh">
              <IconButton
                color="inherit"
                onClick={() => window.location.reload()}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  },
                }}
              >
                <RefreshIcon />
              </IconButton>
            </Tooltip>

            {/* User Menu */}
            <UserMenu />
          </Box>
        </Toolbar>
      </AppBar>

      {/* Navigation Drawer */}
      <Box
        component="nav"
        sx={{ width: { md: currentDrawerWidth }, flexShrink: { md: 0 } }}
      >
        {/* Mobile drawer */}
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true, // Better open performance on mobile.
          }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: drawerWidth,
              borderRight: '1px solid rgba(0, 0, 0, 0.12)',
            },
          }}
        >
          {drawer}
        </Drawer>

        {/* Desktop drawer */}
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: currentDrawerWidth,
              borderRight: '1px solid rgba(0, 0, 0, 0.12)',
              transition: theme.transitions.create('width', {
                easing: theme.transitions.easing.sharp,
                duration: theme.transitions.duration.enteringScreen,
              }),
            },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>

      {/* Main content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: { xs: 2, sm: 3, md: 4 },
          width: { md: `calc(100% - ${currentDrawerWidth}px)` },
          minHeight: '100vh',
          backgroundColor: theme.palette.background.default,
          transition: theme.transitions.create(['width', 'margin'], {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.enteringScreen,
          }),
        }}
      >
        <Toolbar />
        <Box sx={{ maxWidth: '1600px', margin: '0 auto' }}>
          {children}
        </Box>
      </Box>
    </Box>
  );
};

export default Layout;

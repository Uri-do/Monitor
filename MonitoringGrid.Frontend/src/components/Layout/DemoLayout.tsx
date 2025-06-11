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
  Tooltip,
  Divider,
  Button,
  Alert,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Assessment as KpiIcon,
  Analytics as AnalyticsIcon,
  AdminPanelSettings,
  ChevronLeft as ChevronLeftIcon,
  ChevronRight as ChevronRightIcon,
  DarkMode,
  LightMode,
  Build as WorkerIcon,
  Build,
} from '@mui/icons-material';
import { useTheme as useCustomTheme } from '@/hooks/useTheme';
import mgLogo from '@/assets/images/mglogo.png';

const drawerWidth = 280;
const collapsedDrawerWidth = 64;

interface DemoLayoutProps {
  children: React.ReactNode;
}

interface NavItem {
  text: string;
  icon: React.ReactElement;
  path: string;
}

const DemoLayout: React.FC<DemoLayoutProps> = ({ children }) => {
  const theme = useTheme();
  const { mode, toggleTheme } = useCustomTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

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

  const demoNavItems: NavItem[] = [
    {
      text: 'Basic Demo',
      icon: <DashboardIcon />,
      path: '/demo',
    },
    {
      text: 'Advanced Demo',
      icon: <KpiIcon />,
      path: '/demo/advanced',
    },
    {
      text: 'Next-Gen Features',
      icon: <AnalyticsIcon />,
      path: '/demo/nextgen',
    },
    {
      text: 'Enterprise Features',
      icon: <AdminPanelSettings />,
      path: '/demo/enterprise',
    },
    {
      text: 'Ultimate Enterprise',
      icon: <WorkerIcon />,
      path: '/demo/ultimate',
    },
    {
      text: 'Ultimate Components',
      icon: <Build />,
      path: '/demo/components',
    },
    {
      text: 'Ultimate DataTable',
      icon: <AnalyticsIcon />,
      path: '/demo/datatable',
    },
  ];

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
          background: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
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
          },
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
        {demoNavItems.map(item => (
          <ListItem key={item.text} disablePadding>
            <Tooltip title={sidebarCollapsed ? item.text : ''} placement="right" arrow>
              <ListItemButton
                selected={
                  location.pathname === item.path || location.pathname.startsWith(`${item.path}/`)
                }
                onClick={() => handleNavigation(item.path)}
                sx={{
                  minHeight: 48,
                  justifyContent: sidebarCollapsed ? 'center' : 'initial',
                  px: sidebarCollapsed ? 1.5 : 2.5,
                  '&.Mui-selected': {
                    backgroundColor: `${theme.palette.primary.main}20`,
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
                  {item.icon}
                </ListItemIcon>
                {!sidebarCollapsed && <ListItemText primary={item.text} />}
              </ListItemButton>
            </Tooltip>
          </ListItem>
        ))}
      </List>
    </div>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />

      {/* App Bar */}
      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${currentDrawerWidth}px)` },
          ml: { md: `${currentDrawerWidth}px` },
          background: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
          boxShadow: '0px 4px 20px rgba(255, 107, 107, 0.3)',
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
              {demoNavItems.find(
                item =>
                  location.pathname === item.path || location.pathname.startsWith(`${item.path}/`)
              )?.text || 'MonitoringGrid Demo'}
            </Typography>
            <Typography
              variant="caption"
              sx={{
                opacity: 0.8,
                display: 'block',
                lineHeight: 1,
              }}
            >
              Interactive demo - explore our features
            </Typography>
          </Box>

          {/* Demo Controls */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Tooltip title={mode === 'light' ? 'Switch to Dark Mode' : 'Switch to Light Mode'}>
              <IconButton
                color="inherit"
                onClick={toggleTheme}
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  },
                }}
              >
                {mode === 'light' ? <DarkMode /> : <LightMode />}
              </IconButton>
            </Tooltip>

            <Tooltip title="Demo Mode - Sign in for full access">
              <Button
                variant="outlined"
                size="small"
                onClick={() => navigate('/login')}
                sx={{
                  color: 'white',
                  borderColor: 'rgba(255, 255, 255, 0.5)',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                    borderColor: 'rgba(255, 255, 255, 0.7)',
                  },
                }}
              >
                Sign In
              </Button>
            </Tooltip>
          </Box>
        </Toolbar>
      </AppBar>

      {/* Navigation Drawer */}
      <Box component="nav" sx={{ width: { md: currentDrawerWidth }, flexShrink: { md: 0 } }}>
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
          {/* Demo Mode Banner */}
          <Alert
            severity="info"
            sx={{
              mb: 3,
              backgroundColor: 'rgba(255, 107, 107, 0.1)',
              borderColor: 'rgba(255, 107, 107, 0.3)',
              '& .MuiAlert-icon': {
                color: '#ff6b6b',
              },
            }}
            action={
              <Button
                color="inherit"
                size="small"
                onClick={() => navigate('/login')}
                sx={{ color: '#ff6b6b' }}
              >
                Sign In for Full Access
              </Button>
            }
          >
            <Typography variant="body2">
              <strong>Demo Mode:</strong> You're exploring MonitoringGrid's features. Some
              functionality may be limited or simulated.
            </Typography>
          </Alert>
          {children}
        </Box>
      </Box>
    </Box>
  );
};

export default DemoLayout;

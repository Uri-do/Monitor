import React, { useState } from 'react';
import {
  Avatar,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Typography,
  Divider,
  ListItemIcon,
  ListItemText,
  Chip,
  Badge,
} from '@mui/material';
import {
  Settings,
  Logout,
  Person,
  Security,
  Notifications,
  Help,
  DarkMode,
  LightMode,
} from '@mui/icons-material';
import { useAuth } from '@/hooks/useAuth';
import { useTheme } from '@/hooks/useTheme';
import { useNavigate } from 'react-router-dom';

const UserMenu: React.FC = () => {
  const { user, logout } = useAuth();
  const { mode, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleProfile = () => {
    navigate('/profile');
    handleClose();
  };

  const handleSettings = () => {
    navigate('/settings');
    handleClose();
  };

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login');
    } catch (error) {
      // Logout failed - continue with navigation
      // Force navigation even if logout fails
      navigate('/login');
    }
    handleClose();
  };

  if (!user) {
    return null;
  }

  const getInitials = (displayName: string | undefined | null) => {
    if (!displayName || typeof displayName !== 'string') {
      return user.email?.charAt(0)?.toUpperCase() || 'U';
    }

    return displayName
      .split(' ')
      .map(name => name.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getPrimaryRole = () => {
    if (!user.roles || user.roles.length === 0) return 'User';
    return user.roles[0].name;
  };

  return (
    <Box>
      <IconButton
        onClick={handleClick}
        size="small"
        sx={{ ml: 2 }}
        aria-controls={open ? 'user-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={open ? 'true' : undefined}
      >
        <Badge
          color="secondary"
          variant="dot"
          invisible={user.emailConfirmed}
          sx={{
            '& .MuiBadge-badge': {
              backgroundColor: 'warning.main',
            },
          }}
        >
          <Avatar
            sx={{
              width: 32,
              height: 32,
              bgcolor: 'primary.main',
              fontSize: '0.875rem',
            }}
          >
            {getInitials(user.displayName)}
          </Avatar>
        </Badge>
      </IconButton>

      <Menu
        anchorEl={anchorEl}
        id="user-menu"
        open={open}
        onClose={handleClose}
        onClick={handleClose}
        PaperProps={{
          elevation: 0,
          sx: {
            overflow: 'visible',
            filter: 'drop-shadow(0px 2px 8px rgba(0,0,0,0.32))',
            mt: 1.5,
            minWidth: 280,
            '& .MuiAvatar-root': {
              width: 32,
              height: 32,
              ml: -0.5,
              mr: 1,
            },
            '&:before': {
              content: '""',
              display: 'block',
              position: 'absolute',
              top: 0,
              right: 14,
              width: 10,
              height: 10,
              bgcolor: 'background.paper',
              transform: 'translateY(-50%) rotate(45deg)',
              zIndex: 0,
            },
          },
        }}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
      >
        {/* User Info Header */}
        <Box sx={{ px: 2, py: 1.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Avatar
              sx={{
                width: 40,
                height: 40,
                bgcolor: 'primary.main',
              }}
            >
              {getInitials(user.displayName)}
            </Avatar>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="subtitle2" noWrap>
                {user.displayName}
              </Typography>
              <Typography variant="body2" color="text.secondary" noWrap>
                {user.email}
              </Typography>
              <Box sx={{ display: 'flex', gap: 0.5, mt: 0.5 }}>
                <Chip
                  label={getPrimaryRole()}
                  size="small"
                  variant="outlined"
                  sx={{ fontSize: '0.75rem', height: 20 }}
                />
                {!user.emailConfirmed && (
                  <Chip
                    label="Unverified"
                    size="small"
                    color="warning"
                    sx={{ fontSize: '0.75rem', height: 20 }}
                  />
                )}
              </Box>
            </Box>
          </Box>
        </Box>

        <Divider />

        {/* Menu Items */}
        <MenuItem onClick={handleProfile}>
          <ListItemIcon>
            <Person fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Profile</Typography>
          </ListItemText>
        </MenuItem>

        <MenuItem onClick={handleSettings}>
          <ListItemIcon>
            <Settings fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Settings</Typography>
          </ListItemText>
        </MenuItem>

        <MenuItem onClick={() => navigate('/security')}>
          <ListItemIcon>
            <Security fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Security</Typography>
          </ListItemText>
        </MenuItem>

        <MenuItem onClick={() => navigate('/notifications')}>
          <ListItemIcon>
            <Notifications fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Notifications</Typography>
          </ListItemText>
        </MenuItem>

        <MenuItem onClick={() => navigate('/help')}>
          <ListItemIcon>
            <Help fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Help & Support</Typography>
          </ListItemText>
        </MenuItem>

        <MenuItem onClick={toggleTheme}>
          <ListItemIcon>
            {mode === 'light' ? <DarkMode fontSize="small" /> : <LightMode fontSize="small" />}
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">{mode === 'light' ? 'Dark Mode' : 'Light Mode'}</Typography>
          </ListItemText>
        </MenuItem>

        <Divider />

        <MenuItem onClick={handleLogout}>
          <ListItemIcon>
            <Logout fontSize="small" />
          </ListItemIcon>
          <ListItemText>
            <Typography variant="body2">Sign Out</Typography>
          </ListItemText>
        </MenuItem>
      </Menu>
    </Box>
  );
};

export default UserMenu;

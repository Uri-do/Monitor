import React from 'react';
import {
  Box,
  Typography,
  Button,
  IconButton,
  Breadcrumbs,
  Link,
  Chip,
  Stack,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

export interface BreadcrumbItem {
  label: string;
  href?: string;
  onClick?: () => void;
}

export interface PageAction {
  label: string;
  icon?: React.ReactNode;
  onClick: () => void;
  color?: 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success';
  variant?: 'contained' | 'outlined' | 'text';
  disabled?: boolean;
  loading?: boolean;
}

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  icon?: React.ReactNode;
  breadcrumbs?: BreadcrumbItem[];
  showBackButton?: boolean;
  backButtonPath?: string;
  actions?: PageAction[];
  primaryAction?: PageAction;
  status?: {
    label: string;
    color: 'success' | 'error' | 'warning' | 'info' | 'default';
  };
  metadata?: Array<{
    label: string;
    value: string | React.ReactNode;
  }>;
  onRefresh?: () => void;
  refreshing?: boolean;
}

const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  subtitle,
  icon,
  breadcrumbs,
  showBackButton = false,
  backButtonPath,
  actions = [],
  primaryAction,
  status,
  metadata,
  onRefresh,
  refreshing = false,
}) => {
  const navigate = useNavigate();

  const handleBack = () => {
    if (backButtonPath) {
      navigate(backButtonPath);
    } else {
      navigate(-1);
    }
  };

  const handleBreadcrumbClick = (item: BreadcrumbItem) => {
    if (item.onClick) {
      item.onClick();
    } else if (item.href) {
      navigate(item.href);
    }
  };

  return (
    <Box sx={{ mb: 4 }}>
      {/* Breadcrumbs */}
      {breadcrumbs && breadcrumbs.length > 0 && (
        <Breadcrumbs sx={{ mb: 2, px: 1 }}>
          {breadcrumbs.map((item, index) => (
            <Link
              key={index}
              color={index === breadcrumbs.length - 1 ? 'text.primary' : 'inherit'}
              href="#"
              onClick={e => {
                e.preventDefault();
                if (index < breadcrumbs.length - 1) {
                  handleBreadcrumbClick(item);
                }
              }}
              sx={{
                cursor: index < breadcrumbs.length - 1 ? 'pointer' : 'default',
                textDecoration: 'none',
                '&:hover': {
                  textDecoration: index < breadcrumbs.length - 1 ? 'underline' : 'none',
                },
              }}
            >
              {item.label}
            </Link>
          ))}
        </Breadcrumbs>
      )}

      {/* Header Content */}
      <Box
        sx={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          borderRadius: 1,
          p: 3,
          color: 'white',
          boxShadow: '0px 2px 8px rgba(102, 126, 234, 0.2)',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            right: 0,
            width: '100px',
            height: '100px',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '50%',
            transform: 'translate(30px, -30px)',
          },
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'flex-start',
            justifyContent: 'space-between',
            position: 'relative',
            zIndex: 1,
          }}
        >
          {/* Left Side - Title and Info */}
          <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, flex: 1 }}>
            {showBackButton && (
              <IconButton
                onClick={handleBack}
                sx={{
                  mt: 0.5,
                  color: 'white',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  },
                }}
              >
                <BackIcon />
              </IconButton>
            )}

            <Box sx={{ flex: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                {icon && <Box sx={{ color: 'white' }}>{icon}</Box>}
                <Typography variant="h4" component="h1" sx={{ fontWeight: 700 }}>
                  {title}
                </Typography>
                {status && (
                  <Chip
                    label={status.label}
                    color={status.color}
                    size="small"
                    variant="outlined"
                    sx={{
                      backgroundColor: 'rgba(255, 255, 255, 0.2)',
                      color: 'white',
                      borderColor: 'rgba(255, 255, 255, 0.3)',
                    }}
                  />
                )}
              </Box>

              {subtitle && (
                <Typography variant="subtitle1" sx={{ opacity: 0.9, mb: 2 }}>
                  {subtitle}
                </Typography>
              )}

              {/* Metadata */}
              {metadata && metadata.length > 0 && (
                <Stack direction="row" spacing={3} sx={{ mb: 2 }}>
                  {metadata.map((item, index) => (
                    <Box key={index}>
                      <Typography variant="caption" sx={{ opacity: 0.8 }} display="block">
                        {item.label}
                      </Typography>
                      <Typography variant="body2" fontWeight="medium">
                        {item.value}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
          </Box>

          {/* Right Side - Actions */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexShrink: 0 }}>
            {onRefresh && (
              <IconButton
                onClick={onRefresh}
                disabled={refreshing}
                sx={{
                  color: 'white',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                  },
                }}
              >
                <RefreshIcon
                  sx={{
                    animation: refreshing ? 'spin 1s linear infinite' : 'none',
                    '@keyframes spin': {
                      '0%': { transform: 'rotate(0deg)' },
                      '100%': { transform: 'rotate(360deg)' },
                    },
                  }}
                />
              </IconButton>
            )}

            {actions.map((action, index) => (
              <Button
                key={index}
                variant={action.variant || 'outlined'}
                startIcon={action.icon}
                onClick={action.onClick}
                disabled={action.disabled || action.loading}
                size="medium"
                sx={{
                  color: 'white',
                  borderColor: 'rgba(255, 255, 255, 0.3)',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                    borderColor: 'rgba(255, 255, 255, 0.5)',
                  },
                }}
              >
                {action.label}
              </Button>
            ))}

            {primaryAction && (
              <Button
                variant="contained"
                startIcon={primaryAction.icon}
                onClick={primaryAction.onClick}
                disabled={primaryAction.disabled || primaryAction.loading}
                size="medium"
                sx={{
                  backgroundColor: 'white',
                  color: 'primary.main',
                  fontWeight: 600,
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.9)',
                  },
                }}
              >
                {primaryAction.label}
              </Button>
            )}
          </Box>
        </Box>
      </Box>
    </Box>
  );
};

export default PageHeader;

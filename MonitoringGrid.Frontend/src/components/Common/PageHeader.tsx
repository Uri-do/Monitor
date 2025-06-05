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
  Divider,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Add as AddIcon,
  Refresh as RefreshIcon,
  Download as ExportIcon,
  Upload as ImportIcon,
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
    <Box sx={{ mb: 3 }}>
      {/* Breadcrumbs */}
      {breadcrumbs && breadcrumbs.length > 0 && (
        <Breadcrumbs sx={{ mb: 2 }}>
          {breadcrumbs.map((item, index) => (
            <Link
              key={index}
              color={index === breadcrumbs.length - 1 ? 'text.primary' : 'inherit'}
              href="#"
              onClick={(e) => {
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
      <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 2 }}>
        {/* Left Side - Title and Info */}
        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, flex: 1 }}>
          {showBackButton && (
            <IconButton onClick={handleBack} sx={{ mt: 0.5 }}>
              <BackIcon />
            </IconButton>
          )}
          
          <Box sx={{ flex: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
              <Typography variant="h4" component="h1">
                {title}
              </Typography>
              {status && (
                <Chip
                  label={status.label}
                  color={status.color}
                  size="small"
                  variant="outlined"
                />
              )}
            </Box>
            
            {subtitle && (
              <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
                {subtitle}
              </Typography>
            )}

            {/* Metadata */}
            {metadata && metadata.length > 0 && (
              <Stack direction="row" spacing={3} sx={{ mb: 2 }}>
                {metadata.map((item, index) => (
                  <Box key={index}>
                    <Typography variant="caption" color="text.secondary" display="block">
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
              color="primary"
            >
              <RefreshIcon sx={{ 
                animation: refreshing ? 'spin 1s linear infinite' : 'none',
                '@keyframes spin': {
                  '0%': { transform: 'rotate(0deg)' },
                  '100%': { transform: 'rotate(360deg)' },
                }
              }} />
            </IconButton>
          )}
          
          {actions.map((action, index) => (
            <Button
              key={index}
              variant={action.variant || 'outlined'}
              color={action.color || 'primary'}
              startIcon={action.icon}
              onClick={action.onClick}
              disabled={action.disabled || action.loading}
              size="medium"
            >
              {action.label}
            </Button>
          ))}
          
          {primaryAction && (
            <Button
              variant={primaryAction.variant || 'contained'}
              color={primaryAction.color || 'primary'}
              startIcon={primaryAction.icon}
              onClick={primaryAction.onClick}
              disabled={primaryAction.disabled || primaryAction.loading}
              size="medium"
            >
              {primaryAction.label}
            </Button>
          )}
        </Box>
      </Box>

      <Divider />
    </Box>
  );
};

export default PageHeader;

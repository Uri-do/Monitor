import React from 'react';
import { Box, Typography, Stack, IconButton, Tooltip } from '@mui/material';
import { Refresh as RefreshIcon } from '@mui/icons-material';
import Button from './Button';
import Card from './Card';

export interface PageHeaderProps {
  title: string;
  subtitle?: string;
  icon?: React.ReactElement;
  primaryAction?: {
    label: string;
    icon?: React.ReactElement;
    onClick: () => void;
    gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  };
  secondaryActions?: Array<{
    label: string;
    icon?: React.ReactElement;
    onClick: () => void;
    gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  }>;
  actions?: Array<{
    label: string;
    icon?: React.ReactElement;
    onClick: () => void;
    variant?: 'contained' | 'outlined' | 'text';
    gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  }>;
  onRefresh?: () => void;
  refreshing?: boolean;
  breadcrumbs?: Array<{
    label: string;
    onClick?: () => void;
    href?: string;
  }>;
}

export const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  subtitle,
  icon,
  primaryAction,
  secondaryActions = [],
  actions = [],
  onRefresh,
  refreshing = false,
  breadcrumbs = [],
}) => {
  return (
    <Card gradient="primary" glowEffect={true} sx={{ mb: 3 }}>
      <Box sx={{ p: 3 }}>
        {/* Breadcrumbs */}
        {breadcrumbs.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Stack direction="row" spacing={1} alignItems="center">
              {breadcrumbs.map((crumb, index) => (
                <React.Fragment key={index}>
                  <Typography
                    variant="body2"
                    sx={{
                      color: 'rgba(255, 255, 255, 0.8)',
                      cursor: crumb.onClick ? 'pointer' : 'default',
                      '&:hover': crumb.onClick
                        ? {
                            color: 'white',
                            textDecoration: 'underline',
                          }
                        : {},
                    }}
                    onClick={crumb.onClick}
                  >
                    {crumb.label}
                  </Typography>
                  {index < breadcrumbs.length - 1 && (
                    <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.6)' }}>
                      /
                    </Typography>
                  )}
                </React.Fragment>
              ))}
            </Stack>
          </Box>
        )}

        {/* Header Content */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          spacing={2}
        >
          {/* Title and Subtitle */}
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: subtitle ? 1 : 0 }}>
              {icon && (
                <Box
                  sx={{
                    color: 'white',
                    display: 'flex',
                    alignItems: 'center',
                    filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.3))',
                  }}
                >
                  {icon}
                </Box>
              )}
              <Typography
                variant="h4"
                sx={{
                  color: 'white',
                  fontWeight: 700,
                  textShadow: '0 2px 4px rgba(0,0,0,0.3)',
                }}
              >
                {title}
              </Typography>
            </Box>
            {subtitle && (
              <Typography
                variant="body1"
                sx={{
                  color: 'rgba(255, 255, 255, 0.9)',
                  fontWeight: 400,
                }}
              >
                {subtitle}
              </Typography>
            )}
          </Box>

          {/* Actions */}
          <Stack direction="row" spacing={1} alignItems="center">
            {/* Refresh Button */}
            {onRefresh && (
              <Tooltip title="Refresh">
                <IconButton
                  onClick={onRefresh}
                  disabled={refreshing}
                  sx={{
                    color: 'white',
                    backgroundColor: 'rgba(255, 255, 255, 0.1)',
                    '&:hover': {
                      backgroundColor: 'rgba(255, 255, 255, 0.2)',
                    },
                    '&:disabled': {
                      color: 'rgba(255, 255, 255, 0.5)',
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
              </Tooltip>
            )}

            {/* Secondary Actions */}
            {secondaryActions.map((action, index) => (
              <Button
                key={index}
                variant="outlined"
                gradient={action.gradient || 'primary'}
                startIcon={action.icon}
                onClick={action.onClick}
                sx={{
                  color: 'white',
                  borderColor: 'rgba(255, 255, 255, 0.3)',
                  '&:hover': {
                    borderColor: 'rgba(255, 255, 255, 0.6)',
                    backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  },
                }}
              >
                {action.label}
              </Button>
            ))}

            {/* Additional Actions */}
            {actions.map((action, index) => (
              <Button
                key={`action-${index}`}
                variant={action.variant || 'outlined'}
                gradient={action.gradient || 'primary'}
                startIcon={action.icon}
                onClick={action.onClick}
                sx={{
                  color: 'white',
                  borderColor: 'rgba(255, 255, 255, 0.3)',
                  '&:hover': {
                    borderColor: 'rgba(255, 255, 255, 0.6)',
                    backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  },
                }}
              >
                {action.label}
              </Button>
            ))}

            {/* Primary Action */}
            {primaryAction && (
              <Button
                gradient={primaryAction.gradient || 'secondary'}
                startIcon={primaryAction.icon}
                onClick={primaryAction.onClick}
                sx={{
                  boxShadow: '0 4px 15px rgba(255, 255, 255, 0.3)',
                  '&:hover': {
                    boxShadow: '0 6px 20px rgba(255, 255, 255, 0.4)',
                  },
                }}
              >
                {primaryAction.label}
              </Button>
            )}
          </Stack>
        </Stack>
      </Box>
    </Card>
  );
};

export default PageHeader;

// Keep Ultimate export for backward compatibility during migration
export { PageHeader as UltimatePageHeader };
export type { PageHeaderProps as UltimatePageHeaderProps };

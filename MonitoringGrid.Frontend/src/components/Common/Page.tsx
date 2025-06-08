import React from 'react';
import { Box, Container, Typography, Breadcrumbs, Link, Stack } from '@mui/material';
import { NavigateNext as NavigateNextIcon } from '@mui/icons-material';

export interface PageBreadcrumb {
  label: string;
  href?: string;
  onClick?: () => void;
}

export interface PageProps {
  title: string;
  subtitle?: string;
  breadcrumbs?: PageBreadcrumb[];
  headerActions?: React.ReactNode;
  filters?: React.ReactNode;
  mainContent: React.ReactNode;
  sideContent?: React.ReactNode;
  footerContent?: React.ReactNode;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | false;
  disableGutters?: boolean;
  loading?: boolean;
  error?: string;
}

const Page: React.FC<PageProps> = ({
  title,
  subtitle,
  breadcrumbs,
  headerActions,
  filters,
  mainContent,
  sideContent,
  footerContent,
  maxWidth = 'xl',
  disableGutters = false,
  loading = false,
  error,
}) => {
  const renderBreadcrumbs = () => {
    if (!breadcrumbs || breadcrumbs.length === 0) return null;

    return (
      <Breadcrumbs separator={<NavigateNextIcon fontSize="small" />} sx={{ mb: 2 }}>
        {breadcrumbs.map((breadcrumb, index) => {
          const isLast = index === breadcrumbs.length - 1;

          if (isLast || (!breadcrumb.href && !breadcrumb.onClick)) {
            return (
              <Typography key={index} color="text.primary" variant="body2">
                {breadcrumb.label}
              </Typography>
            );
          }

          return (
            <Link
              key={index}
              color="inherit"
              href={breadcrumb.href}
              onClick={breadcrumb.onClick}
              sx={{
                textDecoration: 'none',
                '&:hover': { textDecoration: 'underline' },
              }}
            >
              {breadcrumb.label}
            </Link>
          );
        })}
      </Breadcrumbs>
    );
  };

  const renderHeader = () => (
    <Box sx={{ mb: 3 }}>
      {renderBreadcrumbs()}

      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="flex-start"
        spacing={2}
        sx={{ mb: 2 }}
      >
        <Box>
          <Typography
            variant="h4"
            component="h1"
            sx={{
              fontWeight: 600,
              color: 'text.primary',
              mb: subtitle ? 1 : 0,
            }}
          >
            {title}
          </Typography>
          {subtitle && (
            <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 600 }}>
              {subtitle}
            </Typography>
          )}
        </Box>

        {headerActions && <Box sx={{ flexShrink: 0 }}>{headerActions}</Box>}
      </Stack>

      {filters && <Box sx={{ mt: 2 }}>{filters}</Box>}
    </Box>
  );

  const renderContent = () => {
    if (loading) {
      return (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: 200,
          }}
        >
          <Typography variant="body1" color="text.secondary">
            Loading...
          </Typography>
        </Box>
      );
    }

    if (error) {
      return (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: 200,
            flexDirection: 'column',
            gap: 2,
          }}
        >
          <Typography variant="h6" color="error">
            Error
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {error}
          </Typography>
        </Box>
      );
    }

    if (sideContent) {
      return (
        <Stack direction="row" spacing={3}>
          <Box sx={{ flex: 1 }}>{mainContent}</Box>
          <Box sx={{ width: 300, flexShrink: 0 }}>{sideContent}</Box>
        </Stack>
      );
    }

    return mainContent;
  };

  return (
    <Container
      maxWidth={maxWidth}
      disableGutters={disableGutters}
      sx={{
        py: 3,
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      {renderHeader()}

      <Box sx={{ flex: 1 }}>{renderContent()}</Box>

      {footerContent && <Box sx={{ mt: 3 }}>{footerContent}</Box>}
    </Container>
  );
};

export default Page;

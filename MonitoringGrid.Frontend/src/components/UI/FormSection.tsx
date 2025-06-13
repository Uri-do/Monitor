import React from 'react';
import { Card, CardContent, Typography, Divider, Box, Grid, GridProps } from '@mui/material';

export interface FormSectionProps {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  icon?: React.ReactElement;
  spacing?: number;
  gridProps?: GridProps;
  cardProps?: React.ComponentProps<typeof Card>;
  contentProps?: React.ComponentProps<typeof CardContent>;
}

/**
 * FormSection - Standardized section component for forms
 * Provides consistent card layout with title, optional subtitle, and content area
 */
const FormSection: React.FC<FormSectionProps> = ({
  title,
  subtitle,
  children,
  icon,
  spacing = 2,
  gridProps,
  cardProps,
  contentProps,
}) => {
  return (
    <Card {...cardProps}>
      <CardContent sx={{ p: 3, ...contentProps?.sx }} {...contentProps}>
        {/* Section Header */}
        <Box sx={{ mb: subtitle ? 2 : 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: subtitle ? 1 : 0 }}>
            {icon && (
              <Box sx={{ color: 'primary.main', display: 'flex', alignItems: 'center' }}>
                {icon}
              </Box>
            )}
            <Typography variant="h6" component="h2">
              {title}
            </Typography>
          </Box>
          {subtitle && (
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          )}
          <Divider sx={{ mt: 2 }} />
        </Box>

        {/* Section Content */}
        <Grid container spacing={spacing} {...gridProps}>
          {children}
        </Grid>
      </CardContent>
    </Card>
  );
};

export default FormSection;

import React from 'react';
import { Box, Container, Grid, GridProps } from '@mui/material';

export interface FormLayoutProps {
  children: React.ReactNode;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | false;
  spacing?: number;
  fullWidth?: boolean;
  containerProps?: React.ComponentProps<typeof Container>;
  gridProps?: GridProps;
}

/**
 * FormLayout - Standardized layout component for all form pages
 * Provides consistent spacing, width constraints, and responsive behavior
 */
const FormLayout: React.FC<FormLayoutProps> = ({
  children,
  maxWidth = 'md',
  spacing = 3,
  fullWidth = false,
  containerProps,
  gridProps,
}) => {
  const content = (
    <Grid container spacing={spacing} {...gridProps}>
      {children}
    </Grid>
  );

  if (fullWidth) {
    return <Box sx={{ width: '100%' }}>{content}</Box>;
  }

  return (
    <Container maxWidth={maxWidth} {...containerProps}>
      {content}
    </Container>
  );
};

export { FormLayout };
export default FormLayout;

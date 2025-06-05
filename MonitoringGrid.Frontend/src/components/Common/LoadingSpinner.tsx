import React from 'react';
import {
  Box,
  CircularProgress,
  Typography,
  Skeleton,
  Card,
  CardContent,
} from '@mui/material';

interface LoadingSpinnerProps {
  size?: number;
  message?: string;
  fullScreen?: boolean;
  variant?: 'spinner' | 'skeleton' | 'card';
  rows?: number;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 40,
  message = 'Loading...',
  fullScreen = false,
  variant = 'spinner',
  rows = 5,
}) => {
  const renderSkeleton = () => (
    <Box sx={{ width: '100%' }}>
      {Array.from({ length: rows }).map((_, index) => (
        <Skeleton
          key={index}
          variant="rectangular"
          height={60}
          sx={{ mb: 1, borderRadius: 1 }}
        />
      ))}
    </Box>
  );

  const renderCardSkeleton = () => (
    <Card>
      <CardContent>
        <Skeleton variant="text" width="60%" height={32} sx={{ mb: 2 }} />
        <Skeleton variant="rectangular" height={200} sx={{ mb: 2 }} />
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Skeleton variant="rectangular" width={80} height={32} />
          <Skeleton variant="rectangular" width={80} height={32} />
          <Skeleton variant="rectangular" width={80} height={32} />
        </Box>
      </CardContent>
    </Card>
  );

  const renderSpinner = () => (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        ...(fullScreen && {
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(255, 255, 255, 0.8)',
          zIndex: 9999,
        }),
        ...(!fullScreen && {
          py: 4,
        }),
      }}
    >
      <CircularProgress size={size} />
      {message && (
        <Typography variant="body2" color="text.secondary">
          {message}
        </Typography>
      )}
    </Box>
  );

  switch (variant) {
    case 'skeleton':
      return renderSkeleton();
    case 'card':
      return renderCardSkeleton();
    default:
      return renderSpinner();
  }
};

export default LoadingSpinner;

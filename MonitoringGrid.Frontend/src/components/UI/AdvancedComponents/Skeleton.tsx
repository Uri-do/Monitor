import React from 'react';
import { Box, Skeleton as MuiSkeleton, SkeletonProps as MuiSkeletonProps } from '@mui/material';

/**
 * Advanced Skeleton Components
 * Enhanced loading states with realistic content shapes
 */

interface SkeletonProps extends Omit<MuiSkeletonProps, 'variant'> {
  variant?: 'text' | 'rectangular' | 'circular' | 'rounded';
  lines?: number;
  spacing?: number;
}

export const Skeleton: React.FC<SkeletonProps> = ({
  variant = 'text',
  lines = 1,
  spacing = 1,
  ...props
}) => {
  if (lines > 1) {
    return (
      <Box>
        {Array.from({ length: lines }).map((_, index) => (
          <MuiSkeleton
            key={index}
            variant={variant === 'rounded' ? 'rectangular' : variant}
            sx={{
              mb: index < lines - 1 ? spacing : 0,
              borderRadius: variant === 'rounded' ? 1 : undefined,
              ...props.sx,
            }}
            {...props}
          />
        ))}
      </Box>
    );
  }

  return (
    <MuiSkeleton
      variant={variant === 'rounded' ? 'rectangular' : variant}
      sx={{
        borderRadius: variant === 'rounded' ? 1 : undefined,
        ...props.sx,
      }}
      {...props}
    />
  );
};

// Predefined skeleton layouts
export const CardSkeleton: React.FC<{ lines?: number }> = ({ lines = 3 }) => (
  <Box sx={{ p: 2 }}>
    <Skeleton variant="text" width="60%" height={24} sx={{ mb: 1 }} />
    <Skeleton variant="text" lines={lines} height={16} />
    <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
      <Skeleton variant="rectangular" width={80} height={32} />
      <Skeleton variant="rectangular" width={60} height={32} />
    </Box>
  </Box>
);

export const TableSkeleton: React.FC<{ rows?: number; columns?: number }> = ({ 
  rows = 5, 
  columns = 4 
}) => (
  <Box>
    {/* Header */}
    <Box sx={{ display: 'flex', gap: 2, mb: 2, p: 1 }}>
      {Array.from({ length: columns }).map((_, index) => (
        <Skeleton key={index} variant="text" width="100%" height={20} />
      ))}
    </Box>
    {/* Rows */}
    {Array.from({ length: rows }).map((_, rowIndex) => (
      <Box key={rowIndex} sx={{ display: 'flex', gap: 2, mb: 1, p: 1 }}>
        {Array.from({ length: columns }).map((_, colIndex) => (
          <Skeleton key={colIndex} variant="text" width="100%" height={16} />
        ))}
      </Box>
    ))}
  </Box>
);

export const ListSkeleton: React.FC<{ items?: number }> = ({ items = 5 }) => (
  <Box>
    {Array.from({ length: items }).map((_, index) => (
      <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
        <Skeleton variant="circular" width={40} height={40} />
        <Box sx={{ flex: 1 }}>
          <Skeleton variant="text" width="70%" height={16} />
          <Skeleton variant="text" width="50%" height={14} />
        </Box>
        <Skeleton variant="rectangular" width={60} height={24} />
      </Box>
    ))}
  </Box>
);

export const DashboardSkeleton: React.FC = () => (
  <Box sx={{ p: 3 }}>
    {/* Header */}
    <Skeleton variant="text" width="40%" height={32} sx={{ mb: 3 }} />
    
    {/* Stats Cards */}
    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: 2, mb: 3 }}>
      {Array.from({ length: 4 }).map((_, index) => (
        <Box key={index} sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
          <Skeleton variant="text" width="60%" height={16} sx={{ mb: 1 }} />
          <Skeleton variant="text" width="40%" height={24} sx={{ mb: 1 }} />
          <Skeleton variant="rectangular" width="100%" height={4} />
        </Box>
      ))}
    </Box>
    
    {/* Charts */}
    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: 2 }}>
      <Box sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
        <Skeleton variant="text" width="50%" height={20} sx={{ mb: 2 }} />
        <Skeleton variant="rectangular" width="100%" height={200} />
      </Box>
      <Box sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
        <Skeleton variant="text" width="50%" height={20} sx={{ mb: 2 }} />
        <Skeleton variant="rectangular" width="100%" height={200} />
      </Box>
    </Box>
  </Box>
);

export const FormSkeleton: React.FC<{ fields?: number }> = ({ fields = 5 }) => (
  <Box sx={{ p: 2 }}>
    <Skeleton variant="text" width="40%" height={24} sx={{ mb: 3 }} />
    {Array.from({ length: fields }).map((_, index) => (
      <Box key={index} sx={{ mb: 3 }}>
        <Skeleton variant="text" width="30%" height={16} sx={{ mb: 1 }} />
        <Skeleton variant="rectangular" width="100%" height={40} />
      </Box>
    ))}
    <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
      <Skeleton variant="rectangular" width={100} height={36} />
      <Skeleton variant="rectangular" width={80} height={36} />
    </Box>
  </Box>
);

export const ProfileSkeleton: React.FC = () => (
  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
    <Skeleton variant="circular" width={48} height={48} />
    <Box sx={{ flex: 1 }}>
      <Skeleton variant="text" width="60%" height={16} />
      <Skeleton variant="text" width="40%" height={14} />
    </Box>
  </Box>
);

export const ChartSkeleton: React.FC<{ height?: number }> = ({ height = 300 }) => (
  <Box>
    <Skeleton variant="text" width="40%" height={20} sx={{ mb: 2 }} />
    <Skeleton variant="rectangular" width="100%" height={height} />
    <Box sx={{ display: 'flex', justifyContent: 'center', gap: 2, mt: 2 }}>
      {Array.from({ length: 3 }).map((_, index) => (
        <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Skeleton variant="rectangular" width={12} height={12} />
          <Skeleton variant="text" width={60} height={14} />
        </Box>
      ))}
    </Box>
  </Box>
);

export const NavigationSkeleton: React.FC<{ items?: number }> = ({ items = 6 }) => (
  <Box sx={{ p: 1 }}>
    {Array.from({ length: items }).map((_, index) => (
      <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 1, mb: 1 }}>
        <Skeleton variant="rectangular" width={20} height={20} />
        <Skeleton variant="text" width="70%" height={16} />
      </Box>
    ))}
  </Box>
);

export const MetricCardSkeleton: React.FC = () => (
  <Box sx={{ p: 2 }}>
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
      <Skeleton variant="text" width="60%" height={16} />
      <Skeleton variant="circular" width={24} height={24} />
    </Box>
    <Skeleton variant="text" width="40%" height={32} sx={{ mb: 1 }} />
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <Skeleton variant="rectangular" width={16} height={16} />
      <Skeleton variant="text" width="50%" height={14} />
    </Box>
  </Box>
);

export const TimelineSkeleton: React.FC<{ events?: number }> = ({ events = 5 }) => (
  <Box>
    {Array.from({ length: events }).map((_, index) => (
      <Box key={index} sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Skeleton variant="circular" width={12} height={12} />
          {index < events - 1 && (
            <Box sx={{ width: 2, height: 40, bgcolor: 'divider', mt: 1 }} />
          )}
        </Box>
        <Box sx={{ flex: 1 }}>
          <Skeleton variant="text" width="80%" height={16} sx={{ mb: 1 }} />
          <Skeleton variant="text" width="60%" height={14} sx={{ mb: 1 }} />
          <Skeleton variant="text" width="30%" height={12} />
        </Box>
      </Box>
    ))}
  </Box>
);

export default Skeleton;

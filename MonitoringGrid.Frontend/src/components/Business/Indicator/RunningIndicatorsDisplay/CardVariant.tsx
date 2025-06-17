import React from 'react';
import {
  Box,
  Typography,
  Grid,
  CardContent,
  List,
  IconButton,
  Badge,
} from '@mui/material';
import { PlayCircle, PlayArrow } from '@mui/icons-material';
import { Card } from '@/components';
import { IndicatorItem } from './IndicatorItem';
import { EmptyState } from './EmptyState';
import { RunningIndicator } from '../types';

interface CardVariantProps {
  title: string;
  runningIndicators: RunningIndicator[];
  displayIndicators: RunningIndicator[];
  showProgress: boolean;
  showNavigateButton: boolean;
  onNavigate?: () => void;
}

export const CardVariant: React.FC<CardVariantProps> = ({
  title,
  runningIndicators,
  displayIndicators,
  showProgress,
  showNavigateButton,
  onNavigate,
}) => {
  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <Badge
                badgeContent={runningIndicators.length}
                color="primary"
                sx={{ '& .MuiBadge-badge': { fontSize: '0.7rem' } }}
              >
                <PlayCircle sx={{ color: 'primary.main' }} />
              </Badge>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {title}
              </Typography>
            </Box>
            {showNavigateButton && onNavigate && (
              <IconButton
                size="small"
                onClick={onNavigate}
                sx={{
                  backgroundColor: 'primary.main',
                  color: 'white',
                  '&:hover': {
                    backgroundColor: 'primary.dark',
                  },
                }}
              >
                <PlayArrow />
              </IconButton>
            )}
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {runningIndicators.length > 0 ? (
              <List sx={{ p: 0 }}>
                {displayIndicators.map((indicator) => (
                  <IndicatorItem
                    key={indicator.indicatorID}
                    indicator={indicator}
                    variant="card"
                    showProgress={showProgress}
                  />
                ))}
              </List>
            ) : (
              <EmptyState />
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default CardVariant;

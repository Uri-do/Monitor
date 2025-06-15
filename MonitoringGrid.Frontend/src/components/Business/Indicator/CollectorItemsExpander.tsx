import React from 'react';
import {
  Box,
  Typography,
  CircularProgress,
  Popover,
  Chip,
} from '@mui/material';
import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material';
import { useCollectorItemNames, useCollector, useCollectorStatistics } from '@/hooks/useMonitorStatistics';

interface CollectorItemsExpanderProps {
  collectorId: number;
  selectedItemName?: string;
  showStats?: boolean;
}

/**
 * Component to show collector items expansion with optional statistics
 */
export const CollectorItemsExpander: React.FC<CollectorItemsExpanderProps> = ({
  collectorId,
  selectedItemName,
  showStats = false,
}) => {
  const [anchorEl, setAnchorEl] = React.useState<HTMLElement | null>(null);
  const { data: itemNames, isLoading } = useCollectorItemNames(collectorId);
  const { data: collector } = useCollector(collectorId);

  // Fetch last 30 days statistics if showStats is enabled
  const fromDate = React.useMemo(() => {
    const date = new Date();
    date.setDate(date.getDate() - 30);
    return date.toISOString().split('T')[0];
  }, []);

  const toDate = React.useMemo(() => {
    return new Date().toISOString().split('T')[0];
  }, []);

  const { data: statistics, isLoading: statsLoading } = useCollectorStatistics(
    collectorId,
    showStats ? { fromDate, toDate } : undefined
  );

  // Calculate summary stats for ALL items
  const allItemsStats = React.useMemo(() => {
    if (!statistics || !showStats) return null;

    // Group by item name and calculate totals
    const itemGroups = statistics.reduce(
      (acc, stat) => {
        const itemName = stat.itemName || 'Unknown';
        if (!acc[itemName]) {
          acc[itemName] = { totalSum: 0, markedSum: 0, recordCount: 0 };
        }
        acc[itemName].totalSum += stat.total || 0;
        acc[itemName].markedSum += stat.marked || 0;
        acc[itemName].recordCount += 1;
        return acc;
      },
      {} as Record<string, { totalSum: number; markedSum: number; recordCount: number }>
    );

    // Convert to array and calculate percentages
    return Object.entries(itemGroups)
      .map(([itemName, stats]) => ({
        itemName,
        totalSum: stats.totalSum,
        markedSum: stats.markedSum,
        avgTotal: stats.recordCount > 0 ? stats.totalSum / stats.recordCount : 0,
        avgMarked: stats.recordCount > 0 ? stats.markedSum / stats.recordCount : 0,
        markedPercent: stats.totalSum > 0 ? (stats.markedSum / stats.totalSum) * 100 : 0,
        recordCount: stats.recordCount,
      }))
      .sort((a, b) => b.totalSum - a.totalSum); // Sort by total descending
  }, [statistics, showStats]);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const open = Boolean(anchorEl);

  if (isLoading) {
    return <CircularProgress size={16} />;
  }

  if (!itemNames || itemNames.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No items available for this collector
      </Typography>
    );
  }

  const collectorName =
    collector?.displayName || collector?.collectorDesc || collector?.collectorCode || 'Collector';

  return (
    <>
      <Box
        sx={{
          cursor: 'pointer',
          p: 2,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          '&:hover': {
            bgcolor: 'action.hover',
          },
        }}
        onClick={handleClick}
      >
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              {selectedItemName} • See all collector items ({itemNames.length} available)
            </Typography>
          </Box>
          <ExpandMoreIcon color="action" />
        </Box>
      </Box>

      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
        slotProps={{
          paper: {
            sx: {
              width: anchorEl?.offsetWidth || 'auto',
              maxHeight: 500,
              overflow: 'auto',
            },
          },
        }}
      >
        <Box sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            {collectorName} - All Items
          </Typography>

          {showStats && allItemsStats && (
            <Box sx={{ mb: 3 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Last 30 Days Statistics (All Items)
              </Typography>
              {statsLoading ? (
                <CircularProgress size={16} />
              ) : (
                <Box sx={{ maxHeight: 200, overflow: 'auto' }}>
                  {allItemsStats.map(itemStat => (
                    <Box
                      key={itemStat.itemName}
                      sx={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        py: 0.5,
                        px: 1,
                        borderBottom: '1px solid',
                        borderColor: 'divider',
                        '&:last-child': { borderBottom: 'none' },
                      }}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography
                          variant="body2"
                          sx={{ fontWeight: itemStat.itemName === selectedItemName ? 600 : 400 }}
                        >
                          {itemStat.itemName}
                        </Typography>
                        {itemStat.itemName === selectedItemName && (
                          <Chip label="Selected" size="small" color="primary" />
                        )}
                      </Box>
                      <Typography variant="caption" color="text.secondary">
                        {itemStat.totalSum.toLocaleString()} • {itemStat.markedSum.toLocaleString()}{' '}
                        ({itemStat.markedPercent.toFixed(1)}%)
                      </Typography>
                    </Box>
                  ))}
                </Box>
              )}
            </Box>
          )}
        </Box>
      </Popover>
    </>
  );
};

export default CollectorItemsExpander;

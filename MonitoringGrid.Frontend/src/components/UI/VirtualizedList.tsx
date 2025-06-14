import React, { useMemo, useCallback } from 'react';
import { FixedSizeList as List, VariableSizeList, ListChildComponentProps } from 'react-window';
import { Box, CircularProgress, Typography } from '@mui/material';
import InfiniteLoader from 'react-window-infinite-loader';

interface VirtualizedListProps<T> {
  items: T[];
  height: number;
  itemHeight: number | ((index: number) => number);
  renderItem: (item: T, index: number, style: React.CSSProperties) => React.ReactNode;
  hasNextPage?: boolean;
  isNextPageLoading?: boolean;
  loadNextPage?: () => Promise<void>;
  threshold?: number;
  overscan?: number;
  className?: string;
  emptyMessage?: string;
  loadingMessage?: string;
}

export const VirtualizedList = <T,>({
  items,
  height,
  itemHeight,
  renderItem,
  hasNextPage = false,
  isNextPageLoading = false,
  loadNextPage,
  threshold = 15,
  overscan = 5,
  className,
  emptyMessage = 'No items to display',
  loadingMessage = 'Loading...',
}: VirtualizedListProps<T>) => {
  const isItemLoaded = useCallback(
    (index: number) => {
      return !!items[index];
    },
    [items]
  );

  const itemCount = hasNextPage ? items.length + 1 : items.length;

  const Item = useCallback(
    ({ index, style }: ListChildComponentProps) => {
      let content;

      if (index >= items.length) {
        // Loading item
        content = (
          <Box
            display="flex"
            alignItems="center"
            justifyContent="center"
            height="100%"
          >
            <CircularProgress size={24} />
            <Typography variant="body2" sx={{ ml: 1 }}>
              {loadingMessage}
            </Typography>
          </Box>
        );
      } else {
        // Regular item
        const item = items[index];
        content = renderItem(item, index, style);
      }

      return <div style={style}>{content}</div>;
    },
    [items, renderItem, loadingMessage]
  );

  const ListComponent = useMemo(() => {
    if (typeof itemHeight === 'function') {
      return VariableSizeList;
    }
    return List;
  }, [itemHeight]);

  if (items.length === 0 && !isNextPageLoading) {
    return (
      <Box
        display="flex"
        alignItems="center"
        justifyContent="center"
        height={height}
        className={className}
      >
        <Typography variant="body1" color="textSecondary">
          {emptyMessage}
        </Typography>
      </Box>
    );
  }

  if (loadNextPage && hasNextPage) {
    return (
      <InfiniteLoader
        isItemLoaded={isItemLoaded}
        itemCount={itemCount}
        loadMoreItems={loadNextPage}
        threshold={threshold}
      >
        {({ onItemsRendered, ref }) => (
          <ListComponent
            ref={ref}
            height={height}
            itemCount={itemCount}
            itemSize={itemHeight}
            onItemsRendered={onItemsRendered}
            overscanCount={overscan}
            className={className}
          >
            {Item}
          </ListComponent>
        )}
      </InfiniteLoader>
    );
  }

  return (
    <ListComponent
      height={height}
      itemCount={itemCount}
      itemSize={itemHeight}
      overscanCount={overscan}
      className={className}
    >
      {Item}
    </ListComponent>
  );
};

// Grid virtualization component
interface VirtualizedGridProps<T> {
  items: T[];
  height: number;
  width: number;
  columnCount: number;
  rowHeight: number;
  columnWidth: number;
  renderItem: (item: T, rowIndex: number, columnIndex: number, style: React.CSSProperties) => React.ReactNode;
  hasNextPage?: boolean;
  isNextPageLoading?: boolean;
  loadNextPage?: () => Promise<void>;
  className?: string;
  emptyMessage?: string;
}

export const VirtualizedGrid = <T,>({
  items,
  height,
  width,
  columnCount,
  rowHeight,
  columnWidth,
  renderItem,
  hasNextPage = false,
  isNextPageLoading = false,
  loadNextPage,
  className,
  emptyMessage = 'No items to display',
}: VirtualizedGridProps<T>) => {
  const rowCount = Math.ceil(items.length / columnCount);

  const Cell = useCallback(
    ({ columnIndex, rowIndex, style }: any) => {
      const itemIndex = rowIndex * columnCount + columnIndex;
      
      if (itemIndex >= items.length) {
        if (itemIndex === items.length && isNextPageLoading) {
          return (
            <div style={style}>
              <Box
                display="flex"
                alignItems="center"
                justifyContent="center"
                height="100%"
              >
                <CircularProgress size={20} />
              </Box>
            </div>
          );
        }
        return <div style={style} />;
      }

      const item = items[itemIndex];
      return renderItem(item, rowIndex, columnIndex, style);
    },
    [items, columnCount, renderItem, isNextPageLoading]
  );

  if (items.length === 0 && !isNextPageLoading) {
    return (
      <Box
        display="flex"
        alignItems="center"
        justifyContent="center"
        height={height}
        width={width}
        className={className}
      >
        <Typography variant="body1" color="textSecondary">
          {emptyMessage}
        </Typography>
      </Box>
    );
  }

  // Note: For grid virtualization, you would typically use react-window's FixedSizeGrid
  // This is a simplified version for demonstration
  return (
    <Box className={className}>
      {/* Grid implementation would go here */}
      <Typography variant="body2" color="textSecondary">
        Grid virtualization implementation
      </Typography>
    </Box>
  );
};

// Hook for virtualized data management
export const useVirtualizedData = <T,>(
  initialData: T[],
  pageSize: number = 50
) => {
  const [data, setData] = React.useState<T[]>(initialData);
  const [hasNextPage, setHasNextPage] = React.useState(true);
  const [isLoading, setIsLoading] = React.useState(false);

  const loadNextPage = useCallback(async () => {
    if (isLoading || !hasNextPage) return;

    setIsLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // In real implementation, this would fetch from API
      const newItems = Array.from({ length: pageSize }, (_, i) => ({
        id: data.length + i,
        // ... other properties
      })) as T[];

      setData(prev => [...prev, ...newItems]);
      
      // Check if there are more items (this would come from API response)
      if (data.length + newItems.length >= 1000) {
        setHasNextPage(false);
      }
    } catch (error) {
      console.error('Failed to load next page:', error);
    } finally {
      setIsLoading(false);
    }
  }, [data.length, hasNextPage, isLoading, pageSize]);

  const reset = useCallback(() => {
    setData(initialData);
    setHasNextPage(true);
    setIsLoading(false);
  }, [initialData]);

  return {
    data,
    hasNextPage,
    isLoading,
    loadNextPage,
    reset,
  };
};

// Performance optimized list item component
export const MemoizedListItem = React.memo<{
  item: any;
  index: number;
  style: React.CSSProperties;
  children: (item: any, index: number) => React.ReactNode;
}>(({ item, index, style, children }) => {
  return (
    <div style={style}>
      {children(item, index)}
    </div>
  );
});

MemoizedListItem.displayName = 'MemoizedListItem';

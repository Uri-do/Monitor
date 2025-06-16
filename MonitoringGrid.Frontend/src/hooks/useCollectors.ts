import { useQuery } from '@tanstack/react-query';
import { monitorStatisticsApi } from '@/services/api';

// Hook for fetching collectors from the API
export const useCollectors = () => {
  return useQuery({
    queryKey: ['collectors'],
    queryFn: async () => {
      const response = await monitorStatisticsApi.getActiveCollectors();
      // Ensure response is an array before mapping
      const collectors = Array.isArray(response) ? response : [];
      // Transform the response to match the expected format
      return collectors.map((collector: any) => ({
        id: collector.collectorID,
        collectorID: collector.collectorID,
        name: collector.collectorCode,
        description: collector.collectorDesc || '',
        isActive: collector.isActive
      }));
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

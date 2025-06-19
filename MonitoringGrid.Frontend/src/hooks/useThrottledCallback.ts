import { useCallback, useRef } from 'react';

/**
 * Custom hook to throttle callback execution
 * @param callback - The callback function to throttle
 * @param delay - The throttle delay in milliseconds
 * @returns Throttled callback function
 */
export const useThrottledCallback = <T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T => {
  const lastExecuted = useRef<number>(0);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  return useCallback(
    ((...args: Parameters<T>) => {
      const now = Date.now();
      
      if (now - lastExecuted.current >= delay) {
        // Execute immediately if enough time has passed
        lastExecuted.current = now;
        callback(...args);
      } else {
        // Clear any pending timeout
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }
        
        // Schedule execution for later
        timeoutRef.current = setTimeout(() => {
          lastExecuted.current = Date.now();
          callback(...args);
          timeoutRef.current = null;
        }, delay - (now - lastExecuted.current));
      }
    }) as T,
    [callback, delay]
  );
};

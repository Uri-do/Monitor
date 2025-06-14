import React, { useState, useRef, useEffect, ImgHTMLAttributes } from 'react';
import { Box, Skeleton } from '@mui/material';
import { createIntersectionObserver } from '@/utils/performance';

interface OptimizedImageProps extends Omit<ImgHTMLAttributes<HTMLImageElement>, 'loading'> {
  src: string;
  alt: string;
  width?: number;
  height?: number;
  lazy?: boolean;
  placeholder?: string;
  fallback?: string;
  quality?: number;
  format?: 'webp' | 'avif' | 'jpg' | 'png';
  sizes?: string;
  priority?: boolean;
  onLoad?: () => void;
  onError?: () => void;
}

export const OptimizedImage: React.FC<OptimizedImageProps> = ({
  src,
  alt,
  width,
  height,
  lazy = true,
  placeholder,
  fallback = '/images/placeholder.png',
  quality = 75,
  format = 'webp',
  sizes,
  priority = false,
  onLoad,
  onError,
  className,
  style,
  ...props
}) => {
  const [isLoaded, setIsLoaded] = useState(false);
  const [isInView, setIsInView] = useState(!lazy || priority);
  const [hasError, setHasError] = useState(false);
  const [currentSrc, setCurrentSrc] = useState<string>('');
  const imgRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Generate optimized image URLs
  const generateOptimizedSrc = (originalSrc: string, width?: number, height?: number): string => {
    // This would typically integrate with an image optimization service
    // For now, we'll return the original src with query parameters
    const url = new URL(originalSrc, window.location.origin);
    
    if (width) url.searchParams.set('w', width.toString());
    if (height) url.searchParams.set('h', height.toString());
    if (quality !== 75) url.searchParams.set('q', quality.toString());
    if (format !== 'jpg') url.searchParams.set('f', format);
    
    return url.toString();
  };

  // Generate srcSet for responsive images
  const generateSrcSet = (originalSrc: string): string => {
    if (!width) return '';
    
    const breakpoints = [0.5, 1, 1.5, 2];
    return breakpoints
      .map(multiplier => {
        const scaledWidth = Math.round(width * multiplier);
        const optimizedSrc = generateOptimizedSrc(originalSrc, scaledWidth, height);
        return `${optimizedSrc} ${scaledWidth}w`;
      })
      .join(', ');
  };

  // Intersection Observer for lazy loading
  useEffect(() => {
    if (!lazy || priority || isInView) return;

    const observer = createIntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsInView(true);
            observer.disconnect();
          }
        });
      },
      { rootMargin: '50px' }
    );

    if (containerRef.current) {
      observer.observe(containerRef.current);
    }

    return () => observer.disconnect();
  }, [lazy, priority, isInView]);

  // Load image when in view
  useEffect(() => {
    if (isInView && !currentSrc) {
      const optimizedSrc = generateOptimizedSrc(src, width, height);
      setCurrentSrc(optimizedSrc);
    }
  }, [isInView, src, width, height, currentSrc]);

  // Preload critical images
  useEffect(() => {
    if (priority && src) {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.as = 'image';
      link.href = generateOptimizedSrc(src, width, height);
      if (sizes) link.setAttribute('imagesizes', sizes);
      document.head.appendChild(link);

      return () => {
        document.head.removeChild(link);
      };
    }
  }, [priority, src, width, height, sizes]);

  const handleLoad = () => {
    setIsLoaded(true);
    onLoad?.();
  };

  const handleError = () => {
    setHasError(true);
    if (fallback && currentSrc !== fallback) {
      setCurrentSrc(fallback);
      setHasError(false);
    } else {
      onError?.();
    }
  };

  const containerStyle = {
    width: width ? `${width}px` : '100%',
    height: height ? `${height}px` : 'auto',
    position: 'relative' as const,
    overflow: 'hidden' as const,
    ...style,
  };

  const imageStyle = {
    width: '100%',
    height: '100%',
    objectFit: 'cover' as const,
    transition: 'opacity 0.3s ease-in-out',
    opacity: isLoaded ? 1 : 0,
  };

  return (
    <Box ref={containerRef} style={containerStyle} className={className}>
      {/* Placeholder/Skeleton */}
      {!isLoaded && !hasError && (
        <Box
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            zIndex: 1,
          }}
        >
          {placeholder ? (
            <img
              src={placeholder}
              alt=""
              style={{
                width: '100%',
                height: '100%',
                objectFit: 'cover',
                filter: 'blur(5px)',
              }}
            />
          ) : (
            <Skeleton
              variant="rectangular"
              width="100%"
              height="100%"
              animation="wave"
            />
          )}
        </Box>
      )}

      {/* Main Image */}
      {isInView && currentSrc && (
        <img
          ref={imgRef}
          src={currentSrc}
          srcSet={generateSrcSet(src)}
          sizes={sizes}
          alt={alt}
          style={imageStyle}
          onLoad={handleLoad}
          onError={handleError}
          loading={lazy && !priority ? 'lazy' : 'eager'}
          decoding="async"
          {...props}
        />
      )}

      {/* Error State */}
      {hasError && (
        <Box
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: '#f5f5f5',
            color: '#666',
            fontSize: '14px',
          }}
        >
          Image failed to load
        </Box>
      )}
    </Box>
  );
};

// Hook for image preloading
export const useImagePreloader = () => {
  const preloadImages = (urls: string[]) => {
    urls.forEach(url => {
      const img = new Image();
      img.src = url;
    });
  };

  return { preloadImages };
};

// Progressive image loading component
export const ProgressiveImage: React.FC<{
  lowQualitySrc: string;
  highQualitySrc: string;
  alt: string;
  width?: number;
  height?: number;
}> = ({ lowQualitySrc, highQualitySrc, alt, width, height }) => {
  const [highQualityLoaded, setHighQualityLoaded] = useState(false);

  useEffect(() => {
    const img = new Image();
    img.onload = () => setHighQualityLoaded(true);
    img.src = highQualitySrc;
  }, [highQualitySrc]);

  return (
    <Box style={{ position: 'relative', width, height }}>
      <img
        src={lowQualitySrc}
        alt={alt}
        style={{
          width: '100%',
          height: '100%',
          objectFit: 'cover',
          filter: highQualityLoaded ? 'blur(0px)' : 'blur(5px)',
          transition: 'filter 0.3s ease-in-out',
        }}
      />
      {highQualityLoaded && (
        <img
          src={highQualitySrc}
          alt={alt}
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            opacity: highQualityLoaded ? 1 : 0,
            transition: 'opacity 0.3s ease-in-out',
          }}
        />
      )}
    </Box>
  );
};

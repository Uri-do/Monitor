import { useEffect, useCallback, useRef } from 'react';
import { useAppStore, useAppSelectors } from '@/stores/appStore';

interface AccessibilityOptions {
  announcePageChanges?: boolean;
  manageFocus?: boolean;
  trapFocus?: boolean;
  announceErrors?: boolean;
  announceLoading?: boolean;
  keyboardNavigation?: boolean;
}

interface FocusTrapOptions {
  initialFocus?: HTMLElement | string;
  returnFocus?: HTMLElement;
  allowOutsideClick?: boolean;
}

/**
 * Advanced accessibility hook providing comprehensive a11y features
 */
export const useAccessibility = (options: AccessibilityOptions = {}) => {
  const {
    announcePageChanges = true,
    manageFocus = true,
    trapFocus = false,
    announceErrors = true,
    announceLoading = true,
    keyboardNavigation = true,
  } = options;

  const preferences = useAppSelectors.accessibility();
  const currentPage = useAppStore(state => state.currentPage);
  const globalError = useAppStore(state => state.globalError);
  const globalLoading = useAppStore(state => state.globalLoading);

  const announcementRef = useRef<HTMLDivElement | null>(null);
  const focusTrapRef = useRef<HTMLElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  // Create or get the live region for announcements
  const getLiveRegion = useCallback(() => {
    if (!announcementRef.current) {
      const existing = document.getElementById('a11y-live-region');
      if (existing) {
        announcementRef.current = existing as HTMLDivElement;
      } else {
        const liveRegion = document.createElement('div');
        liveRegion.id = 'a11y-live-region';
        liveRegion.setAttribute('aria-live', 'polite');
        liveRegion.setAttribute('aria-atomic', 'true');
        liveRegion.style.position = 'absolute';
        liveRegion.style.left = '-10000px';
        liveRegion.style.width = '1px';
        liveRegion.style.height = '1px';
        liveRegion.style.overflow = 'hidden';
        document.body.appendChild(liveRegion);
        announcementRef.current = liveRegion;
      }
    }
    return announcementRef.current;
  }, []);

  // Announce messages to screen readers
  const announce = useCallback(
    (message: string, priority: 'polite' | 'assertive' = 'polite') => {
      const liveRegion = getLiveRegion();
      liveRegion.setAttribute('aria-live', priority);

      // Clear and then set the message to ensure it's announced
      liveRegion.textContent = '';
      setTimeout(() => {
        liveRegion.textContent = message;
      }, 100);
    },
    [getLiveRegion]
  );

  // Focus management utilities
  const focusElement = useCallback(
    (element: HTMLElement | string) => {
      const target =
        typeof element === 'string' ? (document.querySelector(element) as HTMLElement) : element;

      if (target && manageFocus) {
        target.focus();

        // Announce focus change for screen readers
        const label =
          target.getAttribute('aria-label') ||
          target.getAttribute('title') ||
          target.textContent ||
          'Element';
        announce(`Focused on ${label}`, 'polite');
      }
    },
    [manageFocus, announce]
  );

  // Focus trap implementation
  const createFocusTrap = useCallback(
    (container: HTMLElement, options: FocusTrapOptions = {}) => {
      if (!trapFocus) return () => {};

      const focusableElements = container.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      ) as NodeListOf<HTMLElement>;

      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];

      // Store previous focus
      previousFocusRef.current = document.activeElement as HTMLElement;

      // Focus initial element
      if (options.initialFocus) {
        const initialTarget =
          typeof options.initialFocus === 'string'
            ? (container.querySelector(options.initialFocus) as HTMLElement)
            : options.initialFocus;
        initialTarget?.focus();
      } else {
        firstElement?.focus();
      }

      const handleTabKey = (e: KeyboardEvent) => {
        if (e.key !== 'Tab') return;

        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            e.preventDefault();
            lastElement?.focus();
          }
        } else {
          if (document.activeElement === lastElement) {
            e.preventDefault();
            firstElement?.focus();
          }
        }
      };

      const handleEscapeKey = (e: KeyboardEvent) => {
        if (e.key === 'Escape') {
          e.preventDefault();
          returnFocus();
        }
      };

      const returnFocus = () => {
        if (options.returnFocus) {
          options.returnFocus.focus();
        } else if (previousFocusRef.current) {
          previousFocusRef.current.focus();
        }
      };

      container.addEventListener('keydown', handleTabKey);
      container.addEventListener('keydown', handleEscapeKey);

      return () => {
        container.removeEventListener('keydown', handleTabKey);
        container.removeEventListener('keydown', handleEscapeKey);
        returnFocus();
      };
    },
    [trapFocus]
  );

  // Keyboard navigation helpers
  const addKeyboardNavigation = useCallback(
    (
      container: HTMLElement,
      options: {
        arrowKeys?: boolean;
        homeEnd?: boolean;
        typeAhead?: boolean;
      } = {}
    ) => {
      if (!keyboardNavigation) return () => {};

      const { arrowKeys = true, homeEnd = true, typeAhead = false } = options;
      let typeAheadString = '';
      let typeAheadTimeout: NodeJS.Timeout;

      const handleKeyDown = (e: KeyboardEvent) => {
        const focusableElements = Array.from(
          container.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
          )
        ) as HTMLElement[];

        const currentIndex = focusableElements.indexOf(document.activeElement as HTMLElement);

        switch (e.key) {
          case 'ArrowDown':
          case 'ArrowRight':
            if (arrowKeys) {
              e.preventDefault();
              const nextIndex = (currentIndex + 1) % focusableElements.length;
              focusableElements[nextIndex]?.focus();
            }
            break;

          case 'ArrowUp':
          case 'ArrowLeft':
            if (arrowKeys) {
              e.preventDefault();
              const prevIndex =
                currentIndex === 0 ? focusableElements.length - 1 : currentIndex - 1;
              focusableElements[prevIndex]?.focus();
            }
            break;

          case 'Home':
            if (homeEnd) {
              e.preventDefault();
              focusableElements[0]?.focus();
            }
            break;

          case 'End':
            if (homeEnd) {
              e.preventDefault();
              focusableElements[focusableElements.length - 1]?.focus();
            }
            break;

          default:
            if (typeAhead && e.key.length === 1) {
              clearTimeout(typeAheadTimeout);
              typeAheadString += e.key.toLowerCase();

              const matchingElement = focusableElements.find(el =>
                el.textContent?.toLowerCase().startsWith(typeAheadString)
              );

              if (matchingElement) {
                matchingElement.focus();
              }

              typeAheadTimeout = setTimeout(() => {
                typeAheadString = '';
              }, 1000);
            }
            break;
        }
      };

      container.addEventListener('keydown', handleKeyDown);
      return () => {
        container.removeEventListener('keydown', handleKeyDown);
        clearTimeout(typeAheadTimeout);
      };
    },
    [keyboardNavigation]
  );

  // Skip link functionality
  const addSkipLinks = useCallback(() => {
    const skipLinks = [
      { href: '#main-content', text: 'Skip to main content' },
      { href: '#navigation', text: 'Skip to navigation' },
      { href: '#sidebar', text: 'Skip to sidebar' },
    ];

    const skipContainer = document.createElement('div');
    skipContainer.className = 'skip-links';
    skipContainer.style.position = 'absolute';
    skipContainer.style.top = '-40px';
    skipContainer.style.left = '6px';
    skipContainer.style.zIndex = '1000';

    skipLinks.forEach(link => {
      const skipLink = document.createElement('a');
      skipLink.href = link.href;
      skipLink.textContent = link.text;
      skipLink.className = 'skip-link';
      skipLink.style.position = 'absolute';
      skipLink.style.padding = '8px';
      skipLink.style.backgroundColor = '#000';
      skipLink.style.color = '#fff';
      skipLink.style.textDecoration = 'none';
      skipLink.style.borderRadius = '4px';
      skipLink.style.transform = 'translateY(-100%)';
      skipLink.style.transition = 'transform 0.3s';

      skipLink.addEventListener('focus', () => {
        skipLink.style.transform = 'translateY(0)';
      });

      skipLink.addEventListener('blur', () => {
        skipLink.style.transform = 'translateY(-100%)';
      });

      skipContainer.appendChild(skipLink);
    });

    document.body.insertBefore(skipContainer, document.body.firstChild);

    return () => {
      skipContainer.remove();
    };
  }, []);

  // Page change announcements
  useEffect(() => {
    if (announcePageChanges && currentPage) {
      announce(`Navigated to ${currentPage} page`);
    }
  }, [currentPage, announcePageChanges, announce]);

  // Error announcements
  useEffect(() => {
    if (announceErrors && globalError) {
      announce(`Error: ${globalError}`, 'assertive');
    }
  }, [globalError, announceErrors, announce]);

  // Loading announcements
  useEffect(() => {
    if (announceLoading) {
      if (globalLoading) {
        announce('Loading content, please wait', 'polite');
      }
    }
  }, [globalLoading, announceLoading, announce]);

  // Apply high contrast mode
  useEffect(() => {
    if (preferences.highContrast) {
      document.body.classList.add('high-contrast');
    } else {
      document.body.classList.remove('high-contrast');
    }
  }, [preferences.highContrast]);

  // Apply reduced motion
  useEffect(() => {
    if (preferences.reducedMotion) {
      document.body.classList.add('reduced-motion');
    } else {
      document.body.classList.remove('reduced-motion');
    }
  }, [preferences.reducedMotion]);

  // Initialize skip links
  useEffect(() => {
    const cleanup = addSkipLinks();
    return cleanup;
  }, [addSkipLinks]);

  return {
    announce,
    focusElement,
    createFocusTrap,
    addKeyboardNavigation,
    preferences,
  };
};

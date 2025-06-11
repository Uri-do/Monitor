import { useState, useEffect, useCallback } from 'react';
import { useAppStore } from '@/stores/appStore';

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

interface PWAState {
  isInstallable: boolean;
  isInstalled: boolean;
  isOnline: boolean;
  isUpdateAvailable: boolean;
  registration: ServiceWorkerRegistration | null;
  installPrompt: BeforeInstallPromptEvent | null;
}

interface PWAActions {
  install: () => Promise<boolean>;
  update: () => Promise<void>;
  skipWaiting: () => Promise<void>;
  unregister: () => Promise<boolean>;
  showInstallPrompt: () => Promise<boolean>;
  requestBackgroundSync: (tag: string) => Promise<void>;
  subscribeToPushNotifications: () => Promise<PushSubscription | null>;
}

/**
 * Advanced PWA hook with comprehensive service worker management
 */
export const usePWA = (): PWAState & PWAActions => {
  const [state, setState] = useState<PWAState>({
    isInstallable: false,
    isInstalled: false,
    isOnline: navigator.onLine,
    isUpdateAvailable: false,
    registration: null,
    installPrompt: null,
  });

  const setOnlineStatus = useAppStore(state => state.setOnlineStatus);
  const addError = useAppStore(state => state.addError);

  // Register service worker
  const registerServiceWorker = useCallback(async () => {
    if ('serviceWorker' in navigator) {
      try {
        const registration = await navigator.serviceWorker.register('/sw.js', {
          scope: '/',
        });

        console.log('[PWA] Service Worker registered:', registration);

        // Check for updates
        registration.addEventListener('updatefound', () => {
          const newWorker = registration.installing;
          if (newWorker) {
            newWorker.addEventListener('statechange', () => {
              if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                setState(prev => ({ ...prev, isUpdateAvailable: true }));
                console.log('[PWA] New content is available; please refresh.');
              }
            });
          }
        });

        setState(prev => ({ ...prev, registration }));
        return registration;
      } catch (error) {
        console.error('[PWA] Service Worker registration failed:', error);
        addError('Failed to register service worker');
        return null;
      }
    }
    return null;
  }, [addError]);

  // Install PWA
  const install = useCallback(async (): Promise<boolean> => {
    if (!state.installPrompt) {
      console.log('[PWA] No install prompt available');
      return false;
    }

    try {
      await state.installPrompt.prompt();
      const choiceResult = await state.installPrompt.userChoice;

      if (choiceResult.outcome === 'accepted') {
        console.log('[PWA] User accepted the install prompt');
        setState(prev => ({
          ...prev,
          isInstalled: true,
          isInstallable: false,
          installPrompt: null,
        }));
        return true;
      } else {
        console.log('[PWA] User dismissed the install prompt');
        setState(prev => ({ ...prev, installPrompt: null }));
        return false;
      }
    } catch (error) {
      console.error('[PWA] Install failed:', error);
      addError('Failed to install PWA');
      return false;
    }
  }, [state.installPrompt, addError]);

  // Update service worker
  const update = useCallback(async (): Promise<void> => {
    if (!state.registration) {
      console.log('[PWA] No service worker registration available');
      return;
    }

    try {
      await state.registration.update();
      console.log('[PWA] Service Worker updated');
    } catch (error) {
      console.error('[PWA] Update failed:', error);
      addError('Failed to update service worker');
    }
  }, [state.registration, addError]);

  // Skip waiting for new service worker
  const skipWaiting = useCallback(async (): Promise<void> => {
    if (!state.registration || !state.registration.waiting) {
      return;
    }

    try {
      state.registration.waiting.postMessage({ type: 'SKIP_WAITING' });

      // Listen for controlling change
      navigator.serviceWorker.addEventListener('controllerchange', () => {
        window.location.reload();
      });

      setState(prev => ({ ...prev, isUpdateAvailable: false }));
      console.log('[PWA] Skipped waiting for new service worker');
    } catch (error) {
      console.error('[PWA] Skip waiting failed:', error);
      addError('Failed to activate new service worker');
    }
  }, [state.registration, addError]);

  // Unregister service worker
  const unregister = useCallback(async (): Promise<boolean> => {
    if (!state.registration) {
      return false;
    }

    try {
      const result = await state.registration.unregister();
      if (result) {
        setState(prev => ({
          ...prev,
          registration: null,
          isUpdateAvailable: false,
        }));
        console.log('[PWA] Service Worker unregistered');
      }
      return result;
    } catch (error) {
      console.error('[PWA] Unregister failed:', error);
      addError('Failed to unregister service worker');
      return false;
    }
  }, [state.registration, addError]);

  // Show install prompt
  const showInstallPrompt = useCallback(async (): Promise<boolean> => {
    return install();
  }, [install]);

  // Setup event listeners
  useEffect(() => {
    // Online/offline detection
    const handleOnline = () => {
      setState(prev => ({ ...prev, isOnline: true }));
      setOnlineStatus(true);
      console.log('[PWA] App is online');
    };

    const handleOffline = () => {
      setState(prev => ({ ...prev, isOnline: false }));
      setOnlineStatus(false);
      console.log('[PWA] App is offline');
    };

    // Install prompt detection
    const handleBeforeInstallPrompt = (e: Event) => {
      e.preventDefault();
      const installEvent = e as BeforeInstallPromptEvent;
      setState(prev => ({
        ...prev,
        isInstallable: true,
        installPrompt: installEvent,
      }));
      console.log('[PWA] Install prompt available');
    };

    // App installed detection
    const handleAppInstalled = () => {
      setState(prev => ({
        ...prev,
        isInstalled: true,
        isInstallable: false,
        installPrompt: null,
      }));
      console.log('[PWA] App was installed');
    };

    // Service worker message handling
    const handleServiceWorkerMessage = (event: MessageEvent) => {
      if (event.data && event.data.type) {
        switch (event.data.type) {
          case 'SW_UPDATE_AVAILABLE':
            setState(prev => ({ ...prev, isUpdateAvailable: true }));
            break;
          case 'SW_OFFLINE_READY':
            console.log('[PWA] App ready to work offline');
            break;
          case 'SW_CACHED':
            console.log('[PWA] Content has been cached for offline use');
            break;
        }
      }
    };

    // Add event listeners
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.addEventListener('appinstalled', handleAppInstalled);
    navigator.serviceWorker?.addEventListener('message', handleServiceWorkerMessage);

    // Check if app is already installed
    if (window.matchMedia('(display-mode: standalone)').matches) {
      setState(prev => ({ ...prev, isInstalled: true }));
    }

    // Register service worker
    registerServiceWorker();

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
      window.removeEventListener('appinstalled', handleAppInstalled);
      navigator.serviceWorker?.removeEventListener('message', handleServiceWorkerMessage);
    };
  }, [registerServiceWorker, setOnlineStatus]);

  // Background sync for offline actions
  const requestBackgroundSync = useCallback(async (tag: string) => {
    if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
      try {
        const registration = await navigator.serviceWorker.ready;
        // Type assertion for experimental Background Sync API
        const syncManager = (registration as any).sync;
        if (syncManager && typeof syncManager.register === 'function') {
          await syncManager.register(tag);
          console.log(`[PWA] Background sync registered: ${tag}`);
        } else {
          console.warn('[PWA] Background sync not supported');
        }
      } catch (error) {
        console.error('[PWA] Background sync registration failed:', error);
      }
    }
  }, []);

  // Push notification subscription
  const subscribeToPushNotifications = useCallback(async () => {
    if (!('Notification' in window) || !state.registration) {
      return null;
    }

    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        console.log('[PWA] Notification permission denied');
        return null;
      }

      const subscription = await state.registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: process.env.VITE_VAPID_PUBLIC_KEY,
      });

      console.log('[PWA] Push notification subscription created');
      return subscription;
    } catch (error) {
      console.error('[PWA] Push notification subscription failed:', error);
      addError('Failed to subscribe to push notifications');
      return null;
    }
  }, [state.registration, addError]);

  return {
    // State
    ...state,

    // Actions
    install,
    update,
    skipWaiting,
    unregister,
    showInstallPrompt,

    // Additional utilities
    requestBackgroundSync,
    subscribeToPushNotifications,
  };
};

import { create } from 'zustand';
import { devtools, persist, subscribeWithSelector } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';

// User preferences and settings
interface UserPreferences {
  theme: 'light' | 'dark' | 'auto';
  language: string;
  timezone: string;
  dateFormat: string;
  numberFormat: string;
  dashboardLayout: 'grid' | 'list';
  tablePageSize: number;
  autoRefresh: boolean;
  refreshInterval: number;
  notifications: {
    desktop: boolean;
    email: boolean;
    sound: boolean;
  };
  accessibility: {
    highContrast: boolean;
    reducedMotion: boolean;
    screenReader: boolean;
  };
}

// Application state
interface AppState {
  // UI State
  sidebarOpen: boolean;
  sidebarCollapsed: boolean;
  currentPage: string;
  breadcrumbs: Array<{ label: string; href?: string }>;

  // User preferences
  preferences: UserPreferences;

  // Performance monitoring
  performance: {
    renderTimes: number[];
    apiCallCount: number;
    errorCount: number;
    lastUpdate: Date;
  };

  // Offline state
  isOnline: boolean;
  offlineActions: Array<{
    id: string;
    type: string;
    payload: any;
    timestamp: Date;
  }>;

  // Real-time connection state
  connectionState: 'connected' | 'disconnected' | 'reconnecting';
  lastConnectionTime: Date | null;

  // Global loading states
  globalLoading: boolean;
  loadingOperations: Set<string>;

  // Error handling
  globalError: string | null;
  errorHistory: Array<{
    id: string;
    message: string;
    timestamp: Date;
    context?: any;
  }>;
}

// Actions interface
interface AppActions {
  // UI Actions
  setSidebarOpen: (open: boolean) => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  setCurrentPage: (page: string) => void;
  setBreadcrumbs: (breadcrumbs: Array<{ label: string; href?: string }>) => void;

  // Preferences Actions
  updatePreferences: (preferences: Partial<UserPreferences>) => void;
  resetPreferences: () => void;

  // Performance Actions
  addRenderTime: (time: number) => void;
  incrementApiCallCount: () => void;
  incrementErrorCount: () => void;
  resetPerformanceMetrics: () => void;

  // Offline Actions
  setOnlineStatus: (online: boolean) => void;
  addOfflineAction: (action: { type: string; payload: any }) => void;
  clearOfflineActions: () => void;

  // Connection Actions
  setConnectionState: (state: 'connected' | 'disconnected' | 'reconnecting') => void;
  updateLastConnectionTime: () => void;

  // Loading Actions
  setGlobalLoading: (loading: boolean) => void;
  addLoadingOperation: (operation: string) => void;
  removeLoadingOperation: (operation: string) => void;

  // Error Actions
  setGlobalError: (error: string | null) => void;
  addError: (message: string, context?: any) => void;
  clearErrors: () => void;

  // Utility Actions
  resetStore: () => void;
}

// Default preferences
const defaultPreferences: UserPreferences = {
  theme: 'auto',
  language: 'en',
  timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  dateFormat: 'MM/dd/yyyy',
  numberFormat: 'en-US',
  dashboardLayout: 'grid',
  tablePageSize: 25,
  autoRefresh: true,
  refreshInterval: 30000, // 30 seconds
  notifications: {
    desktop: true,
    email: true,
    sound: false,
  },
  accessibility: {
    highContrast: false,
    reducedMotion: false,
    screenReader: false,
  },
};

// Initial state
const initialState: AppState = {
  sidebarOpen: true,
  sidebarCollapsed: false,
  currentPage: '',
  breadcrumbs: [],
  preferences: defaultPreferences,
  performance: {
    renderTimes: [],
    apiCallCount: 0,
    errorCount: 0,
    lastUpdate: new Date(),
  },
  isOnline: navigator.onLine,
  offlineActions: [],
  connectionState: 'disconnected',
  lastConnectionTime: null,
  globalLoading: false,
  loadingOperations: new Set(),
  globalError: null,
  errorHistory: [],
};

// Create the store with middleware
export const useAppStore = create<AppState & AppActions>()(
  devtools(
    persist(
      subscribeWithSelector(
        immer((set, get) => ({
          ...initialState,

          // UI Actions
          setSidebarOpen: open =>
            set(state => {
              state.sidebarOpen = open;
            }),

          setSidebarCollapsed: collapsed =>
            set(state => {
              state.sidebarCollapsed = collapsed;
            }),

          setCurrentPage: page =>
            set(state => {
              state.currentPage = page;
            }),

          setBreadcrumbs: breadcrumbs =>
            set(state => {
              state.breadcrumbs = breadcrumbs;
            }),

          // Preferences Actions
          updatePreferences: preferences =>
            set(state => {
              Object.assign(state.preferences, preferences);
            }),

          resetPreferences: () =>
            set(state => {
              state.preferences = defaultPreferences;
            }),

          // Performance Actions
          addRenderTime: time =>
            set(state => {
              state.performance.renderTimes.push(time);
              // Keep only last 100 render times
              if (state.performance.renderTimes.length > 100) {
                state.performance.renderTimes = state.performance.renderTimes.slice(-100);
              }
              state.performance.lastUpdate = new Date();
            }),

          incrementApiCallCount: () =>
            set(state => {
              state.performance.apiCallCount++;
              state.performance.lastUpdate = new Date();
            }),

          incrementErrorCount: () =>
            set(state => {
              state.performance.errorCount++;
              state.performance.lastUpdate = new Date();
            }),

          resetPerformanceMetrics: () =>
            set(state => {
              state.performance = {
                renderTimes: [],
                apiCallCount: 0,
                errorCount: 0,
                lastUpdate: new Date(),
              };
            }),

          // Offline Actions
          setOnlineStatus: online =>
            set(state => {
              state.isOnline = online;
            }),

          addOfflineAction: action =>
            set(state => {
              state.offlineActions.push({
                id: crypto.randomUUID(),
                ...action,
                timestamp: new Date(),
              });
            }),

          clearOfflineActions: () =>
            set(state => {
              state.offlineActions = [];
            }),

          // Connection Actions
          setConnectionState: connectionState =>
            set(state => {
              state.connectionState = connectionState;
              if (connectionState === 'connected') {
                state.lastConnectionTime = new Date();
              }
            }),

          updateLastConnectionTime: () =>
            set(state => {
              state.lastConnectionTime = new Date();
            }),

          // Loading Actions
          setGlobalLoading: loading =>
            set(state => {
              state.globalLoading = loading;
            }),

          addLoadingOperation: operation =>
            set(state => {
              state.loadingOperations.add(operation);
              state.globalLoading = state.loadingOperations.size > 0;
            }),

          removeLoadingOperation: operation =>
            set(state => {
              state.loadingOperations.delete(operation);
              state.globalLoading = state.loadingOperations.size > 0;
            }),

          // Error Actions
          setGlobalError: error =>
            set(state => {
              state.globalError = error;
            }),

          addError: (message, context) =>
            set(state => {
              const error = {
                id: crypto.randomUUID(),
                message,
                timestamp: new Date(),
                context,
              };
              state.errorHistory.push(error);
              // Keep only last 50 errors
              if (state.errorHistory.length > 50) {
                state.errorHistory = state.errorHistory.slice(-50);
              }
              state.globalError = message;
            }),

          clearErrors: () =>
            set(state => {
              state.globalError = null;
              state.errorHistory = [];
            }),

          // Utility Actions
          resetStore: () => set(initialState),
        }))
      ),
      {
        name: 'monitoring-grid-app-store',
        partialize: state => ({
          preferences: state.preferences,
          sidebarCollapsed: state.sidebarCollapsed,
        }),
      }
    ),
    {
      name: 'MonitoringGrid App Store',
    }
  )
);

// Selectors for better performance
export const useAppSelectors = {
  // UI Selectors
  sidebar: () =>
    useAppStore(state => ({
      open: state.sidebarOpen,
      collapsed: state.sidebarCollapsed,
    })),

  navigation: () =>
    useAppStore(state => ({
      currentPage: state.currentPage,
      breadcrumbs: state.breadcrumbs,
    })),

  // Performance Selectors
  performance: () => useAppStore(state => state.performance),

  averageRenderTime: () =>
    useAppStore(state => {
      const times = state.performance.renderTimes;
      return times.length > 0 ? times.reduce((a, b) => a + b, 0) / times.length : 0;
    }),

  // Connection Selectors
  connectionStatus: () =>
    useAppStore(state => ({
      isOnline: state.isOnline,
      connectionState: state.connectionState,
      lastConnectionTime: state.lastConnectionTime,
    })),

  // Loading Selectors
  loading: () =>
    useAppStore(state => ({
      global: state.globalLoading,
      operations: Array.from(state.loadingOperations),
    })),

  // Error Selectors
  errors: () =>
    useAppStore(state => ({
      current: state.globalError,
      history: state.errorHistory,
    })),

  // Preferences Selectors
  preferences: () => useAppStore(state => state.preferences),
  theme: () => useAppStore(state => state.preferences.theme),
  notifications: () => useAppStore(state => state.preferences.notifications),
  accessibility: () => useAppStore(state => state.preferences.accessibility),
};

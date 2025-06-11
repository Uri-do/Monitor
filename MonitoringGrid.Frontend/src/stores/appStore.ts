import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';

// User preferences and settings
interface UserPreferences {
  theme: 'light' | 'dark' | 'auto';
  language: string;
  tablePageSize: number;
  autoRefresh: boolean;
  refreshInterval: number;
}

// Application state
interface AppState {
  // UI State
  sidebarOpen: boolean;
  sidebarCollapsed: boolean;
  currentPage: string;

  // User preferences
  preferences: UserPreferences;

  // Real-time connection state
  connectionState: 'connected' | 'disconnected' | 'reconnecting';

  // Global loading states
  globalLoading: boolean;

  // Error handling
  globalError: string | null;
}

// Actions interface
interface AppActions {
  // UI Actions
  setSidebarOpen: (open: boolean) => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  setCurrentPage: (page: string) => void;

  // Preferences Actions
  updatePreferences: (preferences: Partial<UserPreferences>) => void;

  // Connection Actions
  setConnectionState: (state: 'connected' | 'disconnected' | 'reconnecting') => void;

  // Loading Actions
  setGlobalLoading: (loading: boolean) => void;

  // Error Actions
  setGlobalError: (error: string | null) => void;
}

// Default preferences
const defaultPreferences: UserPreferences = {
  theme: 'auto',
  language: 'en',
  tablePageSize: 25,
  autoRefresh: true,
  refreshInterval: 30000, // 30 seconds
};

// Initial state
const initialState: AppState = {
  sidebarOpen: true,
  sidebarCollapsed: false,
  currentPage: '',
  preferences: defaultPreferences,
  connectionState: 'disconnected',
  globalLoading: false,
  globalError: null,
};

// Create the store with middleware
export const useAppStore = create<AppState & AppActions>()(
  devtools(
    persist(
      immer(set => ({
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

        // Preferences Actions
        updatePreferences: preferences =>
          set(state => {
            Object.assign(state.preferences, preferences);
          }),

        // Connection Actions
        setConnectionState: connectionState =>
          set(state => {
            state.connectionState = connectionState;
          }),

        // Loading Actions
        setGlobalLoading: loading =>
          set(state => {
            state.globalLoading = loading;
          }),

        // Error Actions
        setGlobalError: error =>
          set(state => {
            state.globalError = error;
          }),
      })),
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

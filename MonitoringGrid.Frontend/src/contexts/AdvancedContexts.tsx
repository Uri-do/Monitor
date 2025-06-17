import React, { createContext, useContext, useReducer, useCallback, useMemo, useRef, useEffect } from 'react';
import { useOptimizedCallback } from '@/utils/componentOptimization';

/**
 * Advanced Context Patterns
 * Enterprise-grade context management with optimizations
 */

// Advanced notification system
export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  duration?: number;
  persistent?: boolean;
  actions?: Array<{
    label: string;
    onClick: () => void;
    variant?: 'text' | 'outlined' | 'contained';
  }>;
  timestamp: Date;
}

interface NotificationState {
  notifications: Notification[];
  maxNotifications: number;
}

type NotificationAction =
  | { type: 'ADD_NOTIFICATION'; payload: Omit<Notification, 'id' | 'timestamp'> }
  | { type: 'REMOVE_NOTIFICATION'; payload: string }
  | { type: 'CLEAR_ALL' }
  | { type: 'SET_MAX_NOTIFICATIONS'; payload: number };

const notificationReducer = (state: NotificationState, action: NotificationAction): NotificationState => {
  switch (action.type) {
    case 'ADD_NOTIFICATION': {
      const newNotification: Notification = {
        ...action.payload,
        id: `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        timestamp: new Date(),
      };

      const notifications = [newNotification, ...state.notifications];
      
      // Limit notifications
      if (notifications.length > state.maxNotifications) {
        notifications.splice(state.maxNotifications);
      }

      return { ...state, notifications };
    }
    case 'REMOVE_NOTIFICATION':
      return {
        ...state,
        notifications: state.notifications.filter(n => n.id !== action.payload),
      };
    case 'CLEAR_ALL':
      return { ...state, notifications: [] };
    case 'SET_MAX_NOTIFICATIONS':
      return { ...state, maxNotifications: action.payload };
    default:
      return state;
  }
};

interface NotificationContextType {
  notifications: Notification[];
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp'>) => string;
  removeNotification: (id: string) => void;
  clearAll: () => void;
  setMaxNotifications: (max: number) => void;
  // Convenience methods
  success: (title: string, message: string, options?: Partial<Notification>) => string;
  error: (title: string, message: string, options?: Partial<Notification>) => string;
  warning: (title: string, message: string, options?: Partial<Notification>) => string;
  info: (title: string, message: string, options?: Partial<Notification>) => string;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(notificationReducer, {
    notifications: [],
    maxNotifications: 5,
  });

  const timeoutRefs = useRef<Map<string, NodeJS.Timeout>>(new Map());

  const addNotification = useOptimizedCallback((notification: Omit<Notification, 'id' | 'timestamp'>) => {
    dispatch({ type: 'ADD_NOTIFICATION', payload: notification });
    
    // Generate ID for return value
    const id = `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    // Auto-remove after duration
    if (!notification.persistent && notification.duration !== 0) {
      const duration = notification.duration || 5000;
      const timeoutId = setTimeout(() => {
        removeNotification(id);
      }, duration);
      
      timeoutRefs.current.set(id, timeoutId);
    }
    
    return id;
  }, []);

  const removeNotification = useOptimizedCallback((id: string) => {
    dispatch({ type: 'REMOVE_NOTIFICATION', payload: id });
    
    // Clear timeout if exists
    const timeoutId = timeoutRefs.current.get(id);
    if (timeoutId) {
      clearTimeout(timeoutId);
      timeoutRefs.current.delete(id);
    }
  }, []);

  const clearAll = useOptimizedCallback(() => {
    dispatch({ type: 'CLEAR_ALL' });
    
    // Clear all timeouts
    timeoutRefs.current.forEach(timeoutId => clearTimeout(timeoutId));
    timeoutRefs.current.clear();
  }, []);

  const setMaxNotifications = useOptimizedCallback((max: number) => {
    dispatch({ type: 'SET_MAX_NOTIFICATIONS', payload: max });
  }, []);

  // Convenience methods
  const success = useOptimizedCallback((title: string, message: string, options?: Partial<Notification>) => {
    return addNotification({ ...options, type: 'success', title, message });
  }, [addNotification]);

  const error = useOptimizedCallback((title: string, message: string, options?: Partial<Notification>) => {
    return addNotification({ ...options, type: 'error', title, message, persistent: true });
  }, [addNotification]);

  const warning = useOptimizedCallback((title: string, message: string, options?: Partial<Notification>) => {
    return addNotification({ ...options, type: 'warning', title, message });
  }, [addNotification]);

  const info = useOptimizedCallback((title: string, message: string, options?: Partial<Notification>) => {
    return addNotification({ ...options, type: 'info', title, message });
  }, [addNotification]);

  // Cleanup timeouts on unmount
  useEffect(() => {
    return () => {
      timeoutRefs.current.forEach(timeoutId => clearTimeout(timeoutId));
    };
  }, []);

  const contextValue = useMemo(() => ({
    notifications: state.notifications,
    addNotification,
    removeNotification,
    clearAll,
    setMaxNotifications,
    success,
    error,
    warning,
    info,
  }), [
    state.notifications,
    addNotification,
    removeNotification,
    clearAll,
    setMaxNotifications,
    success,
    error,
    warning,
    info,
  ]);

  return (
    <NotificationContext.Provider value={contextValue}>
      {children}
    </NotificationContext.Provider>
  );
};

export const useNotifications = (): NotificationContextType => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

// Advanced command system for undo/redo operations
export interface Command {
  id: string;
  name: string;
  execute: () => void | Promise<void>;
  undo: () => void | Promise<void>;
  timestamp: Date;
}

interface CommandState {
  history: Command[];
  currentIndex: number;
  maxHistorySize: number;
}

type CommandAction =
  | { type: 'EXECUTE_COMMAND'; payload: Omit<Command, 'id' | 'timestamp'> }
  | { type: 'UNDO' }
  | { type: 'REDO' }
  | { type: 'CLEAR_HISTORY' }
  | { type: 'SET_MAX_HISTORY_SIZE'; payload: number };

const commandReducer = (state: CommandState, action: CommandAction): CommandState => {
  switch (action.type) {
    case 'EXECUTE_COMMAND': {
      const command: Command = {
        ...action.payload,
        id: `command-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        timestamp: new Date(),
      };

      // Remove any commands after current index (when executing new command after undo)
      const newHistory = state.history.slice(0, state.currentIndex + 1);
      newHistory.push(command);

      // Limit history size
      if (newHistory.length > state.maxHistorySize) {
        newHistory.shift();
      }

      return {
        ...state,
        history: newHistory,
        currentIndex: newHistory.length - 1,
      };
    }
    case 'UNDO':
      return {
        ...state,
        currentIndex: Math.max(-1, state.currentIndex - 1),
      };
    case 'REDO':
      return {
        ...state,
        currentIndex: Math.min(state.history.length - 1, state.currentIndex + 1),
      };
    case 'CLEAR_HISTORY':
      return {
        ...state,
        history: [],
        currentIndex: -1,
      };
    case 'SET_MAX_HISTORY_SIZE':
      return {
        ...state,
        maxHistorySize: action.payload,
      };
    default:
      return state;
  }
};

interface CommandContextType {
  canUndo: boolean;
  canRedo: boolean;
  currentCommand: Command | null;
  historySize: number;
  executeCommand: (command: Omit<Command, 'id' | 'timestamp'>) => Promise<void>;
  undo: () => Promise<void>;
  redo: () => Promise<void>;
  clearHistory: () => void;
  setMaxHistorySize: (size: number) => void;
}

const CommandContext = createContext<CommandContextType | undefined>(undefined);

export const CommandProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(commandReducer, {
    history: [],
    currentIndex: -1,
    maxHistorySize: 50,
  });

  const executeCommand = useOptimizedCallback(async (command: Omit<Command, 'id' | 'timestamp'>) => {
    try {
      await command.execute();
      dispatch({ type: 'EXECUTE_COMMAND', payload: command });
    } catch (error) {
      console.error('Command execution failed:', error);
      throw error;
    }
  }, []);

  const undo = useOptimizedCallback(async () => {
    if (state.currentIndex >= 0) {
      const command = state.history[state.currentIndex];
      try {
        await command.undo();
        dispatch({ type: 'UNDO' });
      } catch (error) {
        console.error('Command undo failed:', error);
        throw error;
      }
    }
  }, [state.currentIndex, state.history]);

  const redo = useOptimizedCallback(async () => {
    if (state.currentIndex < state.history.length - 1) {
      const command = state.history[state.currentIndex + 1];
      try {
        await command.execute();
        dispatch({ type: 'REDO' });
      } catch (error) {
        console.error('Command redo failed:', error);
        throw error;
      }
    }
  }, [state.currentIndex, state.history]);

  const clearHistory = useOptimizedCallback(() => {
    dispatch({ type: 'CLEAR_HISTORY' });
  }, []);

  const setMaxHistorySize = useOptimizedCallback((size: number) => {
    dispatch({ type: 'SET_MAX_HISTORY_SIZE', payload: size });
  }, []);

  const contextValue = useMemo(() => ({
    canUndo: state.currentIndex >= 0,
    canRedo: state.currentIndex < state.history.length - 1,
    currentCommand: state.currentIndex >= 0 ? state.history[state.currentIndex] : null,
    historySize: state.history.length,
    executeCommand,
    undo,
    redo,
    clearHistory,
    setMaxHistorySize,
  }), [
    state.currentIndex,
    state.history,
    executeCommand,
    undo,
    redo,
    clearHistory,
    setMaxHistorySize,
  ]);

  return (
    <CommandContext.Provider value={contextValue}>
      {children}
    </CommandContext.Provider>
  );
};

export const useCommands = (): CommandContextType => {
  const context = useContext(CommandContext);
  if (!context) {
    throw new Error('useCommands must be used within a CommandProvider');
  }
  return context;
};

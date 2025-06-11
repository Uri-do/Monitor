import { useState, useEffect, useCallback, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAppStore } from '@/stores/appStore';
import { useRealtime } from '@/contexts/RealtimeContext';

interface CollaborationUser {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  color: string;
  isOnline: boolean;
  lastSeen: Date;
  currentPage?: string;
  currentAction?: string;
}

interface CollaborationCursor {
  userId: string;
  x: number;
  y: number;
  timestamp: Date;
}

interface CollaborationEdit {
  id: string;
  userId: string;
  entityType: 'kpi' | 'alert' | 'contact' | 'user';
  entityId: string;
  field: string;
  oldValue: any;
  newValue: any;
  timestamp: Date;
  isConflict?: boolean;
}

interface CollaborationComment {
  id: string;
  userId: string;
  entityType: string;
  entityId: string;
  content: string;
  timestamp: Date;
  resolved: boolean;
  replies: CollaborationComment[];
}

interface UseCollaborationOptions {
  enableCursors?: boolean;
  enablePresence?: boolean;
  enableComments?: boolean;
  enableConflictDetection?: boolean;
  cursorUpdateInterval?: number;
}

/**
 * Advanced real-time collaboration hook
 */
export const useCollaboration = (options: UseCollaborationOptions = {}) => {
  const {
    enableCursors = true,
    enablePresence = true,
    enableComments = true,
    enableConflictDetection = true,
    cursorUpdateInterval = 100,
  } = options;

  const queryClient = useQueryClient();
  const { connection, isConnected } = useRealtime();
  const currentPage = useAppStore((state) => state.currentPage);
  const setConnectionState = useAppStore((state) => state.setConnectionState);

  const [activeUsers, setActiveUsers] = useState<CollaborationUser[]>([]);
  const [cursors, setCursors] = useState<Map<string, CollaborationCursor>>(new Map());
  const [activeEdits, setActiveEdits] = useState<CollaborationEdit[]>([]);
  const [comments, setComments] = useState<CollaborationComment[]>([]);
  const [conflicts, setConflicts] = useState<CollaborationEdit[]>([]);

  const cursorTimeoutRef = useRef<NodeJS.Timeout>();
  const lastCursorUpdate = useRef<Date>(new Date());

  // Generate user color
  const generateUserColor = useCallback((userId: string) => {
    const colors = [
      '#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4', '#FFEAA7',
      '#DDA0DD', '#98D8C8', '#F7DC6F', '#BB8FCE', '#85C1E9'
    ];
    const hash = userId.split('').reduce((a, b) => {
      a = ((a << 5) - a) + b.charCodeAt(0);
      return a & a;
    }, 0);
    return colors[Math.abs(hash) % colors.length];
  }, []);

  // Send cursor position
  const sendCursorPosition = useCallback((x: number, y: number) => {
    if (!enableCursors || !isConnected || !connection) return;

    const now = new Date();
    if (now.getTime() - lastCursorUpdate.current.getTime() < cursorUpdateInterval) {
      return;
    }

    lastCursorUpdate.current = now;
    connection.invoke('UpdateCursor', { x, y, page: currentPage });
  }, [enableCursors, isConnected, connection, currentPage, cursorUpdateInterval]);

  // Send presence update
  const sendPresenceUpdate = useCallback((action?: string) => {
    if (!enablePresence || !isConnected || !connection) return;

    connection.invoke('UpdatePresence', {
      page: currentPage,
      action,
      timestamp: new Date(),
    });
  }, [enablePresence, isConnected, connection, currentPage]);

  // Send edit notification
  const sendEditNotification = useCallback((edit: Omit<CollaborationEdit, 'id' | 'userId' | 'timestamp'>) => {
    if (!isConnected || !connection) return;

    const editWithMetadata = {
      ...edit,
      id: crypto.randomUUID(),
      timestamp: new Date(),
    };

    connection.invoke('NotifyEdit', editWithMetadata);
    setActiveEdits(prev => [...prev, editWithMetadata as CollaborationEdit]);
  }, [isConnected, connection]);

  // Add comment
  const addComment = useCallback((comment: Omit<CollaborationComment, 'id' | 'userId' | 'timestamp' | 'replies'>) => {
    if (!enableComments || !isConnected || !connection) return;

    const commentWithMetadata = {
      ...comment,
      id: crypto.randomUUID(),
      timestamp: new Date(),
      replies: [],
    };

    connection.invoke('AddComment', commentWithMetadata);
    setComments(prev => [...prev, commentWithMetadata as CollaborationComment]);
  }, [enableComments, isConnected, connection]);

  // Resolve comment
  const resolveComment = useCallback((commentId: string) => {
    if (!enableComments || !isConnected || !connection) return;

    connection.invoke('ResolveComment', commentId);
    setComments(prev => prev.map(comment => 
      comment.id === commentId ? { ...comment, resolved: true } : comment
    ));
  }, [enableComments, isConnected, connection]);

  // Detect conflicts
  const detectConflict = useCallback((edit: CollaborationEdit) => {
    if (!enableConflictDetection) return false;

    const recentEdits = activeEdits.filter(e => 
      e.entityType === edit.entityType &&
      e.entityId === edit.entityId &&
      e.field === edit.field &&
      e.userId !== edit.userId &&
      new Date().getTime() - e.timestamp.getTime() < 30000 // 30 seconds
    );

    return recentEdits.length > 0;
  }, [enableConflictDetection, activeEdits]);

  // Mouse move handler for cursor tracking
  const handleMouseMove = useCallback((event: MouseEvent) => {
    if (enableCursors) {
      clearTimeout(cursorTimeoutRef.current);
      cursorTimeoutRef.current = setTimeout(() => {
        sendCursorPosition(event.clientX, event.clientY);
      }, 16); // ~60fps
    }
  }, [enableCursors, sendCursorPosition]);

  // Setup SignalR event handlers
  useEffect(() => {
    if (!connection || !isConnected) return;

    // User presence events
    const handleUserJoined = (user: CollaborationUser) => {
      setActiveUsers(prev => {
        const existing = prev.find(u => u.id === user.id);
        if (existing) {
          return prev.map(u => u.id === user.id ? { ...user, isOnline: true } : u);
        }
        return [...prev, { ...user, color: generateUserColor(user.id), isOnline: true }];
      });
    };

    const handleUserLeft = (userId: string) => {
      setActiveUsers(prev => prev.map(u => 
        u.id === userId ? { ...u, isOnline: false, lastSeen: new Date() } : u
      ));
      setCursors(prev => {
        const newCursors = new Map(prev);
        newCursors.delete(userId);
        return newCursors;
      });
    };

    const handlePresenceUpdate = (userId: string, presence: { page: string; action?: string }) => {
      setActiveUsers(prev => prev.map(u => 
        u.id === userId ? { ...u, currentPage: presence.page, currentAction: presence.action } : u
      ));
    };

    // Cursor events
    const handleCursorUpdate = (userId: string, cursor: { x: number; y: number }) => {
      if (enableCursors) {
        setCursors(prev => new Map(prev.set(userId, {
          userId,
          x: cursor.x,
          y: cursor.y,
          timestamp: new Date(),
        })));
      }
    };

    // Edit events
    const handleEditNotification = (edit: CollaborationEdit) => {
      const isConflict = detectConflict(edit);
      const editWithConflict = { ...edit, isConflict };

      setActiveEdits(prev => [...prev, editWithConflict]);

      if (isConflict) {
        setConflicts(prev => [...prev, editWithConflict]);
      }

      // Invalidate related queries
      queryClient.invalidateQueries({ 
        queryKey: [edit.entityType.toLowerCase(), edit.entityId] 
      });
    };

    // Comment events
    const handleCommentAdded = (comment: CollaborationComment) => {
      if (enableComments) {
        setComments(prev => [...prev, comment]);
      }
    };

    const handleCommentResolved = (commentId: string) => {
      if (enableComments) {
        setComments(prev => prev.map(comment => 
          comment.id === commentId ? { ...comment, resolved: true } : comment
        ));
      }
    };

    // Register event handlers
    connection.on('UserJoined', handleUserJoined);
    connection.on('UserLeft', handleUserLeft);
    connection.on('PresenceUpdate', handlePresenceUpdate);
    connection.on('CursorUpdate', handleCursorUpdate);
    connection.on('EditNotification', handleEditNotification);
    connection.on('CommentAdded', handleCommentAdded);
    connection.on('CommentResolved', handleCommentResolved);

    return () => {
      connection.off('UserJoined', handleUserJoined);
      connection.off('UserLeft', handleUserLeft);
      connection.off('PresenceUpdate', handlePresenceUpdate);
      connection.off('CursorUpdate', handleCursorUpdate);
      connection.off('EditNotification', handleEditNotification);
      connection.off('CommentAdded', handleCommentAdded);
      connection.off('CommentResolved', handleCommentResolved);
    };
  }, [connection, isConnected, enableCursors, enableComments, detectConflict, generateUserColor, queryClient]);

  // Setup mouse tracking
  useEffect(() => {
    if (enableCursors) {
      document.addEventListener('mousemove', handleMouseMove);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        clearTimeout(cursorTimeoutRef.current);
      };
    }
  }, [enableCursors, handleMouseMove]);

  // Send presence update on page change
  useEffect(() => {
    sendPresenceUpdate();
  }, [currentPage, sendPresenceUpdate]);

  // Cleanup old cursors
  useEffect(() => {
    const interval = setInterval(() => {
      const now = new Date();
      setCursors(prev => {
        const newCursors = new Map();
        prev.forEach((cursor, userId) => {
          if (now.getTime() - cursor.timestamp.getTime() < 5000) { // 5 seconds
            newCursors.set(userId, cursor);
          }
        });
        return newCursors;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  return {
    // State
    activeUsers: activeUsers.filter(u => u.currentPage === currentPage),
    allActiveUsers: activeUsers,
    cursors: Array.from(cursors.values()),
    activeEdits,
    comments,
    conflicts,
    
    // Actions
    sendPresenceUpdate,
    sendEditNotification,
    addComment,
    resolveComment,
    
    // Utilities
    isCollaborationEnabled: isConnected,
    getUserColor: generateUserColor,
  };
};

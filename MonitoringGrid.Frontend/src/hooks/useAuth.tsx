import { useState, useEffect, useContext, createContext, ReactNode } from 'react';
import { AuthState, LoginRequest, RegisterRequest } from '@/types/auth';
import { authService } from '@/services/authService';
import { signalRService } from '@/services/signalRService';

interface AuthContextType extends AuthState {
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<string>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, setState] = useState<AuthState>({
    user: null,
    token: null,
    isAuthenticated: false,
    isLoading: true,
    error: null,
  });

  useEffect(() => {
    const initAuth = async () => {
      const token = authService.getToken();
      const refreshToken = authService.getRefreshToken();
      const storedUser = authService.getUser();

      if (token) {
        try {
          console.log('Attempting to validate existing token...');
          // Try to use stored user first, fallback to API call
          let user = storedUser;
          if (!user) {
            console.log('No stored user, fetching from API...');
            user = await authService.getCurrentUser();
          } else {
            console.log('Using stored user:', { userId: user.userId, username: user.username, roles: user.roles?.map(r => r.name) });
          }
          console.log('Token validation successful, user:', user);
          setState({
            user,
            token,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (error: any) {
          console.log('Token validation failed:', error);

          // If we have a refresh token, try to refresh instead of clearing immediately
          if (refreshToken && error.message?.includes('expired')) {
            try {
              console.log('Attempting to refresh expired token...');
              const response = await authService.refreshToken();

              if (response.accessToken) {
                console.log('Token refresh successful during initialization');

                // Get user info using the new token directly
                try {
                  const user = await authService.getCurrentUser(response.accessToken);
                  setState({
                    user: user,
                    token: response.accessToken,
                    isAuthenticated: true,
                    isLoading: false,
                    error: null,
                  });
                  return;
                } catch (userError) {
                  console.log('Failed to get user after token refresh:', userError);
                }
              }
            } catch (refreshError) {
              console.log('Token refresh failed during initialization:', refreshError);
            }
          }

          // Clear tokens and set unauthenticated state
          console.log('Clearing tokens and setting unauthenticated state');
          authService.clearToken();
          setState({
            user: null,
            token: null,
            isAuthenticated: false,
            isLoading: false,
            error: null,
          });
        }
      } else {
        console.log('No token found, user needs to login');
        setState({
          user: null,
          token: null,
          isAuthenticated: false,
          isLoading: false,
          error: null,
        });
      }
    };

    initAuth();
  }, []);

  const login = async (request: LoginRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const response = await authService.login(request);

      if (response.token && response.user) {
        authService.setToken(response.token.accessToken);
        authService.setRefreshToken(response.token.refreshToken);

        // Convert string roles to Role objects if needed
        const userWithRoles = {
          ...response.user,
          roles: response.user.roles?.map(role => {
            // If role is already an object, use it as is
            if (typeof role === 'object' && role.name) {
              return role;
            }
            // If role is a string, convert it to a Role object
            const roleName = typeof role === 'string' ? role : role.name || 'Unknown';
            return {
              roleId: `role-${roleName.toLowerCase()}`,
              name: roleName,
              description: `${roleName} role`,
              isSystemRole: true,
              isActive: true,
              permissions: []
            };
          }) || []
        };

        authService.setUser(userWithRoles);

        console.log('Storing user in auth context:', {
          userId: userWithRoles.userId,
          username: userWithRoles.username,
          roles: userWithRoles.roles?.map(r => r.name) || []
        });

        // Update SignalR with new token
        signalRService.updateAuthToken(response.token.accessToken);

        setState({
          user: userWithRoles,
          token: response.token.accessToken,
          isAuthenticated: true,
          isLoading: false,
          error: null,
        });
      } else {
        throw new Error(response.errorMessage || 'Login failed');
      }
    } catch (error) {
      setState({
        user: null,
        token: null,
        isAuthenticated: false,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Login failed',
      });
      throw error;
    }
  };

  const register = async (request: RegisterRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const response = await authService.register(request);

      if (response.isSuccess && response.data) {
        setState(prev => ({
          ...prev,
          isLoading: false,
          error: null,
        }));
      } else {
        throw new Error(response.message || response.errors?.join(', ') || 'Registration failed');
      }
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Registration failed',
      }));
      throw error;
    }
  };

  const logout = async () => {
    setState(prev => ({ ...prev, isLoading: true }));

    try {
      await authService.logout();
    } catch (error) {
      // Logout error handled silently
    }

    // Stop SignalR connection on logout
    await signalRService.stop();

    setState({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    });
  };

  const refreshToken = async () => {
    try {
      const response = await authService.refreshToken();
      const newToken = response.token?.accessToken || response.token || '';

      setState(prev => ({
        ...prev,
        user: response.user || prev.user,
        token: typeof newToken === 'string' ? newToken : null,
        error: null,
      }));

      return typeof newToken === 'string' ? newToken : '';
    } catch (error) {
      console.error('Token refresh failed in useAuth:', error);
      await logout();
      throw error;
    }
  };



  // Proactive token refresh - check every 5 minutes
  useEffect(() => {
    if (!state.isAuthenticated || !state.token) {
      return;
    }

    const checkTokenExpiration = () => {
      if (authService.shouldRefreshToken()) {
        console.log('Token needs refresh, refreshing proactively...');
        refreshToken().catch(error => {
          console.error('Proactive token refresh failed:', error);
        });
      }
    };

    // Check immediately
    checkTokenExpiration();

    // Set up interval to check every 5 minutes
    const interval = setInterval(checkTokenExpiration, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, [state.isAuthenticated, state.token]);

  const value: AuthContextType = {
    ...state,
    login,
    register,
    logout,
    refreshToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

# Authentication Logout Fix

## Problem Description

The application was logging users out when refreshing the page, causing a poor user experience. This issue occurred due to several problems in the authentication flow:

1. **Aggressive Token Clearing**: The axios response interceptor immediately cleared tokens and redirected to login on any 401 response, including during token validation.
2. **No Automatic Token Refresh**: Failed API calls due to expired tokens weren't automatically retried with refreshed tokens.
3. **Race Conditions**: Token validation and refresh attempts could interfere with each other.
4. **Poor Error Handling**: The authentication initialization didn't gracefully handle token expiration scenarios.

## Root Cause Analysis

### 1. API Interceptor Issues
- The response interceptor in `api.ts` was too aggressive in clearing tokens
- No retry mechanism for failed requests after token refresh
- No queuing system for concurrent requests during token refresh

### 2. Authentication Flow Problems
- Token validation in `useAuth` didn't attempt refresh on expiration
- No proactive token refresh before expiration
- Poor error handling during authentication initialization

### 3. Token Management Issues
- No client-side token expiration checking
- Refresh token logic wasn't robust enough
- No automatic token refresh scheduling

## Solution Implementation

### 1. Enhanced API Response Interceptor (`api.ts`)

**Added automatic token refresh with request queuing:**
```typescript
// Token refresh state management
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (error?: any) => void;
}> = [];

// Enhanced response interceptor with automatic token refresh
api.interceptors.response.use(
  response => response,
  async error => {
    const originalRequest = error.config;
    
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Queue concurrent requests
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return api(originalRequest);
        });
      }

      // Attempt token refresh and retry original request
      originalRequest._retry = true;
      isRefreshing = true;
      
      try {
        const refreshToken = localStorage.getItem('refresh_token');
        const response = await fetch('/api/auth/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken }),
        });

        const result = await response.json();
        const newToken = result.token?.accessToken || result.accessToken;
        
        // Update tokens and retry request
        localStorage.setItem('auth_token', newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        
        processQueue(null, newToken);
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        // Only clear tokens and redirect after refresh fails
        localStorage.removeItem('auth_token');
        localStorage.removeItem('refresh_token');
        window.location.href = '/login';
      } finally {
        isRefreshing = false;
      }
    }
    
    return Promise.reject(error);
  }
);
```

### 2. Improved Authentication Hook (`useAuth.tsx`)

**Enhanced initialization with token refresh:**
```typescript
useEffect(() => {
  const initAuth = async () => {
    const token = authService.getToken();
    const refreshToken = authService.getRefreshToken();
    
    if (token) {
      try {
        const user = await authService.getCurrentUser();
        setState({ user, token, isAuthenticated: true, isLoading: false, error: null });
      } catch (error: any) {
        // Try refresh if token is expired and refresh token exists
        if (refreshToken && error.message?.includes('expired')) {
          try {
            const response = await authService.refreshToken();
            setState({
              user: response.user,
              token: response.token.accessToken,
              isAuthenticated: true,
              isLoading: false,
              error: null,
            });
            return;
          } catch (refreshError) {
            console.log('Token refresh failed during initialization:', refreshError);
          }
        }
        
        // Clear tokens only after refresh attempts fail
        authService.clearToken();
        setState({ user: null, token: null, isAuthenticated: false, isLoading: false, error: null });
      }
    } else {
      setState(prev => ({ ...prev, isLoading: false }));
    }
  };

  initAuth();
}, []);
```

**Added proactive token refresh:**
```typescript
// Proactive token refresh - check every 5 minutes
useEffect(() => {
  if (!state.isAuthenticated || !state.token) return;

  const checkTokenExpiration = () => {
    if (authService.shouldRefreshToken()) {
      refreshToken().catch(error => {
        console.error('Proactive token refresh failed:', error);
      });
    }
  };

  checkTokenExpiration();
  const interval = setInterval(checkTokenExpiration, 5 * 60 * 1000);
  return () => clearInterval(interval);
}, [state.isAuthenticated, state.token]);
```

### 3. Enhanced Auth Service (`authService.ts`)

**Added token expiration checking:**
```typescript
private isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const currentTime = Math.floor(Date.now() / 1000);
    // Check if token expires within the next 5 minutes
    return payload.exp && payload.exp < (currentTime + 300);
  } catch (error) {
    return true; // Consider invalid tokens as expired
  }
}

shouldRefreshToken(): boolean {
  const token = this.getToken();
  if (!token) return false;
  
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const currentTime = Math.floor(Date.now() / 1000);
    // Refresh if token expires within the next 10 minutes
    return payload.exp && payload.exp < (currentTime + 600);
  } catch (error) {
    return true;
  }
}
```

**Improved refresh token handling:**
```typescript
async refreshToken(): Promise<LoginResponse> {
  const refreshToken = this.getRefreshToken();
  if (!refreshToken) {
    throw new Error('No refresh token available');
  }

  const response = await fetch(`${this.baseUrl}/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    this.clearToken(); // Clear tokens if refresh fails
    throw new Error(`Token refresh failed: ${response.status}`);
  }

  const result = await response.json();
  const accessToken = result.token?.accessToken || result.accessToken;
  const newRefreshToken = result.token?.refreshToken || result.refreshToken || refreshToken;
  
  if (!accessToken) {
    throw new Error('No access token in refresh response');
  }
  
  this.setToken(accessToken);
  this.setRefreshToken(newRefreshToken);
  
  return result;
}
```

## Testing

### Auth Test Page
Created `/auth-test` page for testing authentication functionality:
- Token validation testing
- Manual token refresh testing
- Token expiration checking
- Real-time authentication status monitoring

### Test Scenarios
1. **Page Refresh**: Authentication state should be maintained
2. **Token Expiration**: Automatic refresh should occur
3. **Concurrent Requests**: Multiple API calls should be queued during refresh
4. **Refresh Failure**: Graceful logout and redirect to login

## Configuration

### JWT Settings (appsettings.json)
```json
{
  "Security": {
    "Jwt": {
      "AccessTokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 30
    }
  }
}
```

### Token Refresh Timing
- **Proactive Refresh**: 10 minutes before expiration
- **Emergency Refresh**: 5 minutes before expiration
- **Check Interval**: Every 5 minutes

## Benefits

1. **Improved User Experience**: No unexpected logouts on page refresh
2. **Seamless Token Management**: Automatic token refresh without user intervention
3. **Better Error Handling**: Graceful handling of authentication failures
4. **Concurrent Request Support**: Multiple API calls handled properly during refresh
5. **Proactive Refresh**: Tokens refreshed before expiration to prevent failures

## Monitoring and Debugging

- Enhanced logging for authentication events
- Debug information in browser console
- Auth test page for manual testing
- Token expiration monitoring

## Future Enhancements

1. **HttpOnly Cookies**: Consider moving to HttpOnly cookies for better security
2. **Refresh Token Rotation**: Implement refresh token rotation for enhanced security
3. **Session Management**: Add session timeout and idle detection
4. **Multi-tab Support**: Synchronize authentication state across browser tabs

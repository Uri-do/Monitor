# Monitoring Grid Frontend

A modern, clean React frontend for the Monitoring Grid KPI monitoring system.

## Prerequisites

Before running the frontend, ensure you have:

1. **Node.js** (version 18.x or higher)
   - Download from: https://nodejs.org/
   - Verify installation: `node --version`

2. **npm** (comes with Node.js)
   - Verify installation: `npm --version`

## Quick Start

### Option 1: Use the startup script
```bash
# Windows
start.bat

# This will automatically install dependencies and start the dev server
```

### Option 2: Manual setup
```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The application will be available at: http://localhost:3000

### 3. Ensure API is Running

Make sure the Monitoring Grid API is running at: https://localhost:7000

## Common Issues and Solutions

### Issue 1: "node is not recognized"
**Solution**: Install Node.js from https://nodejs.org/

### Issue 2: Path resolution errors
**Solution**: The project uses TypeScript path mapping. Ensure all imports use the `@/` prefix for src files.

### Issue 3: API connection errors
**Solution**: 
- Ensure the API is running on https://localhost:7000
- Check the proxy configuration in `vite.config.ts`
- Verify CORS is enabled in the API

### Issue 4: Module not found errors
**Solution**: Run `npm install` to ensure all dependencies are installed

## Development Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint
- `npm run test` - Run tests

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── Auth/           # Authentication components
│   ├── Business/       # Business logic components (KPI, Contact, Worker)
│   ├── Charts/         # Chart components
│   ├── Common/         # Common reusable components
│   ├── Layout/         # Layout components
│   └── UI/             # Core UI design system components
├── contexts/           # React contexts
├── hooks/              # Custom React hooks
│   └── mutations/      # TanStack Query mutations
├── pages/              # Page components
│   ├── Admin/          # Administration (users, roles, security, API keys)
│   ├── Alert/          # Alert management pages
│   ├── Analytics/      # Analytics page
│   ├── Auth/           # Authentication pages
│   ├── Contact/        # Contact management pages
│   ├── Dashboard/      # Main dashboard page
│   ├── ExecutionHistory/ # Execution history pages
│   ├── KPI/            # KPI management pages
│   ├── Settings/       # Application settings
│   ├── User/           # User profile pages
│   ├── Users/          # User management pages
│   └── Worker/         # Worker management pages
├── services/           # API service functions
├── test/               # Test utilities and setup
├── theme/              # Theme configuration
├── types/              # TypeScript type definitions
├── utils/              # Utility functions
├── App.tsx             # Main application component
└── main.tsx            # Application entry point
```

## Technology Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Material-UI (MUI)** - Component library
- **TanStack Query** - Data fetching and caching
- **React Router** - Client-side routing
- **Vite** - Build tool and dev server
- **Axios** - HTTP client
- **Zustand** - State management
- **React Hook Form** - Form handling

## API Integration

The frontend communicates with the Monitoring Grid API through:

- **Proxy configuration** in Vite for development
- **Axios interceptors** for request/response handling
- **TanStack Query** for caching and synchronization
- **TypeScript types** for API responses

## Features

### Core Features
- ✅ Dashboard with real-time KPI monitoring
- ✅ KPI management (create, edit, execute, schedule)
- ✅ Contact management
- ✅ Execution history tracking
- ✅ Alert management
- ✅ User and role management
- ✅ Security settings and API key management
- ✅ Worker service management
- ✅ Analytics and reporting

### Technical Features
- ✅ Clean component architecture
- ✅ TypeScript throughout
- ✅ Responsive design
- ✅ Real-time updates via SignalR
- ✅ Error handling and notifications
- ✅ Form validation
- ✅ Data virtualization for large lists
- ✅ Progressive Web App (PWA) capabilities
- ✅ Offline functionality with service worker
- ✅ Security headers and CSP
- ✅ Accessibility compliance (WCAG 2.1)
- ✅ Performance optimization
- ✅ Code splitting and lazy loading

## Troubleshooting

### Clear npm cache
```bash
npm cache clean --force
```

### Reinstall dependencies
```bash
rm -rf node_modules package-lock.json
npm install
```

### Check for TypeScript errors
```bash
npx tsc --noEmit
```

## Performance

### Bundle Optimization
- Code splitting by feature and vendor libraries
- Lazy loading for all routes
- Tree shaking for unused code elimination
- Optimized chunk sizes with manual chunking

### Caching Strategy
- Service worker with intelligent caching
- Static asset caching with long-term cache headers
- API response caching with stale-while-revalidate
- Browser caching optimization

## Security

### Security Headers
- Content Security Policy (CSP)
- X-Frame-Options for clickjacking protection
- X-Content-Type-Options for MIME sniffing protection
- Strict Transport Security (HSTS)

### Authentication & Authorization
- JWT token-based authentication
- Role-based access control (RBAC)
- Secure cookie handling
- API key management

## Accessibility

### WCAG 2.1 Compliance
- Semantic HTML structure
- ARIA labels and roles
- Keyboard navigation support
- Screen reader compatibility
- High contrast mode support
- Skip links for navigation

### Testing
- Automated accessibility testing
- Manual testing with screen readers
- Keyboard-only navigation testing

## Support

For issues related to:
- **Frontend**: Check browser console for errors
- **API connectivity**: Verify API is running and accessible
- **Build issues**: Check Node.js and npm versions
- **Performance**: Use browser dev tools and Lighthouse
- **Security**: Review CSP violations in console
- **Accessibility**: Test with screen readers and keyboard navigation

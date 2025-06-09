# Monitoring Grid Frontend

A modern React frontend for the Monitoring Grid KPI monitoring system.

## Prerequisites

Before running the frontend, ensure you have:

1. **Node.js** (version 18.x or higher)
   - Download from: https://nodejs.org/
   - Verify installation: `node --version`

2. **npm** (comes with Node.js)
   - Verify installation: `npm --version`

## Quick Start

### 1. Install Dependencies

```bash
npm install
```

### 2. Start Development Server

```bash
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

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── Auth/           # Authentication components
│   ├── Charts/         # Chart components
│   ├── Common/         # Common reusable components
│   ├── Dashboard/      # Dashboard-specific components
│   ├── KPI/           # KPI-specific components
│   ├── Layout/        # Layout components
│   ├── Worker/        # Worker management components
│   └── enhanced/      # Enhanced hooks and utilities
├── contexts/          # React contexts
├── hooks/             # Custom React hooks
├── pages/             # Page components
│   ├── Admin/         # Admin pages (role management, system settings)
│   ├── Administration/ # Unified administration hub
│   ├── Alert/         # Alert management pages
│   ├── Analytics/     # Analytics page
│   ├── Auth/          # Authentication pages
│   ├── Contact/       # Contact management pages
│   ├── Dashboard/     # Main dashboard page
│   ├── Debug/         # Debug and monitoring tools
│   ├── ExecutionHistory/ # Execution history pages
│   ├── KPI/           # KPI management pages
│   ├── Settings/      # Application settings
│   ├── User/          # User profile pages
│   └── Users/         # User management pages
├── services/          # API service functions
├── test/              # Test utilities and setup
├── theme/             # Theme configuration
├── types/             # TypeScript type definitions
├── utils/             # Utility functions
├── App.tsx            # Main application component
└── main.tsx           # Application entry point
```

## Technology Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Material-UI (MUI)** - Component library
- **React Query** - Data fetching and caching
- **React Router** - Client-side routing
- **Vite** - Build tool and dev server
- **Axios** - HTTP client

## API Integration

The frontend communicates with the Monitoring Grid API through:

- **Proxy configuration** in Vite for development
- **Axios interceptors** for request/response handling
- **React Query** for caching and synchronization
- **TypeScript types** for API responses

## Features

### Implemented
- ✅ Dashboard with real-time KPI status
- ✅ KPI list with filtering and actions
- ✅ Navigation and layout
- ✅ API integration
- ✅ Error handling and notifications

### In Development
- 🚧 KPI creation and editing forms
- 🚧 Contact management
- 🚧 Alert management
- 🚧 Analytics and reporting
- 🚧 Settings configuration

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

## Support

For issues related to:
- **Frontend**: Check browser console for errors
- **API connectivity**: Verify API is running and accessible
- **Build issues**: Check Node.js and npm versions

# Assets Directory

This directory contains static assets for the MonitoringGrid frontend application.

## Structure
- `images/` - Logo files, icons, and other image assets
- `fonts/` - Custom font files (if needed)
- `icons/` - SVG icons and icon assets

## Usage
Import assets using relative paths:
```typescript
import logo from '../assets/images/logo.png';
```

Or using absolute imports (if configured):
```typescript
import logo from '@/assets/images/logo.png';
```

## Login Page Logo
Place your login page logo in the `images/` directory. Recommended formats:
- PNG with transparent background
- SVG for scalability
- Recommended size: 200-300px width for optimal display

## File Naming Convention
- Use lowercase with hyphens: `company-logo.png`
- Be descriptive: `login-logo.svg`, `dashboard-icon.png`
- Include size in filename if multiple sizes: `logo-large.png`, `logo-small.png`

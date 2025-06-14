#!/usr/bin/env node

/**
 * Frontend Cleanup Script
 * Performs comprehensive cleanup of the MonitoringGrid Frontend project
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

console.log('🧹 Starting Frontend Cleanup...\n');

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  cyan: '\x1b[36m',
};

function log(message, color = colors.reset) {
  console.log(`${color}${message}${colors.reset}`);
}

function step(message) {
  log(`\n📋 ${message}`, colors.cyan);
}

function success(message) {
  log(`✅ ${message}`, colors.green);
}

function warning(message) {
  log(`⚠️  ${message}`, colors.yellow);
}

function error(message) {
  log(`❌ ${message}`, colors.red);
}

// 1. Clean build artifacts
step('Cleaning build artifacts...');
try {
  const dirsToClean = ['dist', 'build', 'coverage', '.vite'];
  dirsToClean.forEach(dir => {
    if (fs.existsSync(dir)) {
      fs.rmSync(dir, { recursive: true, force: true });
      success(`Removed ${dir}/`);
    }
  });
} catch (err) {
  error(`Failed to clean build artifacts: ${err.message}`);
}

// 2. Clean cache files
step('Cleaning cache files...');
try {
  const cacheFiles = ['.eslintcache', 'tsconfig.tsbuildinfo'];
  cacheFiles.forEach(file => {
    if (fs.existsSync(file)) {
      fs.unlinkSync(file);
      success(`Removed ${file}`);
    }
  });
} catch (err) {
  error(`Failed to clean cache files: ${err.message}`);
}

// 3. Clean node_modules and reinstall
step('Cleaning and reinstalling dependencies...');
try {
  if (fs.existsSync('node_modules')) {
    fs.rmSync('node_modules', { recursive: true, force: true });
    success('Removed node_modules/');
  }
  
  if (fs.existsSync('package-lock.json')) {
    fs.unlinkSync('package-lock.json');
    success('Removed package-lock.json');
  }
  
  log('Installing fresh dependencies...', colors.blue);
  execSync('npm install', { stdio: 'inherit' });
  success('Dependencies installed');
} catch (err) {
  error(`Failed to reinstall dependencies: ${err.message}`);
}

// 4. Run type checking
step('Running type checking...');
try {
  execSync('npm run type-check', { stdio: 'inherit' });
  success('Type checking passed');
} catch (err) {
  warning('Type checking failed - please review TypeScript errors');
}

// 5. Run linting
step('Running linting...');
try {
  execSync('npm run lint', { stdio: 'inherit' });
  success('Linting passed');
} catch (err) {
  warning('Linting failed - running auto-fix...');
  try {
    execSync('npm run lint:fix', { stdio: 'inherit' });
    success('Auto-fix completed');
  } catch (fixErr) {
    error('Auto-fix failed - please review linting errors manually');
  }
}

// 6. Check for unused dependencies
step('Checking for unused dependencies...');
try {
  execSync('npm run deps:unused', { stdio: 'inherit' });
} catch (err) {
  warning('Dependency check completed - review output above');
}

// 7. Security audit
step('Running security audit...');
try {
  execSync('npm audit --audit-level moderate', { stdio: 'inherit' });
  success('Security audit passed');
} catch (err) {
  warning('Security vulnerabilities found - consider running: npm audit fix');
}

// 8. Generate cleanup report
step('Generating cleanup report...');
const report = {
  timestamp: new Date().toISOString(),
  version: require('../package.json').version,
  cleanup: {
    buildArtifacts: 'cleaned',
    cacheFiles: 'cleaned',
    dependencies: 'reinstalled',
    typeChecking: 'completed',
    linting: 'completed',
    securityAudit: 'completed',
  },
  nextSteps: [
    'Review any TypeScript or linting errors',
    'Consider updating outdated dependencies',
    'Run tests to ensure everything works',
    'Build the project to verify production readiness',
  ],
};

fs.writeFileSync('cleanup-report.json', JSON.stringify(report, null, 2));
success('Cleanup report generated: cleanup-report.json');

log('\n🎉 Frontend cleanup completed!', colors.green);
log('\n📋 Next steps:', colors.cyan);
report.nextSteps.forEach((step, index) => {
  log(`   ${index + 1}. ${step}`, colors.blue);
});

log('\n🚀 Ready for development!', colors.magenta);

#!/usr/bin/env node

/**
 * Enterprise Build Optimization Script
 * Advanced build process with comprehensive optimization and validation
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

console.log('🚀 Starting Enterprise Build Process...\n');

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

// Build metrics
const buildMetrics = {
  startTime: Date.now(),
  endTime: 0,
  duration: 0,
  bundleSize: 0,
  chunkCount: 0,
  compressionRatio: 0,
  treeShakingEfficiency: 0,
  codeQualityScore: 0,
  securityScore: 0,
  performanceScore: 0,
  errors: [],
  warnings: [],
};

// 1. Pre-build validation
step('Running pre-build validation...');
try {
  // Type checking
  execSync('npx tsc --noEmit', { stdio: 'pipe' });
  success('TypeScript validation passed');
  
  // Linting
  const eslintOutput = execSync('npx eslint src --ext .ts,.tsx --format json', { 
    stdio: 'pipe',
    encoding: 'utf8'
  });
  
  const eslintResults = JSON.parse(eslintOutput);
  let totalErrors = 0;
  let totalWarnings = 0;
  
  eslintResults.forEach(result => {
    totalErrors += result.errorCount;
    totalWarnings += result.warningCount;
  });
  
  if (totalErrors > 0) {
    error(`ESLint found ${totalErrors} errors - build aborted`);
    process.exit(1);
  }
  
  if (totalWarnings > 0) {
    warning(`ESLint found ${totalWarnings} warnings`);
    buildMetrics.warnings.push(`ESLint: ${totalWarnings} warnings`);
  } else {
    success('ESLint validation passed');
  }
  
} catch (err) {
  error('Pre-build validation failed');
  buildMetrics.errors.push('Pre-build validation failed');
  process.exit(1);
}

// 2. Dependency optimization
step('Optimizing dependencies...');
try {
  // Check for unused dependencies
  const depcheckOutput = execSync('npx depcheck --json', { 
    stdio: 'pipe',
    encoding: 'utf8'
  });
  const depcheckResults = JSON.parse(depcheckOutput);
  
  if (depcheckResults.dependencies && depcheckResults.dependencies.length > 0) {
    warning(`Found ${depcheckResults.dependencies.length} unused dependencies`);
    buildMetrics.warnings.push(`Unused dependencies: ${depcheckResults.dependencies.length}`);
  } else {
    success('No unused dependencies found');
  }
  
  // Bundle analysis preparation
  process.env.ANALYZE = 'true';
  
} catch (err) {
  warning('Dependency optimization check failed');
}

// 3. Security audit
step('Running security audit...');
try {
  execSync('npm audit --audit-level moderate', { stdio: 'pipe' });
  success('Security audit passed');
  buildMetrics.securityScore = 100;
} catch (err) {
  warning('Security vulnerabilities found');
  buildMetrics.securityScore = 75;
  buildMetrics.warnings.push('Security vulnerabilities detected');
}

// 4. Build process
step('Building application...');
const buildStartTime = Date.now();

try {
  // Clean previous build
  if (fs.existsSync('dist')) {
    execSync('rm -rf dist', { stdio: 'inherit' });
  }
  
  // Run build with optimizations
  execSync('npm run build', { 
    stdio: 'inherit',
    env: {
      ...process.env,
      NODE_ENV: 'production',
      GENERATE_SOURCEMAP: 'true',
      INLINE_RUNTIME_CHUNK: 'false',
    }
  });
  
  const buildEndTime = Date.now();
  const buildDuration = buildEndTime - buildStartTime;
  
  success(`Build completed in ${(buildDuration / 1000).toFixed(2)}s`);
  
} catch (err) {
  error('Build failed');
  buildMetrics.errors.push('Build process failed');
  process.exit(1);
}

// 5. Post-build analysis
step('Analyzing build output...');
try {
  if (fs.existsSync('dist')) {
    // Calculate bundle size
    const distStats = fs.statSync('dist');
    let totalSize = 0;
    let chunkCount = 0;
    
    function calculateDirSize(dirPath) {
      const files = fs.readdirSync(dirPath);
      
      files.forEach(file => {
        const filePath = path.join(dirPath, file);
        const stats = fs.statSync(filePath);
        
        if (stats.isDirectory()) {
          calculateDirSize(filePath);
        } else {
          totalSize += stats.size;
          if (file.endsWith('.js') || file.endsWith('.css')) {
            chunkCount++;
          }
        }
      });
    }
    
    calculateDirSize('dist');
    
    buildMetrics.bundleSize = Math.round(totalSize / 1024); // KB
    buildMetrics.chunkCount = chunkCount;
    
    success(`Bundle size: ${buildMetrics.bundleSize}KB in ${buildMetrics.chunkCount} chunks`);
    
    // Check bundle size budget
    const budgetLimit = 500; // 500KB
    if (buildMetrics.bundleSize > budgetLimit) {
      warning(`Bundle size exceeds budget: ${buildMetrics.bundleSize}KB > ${budgetLimit}KB`);
      buildMetrics.warnings.push(`Bundle size budget exceeded`);
    }
    
  } else {
    error('Build output directory not found');
    buildMetrics.errors.push('Build output missing');
  }
} catch (err) {
  warning('Post-build analysis failed');
}

// 6. Compression analysis
step('Analyzing compression...');
try {
  if (fs.existsSync('dist')) {
    // Simulate gzip compression analysis
    const jsFiles = [];
    
    function findJSFiles(dirPath) {
      const files = fs.readdirSync(dirPath);
      
      files.forEach(file => {
        const filePath = path.join(dirPath, file);
        const stats = fs.statSync(filePath);
        
        if (stats.isDirectory()) {
          findJSFiles(filePath);
        } else if (file.endsWith('.js')) {
          jsFiles.push({
            path: filePath,
            size: stats.size,
          });
        }
      });
    }
    
    findJSFiles('dist');
    
    if (jsFiles.length > 0) {
      const totalUncompressed = jsFiles.reduce((sum, file) => sum + file.size, 0);
      // Estimate 70% compression ratio for modern JS
      const estimatedCompressed = totalUncompressed * 0.3;
      buildMetrics.compressionRatio = ((totalUncompressed - estimatedCompressed) / totalUncompressed * 100);
      
      success(`Estimated compression ratio: ${buildMetrics.compressionRatio.toFixed(1)}%`);
    }
  }
} catch (err) {
  warning('Compression analysis failed');
}

// 7. Performance validation
step('Validating performance...');
try {
  // Check for performance anti-patterns in build output
  let performanceScore = 100;
  
  // Check chunk sizes
  if (buildMetrics.bundleSize > 300) performanceScore -= 10;
  if (buildMetrics.chunkCount > 20) performanceScore -= 5;
  if (buildMetrics.compressionRatio < 60) performanceScore -= 10;
  
  buildMetrics.performanceScore = Math.max(0, performanceScore);
  
  if (buildMetrics.performanceScore >= 90) {
    success(`Performance score: ${buildMetrics.performanceScore}/100`);
  } else if (buildMetrics.performanceScore >= 70) {
    warning(`Performance score: ${buildMetrics.performanceScore}/100`);
  } else {
    error(`Performance score: ${buildMetrics.performanceScore}/100`);
  }
  
} catch (err) {
  warning('Performance validation failed');
}

// 8. Generate build report
step('Generating build report...');
buildMetrics.endTime = Date.now();
buildMetrics.duration = buildMetrics.endTime - buildMetrics.startTime;

const buildReport = {
  timestamp: new Date().toISOString(),
  version: require('../package.json').version,
  duration: buildMetrics.duration,
  metrics: buildMetrics,
  environment: {
    nodeVersion: process.version,
    platform: process.platform,
    arch: process.arch,
  },
  recommendations: generateRecommendations(),
  summary: generateSummary(),
};

fs.writeFileSync('build-report.json', JSON.stringify(buildReport, null, 2));
success('Build report generated: build-report.json');

// 9. Display summary
log('\n📊 Enterprise Build Summary', colors.cyan);
log('===============================', colors.cyan);
log(`Build Duration: ${(buildMetrics.duration / 1000).toFixed(2)}s`, colors.blue);
log(`Bundle Size: ${buildMetrics.bundleSize}KB`, getBundleSizeColor(buildMetrics.bundleSize));
log(`Chunk Count: ${buildMetrics.chunkCount}`, colors.blue);
log(`Compression: ${buildMetrics.compressionRatio.toFixed(1)}%`, colors.blue);
log(`Performance Score: ${buildMetrics.performanceScore}/100`, getScoreColor(buildMetrics.performanceScore));
log(`Security Score: ${buildMetrics.securityScore}/100`, getScoreColor(buildMetrics.securityScore));
log(`Errors: ${buildMetrics.errors.length}`, buildMetrics.errors.length === 0 ? colors.green : colors.red);
log(`Warnings: ${buildMetrics.warnings.length}`, buildMetrics.warnings.length === 0 ? colors.green : colors.yellow);

// Display errors and warnings
if (buildMetrics.errors.length > 0) {
  log('\n❌ Errors:', colors.red);
  buildMetrics.errors.forEach((err, index) => {
    log(`   ${index + 1}. ${err}`, colors.red);
  });
}

if (buildMetrics.warnings.length > 0) {
  log('\n⚠️  Warnings:', colors.yellow);
  buildMetrics.warnings.forEach((warning, index) => {
    log(`   ${index + 1}. ${warning}`, colors.yellow);
  });
}

// Display recommendations
const recommendations = buildReport.recommendations;
if (recommendations.length > 0) {
  log('\n💡 Optimization Recommendations:', colors.magenta);
  recommendations.forEach((rec, index) => {
    log(`   ${index + 1}. ${rec}`, colors.blue);
  });
}

log(`\n🎯 Overall Build Quality: ${buildReport.summary}`, getScoreColor(buildMetrics.performanceScore));

function generateRecommendations() {
  const recommendations = [];
  
  if (buildMetrics.bundleSize > 500) {
    recommendations.push('Consider implementing code splitting to reduce bundle size');
  }
  
  if (buildMetrics.chunkCount > 20) {
    recommendations.push('Optimize chunk splitting strategy to reduce HTTP requests');
  }
  
  if (buildMetrics.compressionRatio < 60) {
    recommendations.push('Enable better compression or optimize assets');
  }
  
  if (buildMetrics.warnings.length > 5) {
    recommendations.push('Address build warnings to improve code quality');
  }
  
  if (buildMetrics.performanceScore < 80) {
    recommendations.push('Implement performance optimizations for better user experience');
  }
  
  return recommendations;
}

function generateSummary() {
  const avgScore = (buildMetrics.performanceScore + buildMetrics.securityScore) / 2;
  
  if (avgScore >= 95) return 'Excellent - Production Ready! 🎉';
  if (avgScore >= 85) return 'Very Good - Minor optimizations possible 👍';
  if (avgScore >= 75) return 'Good - Some improvements recommended 🔧';
  if (avgScore >= 65) return 'Acceptable - Optimization needed ⚠️';
  return 'Needs Improvement - Significant issues detected 🚨';
}

function getBundleSizeColor(size) {
  if (size < 300) return colors.green;
  if (size < 500) return colors.yellow;
  return colors.red;
}

function getScoreColor(score) {
  if (score >= 90) return colors.green;
  if (score >= 80) return colors.blue;
  if (score >= 70) return colors.yellow;
  return colors.red;
}

log('\n🚀 Enterprise Build Process Complete!', colors.green);

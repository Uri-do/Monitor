#!/usr/bin/env node

/**
 * Advanced Code Quality Analysis Script
 * Performs comprehensive code quality checks and generates detailed reports
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

console.log('🔍 Starting Advanced Code Quality Analysis...\n');

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

// Quality metrics
const qualityMetrics = {
  typeScript: { errors: 0, warnings: 0 },
  eslint: { errors: 0, warnings: 0, fixable: 0 },
  dependencies: { unused: [], outdated: [], vulnerable: [] },
  performance: { bundleSize: 0, chunks: 0 },
  accessibility: { violations: 0 },
  coverage: { percentage: 0 },
  complexity: { high: [], medium: [], low: [] },
};

// 1. TypeScript Analysis
step('Analyzing TypeScript code...');
try {
  execSync('npx tsc --noEmit --pretty', { stdio: 'pipe' });
  success('TypeScript analysis passed');
  qualityMetrics.typeScript.errors = 0;
} catch (err) {
  const output = err.stdout?.toString() || err.stderr?.toString() || '';
  const errorCount = (output.match(/error TS/g) || []).length;
  qualityMetrics.typeScript.errors = errorCount;
  
  if (errorCount > 0) {
    error(`TypeScript analysis failed with ${errorCount} errors`);
    console.log(output);
  }
}

// 2. ESLint Analysis
step('Running ESLint analysis...');
try {
  const eslintOutput = execSync('npx eslint src --ext .ts,.tsx --format json', { 
    stdio: 'pipe',
    encoding: 'utf8'
  });
  
  const results = JSON.parse(eslintOutput);
  let totalErrors = 0;
  let totalWarnings = 0;
  let fixableIssues = 0;
  
  results.forEach(result => {
    totalErrors += result.errorCount;
    totalWarnings += result.warningCount;
    fixableIssues += result.fixableErrorCount + result.fixableWarningCount;
  });
  
  qualityMetrics.eslint = {
    errors: totalErrors,
    warnings: totalWarnings,
    fixable: fixableIssues
  };
  
  if (totalErrors === 0 && totalWarnings === 0) {
    success('ESLint analysis passed');
  } else {
    warning(`ESLint found ${totalErrors} errors and ${totalWarnings} warnings (${fixableIssues} fixable)`);
  }
} catch (err) {
  error('ESLint analysis failed');
}

// 3. Dependency Analysis
step('Analyzing dependencies...');
try {
  // Check for unused dependencies
  try {
    const depcheckOutput = execSync('npx depcheck --json', { 
      stdio: 'pipe',
      encoding: 'utf8'
    });
    const depcheckResults = JSON.parse(depcheckOutput);
    qualityMetrics.dependencies.unused = depcheckResults.dependencies || [];
    
    if (qualityMetrics.dependencies.unused.length === 0) {
      success('No unused dependencies found');
    } else {
      warning(`Found ${qualityMetrics.dependencies.unused.length} unused dependencies`);
    }
  } catch (err) {
    warning('Could not check for unused dependencies');
  }
  
  // Check for outdated dependencies
  try {
    const outdatedOutput = execSync('npm outdated --json', { 
      stdio: 'pipe',
      encoding: 'utf8'
    });
    const outdatedResults = JSON.parse(outdatedOutput);
    qualityMetrics.dependencies.outdated = Object.keys(outdatedResults);
    
    if (qualityMetrics.dependencies.outdated.length === 0) {
      success('All dependencies are up to date');
    } else {
      warning(`Found ${qualityMetrics.dependencies.outdated.length} outdated dependencies`);
    }
  } catch (err) {
    // npm outdated returns exit code 1 when outdated packages are found
    if (err.stdout) {
      try {
        const outdatedResults = JSON.parse(err.stdout);
        qualityMetrics.dependencies.outdated = Object.keys(outdatedResults);
        warning(`Found ${qualityMetrics.dependencies.outdated.length} outdated dependencies`);
      } catch (parseErr) {
        success('All dependencies are up to date');
      }
    }
  }
} catch (err) {
  error('Dependency analysis failed');
}

// 4. Security Audit
step('Running security audit...');
try {
  execSync('npm audit --audit-level moderate', { stdio: 'inherit' });
  success('Security audit passed');
} catch (err) {
  warning('Security vulnerabilities found - consider running: npm audit fix');
}

// 5. Bundle Analysis (if build exists)
step('Analyzing bundle size...');
try {
  if (fs.existsSync('dist')) {
    const distStats = fs.statSync('dist');
    const bundleFiles = fs.readdirSync('dist/assets/js').filter(f => f.endsWith('.js'));
    
    let totalSize = 0;
    bundleFiles.forEach(file => {
      const filePath = path.join('dist/assets/js', file);
      const stats = fs.statSync(filePath);
      totalSize += stats.size;
    });
    
    qualityMetrics.performance.bundleSize = Math.round(totalSize / 1024); // KB
    qualityMetrics.performance.chunks = bundleFiles.length;
    
    success(`Bundle analysis: ${qualityMetrics.performance.bundleSize}KB in ${qualityMetrics.performance.chunks} chunks`);
  } else {
    warning('No build found - run npm run build first for bundle analysis');
  }
} catch (err) {
  warning('Bundle analysis failed');
}

// 6. Generate Quality Report
step('Generating quality report...');
const qualityScore = calculateQualityScore(qualityMetrics);
const report = {
  timestamp: new Date().toISOString(),
  version: require('../package.json').version,
  qualityScore,
  metrics: qualityMetrics,
  recommendations: generateRecommendations(qualityMetrics),
  summary: generateSummary(qualityMetrics, qualityScore),
};

fs.writeFileSync('quality-report.json', JSON.stringify(report, null, 2));
success('Quality report generated: quality-report.json');

// Display summary
log('\n📊 Quality Analysis Summary', colors.cyan);
log('================================', colors.cyan);
log(`Overall Quality Score: ${qualityScore}/100`, getScoreColor(qualityScore));
log(`TypeScript Errors: ${qualityMetrics.typeScript.errors}`, qualityMetrics.typeScript.errors === 0 ? colors.green : colors.red);
log(`ESLint Issues: ${qualityMetrics.eslint.errors + qualityMetrics.eslint.warnings}`, qualityMetrics.eslint.errors === 0 ? colors.green : colors.yellow);
log(`Unused Dependencies: ${qualityMetrics.dependencies.unused.length}`, qualityMetrics.dependencies.unused.length === 0 ? colors.green : colors.yellow);
log(`Outdated Dependencies: ${qualityMetrics.dependencies.outdated.length}`, qualityMetrics.dependencies.outdated.length === 0 ? colors.green : colors.yellow);

if (qualityMetrics.performance.bundleSize > 0) {
  log(`Bundle Size: ${qualityMetrics.performance.bundleSize}KB`, qualityMetrics.performance.bundleSize < 500 ? colors.green : colors.yellow);
}

// Display recommendations
if (report.recommendations.length > 0) {
  log('\n💡 Recommendations:', colors.magenta);
  report.recommendations.forEach((rec, index) => {
    log(`   ${index + 1}. ${rec}`, colors.blue);
  });
}

log(`\n🎯 Quality Score: ${qualityScore}/100`, getScoreColor(qualityScore));

function calculateQualityScore(metrics) {
  let score = 100;
  
  // TypeScript errors (critical)
  score -= metrics.typeScript.errors * 10;
  
  // ESLint errors (high impact)
  score -= metrics.eslint.errors * 5;
  score -= metrics.eslint.warnings * 2;
  
  // Dependencies (medium impact)
  score -= metrics.dependencies.unused.length * 3;
  score -= metrics.dependencies.outdated.length * 1;
  
  // Bundle size (performance impact)
  if (metrics.performance.bundleSize > 1000) score -= 10;
  else if (metrics.performance.bundleSize > 500) score -= 5;
  
  return Math.max(0, Math.min(100, score));
}

function generateRecommendations(metrics) {
  const recommendations = [];
  
  if (metrics.typeScript.errors > 0) {
    recommendations.push('Fix TypeScript errors to improve type safety');
  }
  
  if (metrics.eslint.errors > 0) {
    recommendations.push('Fix ESLint errors to improve code quality');
  }
  
  if (metrics.eslint.fixable > 0) {
    recommendations.push('Run npm run lint:fix to auto-fix linting issues');
  }
  
  if (metrics.dependencies.unused.length > 0) {
    recommendations.push('Remove unused dependencies to reduce bundle size');
  }
  
  if (metrics.dependencies.outdated.length > 5) {
    recommendations.push('Update outdated dependencies for security and performance');
  }
  
  if (metrics.performance.bundleSize > 1000) {
    recommendations.push('Optimize bundle size - consider code splitting and tree shaking');
  }
  
  return recommendations;
}

function generateSummary(metrics, score) {
  if (score >= 90) return 'Excellent code quality! 🎉';
  if (score >= 80) return 'Good code quality with minor issues 👍';
  if (score >= 70) return 'Acceptable code quality, some improvements needed 🔧';
  if (score >= 60) return 'Code quality needs attention ⚠️';
  return 'Significant code quality issues require immediate attention 🚨';
}

function getScoreColor(score) {
  if (score >= 90) return colors.green;
  if (score >= 80) return colors.blue;
  if (score >= 70) return colors.yellow;
  return colors.red;
}

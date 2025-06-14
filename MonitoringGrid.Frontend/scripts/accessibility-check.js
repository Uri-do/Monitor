#!/usr/bin/env node

/**
 * Accessibility Checker Script
 * Validates accessibility compliance and generates reports
 */

const fs = require('fs');
const path = require('path');

console.log('♿ Starting Accessibility Analysis...\n');

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

// Accessibility patterns to check
const accessibilityChecks = {
  missingAltText: {
    pattern: /<img(?![^>]*alt=)[^>]*>/gi,
    message: 'Images without alt text found',
    severity: 'error'
  },
  missingAriaLabels: {
    pattern: /<(button|input|select|textarea)(?![^>]*aria-label)(?![^>]*aria-labelledby)[^>]*>/gi,
    message: 'Interactive elements without aria-label found',
    severity: 'warning'
  },
  missingHeadingStructure: {
    pattern: /<h[1-6][^>]*>/gi,
    message: 'Check heading structure for proper hierarchy',
    severity: 'info'
  },
  missingFormLabels: {
    pattern: /<input(?![^>]*type="hidden")(?![^>]*aria-label)(?![^>]*aria-labelledby)[^>]*>/gi,
    message: 'Form inputs without proper labels found',
    severity: 'error'
  },
  lowContrastText: {
    pattern: /color:\s*#[a-fA-F0-9]{3,6}/gi,
    message: 'Potential low contrast colors found - manual review needed',
    severity: 'warning'
  },
  missingLandmarks: {
    pattern: /<(main|nav|aside|section|article|header|footer)/gi,
    message: 'Semantic landmarks usage',
    severity: 'info'
  }
};

// Scan source files for accessibility issues
step('Scanning source files for accessibility issues...');

const srcDir = path.join(process.cwd(), 'src');
const accessibilityReport = {
  timestamp: new Date().toISOString(),
  totalFiles: 0,
  scannedFiles: 0,
  issues: [],
  summary: {
    errors: 0,
    warnings: 0,
    info: 0
  },
  recommendations: []
};

function scanDirectory(dir) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory()) {
      scanDirectory(filePath);
    } else if (file.endsWith('.tsx') || file.endsWith('.jsx') || file.endsWith('.ts') || file.endsWith('.js')) {
      accessibilityReport.totalFiles++;
      scanFile(filePath);
    }
  });
}

function scanFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    const relativePath = path.relative(process.cwd(), filePath);
    
    accessibilityReport.scannedFiles++;
    
    // Check each accessibility pattern
    Object.entries(accessibilityChecks).forEach(([checkName, check]) => {
      const matches = content.match(check.pattern);
      
      if (matches) {
        const issue = {
          file: relativePath,
          check: checkName,
          message: check.message,
          severity: check.severity,
          occurrences: matches.length,
          examples: matches.slice(0, 3) // Show first 3 examples
        };
        
        accessibilityReport.issues.push(issue);
        accessibilityReport.summary[check.severity]++;
      }
    });
    
    // Check for specific React accessibility patterns
    checkReactA11yPatterns(content, relativePath);
    
  } catch (err) {
    error(`Failed to scan file: ${filePath}`);
  }
}

function checkReactA11yPatterns(content, filePath) {
  // Check for onClick without onKeyDown
  const onClickPattern = /onClick={[^}]+}/g;
  const onKeyDownPattern = /onKeyDown={[^}]+}/g;
  
  const onClickMatches = content.match(onClickPattern);
  const onKeyDownMatches = content.match(onKeyDownPattern);
  
  if (onClickMatches && (!onKeyDownMatches || onClickMatches.length > onKeyDownMatches.length)) {
    accessibilityReport.issues.push({
      file: filePath,
      check: 'keyboardAccessibility',
      message: 'onClick handlers should have corresponding keyboard event handlers',
      severity: 'warning',
      occurrences: onClickMatches.length - (onKeyDownMatches?.length || 0)
    });
    accessibilityReport.summary.warnings++;
  }
  
  // Check for missing role attributes on custom interactive elements
  const customInteractivePattern = /<div[^>]*onClick/gi;
  const rolePattern = /role=/gi;
  
  const customInteractiveMatches = content.match(customInteractivePattern);
  if (customInteractiveMatches) {
    customInteractiveMatches.forEach(match => {
      if (!match.match(rolePattern)) {
        accessibilityReport.issues.push({
          file: filePath,
          check: 'missingRole',
          message: 'Interactive div elements should have appropriate role attributes',
          severity: 'error',
          occurrences: 1,
          examples: [match]
        });
        accessibilityReport.summary.errors++;
      }
    });
  }
}

// Scan all files
if (fs.existsSync(srcDir)) {
  scanDirectory(srcDir);
} else {
  error('Source directory not found');
  process.exit(1);
}

// Generate recommendations
step('Generating accessibility recommendations...');

if (accessibilityReport.summary.errors > 0) {
  accessibilityReport.recommendations.push('Fix critical accessibility errors (missing alt text, form labels, roles)');
}

if (accessibilityReport.summary.warnings > 0) {
  accessibilityReport.recommendations.push('Address accessibility warnings for better user experience');
}

accessibilityReport.recommendations.push('Run automated accessibility testing with tools like axe-core');
accessibilityReport.recommendations.push('Perform manual testing with screen readers');
accessibilityReport.recommendations.push('Test keyboard navigation throughout the application');
accessibilityReport.recommendations.push('Verify color contrast ratios meet WCAG guidelines');

// Calculate accessibility score
const totalIssues = accessibilityReport.summary.errors + accessibilityReport.summary.warnings;
const maxPossibleIssues = accessibilityReport.scannedFiles * 5; // Rough estimate
const accessibilityScore = Math.max(0, Math.round(100 - (totalIssues / maxPossibleIssues) * 100));

accessibilityReport.score = accessibilityScore;

// Save report
fs.writeFileSync('accessibility-report.json', JSON.stringify(accessibilityReport, null, 2));
success('Accessibility report generated: accessibility-report.json');

// Display summary
log('\n♿ Accessibility Analysis Summary', colors.cyan);
log('==================================', colors.cyan);
log(`Files Scanned: ${accessibilityReport.scannedFiles}/${accessibilityReport.totalFiles}`, colors.blue);
log(`Accessibility Score: ${accessibilityScore}/100`, getScoreColor(accessibilityScore));
log(`Critical Issues: ${accessibilityReport.summary.errors}`, accessibilityReport.summary.errors === 0 ? colors.green : colors.red);
log(`Warnings: ${accessibilityReport.summary.warnings}`, accessibilityReport.summary.warnings === 0 ? colors.green : colors.yellow);
log(`Info Items: ${accessibilityReport.summary.info}`, colors.blue);

// Show top issues
if (accessibilityReport.issues.length > 0) {
  log('\n🔍 Top Accessibility Issues:', colors.magenta);
  
  const topIssues = accessibilityReport.issues
    .sort((a, b) => {
      const severityOrder = { error: 3, warning: 2, info: 1 };
      return severityOrder[b.severity] - severityOrder[a.severity] || b.occurrences - a.occurrences;
    })
    .slice(0, 5);
  
  topIssues.forEach((issue, index) => {
    const severityColor = issue.severity === 'error' ? colors.red : 
                         issue.severity === 'warning' ? colors.yellow : colors.blue;
    log(`   ${index + 1}. ${issue.message} (${issue.occurrences} occurrences)`, severityColor);
    log(`      File: ${issue.file}`, colors.reset);
  });
}

// Display recommendations
if (accessibilityReport.recommendations.length > 0) {
  log('\n💡 Accessibility Recommendations:', colors.magenta);
  accessibilityReport.recommendations.forEach((rec, index) => {
    log(`   ${index + 1}. ${rec}`, colors.blue);
  });
}

function getScoreColor(score) {
  if (score >= 90) return colors.green;
  if (score >= 80) return colors.blue;
  if (score >= 70) return colors.yellow;
  return colors.red;
}

log(`\n♿ Accessibility Score: ${accessibilityScore}/100`, getScoreColor(accessibilityScore));

if (accessibilityScore >= 90) {
  log('Excellent accessibility compliance! 🎉', colors.green);
} else if (accessibilityScore >= 80) {
  log('Good accessibility with minor improvements needed 👍', colors.blue);
} else if (accessibilityScore >= 70) {
  log('Accessibility needs attention ⚠️', colors.yellow);
} else {
  log('Significant accessibility issues require immediate attention 🚨', colors.red);
}

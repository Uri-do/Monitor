#!/usr/bin/env node

/**
 * Cleanup script to remove obsolete files and optimize the frontend project
 */

const fs = require('fs');
const path = require('path');

// Files and directories to remove (if they exist)
const obsoleteFiles = [
  // Legacy component files
  'src/components/UI/DataTableOld.tsx',
  'src/components/UI/DataTableLegacy.tsx',
  'src/components/Business/KPI', // Old KPI directory
  'src/components/Business/Kpi', // Alternative KPI directory
  
  // Legacy hooks
  'src/hooks/useKPI.ts',
  'src/hooks/useKpis.ts',
  'src/hooks/useKpiMutations.ts',
  
  // Legacy pages
  'src/pages/KPI',
  'src/pages/Kpi',
  
  // Legacy utilities
  'src/utils/kpiUtils.ts',
  'src/utils/kpiTypeUtils.ts',
  
  // Temporary files
  'src/temp',
  'src/tmp',
  '.tmp',
  
  // Old build artifacts
  'dist-old',
  'build-old',
  
  // Legacy configuration
  'webpack.config.old.js',
  'vite.config.old.ts',
  
  // Old documentation
  'docs/old',
  'docs/legacy',
  
  // Test artifacts
  'coverage-old',
  'test-results-old',
];

// Patterns to search for in files that need updating
const obsoletePatterns = [
  {
    pattern: /\/\*\s*TODO:\s*Remove.*\*\//g,
    description: 'TODO comments for removal'
  },
  {
    pattern: /\/\*\s*DEPRECATED.*\*\//g,
    description: 'Deprecated code blocks'
  },
  {
    pattern: /console\.log\(/g,
    description: 'Console.log statements (development only)',
    exclude: ['development', 'debug']
  },
  {
    pattern: /debugger;/g,
    description: 'Debugger statements'
  }
];

// File extensions to check
const fileExtensions = ['.ts', '.tsx', '.js', '.jsx'];

function removeObsoleteFiles() {
  console.log('🧹 Starting cleanup of obsolete files...\n');
  
  let removedCount = 0;
  
  obsoleteFiles.forEach(filePath => {
    const fullPath = path.join(process.cwd(), filePath);
    
    try {
      if (fs.existsSync(fullPath)) {
        const stats = fs.statSync(fullPath);
        
        if (stats.isDirectory()) {
          fs.rmSync(fullPath, { recursive: true, force: true });
          console.log(`📁 Removed directory: ${filePath}`);
        } else {
          fs.unlinkSync(fullPath);
          console.log(`📄 Removed file: ${filePath}`);
        }
        
        removedCount++;
      }
    } catch (error) {
      console.warn(`⚠️  Could not remove ${filePath}: ${error.message}`);
    }
  });
  
  console.log(`\n✅ Removed ${removedCount} obsolete files/directories\n`);
}

function scanForObsoletePatterns(dir = 'src') {
  console.log('🔍 Scanning for obsolete patterns...\n');
  
  const issues = [];
  
  function scanDirectory(dirPath) {
    const items = fs.readdirSync(dirPath);
    
    items.forEach(item => {
      const itemPath = path.join(dirPath, item);
      const stats = fs.statSync(itemPath);
      
      if (stats.isDirectory() && !item.startsWith('.') && item !== 'node_modules') {
        scanDirectory(itemPath);
      } else if (stats.isFile() && fileExtensions.some(ext => item.endsWith(ext))) {
        scanFile(itemPath);
      }
    });
  }
  
  function scanFile(filePath) {
    try {
      const content = fs.readFileSync(filePath, 'utf8');
      
      obsoletePatterns.forEach(({ pattern, description, exclude }) => {
        const matches = content.match(pattern);
        
        if (matches) {
          // Skip if this is an excluded context
          if (exclude && exclude.some(exc => filePath.includes(exc))) {
            return;
          }
          
          issues.push({
            file: filePath.replace(process.cwd(), ''),
            description,
            count: matches.length,
            lines: getLineNumbers(content, pattern)
          });
        }
      });
    } catch (error) {
      console.warn(`⚠️  Could not scan ${filePath}: ${error.message}`);
    }
  }
  
  function getLineNumbers(content, pattern) {
    const lines = content.split('\n');
    const lineNumbers = [];
    
    lines.forEach((line, index) => {
      if (pattern.test(line)) {
        lineNumbers.push(index + 1);
      }
    });
    
    return lineNumbers;
  }
  
  scanDirectory(dir);
  
  if (issues.length > 0) {
    console.log('⚠️  Found potential issues:\n');
    
    issues.forEach(issue => {
      console.log(`📄 ${issue.file}`);
      console.log(`   ${issue.description} (${issue.count} occurrences)`);
      console.log(`   Lines: ${issue.lines.join(', ')}\n`);
    });
  } else {
    console.log('✅ No obsolete patterns found!\n');
  }
  
  return issues;
}

function optimizeImports() {
  console.log('📦 Optimizing imports...\n');
  
  // This would typically use a tool like eslint with auto-fix
  // For now, we'll just report what could be optimized
  
  console.log('💡 Consider running:');
  console.log('   npm run lint:fix');
  console.log('   npm run format');
  console.log('   npm run deps:unused\n');
}

function generateReport() {
  console.log('📊 Generating cleanup report...\n');
  
  const report = {
    timestamp: new Date().toISOString(),
    obsoleteFilesRemoved: obsoleteFiles.filter(file => 
      !fs.existsSync(path.join(process.cwd(), file))
    ).length,
    totalObsoleteFiles: obsoleteFiles.length,
    recommendations: [
      'Run bundle analyzer to check for unused dependencies',
      'Consider implementing tree shaking for better optimization',
      'Review and update component documentation',
      'Run performance audit with Lighthouse',
    ]
  };
  
  const reportPath = path.join(process.cwd(), 'cleanup-report.json');
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));
  
  console.log(`📋 Cleanup report saved to: cleanup-report.json\n`);
  
  return report;
}

// Main execution
function main() {
  console.log('🚀 Frontend Cleanup Script\n');
  console.log('==========================\n');
  
  try {
    removeObsoleteFiles();
    const issues = scanForObsoletePatterns();
    optimizeImports();
    const report = generateReport();
    
    console.log('🎉 Cleanup completed successfully!\n');
    
    if (issues.length > 0) {
      console.log(`⚠️  Found ${issues.length} potential issues to review`);
    }
    
    console.log('📈 Next steps:');
    console.log('   1. Review any reported issues');
    console.log('   2. Run tests to ensure nothing is broken');
    console.log('   3. Run bundle analysis');
    console.log('   4. Update documentation if needed\n');
    
  } catch (error) {
    console.error('❌ Cleanup failed:', error.message);
    process.exit(1);
  }
}

// Run the script
if (require.main === module) {
  main();
}

module.exports = {
  removeObsoleteFiles,
  scanForObsoletePatterns,
  optimizeImports,
  generateReport
};

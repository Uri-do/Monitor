// Test script to execute an indicator and monitor real-time updates
// Run this in the browser console on the Worker Management page

async function testIndicatorExecution() {
    console.log('🧪 Starting indicator execution test...');
    
    try {
        // First, let's get the list of available indicators
        console.log('📋 Fetching available indicators...');
        const indicatorsResponse = await fetch('/api/indicator', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        
        if (!indicatorsResponse.ok) {
            throw new Error(`Failed to fetch indicators: ${indicatorsResponse.status} ${indicatorsResponse.statusText}`);
        }
        
        const indicatorsData = await indicatorsResponse.json();
        console.log('📊 Available indicators:', indicatorsData);
        
        // Get the first active indicator
        const indicators = indicatorsData.data || indicatorsData;
        const activeIndicator = indicators.find(ind => ind.isActive);
        
        if (!activeIndicator) {
            console.error('❌ No active indicators found');
            return;
        }
        
        console.log(`🎯 Selected indicator: ${activeIndicator.indicatorName} (ID: ${activeIndicator.indicatorID})`);
        
        // Execute the indicator using the worker API
        console.log('🚀 Executing indicator...');
        const executeResponse = await fetch(`/api/worker/execute-indicator/${activeIndicator.indicatorID}`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                context: 'Manual Test',
                saveResults: true
            })
        });
        
        if (!executeResponse.ok) {
            throw new Error(`Failed to execute indicator: ${executeResponse.status} ${executeResponse.statusText}`);
        }
        
        const executeData = await executeResponse.json();
        console.log('✅ Indicator execution response:', executeData);
        
        // Monitor for SignalR events
        console.log('👂 Monitoring for SignalR events...');
        console.log('Check the Live Execution Log and Recent Execution Results for real-time updates');
        
        return {
            indicator: activeIndicator,
            executionResult: executeData
        };
        
    } catch (error) {
        console.error('❌ Error during indicator execution test:', error);
        throw error;
    }
}

// Alternative method using the indicator API directly
async function testIndicatorExecutionDirect() {
    console.log('🧪 Starting direct indicator execution test...');
    
    try {
        // Get indicators
        const indicatorsResponse = await fetch('/api/indicator');
        const indicatorsData = await indicatorsResponse.json();
        const indicators = indicatorsData.data || indicatorsData;
        const activeIndicator = indicators.find(ind => ind.isActive);
        
        if (!activeIndicator) {
            console.error('❌ No active indicators found');
            return;
        }
        
        console.log(`🎯 Selected indicator: ${activeIndicator.indicatorName} (ID: ${activeIndicator.indicatorID})`);
        
        // Execute using the indicator API
        const executeResponse = await fetch(`/api/indicator/${activeIndicator.indicatorID}/execute`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                indicatorID: activeIndicator.indicatorID,
                executionContext: 'Manual Test',
                saveResults: true
            })
        });
        
        const executeData = await executeResponse.json();
        console.log('✅ Direct indicator execution response:', executeData);
        
        return {
            indicator: activeIndicator,
            executionResult: executeData
        };
        
    } catch (error) {
        console.error('❌ Error during direct indicator execution test:', error);
        throw error;
    }
}

// Function to check SignalR connection status
function checkSignalRStatus() {
    console.log('🔌 Checking SignalR connection status...');
    
    // Try to access the SignalR service from the window object
    if (window.signalRService) {
        console.log('📡 SignalR service found:', window.signalRService);
        console.log('🔗 Connection state:', window.signalRService.getConnectionState());
        console.log('🆔 Connection ID:', window.signalRService.getConnectionId());
        console.log('✅ Is connected:', window.signalRService.isConnected());
    } else {
        console.log('❌ SignalR service not found on window object');
    }
}

// Run the tests
console.log('🎬 Indicator Execution Test Script Loaded');
console.log('📝 Available functions:');
console.log('  - testIndicatorExecution() - Execute indicator via worker API');
console.log('  - testIndicatorExecutionDirect() - Execute indicator via indicator API');
console.log('  - checkSignalRStatus() - Check SignalR connection');
console.log('');
console.log('💡 Run: await testIndicatorExecution()');

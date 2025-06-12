#!/bin/bash
# Simple script to create one sample indicator using curl

API_URL="https://localhost:7001/api/indicator"

echo "🚀 Creating sample indicator..."

curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "indicatorName": "Sample Transaction Monitor",
    "indicatorCode": "SAMPLE_001",
    "indicatorDesc": "Sample indicator for testing the system",
    "collectorId": 1,
    "collectorItemName": "TransactionCount",
    "scheduleConfiguration": "0 */5 * * * *",
    "isActive": true,
    "lastMinutes": 60,
    "thresholdType": "Count",
    "thresholdField": "Amount",
    "thresholdComparison": ">",
    "thresholdValue": 100,
    "priority": "medium",
    "ownerContactId": 1,
    "averageLastDays": 7,
    "contactIds": [1]
  }'

echo ""
echo "✅ Sample indicator creation request sent!"

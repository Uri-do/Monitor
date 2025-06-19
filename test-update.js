// Test script to verify indicator update API
const testData = {
  indicatorID: 13,
  indicatorName: "BetsByPlayMode_Bingo",
  indicatorCode: "BETS_BINGO",
  indicatorDescription: "Updated description test",
  collectorId: 4,
  collectorItemName: "Bingo",
  schedulerId: 13,
  isActive: true,
  lastMinutes: 60,
  thresholdType: "count",
  thresholdField: "Total",
  thresholdComparison: "gt",
  thresholdValue: 100,
  priority: "high",
  ownerContactId: 1,
  contactIds: []
};

fetch('http://localhost:57653/api/indicator/13', {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify(testData)
})
.then(response => response.json())
.then(data => {
  console.log('Success:', data);
})
.catch((error) => {
  console.error('Error:', error);
});

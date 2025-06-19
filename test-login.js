// Test login to get authentication token
const loginData = {
  username: "admin",
  password: "Admin123!"
};

fetch('http://localhost:57653/api/security/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify(loginData)
})
.then(response => {
  console.log('Response status:', response.status);
  return response.json();
})
.then(data => {
  console.log('Login response:', JSON.stringify(data, null, 2));
  
  if (data.isSuccess && data.data && data.data.token) {
    const token = data.data.token.accessToken;
    console.log('Access token:', token);

    // Now test the update with the correct format that the backend expects
    const updateData = {
      indicatorID: 13,
      indicatorName: "BetsByPlayMode_Bingo",
      indicatorDescription: "Updated description with auth",
      ownerContactId: 1,
      collectorId: 4,
      sqlQuery: "SELECT COUNT(*) as Total FROM SomeTable WHERE PlayMode = 'Bingo'", // Required field
      isActive: true,
      lastMinutes: 60,
      schedulerId: 13,
      alertThreshold: 100,
      alertOperator: "gt",
      updateReason: "Testing update functionality"
    };

    return fetch('http://localhost:57653/api/indicator/13', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(updateData)
    });
  } else {
    throw new Error('Login failed: ' + JSON.stringify(data));
  }
})
.then(response => {
  console.log('Update response status:', response.status);
  return response.json();
})
.then(data => {
  console.log('Update response:', JSON.stringify(data, null, 2));
})
.catch((error) => {
  console.error('Error:', error);
});

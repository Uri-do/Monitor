// Test script to get all indicators and see what exists
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
.then(response => response.json())
.then(data => {
  console.log('Login response status:', data.isSuccess);
  
  if (data.isSuccess && data.data && data.data.token) {
    const token = data.data.token.accessToken;
    console.log('Got access token');
    
    // Get specific indicator by ID
    return fetch('http://localhost:57653/api/indicator/13', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
  } else {
    throw new Error('Login failed: ' + JSON.stringify(data));
  }
})
.then(response => {
  console.log('Get indicator 13 response status:', response.status);
  return response.json();
})
.then(data => {
  console.log('Indicator 13 response:', JSON.stringify(data, null, 2));
})
.catch((error) => {
  console.error('Error:', error);
});

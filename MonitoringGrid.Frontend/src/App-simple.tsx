import React from 'react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline, Box, Typography, Container } from '@mui/material';

// Create theme
const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Container maxWidth="lg">
        <Box sx={{ py: 4 }}>
          <Typography variant="h3" component="h1" gutterBottom>
            🎉 Monitoring Grid Frontend
          </Typography>
          <Typography variant="h5" color="text.secondary" gutterBottom>
            React + TypeScript + Material-UI + Vite
          </Typography>
          <Typography variant="body1" sx={{ mt: 2 }}>
            ✅ Node.js and npm are working correctly!
          </Typography>
          <Typography variant="body1">
            ✅ All dependencies installed successfully!
          </Typography>
          <Typography variant="body1">
            ✅ Vite development server is running!
          </Typography>
          <Typography variant="body1">
            ✅ React application is rendering!
          </Typography>
          <Typography variant="body1">
            ✅ Material-UI theme is applied!
          </Typography>
          <Typography variant="body1">
            ✅ TypeScript compilation is working!
          </Typography>
          
          <Box sx={{ mt: 4, p: 2, bgcolor: 'primary.main', color: 'white', borderRadius: 1 }}>
            <Typography variant="h6">
              🚀 Frontend Setup Complete!
            </Typography>
            <Typography variant="body2">
              The Monitoring Grid React frontend is now ready for development.
            </Typography>
          </Box>
        </Box>
      </Container>
    </ThemeProvider>
  );
}

export default App;

import React from 'react';
import { Box, Typography, Card, CardContent } from '@mui/material';

const TestPage: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Test Page
      </Typography>
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Application Status
          </Typography>
          <Typography variant="body1">
            ✅ React is working
          </Typography>
          <Typography variant="body1">
            ✅ Material-UI is working
          </Typography>
          <Typography variant="body1">
            ✅ Routing is working
          </Typography>
          <Typography variant="body1">
            ✅ TypeScript compilation is successful
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
};

export default TestPage;

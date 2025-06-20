import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Grid,
  Paper,
  Divider,
  Alert,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Send as SendIcon,
  Code as CodeIcon,
  Api as ApiIcon,
} from '@mui/icons-material';
import { useTheme } from '@mui/material/styles';

interface ApiResponse {
  status: number;
  statusText: string;
  data: any;
  headers: Record<string, string>;
  duration: number;
}

const ApiTestingPage: React.FC = () => {
  const theme = useTheme();
  const [method, setMethod] = useState<string>('GET');
  const [url, setUrl] = useState<string>('/api/indicator');
  const [headers, setHeaders] = useState<string>('{\n  "Content-Type": "application/json"\n}');
  const [body, setBody] = useState<string>('{}');
  const [response, setResponse] = useState<ApiResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const commonEndpoints = [
    { label: 'Get All Indicators', method: 'GET', url: '/api/indicator' },
    { label: 'Get Indicator by ID', method: 'GET', url: '/api/indicator/1' },
    { label: 'Create Indicator', method: 'POST', url: '/api/indicator' },
    { label: 'Update Indicator', method: 'PUT', url: '/api/indicator/1' },
    { label: 'Delete Indicator', method: 'DELETE', url: '/api/indicator/1' },
    { label: 'Execute Indicator', method: 'POST', url: '/api/indicator/1/execute' },
    { label: 'Get Alerts', method: 'GET', url: '/api/indicator/alerts' },
    { label: 'Get Dashboard Data', method: 'GET', url: '/api/indicator/dashboard' },
    { label: 'Get System Health', method: 'GET', url: '/health' },
    { label: 'Get Worker Status', method: 'GET', url: '/api/worker/status' },
  ];

  const handleSendRequest = async () => {
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const startTime = Date.now();
      
      // Parse headers
      let parsedHeaders: Record<string, string> = {};
      try {
        parsedHeaders = JSON.parse(headers);
      } catch (e) {
        throw new Error('Invalid JSON in headers');
      }

      // Prepare request options
      const requestOptions: RequestInit = {
        method,
        headers: parsedHeaders,
      };

      // Add body for non-GET requests
      if (method !== 'GET' && method !== 'DELETE') {
        try {
          requestOptions.body = JSON.parse(body);
        } catch (e) {
          requestOptions.body = body;
        }
      }

      // Make the request
      const response = await fetch(url, requestOptions);
      const endTime = Date.now();
      
      // Parse response
      let responseData: any;
      const contentType = response.headers.get('content-type');
      
      if (contentType && contentType.includes('application/json')) {
        responseData = await response.json();
      } else {
        responseData = await response.text();
      }

      // Extract response headers
      const responseHeaders: Record<string, string> = {};
      response.headers.forEach((value, key) => {
        responseHeaders[key] = value;
      });

      setResponse({
        status: response.status,
        statusText: response.statusText,
        data: responseData,
        headers: responseHeaders,
        duration: endTime - startTime,
      });

    } catch (err: any) {
      setError(err.message || 'An error occurred while making the request');
    } finally {
      setLoading(false);
    }
  };

  const handleQuickSelect = (endpoint: typeof commonEndpoints[0]) => {
    setMethod(endpoint.method);
    setUrl(endpoint.url);
    
    // Set default body for POST/PUT requests
    if (endpoint.method === 'POST' || endpoint.method === 'PUT') {
      if (endpoint.url.includes('/indicator') && !endpoint.url.includes('/execute')) {
        setBody(JSON.stringify({
          indicatorName: "Test Indicator",
          indicatorCode: "TEST_001",
          indicatorDesc: "Test indicator description",
          collectorId: 1,
          collectorItemName: "test_item",
          scheduleConfiguration: "0 */5 * * * ?",
          thresholdType: "absolute",
          thresholdField: "value",
          thresholdComparison: "gt",
          thresholdValue: 100,
          priority: "medium",
          ownerContactId: 1
        }, null, 2));
      }
    }
  };

  const getStatusColor = (status: number) => {
    if (status >= 200 && status < 300) return 'success';
    if (status >= 300 && status < 400) return 'info';
    if (status >= 400 && status < 500) return 'warning';
    return 'error';
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <ApiIcon />
        API Testing
      </Typography>
      
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Test API endpoints directly from the frontend. This tool is available only to administrators.
      </Typography>

      <Grid container spacing={3}>
        {/* Request Configuration */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Request Configuration
              </Typography>
              
              {/* Quick Select */}
              <Accordion sx={{ mb: 2 }}>
                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                  <Typography>Quick Select Common Endpoints</Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <Grid container spacing={1}>
                    {commonEndpoints.map((endpoint, index) => (
                      <Grid item key={index}>
                        <Chip
                          label={endpoint.label}
                          onClick={() => handleQuickSelect(endpoint)}
                          variant="outlined"
                          size="small"
                          sx={{ cursor: 'pointer' }}
                        />
                      </Grid>
                    ))}
                  </Grid>
                </AccordionDetails>
              </Accordion>

              {/* Method and URL */}
              <Grid container spacing={2} sx={{ mb: 2 }}>
                <Grid item xs={3}>
                  <FormControl fullWidth>
                    <InputLabel>Method</InputLabel>
                    <Select
                      value={method}
                      label="Method"
                      onChange={(e) => setMethod(e.target.value)}
                    >
                      <MenuItem value="GET">GET</MenuItem>
                      <MenuItem value="POST">POST</MenuItem>
                      <MenuItem value="PUT">PUT</MenuItem>
                      <MenuItem value="DELETE">DELETE</MenuItem>
                      <MenuItem value="PATCH">PATCH</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={9}>
                  <TextField
                    fullWidth
                    label="URL"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    placeholder="/api/endpoint"
                  />
                </Grid>
              </Grid>

              {/* Headers */}
              <TextField
                fullWidth
                label="Headers (JSON)"
                multiline
                rows={4}
                value={headers}
                onChange={(e) => setHeaders(e.target.value)}
                sx={{ mb: 2 }}
              />

              {/* Body */}
              {method !== 'GET' && method !== 'DELETE' && (
                <TextField
                  fullWidth
                  label="Request Body (JSON)"
                  multiline
                  rows={6}
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  sx={{ mb: 2 }}
                />
              )}

              {/* Send Button */}
              <Button
                variant="contained"
                startIcon={<SendIcon />}
                onClick={handleSendRequest}
                disabled={loading}
                fullWidth
                size="large"
              >
                {loading ? 'Sending...' : 'Send Request'}
              </Button>
            </CardContent>
          </Card>
        </Grid>

        {/* Response */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CodeIcon />
                Response
              </Typography>

              {error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error}
                </Alert>
              )}

              {response && (
                <Box>
                  {/* Status */}
                  <Box sx={{ mb: 2 }}>
                    <Chip
                      label={`${response.status} ${response.statusText}`}
                      color={getStatusColor(response.status)}
                      sx={{ mr: 1 }}
                    />
                    <Chip
                      label={`${response.duration}ms`}
                      variant="outlined"
                      size="small"
                    />
                  </Box>

                  <Divider sx={{ mb: 2 }} />

                  {/* Response Headers */}
                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography variant="subtitle2">Response Headers</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
                        <pre style={{ margin: 0, fontSize: '0.875rem' }}>
                          {JSON.stringify(response.headers, null, 2)}
                        </pre>
                      </Paper>
                    </AccordionDetails>
                  </Accordion>

                  {/* Response Body */}
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Response Body
                    </Typography>
                    <Paper sx={{ p: 2, bgcolor: 'grey.50', maxHeight: 400, overflow: 'auto' }}>
                      <pre style={{ margin: 0, fontSize: '0.875rem' }}>
                        {typeof response.data === 'string' 
                          ? response.data 
                          : JSON.stringify(response.data, null, 2)
                        }
                      </pre>
                    </Paper>
                  </Box>
                </Box>
              )}

              {!response && !error && !loading && (
                <Box sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
                  <Typography>
                    Configure your request and click "Send Request" to see the response here.
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ApiTestingPage;

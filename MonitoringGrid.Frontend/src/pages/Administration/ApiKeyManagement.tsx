import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  Alert,
  Grid,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Key as KeyIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { PageHeader } from '@/components/Common';
import { securityService } from '@/services/securityService';
import toast from 'react-hot-toast';

interface ApiKey {
  keyId: string;
  name: string;
  scopes: string[];
  createdAt: string;
  lastUsed?: string;
  isActive: boolean;
}

const ApiKeyManagement: React.FC = () => {
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newKeyName, setNewKeyName] = useState('');
  const [newKeyScopes, setNewKeyScopes] = useState<string[]>([]);
  const [generatedKey, setGeneratedKey] = useState<string | null>(null);
  const [showKeyDialog, setShowKeyDialog] = useState(false);

  const availableScopes = [
    'read',
    'write',
    'admin',
    'kpis:read',
    'kpis:write',
    'contacts:read',
    'contacts:write',
    'alerts:read',
    'alerts:write',
    'users:read',
    'users:write',
  ];

  useEffect(() => {
    loadApiKeys();
  }, []);

  const loadApiKeys = async () => {
    try {
      setLoading(true);
      const keys = await securityService.getApiKeys();
      setApiKeys(keys);
    } catch (error) {
      console.error('Failed to load API keys:', error);
      toast.error('Failed to load API keys');
      // Set some mock data for demonstration
      setApiKeys([
        {
          keyId: 'key-1',
          name: 'Dashboard Integration',
          scopes: ['read', 'kpis:read'],
          createdAt: new Date().toISOString(),
          lastUsed: new Date().toISOString(),
          isActive: true,
        },
        {
          keyId: 'key-2',
          name: 'Mobile App',
          scopes: ['read', 'write', 'alerts:read'],
          createdAt: new Date().toISOString(),
          isActive: true,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateApiKey = async () => {
    if (!newKeyName.trim()) {
      toast.error('API key name is required');
      return;
    }

    try {
      const result = await securityService.createApiKey(newKeyName, newKeyScopes);
      setGeneratedKey(result.key);
      setShowKeyDialog(true);
      
      // Add to local state
      const newKey: ApiKey = {
        keyId: result.keyId,
        name: newKeyName,
        scopes: newKeyScopes,
        createdAt: new Date().toISOString(),
        isActive: true,
      };
      setApiKeys(prev => [...prev, newKey]);
      
      // Reset form
      setNewKeyName('');
      setNewKeyScopes([]);
      setCreateDialogOpen(false);
      
      toast.success('API key created successfully');
    } catch (error) {
      console.error('Failed to create API key:', error);
      toast.error('Failed to create API key');
    }
  };

  const handleRevokeApiKey = async (keyId: string, keyName: string) => {
    if (!confirm(`Are you sure you want to revoke the API key "${keyName}"?`)) {
      return;
    }

    try {
      await securityService.revokeApiKey(keyId);
      setApiKeys(prev => prev.filter(key => key.keyId !== keyId));
      toast.success('API key revoked successfully');
    } catch (error) {
      console.error('Failed to revoke API key:', error);
      toast.error('Failed to revoke API key');
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success('API key copied to clipboard');
  };

  if (loading) {
    return (
      <Box>
        <PageHeader
          title="API Key Management"
          subtitle="Create and manage API keys for system integration"
        />
        <Typography>Loading...</Typography>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="API Key Management"
        subtitle="Create and manage API keys for system integration"
      />

      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Typography variant="h6">
              <KeyIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
              Active API Keys
            </Typography>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateDialogOpen(true)}
            >
              Create API Key
            </Button>
          </Box>

          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Scopes</TableCell>
                  <TableCell>Created</TableCell>
                  <TableCell>Last Used</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {apiKeys.map((apiKey) => (
                  <TableRow key={apiKey.keyId}>
                    <TableCell>{apiKey.name}</TableCell>
                    <TableCell>
                      {apiKey.scopes.map((scope) => (
                        <Chip key={scope} label={scope} size="small" sx={{ mr: 0.5, mb: 0.5 }} />
                      ))}
                    </TableCell>
                    <TableCell>{new Date(apiKey.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      {apiKey.lastUsed ? new Date(apiKey.lastUsed).toLocaleDateString() : 'Never'}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={apiKey.isActive ? 'Active' : 'Revoked'}
                        color={apiKey.isActive ? 'success' : 'error'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <IconButton
                        onClick={() => handleRevokeApiKey(apiKey.keyId, apiKey.name)}
                        disabled={!apiKey.isActive}
                        color="error"
                        size="small"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* Create API Key Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New API Key</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                autoFocus
                fullWidth
                label="API Key Name"
                variant="outlined"
                value={newKeyName}
                onChange={(e) => setNewKeyName(e.target.value)}
                placeholder="Enter a descriptive name for this API key"
              />
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Scopes</InputLabel>
                <Select
                  multiple
                  value={newKeyScopes}
                  onChange={(e) => setNewKeyScopes(e.target.value as string[])}
                  label="Scopes"
                  renderValue={(selected) => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map((value) => (
                        <Chip key={value} label={value} size="small" />
                      ))}
                    </Box>
                  )}
                >
                  {availableScopes.map((scope) => (
                    <MenuItem key={scope} value={scope}>
                      {scope}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateApiKey} variant="contained" disabled={!newKeyName.trim()}>
            Create API Key
          </Button>
        </DialogActions>
      </Dialog>

      {/* Show Generated Key Dialog */}
      <Dialog open={showKeyDialog} onClose={() => setShowKeyDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>API Key Created Successfully</DialogTitle>
        <DialogContent>
          <Alert severity="warning" sx={{ mb: 2 }}>
            Please save this API key securely. You won't be able to see it again.
          </Alert>
          <TextField
            fullWidth
            label="API Key"
            value={generatedKey || ''}
            InputProps={{
              readOnly: true,
            }}
            sx={{ mb: 2 }}
          />
          <Button
            variant="outlined"
            onClick={() => generatedKey && copyToClipboard(generatedKey)}
            startIcon={<ViewIcon />}
          >
            Copy to Clipboard
          </Button>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowKeyDialog(false)} variant="contained">
            I've Saved the Key
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ApiKeyManagement;

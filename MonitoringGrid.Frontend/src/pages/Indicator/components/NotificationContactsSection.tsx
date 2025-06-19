import React from 'react';
import {
  Grid,
  Typography,
  CardContent,
  Stack,
  Chip,
} from '@mui/material';
import {
  Person as PersonIcon,
} from '@mui/icons-material';
import { Card } from '@/components';
import { IndicatorDto } from '@/types/api';

interface NotificationContactsSectionProps {
  indicator: IndicatorDto;
}

export const NotificationContactsSection: React.FC<NotificationContactsSectionProps> = ({
  indicator,
}) => {
  // Ensure we have a valid indicator object
  if (!indicator) {
    return null;
  }

  return (
    <Grid item xs={12}>
      <Card>
        <CardContent>
          <Typography
            variant="h6"
            gutterBottom
            sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
          >
            <PersonIcon color="primary" />
            Notification Contacts
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              label={`${indicator.ownerContact?.name || indicator.ownerName || 'Unknown'} (Owner)`}
              color="primary"
              variant="outlined"
            />
            {indicator.contacts && Array.isArray(indicator.contacts) && indicator.contacts.length > 0 ? (
              indicator.contacts.map(contact => (
                <Chip
                  key={contact.contactID || contact.contactId || Math.random()}
                  label={contact.name || 'Unknown Contact'}
                  variant="outlined"
                />
              ))
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
                No additional notification contacts configured
              </Typography>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default NotificationContactsSection;

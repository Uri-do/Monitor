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
              label={`${indicator.ownerContact?.name || 'Unknown'} (Owner)`}
              color="primary"
              variant="outlined"
            />
            {indicator.contacts.map(contact => (
              <Chip key={contact.contactID} label={contact.name} variant="outlined" />
            ))}
          </Stack>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default NotificationContactsSection;

import React from 'react';
import {
  Box,
  Typography,
  Stack,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  FileDownload as ExportIcon,
  GetApp as DownloadIcon,
  Print as PrintIcon,
  MoreVert as MoreIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { CustomButton } from '../Button';

interface DataTableHeaderProps {
  title: string;
  subtitle?: string;
  gradient: string;
  loading: boolean;
  refreshable: boolean;
  exportable: boolean;
  selected: any[];
  exportMenuAnchor: HTMLElement | null;
  bulkMenuAnchor: HTMLElement | null;
  onRefresh?: () => void;
  onExportMenuOpen: (event: React.MouseEvent<HTMLElement>) => void;
  onExportMenuClose: () => void;
  onBulkMenuOpen: (event: React.MouseEvent<HTMLElement>) => void;
  onBulkMenuClose: () => void;
  onExport: (format: 'csv' | 'excel' | 'pdf') => void;
  onBulkAction: (action: string) => void;
}

export const DataTableHeader: React.FC<DataTableHeaderProps> = ({
  title,
  subtitle,
  gradient,
  loading,
  refreshable,
  exportable,
  selected,
  exportMenuAnchor,
  bulkMenuAnchor,
  onRefresh,
  onExportMenuOpen,
  onExportMenuClose,
  onBulkMenuOpen,
  onBulkMenuClose,
  onExport,
  onBulkAction,
}) => {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
      <Box>
        <Typography variant="h5" fontWeight="bold" gutterBottom>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary">
            {subtitle}
          </Typography>
        )}
      </Box>

      {/* Action Buttons */}
      <Stack direction="row" spacing={1}>
        {refreshable && (
          <CustomButton
            variant="outlined"
            gradient={gradient}
            icon={<RefreshIcon />}
            onClick={onRefresh}
            disabled={loading}
          >
            Refresh
          </CustomButton>
        )}

        {exportable && (
          <>
            <CustomButton
              variant="outlined"
              gradient="success"
              icon={<ExportIcon />}
              onClick={onExportMenuOpen}
            >
              Export
            </CustomButton>
            <Menu
              anchorEl={exportMenuAnchor}
              open={Boolean(exportMenuAnchor)}
              onClose={onExportMenuClose}
            >
              <MenuItem onClick={() => onExport('csv')}>
                <DownloadIcon sx={{ mr: 1 }} /> Export CSV
              </MenuItem>
              <MenuItem onClick={() => onExport('excel')}>
                <DownloadIcon sx={{ mr: 1 }} /> Export Excel
              </MenuItem>
              <MenuItem onClick={() => onExport('pdf')}>
                <PrintIcon sx={{ mr: 1 }} /> Export PDF
              </MenuItem>
            </Menu>
          </>
        )}

        {selected.length > 0 && (
          <>
            <CustomButton
              variant="outlined"
              gradient="warning"
              icon={<MoreIcon />}
              onClick={onBulkMenuOpen}
            >
              Bulk Actions ({selected.length})
            </CustomButton>
            <Menu
              anchorEl={bulkMenuAnchor}
              open={Boolean(bulkMenuAnchor)}
              onClose={onBulkMenuClose}
            >
              <MenuItem onClick={() => onBulkAction('delete')}>
                <DeleteIcon sx={{ mr: 1 }} /> Delete Selected
              </MenuItem>
              <MenuItem onClick={() => onBulkAction('export')}>
                <ExportIcon sx={{ mr: 1 }} /> Export Selected
              </MenuItem>
            </Menu>
          </>
        )}
      </Stack>
    </Box>
  );
};

export default DataTableHeader;

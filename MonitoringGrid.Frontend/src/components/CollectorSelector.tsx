import React, { useState, useEffect } from 'react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Label } from './ui/label';
import { Alert, AlertDescription } from './ui/alert';
import { Loader2, Database, Clock, Activity } from 'lucide-react';
import { useActiveCollectors, useCollectorItemNames } from '../hooks/useMonitorStatistics';

interface CollectorSelectorProps {
  selectedCollectorId?: number;
  selectedItemName?: string;
  onCollectorChange: (collectorId: number) => void;
  onItemNameChange: (itemName: string) => void;
  disabled?: boolean;
  className?: string;
}

export const CollectorSelector: React.FC<CollectorSelectorProps> = ({
  selectedCollectorId,
  selectedItemName,
  onCollectorChange,
  onItemNameChange,
  disabled = false,
  className = '',
}) => {
  const [internalCollectorId, setInternalCollectorId] = useState<number | undefined>(selectedCollectorId);
  
  const {
    data: collectors,
    isLoading: collectorsLoading,
    error: collectorsError,
  } = useActiveCollectors();

  const {
    data: itemNames,
    isLoading: itemNamesLoading,
    error: itemNamesError,
  } = useCollectorItemNames(internalCollectorId || 0);

  // Update internal state when props change
  useEffect(() => {
    setInternalCollectorId(selectedCollectorId);
  }, [selectedCollectorId]);

  const handleCollectorChange = (value: string) => {
    const collectorId = parseInt(value, 10);
    setInternalCollectorId(collectorId);
    onCollectorChange(collectorId);
    // Clear item name when collector changes
    onItemNameChange('');
  };

  const handleItemNameChange = (value: string) => {
    onItemNameChange(value);
  };

  const selectedCollector = collectors?.find(c => c.collectorID === internalCollectorId);

  if (collectorsError) {
    return (
      <Alert variant="destructive" className={className}>
        <AlertDescription>
          Failed to load collectors: {collectorsError.message}
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Collector Selection */}
      <div className="space-y-2">
        <Label htmlFor="collector-select" className="text-sm font-medium">
          <Database className="inline w-4 h-4 mr-1" />
          Data Collector
        </Label>
        <Select
          value={internalCollectorId?.toString() || ''}
          onValueChange={handleCollectorChange}
          disabled={disabled || collectorsLoading}
        >
          <SelectTrigger id="collector-select" className="w-full">
            <SelectValue placeholder={
              collectorsLoading ? (
                <span className="flex items-center">
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Loading collectors...
                </span>
              ) : (
                "Select a data collector"
              )
            } />
          </SelectTrigger>
          <SelectContent>
            {collectors?.map((collector) => (
              <SelectItem key={collector.collectorID} value={collector.collectorID.toString()}>
                <div className="flex flex-col">
                  <span className="font-medium">{collector.displayName}</span>
                  <span className="text-xs text-muted-foreground">
                    ID: {collector.collectorID} • {collector.frequencyDisplay}
                  </span>
                </div>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        
        {/* Collector Details */}
        {selectedCollector && (
          <div className="p-3 bg-muted rounded-md text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="flex items-center">
                <Activity className="w-3 h-3 mr-1" />
                <span className="text-muted-foreground">Status:</span>
                <span className={`ml-1 font-medium ${
                  selectedCollector.isActiveStatus ? 'text-green-600' : 'text-red-600'
                }`}>
                  {selectedCollector.statusDisplay}
                </span>
              </div>
              <div className="flex items-center">
                <Clock className="w-3 h-3 mr-1" />
                <span className="text-muted-foreground">Last Run:</span>
                <span className="ml-1 font-medium">{selectedCollector.lastRunDisplay}</span>
              </div>
            </div>
            {selectedCollector.collectorDesc && (
              <div className="mt-2 text-muted-foreground">
                {selectedCollector.collectorDesc}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Item Name Selection */}
      {internalCollectorId && (
        <div className="space-y-2">
          <Label htmlFor="item-select" className="text-sm font-medium">
            Item Name
          </Label>
          
          {itemNamesError ? (
            <Alert variant="destructive">
              <AlertDescription>
                Failed to load item names: {itemNamesError.message}
              </AlertDescription>
            </Alert>
          ) : (
            <Select
              value={selectedItemName || ''}
              onValueChange={handleItemNameChange}
              disabled={disabled || itemNamesLoading}
            >
              <SelectTrigger id="item-select" className="w-full">
                <SelectValue placeholder={
                  itemNamesLoading ? (
                    <span className="flex items-center">
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Loading items...
                    </span>
                  ) : (
                    "Select an item name"
                  )
                } />
              </SelectTrigger>
              <SelectContent>
                {itemNames?.map((itemName) => (
                  <SelectItem key={itemName} value={itemName}>
                    {itemName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          
          {itemNames && itemNames.length === 0 && !itemNamesLoading && (
            <Alert>
              <AlertDescription>
                No item names found for this collector. The collector may not have any data yet.
              </AlertDescription>
            </Alert>
          )}
        </div>
      )}
    </div>
  );
};

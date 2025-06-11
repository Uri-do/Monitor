# Frontend Enhancement Migration Guide

This guide helps you migrate existing components to use the new enhanced hooks and patterns.

## 🔄 Migration Steps

### Step 1: Replace Data Fetching Hooks

#### Before (Manual State Management)
```tsx
const [data, setData] = useState([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState(null);

useEffect(() => {
  const fetchData = async () => {
    try {
      setLoading(true);
      const result = await api.getData();
      setData(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };
  fetchData();
}, []);
```

#### After (Enhanced Hooks)
```tsx
const { data = [], isLoading, error } = useEnhancedDataHook();
```

### Step 2: Replace Mutation Logic

#### Before (Inline Mutations)
```tsx
const queryClient = useQueryClient();

const createMutation = useMutation({
  mutationFn: api.create,
  onSuccess: () => {
    toast.success('Created successfully');
    queryClient.invalidateQueries(['data']);
  },
  onError: (error) => {
    toast.error(error.message);
  },
});
```

#### After (Custom Mutation Hooks)
```tsx
const createItem = useCreateItem();
// All success/error handling and cache invalidation is built-in
```

### Step 3: Update Component Imports

#### Add New Imports
```tsx
// Data fetching hooks
import { useKpis, useKpi } from '@/hooks/useKpis';
import { useAlerts, useAlert } from '@/hooks/useAlerts';
import { useContacts, useContact } from '@/hooks/useContacts';
import { useUsers, useUser, useRoles } from '@/hooks/useUsers';

// Mutation hooks
import {
  useCreateKpi,
  useUpdateKpi,
  useDeleteKpi,
  useExecuteKpi,
} from '@/hooks/mutations';

// Ultimate Enterprise Components
import {
  UltimateCard,
  UltimateButton,
  UltimateDialog,
  UltimateInputField,
  UltimateSelect,
  UltimateDataTable,
  UltimatePageHeader,
  UltimateStatusChip,
  UltimateLoadingSpinner,
} from '@/components/UltimateEnterprise';
```

#### Remove Old Imports
```tsx
// These have been removed during deep cleanup
import { DataTable, PageHeader, StatusChip, LoadingSpinner, FilterPanel } from '@/components/Common';
import { EnhancedDataTable, ModernDataTable, VirtualizedDataTable } from '@/components/Common';
```

## 📋 Component Migration Examples

### KPI List Component Migration

#### Before
```tsx
const KpiList = () => {
  const queryClient = useQueryClient();
  const [kpis, setKpis] = useState([]);
  const [loading, setLoading] = useState(true);

  const deleteMutation = useMutation({
    mutationFn: kpiApi.deleteKpi,
    onSuccess: () => {
      queryClient.invalidateQueries(['kpis']);
      toast.success('KPI deleted');
    },
  });

  useEffect(() => {
    const fetchKpis = async () => {
      try {
        const data = await kpiApi.getKpis();
        setKpis(data);
      } finally {
        setLoading(false);
      }
    };
    fetchKpis();
  }, []);

  return (
    <DataTable
      data={kpis}
      loading={loading}
      onDelete={(kpi) => deleteMutation.mutate(kpi.kpiId)}
    />
  );
};
```

#### After
```tsx
const KpiList = () => {
  const { data: kpis = [], isLoading } = useKpis();
  const deleteKpi = useDeleteKpi();

  return (
    <EnhancedDataTable
      data={kpis}
      loading={isLoading}
      defaultActions={{
        delete: (kpi) => deleteKpi.mutate(kpi.kpiId),
      }}
      columns={columns}
      rowKey="kpiId"
    />
  );
};
```

### Form Component Migration

#### Before
```tsx
const KpiCreate = () => {
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);

  const createMutation = useMutation({
    mutationFn: kpiApi.createKpi,
    onSuccess: () => {
      toast.success('KPI created');
      queryClient.invalidateQueries(['kpis']);
      setDialogOpen(false);
    },
  });

  return (
    <>
      <Button onClick={() => setDialogOpen(true)}>Create KPI</Button>
      <Dialog open={dialogOpen}>
        {/* Complex form JSX */}
      </Dialog>
    </>
  );
};
```

#### After
```tsx
const KpiCreate = () => {
  const [dialogOpen, setDialogOpen] = useState(false);
  const createKpi = useCreateKpi();

  const handleSubmit = (data) => {
    createKpi.mutate(data, {
      onSuccess: () => setDialogOpen(false),
    });
  };

  return (
    <>
      <Button onClick={() => setDialogOpen(true)}>Create KPI</Button>
      <KpiFormDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        onSubmit={handleSubmit}
        loading={createKpi.isPending}
      />
    </>
  );
};
```

## 🔧 Common Migration Patterns

### Pattern 1: Loading States
```tsx
// Before
const [loading, setLoading] = useState(true);

// After
const { isLoading } = useEnhancedHook();
```

### Pattern 2: Error Handling
```tsx
// Before
const [error, setError] = useState(null);
if (error) {
  toast.error(error);
}

// After
const { error } = useEnhancedHook();
// Error handling is automatic in mutation hooks
```

### Pattern 3: Cache Invalidation
```tsx
// Before
queryClient.invalidateQueries(['kpis']);
queryClient.invalidateQueries(['dashboard']);

// After
// Automatic in mutation hooks, no manual invalidation needed
```

### Pattern 4: Form Dialogs
```tsx
// Before
<Dialog open={open}>
  <DialogTitle>Create Item</DialogTitle>
  <DialogContent>
    {/* Complex form fields */}
  </DialogContent>
  <DialogActions>
    {/* Form buttons */}
  </DialogActions>
</Dialog>

// After
<ItemFormDialog
  open={open}
  onClose={handleClose}
  onSubmit={handleSubmit}
  loading={mutation.isPending}
/>
```

## ✅ Migration Checklist

### For Each Component:
- [ ] Replace manual data fetching with enhanced hooks
- [ ] Replace inline mutations with custom mutation hooks
- [ ] Update imports to use new hooks and components
- [ ] Remove manual loading/error state management
- [ ] Replace complex form dialogs with reusable components
- [ ] Update data table implementations to use EnhancedDataTable
- [ ] Test component functionality after migration

### For Each Hook:
- [ ] Verify proper query key usage
- [ ] Confirm appropriate staleTime and refetchInterval
- [ ] Test error handling and loading states
- [ ] Validate cache invalidation patterns

### For Each Mutation:
- [ ] Confirm success/error toast notifications
- [ ] Verify cache invalidation for related queries
- [ ] Test optimistic updates where applicable
- [ ] Validate loading states during mutations

## 🚀 Benefits After Migration

1. **Reduced Code**: ~70% less boilerplate code
2. **Better Performance**: Automatic caching and background updates
3. **Consistent UX**: Standardized loading states and error handling
4. **Easier Maintenance**: Centralized mutation logic
5. **Type Safety**: Full TypeScript support with proper inference
6. **Real-time Updates**: Automatic cache synchronization with SignalR

## 🔍 Testing After Migration

1. **Functionality**: Ensure all features work as before
2. **Performance**: Verify improved loading times
3. **Error Handling**: Test error scenarios
4. **Real-time**: Confirm real-time updates work correctly
5. **Cache**: Validate data consistency across components

## 📞 Support

If you encounter issues during migration:
1. Check the comprehensive examples in `FRONTEND_ENHANCEMENTS.md`
2. Review the hook implementations for usage patterns
3. Test with the enhanced data table and dialog components
4. Verify query key usage in `src/utils/queryKeys.ts`

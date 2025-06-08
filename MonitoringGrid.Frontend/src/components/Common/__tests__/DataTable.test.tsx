import { render, screen, fireEvent, waitFor } from '@/test/utils';
import { vi } from 'vitest';
import DataTable, { DataTableColumn } from '../DataTable';

// Mock data
const mockData = [
  { id: 1, name: 'John Doe', email: 'john@example.com', status: 'active' },
  { id: 2, name: 'Jane Smith', email: 'jane@example.com', status: 'inactive' },
  { id: 3, name: 'Bob Johnson', email: 'bob@example.com', status: 'active' },
];

const mockColumns: DataTableColumn[] = [
  { id: 'name', label: 'Name', sortable: true },
  { id: 'email', label: 'Email', sortable: true },
  {
    id: 'status',
    label: 'Status',
    render: value => <span style={{ color: value === 'active' ? 'green' : 'red' }}>{value}</span>,
  },
];

describe('DataTable', () => {
  it('renders table with data', () => {
    render(<DataTable columns={mockColumns} data={mockData} rowKey="id" />);

    // Check if headers are rendered
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();

    // Check if data is rendered
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('jane@example.com')).toBeInTheDocument();
    expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    render(<DataTable columns={mockColumns} data={[]} loading={true} rowKey="id" />);

    expect(screen.getByText('Loading data...')).toBeInTheDocument();
  });

  it('shows error state', () => {
    const errorMessage = 'Failed to load data';
    render(<DataTable columns={mockColumns} data={[]} error={errorMessage} rowKey="id" />);

    expect(screen.getByText(errorMessage)).toBeInTheDocument();
  });

  it('shows empty message when no data', () => {
    const emptyMessage = 'No data available';
    render(<DataTable columns={mockColumns} data={[]} emptyMessage={emptyMessage} rowKey="id" />);

    expect(screen.getByText(emptyMessage)).toBeInTheDocument();
  });

  it('handles row selection', async () => {
    const onSelectionChange = vi.fn();

    render(
      <DataTable
        columns={mockColumns}
        data={mockData}
        selectable={true}
        onSelectionChange={onSelectionChange}
        rowKey="id"
      />
    );

    // Click on first row checkbox
    const checkboxes = screen.getAllByRole('checkbox');
    fireEvent.click(checkboxes[1]); // First checkbox is select all

    await waitFor(() => {
      expect(onSelectionChange).toHaveBeenCalledWith([mockData[0]]);
    });
  });

  it('handles select all', async () => {
    const onSelectionChange = vi.fn();

    render(
      <DataTable
        columns={mockColumns}
        data={mockData}
        selectable={true}
        onSelectionChange={onSelectionChange}
        rowKey="id"
      />
    );

    // Click on select all checkbox
    const selectAllCheckbox = screen.getAllByRole('checkbox')[0];
    fireEvent.click(selectAllCheckbox);

    await waitFor(() => {
      expect(onSelectionChange).toHaveBeenCalledWith(mockData);
    });
  });

  it('renders custom actions', () => {
    const mockAction = vi.fn();

    render(
      <DataTable
        columns={mockColumns}
        data={mockData}
        actions={[
          {
            label: 'Edit',
            icon: <span>Edit</span>,
            onClick: mockAction,
          },
        ]}
        rowKey="id"
      />
    );

    // Check if action buttons are rendered
    const editButtons = screen.getAllByLabelText('Edit');
    expect(editButtons).toHaveLength(mockData.length);
  });

  it('renders default actions', () => {
    const mockView = vi.fn();
    const mockEdit = vi.fn();
    const mockDelete = vi.fn();

    render(
      <DataTable
        columns={mockColumns}
        data={mockData}
        defaultActions={{
          view: mockView,
          edit: mockEdit,
          delete: mockDelete,
        }}
        rowKey="id"
      />
    );

    // Check if default action buttons are rendered
    expect(screen.getAllByLabelText('View')).toHaveLength(mockData.length);
    expect(screen.getAllByLabelText('Edit')).toHaveLength(mockData.length);
    expect(screen.getAllByLabelText('Delete')).toHaveLength(mockData.length);
  });

  it('handles pagination', () => {
    const mockPageChange = vi.fn();
    const mockRowsPerPageChange = vi.fn();

    render(
      <DataTable
        columns={mockColumns}
        data={mockData}
        pagination={{
          page: 0,
          rowsPerPage: 10,
          totalCount: 100,
          onPageChange: mockPageChange,
          onRowsPerPageChange: mockRowsPerPageChange,
        }}
        rowKey="id"
      />
    );

    // Check if pagination is rendered
    expect(screen.getByText('1–3 of 100')).toBeInTheDocument();
  });

  it('applies custom row className', () => {
    const rowClassName = (row: any) => (row.status === 'active' ? 'active-row' : 'inactive-row');

    const { container } = render(
      <DataTable columns={mockColumns} data={mockData} rowClassName={rowClassName} rowKey="id" />
    );

    // Check if custom classes are applied
    const rows = container.querySelectorAll('tbody tr');
    expect(rows[0]).toHaveClass('active-row');
    expect(rows[1]).toHaveClass('inactive-row');
    expect(rows[2]).toHaveClass('active-row');
  });
});

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@/test/utils';
import { CustomButton } from '../Button';

describe('CustomButton Component', () => {
  it('renders with default props', () => {
    render(<CustomButton>Click me</CustomButton>);

    const button = screen.getByRole('button', { name: /click me/i });
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('MuiButton-root');
  });

  it('renders with different variants', () => {
    const { rerender } = render(<CustomButton variant="contained">Contained</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-contained');

    rerender(<CustomButton variant="outlined">Outlined</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-outlined');

    rerender(<CustomButton variant="text">Text</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-text');
  });

  it('renders with different gradients', () => {
    const { rerender } = render(<CustomButton gradient="primary">Primary</CustomButton>);
    expect(screen.getByRole('button')).toBeInTheDocument();

    rerender(<CustomButton gradient="secondary">Secondary</CustomButton>);
    expect(screen.getByRole('button')).toBeInTheDocument();

    rerender(<CustomButton gradient="error">Error</CustomButton>);
    expect(screen.getByRole('button')).toBeInTheDocument();
  });

  it('renders with different sizes', () => {
    const { rerender } = render(<CustomButton size="small">Small</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-sizeSmall');

    rerender(<CustomButton size="medium">Medium</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-sizeMedium');

    rerender(<CustomButton size="large">Large</CustomButton>);
    expect(screen.getByRole('button')).toHaveClass('MuiButton-sizeLarge');
  });

  it('handles click events', () => {
    const handleClick = vi.fn();
    render(<CustomButton onClick={handleClick}>Click me</CustomButton>);

    const button = screen.getByRole('button');
    fireEvent.click(button);

    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('can be disabled', () => {
    const handleClick = vi.fn();
    render(<CustomButton disabled onClick={handleClick}>Disabled</CustomButton>);

    const button = screen.getByRole('button');
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('shows loading state', () => {
    render(<CustomButton loading>Loading</CustomButton>);

    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('renders with icon', () => {
    const TestIcon = () => <span data-testid="test-icon">Icon</span>;

    render(
      <CustomButton icon={<TestIcon />}>
        With Icon
      </CustomButton>
    );

    expect(screen.getByTestId('test-icon')).toBeInTheDocument();
  });

  it('renders as fullWidth', () => {
    render(<CustomButton fullWidth>Full Width</CustomButton>);

    const button = screen.getByRole('button');
    expect(button).toHaveClass('MuiButton-fullWidth');
  });

  it('renders with glow effect', () => {
    render(<CustomButton glowEffect={true}>Glow Button</CustomButton>);

    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(<CustomButton className="custom-class">Custom</CustomButton>);

    const button = screen.getByRole('button');
    expect(button).toHaveClass('custom-class');
  });

  it('supports different button types', () => {
    const { rerender } = render(<CustomButton type="button">Button</CustomButton>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');

    rerender(<CustomButton type="submit">Submit</CustomButton>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'submit');

    rerender(<CustomButton type="reset">Reset</CustomButton>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'reset');
  });
});

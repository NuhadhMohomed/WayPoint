import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { Button } from '../Button';
import { Input } from '../Input';
import { Modal } from '../Modal';
import { DataTable } from '../DataTable';
import { TransitBadge } from '../TransitBadge';
import { EmptyState } from '../EmptyState';
import { ErrorState } from '../ErrorState';
import { Skeleton } from '../Skeleton';
import { OfflineBanner } from '../OfflineBanner';

describe('UI Atomic Primitives', () => {
  it('renders Button with loading spinner and disabled state', () => {
    render(<Button isLoading>Submit</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders TransitBadge with correct variant styling', () => {
    render(<TransitBadge variant="luxury">Luxury Express</TransitBadge>);
    expect(screen.getByText('Luxury Express')).toHaveClass('bg-blue-50');
  });

  it('renders EmptyState with title and action button', () => {
    render(<EmptyState title="No routes found" actionLabel="Create Route" onAction={() => {}} />);
    expect(screen.getByText('No routes found')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create route/i })).toBeInTheDocument();
  });

  it('renders ErrorState with error message and retry button', () => {
    const handleRetry = vi.fn();
    render(<ErrorState title="Failed to load" message="Network error" onRetry={handleRetry} />);
    expect(screen.getByText('Failed to load')).toBeInTheDocument();
    expect(screen.getByText('Network error')).toBeInTheDocument();
    const retryBtn = screen.getByRole('button', { name: /retry request/i });
    fireEvent.click(retryBtn);
    expect(handleRetry).toHaveBeenCalledOnce();
  });

  it('renders Skeleton with pulse animation', () => {
    render(<Skeleton className="h-6 w-32" />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveClass('animate-pulse');
  });

  it('renders OfflineBanner when isOffline is true', () => {
    render(<OfflineBanner isOffline={true} />);
    expect(screen.getByText(/offline mode/i)).toBeInTheDocument();
  });

  it('renders Input with label, validation error, and clear button', () => {
    const handleClear = vi.fn();
    render(
      <Input
        label="Route Name"
        value="Colombo - Ella"
        error="Invalid route"
        onClear={handleClear}
        onChange={() => {}}
      />
    );
    expect(screen.getByText(/Route Name/i)).toBeInTheDocument();
    expect(screen.getByText('Invalid route')).toBeInTheDocument();
    const clearBtn = screen.getByTitle('Clear input');
    fireEvent.click(clearBtn);
    expect(handleClear).toHaveBeenCalledOnce();
  });

  it('renders Modal dialog with title and children when open', () => {
    const handleClose = vi.fn();
    render(
      <Modal isOpen={true} onClose={handleClose} title="Edit Service">
        <p>Modal Body Content</p>
      </Modal>
    );
    expect(screen.getByText('Edit Service')).toBeInTheDocument();
    expect(screen.getByText('Modal Body Content')).toBeInTheDocument();
    const closeBtn = screen.getByTitle('Close modal');
    fireEvent.click(closeBtn);
    expect(handleClose).toHaveBeenCalledOnce();
  });

  it('renders DataTable with columns and data rows', () => {
    const columns = [
      { key: 'name', header: 'Route Name' },
      { key: 'distance', header: 'Distance (km)' },
    ];
    const data = [
      { id: 1, name: 'Colombo to Kandy', distance: 115 },
      { id: 2, name: 'Colombo to Galle', distance: 125 },
    ];
    render(<DataTable columns={columns} data={data} showExport={false} />);
    expect(screen.getByText('Route Name')).toBeInTheDocument();
    expect(screen.getByText('Colombo to Kandy')).toBeInTheDocument();
    expect(screen.getByText('115')).toBeInTheDocument();
  });
});

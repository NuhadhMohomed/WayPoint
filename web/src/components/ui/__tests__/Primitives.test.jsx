import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { Button } from '../Button';
import { TransitBadge } from '../TransitBadge';
import { EmptyState } from '../EmptyState';
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

  it('renders Skeleton with pulse animation', () => {
    render(<Skeleton className="h-6 w-32" />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveClass('animate-pulse');
  });

  it('renders OfflineBanner when isOffline is true', () => {
    render(<OfflineBanner isOffline={true} />);
    expect(screen.getByText(/offline mode/i)).toBeInTheDocument();
  });
});

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { IntegrationsTab } from './IntegrationsTab';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_key: string, fallback: string) => fallback })
}));

describe('IntegrationsTab', () => {
  it('renders correctly', () => {
    render(<IntegrationsTab partyId="123" />);
    expect(screen.getByText(/Git Webhooks/i)).toBeDefined();
    expect(screen.getByText(/Webhook URL/i)).toBeDefined();
  });

  it('regenerates secret optimistically', async () => {
    render(<IntegrationsTab partyId="123" />);
    const secretInput = screen.getAllByRole('textbox')[1] as HTMLInputElement;
    const initialSecret = secretInput.value;
    
    const regenBtn = screen.getByText(/Regenerar/i);
    fireEvent.click(regenBtn);
    
    await waitFor(() => {
      expect(secretInput.value).not.toBe(initialSecret);
      expect(secretInput.value).toContain('spar-sec-');
    });
  });
});

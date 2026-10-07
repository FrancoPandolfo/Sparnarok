import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CustomRulesTab } from './CustomRulesTab';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_key: string, fallback: string) => fallback })
}));

describe('CustomRulesTab', () => {
  it('renders paywall and disables inputs when isPremium is false', () => {
    render(<CustomRulesTab isPremium={false} />);
    
    expect(screen.getByText(/Desbloquea las Reglas Custom/i)).toBeDefined();
    
    const inputs = screen.getAllByRole('textbox');
    inputs.forEach(input => {
      expect((input as HTMLInputElement).disabled).toBe(true);
    });
  });

  it('renders active form without paywall when isPremium is true', () => {
    render(<CustomRulesTab isPremium={true} />);
    
    expect(screen.queryByText(/Desbloquea las Reglas Custom/i)).toBeNull();
    
    const inputs = screen.getAllByRole('textbox');
    inputs.forEach(input => {
      expect((input as HTMLInputElement).disabled).toBe(false);
    });
  });
});

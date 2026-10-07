import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SessionZeroModal } from './SessionZeroModal';
import { SENIORITY_PRESETS } from '../../../constants/SeniorityMapper';

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string, fallback: string) => fallback })
}));

describe('SessionZeroModal', () => {
  it('should auto-fill XP inputs when a Seniority is selected', () => {
    render(<SessionZeroModal userId="test-1" onClose={() => {}} onSave={() => {}} />);
    
    const select = screen.getByTestId('seniority-select');
    fireEvent.change(select, { target: { value: 'Senior' } });
    
    const backendInput = screen.getByTestId('xp-input-Backend') as HTMLInputElement;
    const frontendInput = screen.getByTestId('xp-input-Frontend') as HTMLInputElement;
    
    expect(backendInput.value).toBe(SENIORITY_PRESETS.Senior.Backend.toString());
    expect(frontendInput.value).toBe(SENIORITY_PRESETS.Senior.Frontend.toString());
  });
});

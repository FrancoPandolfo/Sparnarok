import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { QuestBoard } from './QuestBoard';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../../i18n';

describe('QuestBoard', () => {
  it('renders all three columns with correct translations in Spanish', () => {
    i18n.changeLanguage('es');
    
    render(
      <I18nextProvider i18n={i18n}>
        <QuestBoard />
      </I18nextProvider>
    );

    expect(screen.getByText(/Pendiente/i)).toBeDefined();
    expect(screen.getByText(/En Progreso/i)).toBeDefined();
    expect(screen.getByText(/Completada/i)).toBeDefined();
  });
});

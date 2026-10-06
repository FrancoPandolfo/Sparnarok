import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { CreateQuestForm } from './CreateQuestForm';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../../../i18n';

describe('CreateQuestForm', () => {
  it('renders the form with correct translations in Spanish', () => {
    // Arrange
    i18n.changeLanguage('es');

    // Act
    render(
      <I18nextProvider i18n={i18n}>
        <CreateQuestForm />
      </I18nextProvider>
    );

    // Assert
    expect(screen.getByText('Forjar una Nueva Quest')).toBeDefined();
    expect(screen.getByLabelText('Nombre de la Quest')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Crear Quest' })).toBeDefined();
  });
});

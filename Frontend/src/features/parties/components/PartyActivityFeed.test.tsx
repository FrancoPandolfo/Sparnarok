import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { PartyActivityFeed } from './PartyActivityFeed';
import { useFeedStore } from '../useFeedStore';
import { useAuthStore } from '../../auth/useAuthStore';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../../i18n';

describe('PartyActivityFeed', () => {
  beforeEach(() => {
    useFeedStore.setState({ feed: [] });
    useAuthStore.setState({ userId: 'sparky-id', username: 'Sparky' });
    vi.clearAllMocks();
  });

  it('formats ally notifications as "El aliado {username}"', async () => {
    const listeners: Record<string, Function> = {};
    const mockConnection = {
      on: vi.fn((event, callback) => {
        listeners[event] = callback;
      }),
      off: vi.fn((event) => {
        delete listeners[event];
      })
    };

    render(
      <I18nextProvider i18n={i18n}>
        <PartyActivityFeed connection={mockConnection} />
      </I18nextProvider>
    );

    await act(async () => {
      listeners['NewPartyActivity']?.({
        id: 'activity-1',
        username: 'Fran',
        sourceUserId: 'fran-id',
        message: "El aliado Fran completó 'Deploy a Prod'",
        xp: 120,
        questId: 'quest-999'
      });
    });

    expect(await screen.findByText('El aliado Fran')).toBeDefined();
    expect(await screen.findByText(/completó 'Deploy a Prod' \(\+120 XP\)/)).toBeDefined();
  });

  it('deduplicates activity when current user already has optimistic entry for the quest', () => {
    // Add optimistic item for Sparky
    useFeedStore.setState({
      feed: [
        {
          id: 'optimistic-quest-123',
          type: 'quest_completed',
          user: 'Sparky',
          action: "ha completado la tarea 'Bug Fix' (+50 XP)",
          time: 'Ahora'
        }
      ]
    });

    const listeners: Record<string, Function> = {};
    const mockConnection = {
      on: vi.fn((event, callback) => {
        listeners[event] = callback;
      }),
      off: vi.fn()
    };

    render(
      <I18nextProvider i18n={i18n}>
        <PartyActivityFeed connection={mockConnection} />
      </I18nextProvider>
    );

    // Broadcast arrives back for Sparky
    listeners['NewPartyActivity']?.({
      id: 'broadcast-456',
      username: 'Sparky',
      sourceUserId: 'sparky-id',
      message: "El aliado Sparky completó 'Bug Fix'",
      xp: 50,
      questId: 'quest-123'
    });

    // Should NOT create duplicate item
    const feed = useFeedStore.getState().feed;
    expect(feed.length).toBe(1);
    expect(feed[0].user).toBe('Sparky');
    expect(feed[0].action).toBe("ha completado la tarea 'Bug Fix' (+50 XP)");
  });
});

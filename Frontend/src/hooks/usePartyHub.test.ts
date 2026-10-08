import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { usePartyHub } from './usePartyHub';
import * as signalR from '@microsoft/signalr';

vi.mock('@microsoft/signalr', () => {
  const startMock = vi.fn().mockResolvedValue(undefined);
  const stopMock = vi.fn().mockResolvedValue(undefined);
  const onMock = vi.fn();
  const offMock = vi.fn();

  class MockHubConnectionBuilder {
    withUrl() { return this; }
    withAutomaticReconnect() { return this; }
    build() {
      return {
        start: startMock,
        stop: stopMock,
        on: onMock,
        off: offMock,
      };
    }
  }

  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    _startMock: startMock
  };
});

describe('usePartyHub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize SignalR connection and connect when partyId is provided', async () => {
    const { result } = renderHook(() => usePartyHub('test-party-id', 'mock-token'));

    expect(result.current).toBeDefined();

    await waitFor(() => {
      expect(result.current?.start).toHaveBeenCalled();
    });
  });

  it('should not initialize connection if partyId is missing', () => {
    const { result } = renderHook(() => usePartyHub('', 'mock-token'));
    
    expect(result.current).toBeNull();
  });
});

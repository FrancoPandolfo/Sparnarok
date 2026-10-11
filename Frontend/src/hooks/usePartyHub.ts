import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export const usePartyHub = (partyId: string, token: string | null) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!partyId) return;

    const cleanPartyId = partyId.trim();

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`/api/partyHub?partyId=${cleanPartyId}`, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [partyId, token]);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected to PartyHub');
          if (partyId && typeof connection.invoke === 'function') {
            connection.invoke('JoinPartyGroup', partyId).catch(() => {});
          }
        })
        .catch(e => console.log('SignalR Connection failed: ', e));

      return () => {
        connection.stop();
      };
    }
  }, [connection, partyId]);

  return connection;
};

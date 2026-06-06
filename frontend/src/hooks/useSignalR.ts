import { useEffect } from 'react';
import { HubConnectionState } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { hub } from '../api/signalr';

export function useSignalR() {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (hub.state === HubConnectionState.Disconnected) {
      hub.start().catch(() => {});
    }

    hub.on('SigAdded', (sig) => queryClient.invalidateQueries({ queryKey: ['sigs', sig.chainSystemId] }));
    hub.on('SigUpdated', (sig) => queryClient.invalidateQueries({ queryKey: ['sigs', sig.chainSystemId] }));
    hub.on('SigCleared', ({ systemId }) => queryClient.invalidateQueries({ queryKey: ['sigs', systemId] }));
    hub.on('SigsBulkUpdated', ({ systemId }) => queryClient.invalidateQueries({ queryKey: ['sigs', systemId] }));
    hub.on('SystemAdded', () => queryClient.invalidateQueries({ queryKey: ['chain'] }));
    hub.on('SystemRemoved', () => queryClient.invalidateQueries({ queryKey: ['chain'] }));
    hub.on('ConnectionAdded', () => queryClient.invalidateQueries({ queryKey: ['chain'] }));
    hub.on('ConnectionUpdated', () => queryClient.invalidateQueries({ queryKey: ['chain'] }));
    hub.on('ConnectionCollapsed', () => queryClient.invalidateQueries({ queryKey: ['chain'] }));
    hub.on('MassPassLogged', ({ connectionId }) => queryClient.invalidateQueries({ queryKey: ['mass', connectionId] }));
    hub.on('MassStatusChanged', ({ connectionId }) => queryClient.invalidateQueries({ queryKey: ['mass', connectionId] }));
    hub.on('RollingSessionStarted', () => queryClient.invalidateQueries({ queryKey: ['rolling'] }));
    hub.on('RollingSessionUpdated', () => queryClient.invalidateQueries({ queryKey: ['rolling'] }));
    hub.on('RollingSessionCompleted', () => {
      queryClient.invalidateQueries({ queryKey: ['chain'] });
      queryClient.invalidateQueries({ queryKey: ['rolling'] });
    });
    hub.on('ActivityEvent', () => queryClient.invalidateQueries({ queryKey: ['activity'] }));

    return () => {
      hub.off('SigAdded');
      hub.off('SigUpdated');
      hub.off('SigCleared');
      hub.off('SigsBulkUpdated');
      hub.off('SystemAdded');
      hub.off('SystemRemoved');
      hub.off('ConnectionAdded');
      hub.off('ConnectionUpdated');
      hub.off('ConnectionCollapsed');
      hub.off('MassPassLogged');
      hub.off('MassStatusChanged');
      hub.off('RollingSessionStarted');
      hub.off('RollingSessionUpdated');
      hub.off('RollingSessionCompleted');
      hub.off('ActivityEvent');
    };
  }, [queryClient]);
}

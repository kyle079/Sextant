import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const hub = new HubConnectionBuilder()
  .withUrl('/hub/sextant')
  .withAutomaticReconnect()
  .configureLogging(LogLevel.Warning)
  .build();

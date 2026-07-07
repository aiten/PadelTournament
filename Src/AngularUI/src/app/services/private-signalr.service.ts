import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import Keycloak from 'keycloak-js';
import { TournamentUpdatedMessage } from '../models/signalr-messages';

@Injectable({ providedIn: 'root' })
export class PrivateSignalRService implements OnDestroy {
  private readonly connection: signalR.HubConnection;
  private readonly ready: Promise<void>;
  private readonly joinedTournamentIds = new Set<number>();
  private readonly onVisible = () => this.ensureConnected();

  readonly tournamentUpdated$ = new Subject<TournamentUpdatedMessage>();
  /** Fires after a reconnect (or a resume from background) that may have caused missed notifications, so listeners can refetch. */
  readonly reconnected$ = new Subject<void>();

  constructor(private keycloak: Keycloak) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tournament', {
        accessTokenFactory: () => Promise.resolve(this.keycloak.token ?? ''),
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('TournamentUpdated', (tournamentId: number) =>
      this.tournamentUpdated$.next({ tournamentId }),
    );

    // A reconnect gets a new ConnectionId, so the server-side group membership
    // (tied to Context.ConnectionId) is gone until we rejoin.
    this.connection.onreconnected(() => {
      this.rejoinGroups();
      this.reconnected$.next();
    });

    this.ready = keycloak.authenticated
      ? this.connection.start()
      : Promise.resolve();

    // iOS Safari frequently suspends the WebSocket when a tab is backgrounded
    // (e.g. switching tabs) without ever cleanly signalling the client, so
    // automatic reconnect may not kick in on its own. Re-check when the tab
    // becomes visible again.
    document.addEventListener('visibilitychange', this.onVisible);
  }

  joinTournamentGroup(tournamentId: number): Promise<void> {
    this.joinedTournamentIds.add(tournamentId);
    return this.ready.then(() => this.connection.invoke('JoinTournamentGroup', tournamentId));
  }

  leaveTournamentGroup(tournamentId: number): Promise<void> {
    this.joinedTournamentIds.delete(tournamentId);
    return this.ready.then(() => this.connection.invoke('LeaveTournamentGroup', tournamentId));
  }

  private rejoinGroups(): void {
    for (const tournamentId of this.joinedTournamentIds) {
      this.connection.invoke('JoinTournamentGroup', tournamentId);
    }
  }

  private async ensureConnected(): Promise<void> {
    if (document.visibilityState !== 'visible' || !this.keycloak.authenticated) return;

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
        this.rejoinGroups();
      } catch {
        return;
      }
    }

    this.reconnected$.next();
  }

  ngOnDestroy(): void {
    document.removeEventListener('visibilitychange', this.onVisible);
    this.connection.stop();
    this.tournamentUpdated$.complete();
    this.reconnected$.complete();
  }
}

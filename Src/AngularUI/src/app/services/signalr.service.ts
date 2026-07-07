import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { TournamentMatchUpdatedMessage, TournamentTeamUpdatedMessage } from '../models/signalr-messages';

@Injectable({ providedIn: 'root' })
export class PublicSignalRService implements OnDestroy {
  private readonly connection: signalR.HubConnection;
  private readonly ready: Promise<void>;
  private readonly joinedPins = new Set<string>();
  private readonly onVisible = () => this.ensureConnected();

  readonly tournamentMatchUpdated$ = new Subject<TournamentMatchUpdatedMessage>();
  readonly tournamentTeamUpdated$ = new Subject<TournamentTeamUpdatedMessage>();
  /** Fires after a reconnect (or a resume from background) that may have caused missed notifications, so listeners can refetch. */
  readonly reconnected$ = new Subject<void>();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/public')
      .withAutomaticReconnect()
      .build();

    this.connection.on('TournamentMatchUpdated', (pin: string, matchId: number | null) =>
      this.tournamentMatchUpdated$.next({ pin, matchId }),
    );

    this.connection.on('TournamentTeamUpdated', (pin: string) =>
      this.tournamentTeamUpdated$.next({ pin }),
    );

    // A reconnect gets a new ConnectionId, so the server-side group membership
    // (tied to Context.ConnectionId) is gone until we rejoin.
    this.connection.onreconnected(() => {
      this.rejoinGroups();
      this.reconnected$.next();
    });

    this.ready = this.connection.start();

    // iOS Safari frequently suspends the WebSocket when a tab is backgrounded
    // (e.g. switching tabs) without ever cleanly signalling the client, so
    // automatic reconnect may not kick in on its own. Re-check when the tab
    // becomes visible again.
    document.addEventListener('visibilitychange', this.onVisible);
  }

  joinTournamentGroup(pin: string): Promise<void> {
    this.joinedPins.add(pin);
    return this.ready.then(() => this.connection.invoke('JoinTournamentGroup', pin));
  }

  leaveTournamentGroup(pin: string): Promise<void> {
    this.joinedPins.delete(pin);
    return this.ready.then(() => this.connection.invoke('LeaveTournamentGroup', pin));
  }

  private rejoinGroups(): void {
    for (const pin of this.joinedPins) {
      this.connection.invoke('JoinTournamentGroup', pin);
    }
  }

  private async ensureConnected(): Promise<void> {
    if (document.visibilityState !== 'visible') return;

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
        this.rejoinGroups();
      } catch {
        return;
      }
    }

    // Even if the connection never dropped, treat every return-to-foreground
    // as a chance we missed a notification, and let listeners refetch.
    this.reconnected$.next();
  }

  ngOnDestroy(): void {
    document.removeEventListener('visibilitychange', this.onVisible);
    this.connection.stop();
    this.tournamentMatchUpdated$.complete();
    this.tournamentTeamUpdated$.complete();
    this.reconnected$.complete();
  }
}

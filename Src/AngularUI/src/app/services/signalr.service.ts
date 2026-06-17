import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { TournamentMatchUpdatedMessage, TournamentTeamUpdatedMessage } from '../models/signalr-messages';

@Injectable({ providedIn: 'root' })
export class PublicSignalRService implements OnDestroy {
  private readonly connection: signalR.HubConnection;
  private readonly ready: Promise<void>;

  readonly tournamentMatchUpdated$ = new Subject<TournamentMatchUpdatedMessage>();
  readonly tournamentTeamUpdated$ = new Subject<TournamentTeamUpdatedMessage>();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/public')
      .withAutomaticReconnect()
      .build();

    this.connection.on('TournamentMatchUpdated', (tournamentId: number) =>
      this.tournamentMatchUpdated$.next({ tournamentId }),
    );

    this.connection.on('TournamentTeamUpdated', (tournamentId: number) =>
      this.tournamentTeamUpdated$.next({ tournamentId }),
    );

    this.ready = this.connection.start();
  }

  joinTournamentGroup(tournamentId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('JoinTournamentGroup', tournamentId));
  }

  leaveTournamentGroup(tournamentId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('LeaveTournamentGroup', tournamentId));
  }

  ngOnDestroy(): void {
    this.connection.stop();
    this.tournamentMatchUpdated$.complete();
    this.tournamentTeamUpdated$.complete();
  }
}

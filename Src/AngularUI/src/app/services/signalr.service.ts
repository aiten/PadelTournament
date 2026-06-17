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

    this.connection.on('TournamentMatchUpdated', (pin: number) =>
      this.tournamentMatchUpdated$.next({ pin }),
    );

    this.connection.on('TournamentTeamUpdated', (pin: number) =>
      this.tournamentTeamUpdated$.next({ pin }),
    );

    this.ready = this.connection.start();
  }

  joinTournamentGroup(pin: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('JoinTournamentGroup', pin));
  }

  leaveTournamentGroup(pin: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('LeaveTournamentGroup', pin));
  }

  ngOnDestroy(): void {
    this.connection.stop();
    this.tournamentMatchUpdated$.complete();
    this.tournamentTeamUpdated$.complete();
  }
}

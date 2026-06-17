import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import Keycloak from 'keycloak-js';
import { TournamentUpdatedMessage } from '../models/signalr-messages';

@Injectable({ providedIn: 'root' })
export class PrivateSignalRService implements OnDestroy {
  private readonly connection: signalR.HubConnection;
  private readonly ready: Promise<void>;

  readonly tournamentUpdated$ = new Subject<TournamentUpdatedMessage>();

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

    this.ready = keycloak.authenticated
      ? this.connection.start()
      : Promise.resolve();
  }

  joinTournamentGroup(tournamentId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('JoinTournamentGroup', tournamentId));
  }

  leaveTournamentGroup(tournamentId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('LeaveTournamentGroup', tournamentId));
  }

  ngOnDestroy(): void {
    this.connection.stop();
    this.tournamentUpdated$.complete();
  }
}

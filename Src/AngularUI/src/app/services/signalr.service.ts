import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import Keycloak from 'keycloak-js';
import {
  ExamUpdatedMessage,
} from '../models/signalr-messages';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private readonly connection: signalR.HubConnection;
  private readonly ready: Promise<void>;

  readonly examUpdated$ = new Subject<ExamUpdatedMessage>();

  constructor(private keycloak: Keycloak) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/examination', {
        accessTokenFactory: () => Promise.resolve(this.keycloak.token ?? ''),
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ExamUpdated', (examId: number) =>
      this.examUpdated$.next({ examId }),
    );

    this.ready = this.connection.start();
  }

  joinExamGroup(examId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('JoinExamGroup', examId));
  }

  leaveExamGroup(examId: number): Promise<void> {
    return this.ready.then(() => this.connection.invoke('LeaveExamGroup', examId));
  }

  ngOnDestroy(): void {
    this.connection.stop();
    this.examUpdated$.complete();
  }
}

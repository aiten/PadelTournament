import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TournamentRegistrationRequest, TournamentRegistrationResult } from '../models/registration.model';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly url = '/api/registration';
  result = signal<TournamentRegistrationResult | null>(null);

  constructor(private http: HttpClient) {}

  register(req: TournamentRegistrationRequest): Observable<TournamentRegistrationResult> {
    return this.http.post<TournamentRegistrationResult>(this.url, req);
  }
}

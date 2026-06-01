import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tournament } from '../models/tournament.model';
import { TournamentOverview } from '../models/tournament-overview.model';

@Injectable({ providedIn: 'root' })
export class TournamentService {
  private readonly url = '/api/tournament';

  constructor(private http: HttpClient) {}

  getAll(): Observable<TournamentOverview[]> {
    return this.http.get<TournamentOverview[]>(`${this.url}`);
  }

  getById(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.url}/${id}`);
  }

  create(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(this.url, tournament);
  }

  update(id: number, tournament: Tournament): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, tournament);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  generateSchedule(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/generate-schedule`, null);
  }

  revertSchedule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}/revert-schedule`);
  }
}

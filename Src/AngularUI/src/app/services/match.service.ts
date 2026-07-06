import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Match, MatchModify } from '../models/match.model';

@Injectable({ providedIn: 'root' })
export class MatchService {
  private readonly baseUrl = '/api/tournament';

  constructor(private http: HttpClient) {}

  getAll(tournamentId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.baseUrl}/${tournamentId}/matches`);
  }

  getById(tournamentId: number, id: number): Observable<Match> {
    return this.http.get<Match>(`${this.baseUrl}/${tournamentId}/matches/${id}`);
  }

  update(tournamentId: number, id: number, dto: MatchModify): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${tournamentId}/matches/${id}`, dto);
  }

  setWinner(tournamentId: number, id: number, winner: 'WonA' | 'WonB', result?: string | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${tournamentId}/matches/${id}/winner`, { winner, result });
  }
}

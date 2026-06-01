import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Team } from '../models/team.model';
import { TournamentRegistrationResult } from '../models/registration.model';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly baseUrl = '/api/tournament';

  constructor(private http: HttpClient) {}

  getAll(tournamentId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.baseUrl}/${tournamentId}/teams`);
  }

  getById(tournamentId: number, id: number): Observable<Team> {
    return this.http.get<Team>(`${this.baseUrl}/${tournamentId}/teams/${id}`);
  }

  create(tournamentId: number, team: Team): Observable<Team> {
    return this.http.post<Team>(`${this.baseUrl}/${tournamentId}/teams`, team);
  }

  update(tournamentId: number, id: number, team: Team): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${tournamentId}/teams/${id}`, team);
  }

  delete(tournamentId: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${tournamentId}/teams/${id}`);
  }

  registerBulk(tournamentId: number, teamsText: string): Observable<TournamentRegistrationResult[]> {
    return this.http.post<TournamentRegistrationResult[]>(`${this.baseUrl}/${tournamentId}/teams/bulk`, { teamsText });
  }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Team } from '../models/team.model';
import { Match } from '../models/match.model';
import { Tournament } from '../models/tournament.model';

@Injectable({ providedIn: 'root' })
export class PublicService {
  private baseUrl(pin: string, code: string): string {
    return `/api/public/${pin}/${code}`;
  }

  constructor(private http: HttpClient) {}

  getMyTeam(pin: string, code: string): Observable<Team> {
    return this.http.get<Team>(`${this.baseUrl(pin, code)}/team`);
  }

  getMyMatches(pin: string, code: string): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.baseUrl(pin, code)}/matches`);
  }

  reportResult(pin: string, code: string, matchId: number, won: boolean, result?: string | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl(pin, code)}/matches/${matchId}/result`, { won, result });
  }

  getTournament(pin: string): Observable<Tournament> {
    return this.http.get<Tournament>(`/api/public/${pin}`);
  }

  getTeams(pin: string): Observable<Team[]> {
    return this.http.get<Team[]>(`/api/public/${pin}/teams`);
  }

  getMatches(pin: string): Observable<Match[]> {
    return this.http.get<Match[]>(`/api/public/${pin}/matches`);
  }
}

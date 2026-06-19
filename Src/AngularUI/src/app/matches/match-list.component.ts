import { Component, OnDestroy, OnInit, signal, computed } from '@angular/core';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Match } from '../models/match.model';
import { Team } from '../models/team.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';
import { PublicSignalRService } from '../services/signalr.service';
import { TournamentService } from '../services/tournament.service';
import { Tournament } from '../models/tournament.model';

type SortCol = 'round' | 'no' | 'teamA' | 'teamB' | 'start' | 'result';

@Component({
  selector: 'app-match-list',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    th.sortable { cursor: pointer; user-select: none; white-space: nowrap; }
    th.sortable:hover { background: #e2e8f0; }
    .sort-icon { margin-left: 4px; font-size: .8em; opacity: .5; }
    th.sort-active .sort-icon { opacity: 1; }
  `],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Matches</h2>
        <a [routerLink]="['/tournaments', tournamentId, 'bracket']" class="btn">Bracket</a>
        @if (matches().length > 0) {
          <button type="button" class="btn btn-danger" (click)="confirmRevertSchedule()">Delete Matches</button>
        }
        <a routerLink="/tournaments" class="btn">Back</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && sortedMatches().length > 0) {
        <table class="table">
          <thead>
            <tr>
              <th class="sortable" [class.sort-active]="sortCol() === 'round'" (click)="sort('round')">
                Round <span class="sort-icon">{{ sortIcon('round') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'no'" (click)="sort('no')">
                No <span class="sort-icon">{{ sortIcon('no') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'teamA'" (click)="sort('teamA')">
                Team A <span class="sort-icon">{{ sortIcon('teamA') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'teamB'" (click)="sort('teamB')">
                Team B <span class="sort-icon">{{ sortIcon('teamB') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'start'" (click)="sort('start')">
                Start <span class="sort-icon">{{ sortIcon('start') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'result'" (click)="sort('result')">
                Result <span class="sort-icon">{{ sortIcon('result') }}</span>
              </th>
              <th>Remark</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (m of sortedMatches(); track m.id) {
              <tr>
                <td>{{ m.round }}</td>
                <td>{{ m.no }}</td>
                <td>{{ teamName(m.teamAId) }}</td>
                <td>{{ teamName(m.teamBId) }}</td>
                <td>{{ formatDateTime(m.start) }}</td>
                <td>{{ resultLabel(m.result) }}</td>
                <td>{{ m.remark ?? '' }}</td>
                <td>
                  <a [routerLink]="['/tournaments', tournamentId, 'matches', m.id]" class="btn btn-sm">Edit</a>
                  @if (m.teamAId && m.teamBId && !m.result) {
                    <button type="button" class="btn btn-sm btn-winner" (click)="setWinner(m, 'WonA')">{{ teamName(m.teamAId) }} wins</button>
                    <button type="button" class="btn btn-sm btn-winner" (click)="setWinner(m, 'WonB')">{{ teamName(m.teamBId) }} wins</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
      @if (!loading() && matches().length === 0) {
        <p class="empty">No matches found.</p>
      }
    </div>
  `
})
export class MatchListComponent implements OnInit, OnDestroy {
  tournamentId = 0;
  tournamentPin = '';
  matches = signal<Match[]>([]);
  teams = signal<Team[]>([]);
  loading = signal(false);
  sortCol = signal<SortCol>('round');
  sortAsc = signal(true);
  private signalRSub?: Subscription;

  sortedMatches = computed(() => {
    const col = this.sortCol();
    const asc = this.sortAsc();
    const teamMap = new Map(this.teams().map(t => [t.id, t.name]));
    return this.matches().slice().sort((a, b) => {
      let cmp: number;
      switch (col) {
        case 'round':  cmp = a.round - b.round; break;
        case 'no':     cmp = a.no - b.no; break;
        case 'teamA':  cmp = (teamMap.get(a.teamAId ?? 0) ?? '').localeCompare(teamMap.get(b.teamAId ?? 0) ?? ''); break;
        case 'teamB':  cmp = (teamMap.get(a.teamBId ?? 0) ?? '').localeCompare(teamMap.get(b.teamBId ?? 0) ?? ''); break;
        case 'start':  cmp = (a.start ?? '').localeCompare(b.start ?? ''); break;
        case 'result': cmp = (a.result ?? '').localeCompare(b.result ?? ''); break;
        default:       cmp = 0;
      }
      return asc ? cmp : -cmp;
    });
  });

  constructor(
    private matchService: MatchService,
    private teamService: TeamService,
    private tournamentService: TournamentService,
    private route: ActivatedRoute,
    private router: Router,
    private signalR: PublicSignalRService
  ) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    this.tournamentService.getById(this.tournamentId).subscribe({
      next: tournament => {
        this.tournamentPin = tournament.registrationPin ?? '';
        this.signalR.joinTournamentGroup(this.tournamentPin);
      }
    });
    this.teamService.getAll(this.tournamentId).subscribe({
      next: teams => this.teams.set(teams)
    });
    this.loadMatches();

    this.signalRSub = this.signalR.tournamentMatchUpdated$.subscribe(({ pin }) => {
      if (pin === this.tournamentPin) {
        this.loadMatches();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.tournamentPin) {
      this.signalR.leaveTournamentGroup(this.tournamentPin);
    }
    this.signalRSub?.unsubscribe();
  }

  private loadMatches(): void {
    this.loading.set(true);
    this.matchService.getAll(this.tournamentId).subscribe({
      next: data => { this.matches.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  confirmRevertSchedule(): void {
    if (!confirm('Delete all matches for this tournament?\nThis will revert the schedule and a new schedule must be generated again.\nResults of played matches will be lost.\nThis cannot be undone.')) return;
    this.tournamentService.revertSchedule(this.tournamentId).subscribe({
      next: () => { this.matches.set([]); },
      error: err => alert(err.error?.detail ?? 'Delete matches failed.')
    });
  }

  sort(col: SortCol): void {
    if (this.sortCol() === col) {
      this.sortAsc.update(v => !v);
    } else {
      this.sortCol.set(col);
      this.sortAsc.set(true);
    }
  }

  sortIcon(col: SortCol): string {
    if (this.sortCol() !== col) return '↕';
    return this.sortAsc() ? '▲' : '▼';
  }

  teamName(id: number | null): string {
    if (id === null) return '—';
    return this.teams().find(t => t.id === id)?.name ?? `#${id}`;
  }

  setWinner(m: Match, winner: 'WonA' | 'WonB'): void {
    this.matchService.setWinner(this.tournamentId, m.id, winner).subscribe({
      next: () => this.loadMatches(),
      error: err => alert(err.error?.detail ?? 'Set winner failed.')
    });
  }

  resultLabel(result: string | null): string {
    if (result === 'WonA') return 'Won A';
    if (result === 'WonB') return 'Won B';
    return '—';
  }

  formatDateTime(dateStr: string | null): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleString('de-AT');
  }
}

import { Component, OnDestroy, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Match } from '../models/match.model';
import { Team } from '../models/team.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';
import { PublicSignalRService } from '../services/signalr.service';
import { TournamentService } from '../services/tournament.service';
import { Tournament } from '../models/tournament.model';
import { MatchScoreInputComponent } from '../shared/match-score-input.component';

type SortCol = 'round' | 'no' | 'teamA' | 'teamB' | 'start' | 'result';

@Component({
  selector: 'app-match-list',
  standalone: true,
  imports: [RouterModule, MatchScoreInputComponent],
  styles: [`
    th.sortable { cursor: pointer; user-select: none; white-space: nowrap; }
    th.sortable:hover { background: #e2e8f0; }
    .sort-icon { margin-left: 4px; font-size: .8em; opacity: .5; }
    th.sort-active .sort-icon { opacity: 1; }
    .score-prompt-row td { background: #f8fafc; }
    .score-prompt { display: flex; flex-direction: column; gap: 10px; padding: 8px 0; }
    .score-prompt-actions { display: flex; gap: 8px; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
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
              <th>Score</th>
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
                <td>{{ setsLabel(m) }}</td>
                <td>{{ m.remark ?? '' }}</td>
                <td>
                  <a [routerLink]="['/tournaments', tournamentId, 'matches', m.id]" class="btn btn-sm">Edit</a>
                  @if (m.teamAId && m.teamBId && !m.result && scorePrompt()?.id !== m.id) {
                    <button type="button" class="btn btn-sm btn-winner" (click)="startReport(m, 'WonA')">{{ teamName(m.teamAId) }} wins</button>
                    <button type="button" class="btn btn-sm btn-winner" (click)="startReport(m, 'WonB')">{{ teamName(m.teamBId) }} wins</button>
                  }
                </td>
              </tr>
              @if (scorePrompt()?.id === m.id) {
                <tr class="score-prompt-row">
                  <td colspan="9">
                    <div class="score-prompt">
                      <strong>{{ teamName(pendingWinner() === 'WonA' ? m.teamAId : m.teamBId) }} wins — enter the set scores (optional)</strong>
                      <app-match-score-input
                        [teamALabel]="teamName(m.teamAId)"
                        [teamBLabel]="teamName(m.teamBId)"
                        [(value)]="scoreValue" />
                      @if (reportError()) {
                        <p class="error">{{ reportError() }}</p>
                      }
                      <div class="score-prompt-actions">
                        <button type="button" class="btn btn-sm btn-primary" [disabled]="submitting()" (click)="confirmReport(m)">Confirm</button>
                        <button type="button" class="btn btn-sm" [disabled]="submitting()" (click)="cancelReport()">Cancel</button>
                      </div>
                    </div>
                  </td>
                </tr>
              }
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

  scorePrompt   = signal<Match | null>(null);
  pendingWinner = signal<'WonA' | 'WonB' | null>(null);
  scoreValue    = signal<string | null>(null);
  submitting    = signal(false);
  reportError   = signal('');

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

    this.signalRSub = new Subscription();
    this.signalRSub.add(this.signalR.tournamentMatchUpdated$.subscribe(({ pin }) => {
      if (pin === this.tournamentPin) {
        this.loadMatches();
      }
    }));
    this.signalRSub.add(this.signalR.reconnected$.subscribe(() => this.loadMatches()));
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

  /** Formats the match's already-recorded sets as "6:4, 6:2, 7:6(2)" to prefill the score input. */
  private initialScoreValue(m: Match): string | null {
    if (!m.sets || m.sets.length === 0) return null;
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(', ');
  }

  startReport(m: Match, winner: 'WonA' | 'WonB'): void {
    this.reportError.set('');
    this.scoreValue.set(this.initialScoreValue(m));
    this.pendingWinner.set(winner);
    this.scorePrompt.set(m);
  }

  cancelReport(): void {
    this.scorePrompt.set(null);
    this.pendingWinner.set(null);
    this.scoreValue.set(null);
  }

  confirmReport(m: Match): void {
    const winner = this.pendingWinner();
    if (!winner) return;

    this.submitting.set(true);
    this.reportError.set('');
    this.matchService.setWinner(this.tournamentId, m.id, winner, this.scoreValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.scorePrompt.set(null);
        this.pendingWinner.set(null);
        this.scoreValue.set(null);
        this.loadMatches();
      },
      error: err => {
        this.submitting.set(false);
        this.reportError.set(err.error?.detail ?? 'Set winner failed.');
      }
    });
  }

  setsLabel(m: Match): string {
    if (!m.sets || m.sets.length === 0) return '';
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(' ');
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

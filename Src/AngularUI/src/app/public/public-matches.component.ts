import { Component, OnInit, OnDestroy, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { PublicService } from '../services/public.service';
import { PublicSignalRService } from '../services/signalr.service';
import { Team } from '../models/team.model';
import { Match } from '../models/match.model';
import { Tournament } from '../models/tournament.model';
import { MatchScoreInputComponent } from '../shared/match-score-input.component';

@Component({
  selector: 'app-public-matches',
  standalone: true,
  imports: [RouterModule, MatchScoreInputComponent],
  styles: [`
    .team-header { margin-bottom: 1.5rem; }
    .team-name { font-size: 1.4rem; font-weight: 700; }
    .team-sub { font-size: .85rem; color: #64748b; margin-top: 2px; }
    .match-table { width: 100%; border-collapse: collapse; }
    .match-table th { text-align: left; padding: 8px 12px; font-size: .8rem;
                      color: #64748b; border-bottom: 2px solid #e2e8f0; }
    .match-table td { padding: 10px 12px; border-bottom: 1px solid #f1f5f9; font-size: .9rem;
                      vertical-align: middle; }
    .match-table tr:hover td { background: #f8fafc; }
    .result-won  { color: #16a34a; font-weight: 700; }
    .result-lost { color: #dc2626; }
    .result-none { color: #94a3b8; font-style: italic; }
    .accept-won  { color: #16a34a; font-weight: 600; }
    .accept-lost { color: #dc2626; font-weight: 600; }
    .accept-none { color: #cbd5e1; }
    .my-team { font-weight: 600; }
    .action-btns { display: flex; gap: 6px; flex-wrap: wrap; }
    .score-prompt-row td { background: #f8fafc; }
    .score-prompt { display: flex; flex-direction: column; gap: 10px; padding: 8px 0; }
    .score-prompt-actions { display: flex; gap: 8px; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>My Matches</h2>
        <a [routerLink]="['/public', pin, code]" class="btn">Back</a>
      </div>

      @if (loading()) {
        <p class="empty">Loading…</p>
      } @else if (error()) {
        <p class="error">{{ error() }}</p>
      } @else {
        <div class="team-header">
          <div class="team-name">{{ team()?.name }}</div>
          <div class="team-sub">
            @if (tournament()) { {{ tournament()!.description }} · }
            @if (team()?.seed) { Seed: {{ team()?.seed }} · }
          </div>
        </div>

        @if (matches().length === 0) {
          <p class="empty">No matches scheduled yet.</p>
        } @else {
          <table class="match-table">
            <thead>
              <tr>
                <th>Round</th>
                <th>Match</th>
                <th>Start</th>
                <th>Team A</th>
                <th>Team B</th>
                <th>Result</th>
                <th>My Report</th>
                <th>Opp. Report</th>
                <th>Score</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (m of matches(); track m.id) {
                <tr>
                  <td>{{ m.round }}</td>
                  <td>{{ m.no }}</td>
                  <td>{{ formatTime(m.start) }}</td>
                  <td [class.my-team]="m.teamAId === team()?.id">{{ teamLabel(m.teamAId) }}</td>
                  <td [class.my-team]="m.teamBId === team()?.id">{{ teamLabel(m.teamBId) }}</td>
                  <td [class]="resultClass(m)">{{ resultLabel(m) }}</td>
                  <td [class]="acceptWonClass(myAcceptWon(m))">{{ acceptWonLabel(myAcceptWon(m)) }}</td>
                  <td [class]="acceptWonClass(oppAcceptWon(m))">{{ acceptWonLabel(oppAcceptWon(m)) }}</td>
                  <td>{{ setsLabel(m) }}</td>
                  <td>
                    @if (isPlayable(m) && scorePrompt()?.id !== m.id) {
                      <div class="action-btns">
                        <button type="button" class="btn btn-sm btn-primary"
                                [disabled]="submitting()"
                                (click)="startReport(m, true)">I Won</button>
                        <button type="button" class="btn btn-sm btn-danger"
                                [disabled]="submitting()"
                                (click)="startReport(m, false)">I Lost</button>
                      </div>
                    }
                  </td>
                </tr>
                @if (scorePrompt()?.id === m.id) {
                  <tr class="score-prompt-row">
                    <td colspan="10">
                      <div class="score-prompt">
                        <strong>{{ scoreWon() ? 'You won' : 'You lost' }} — enter the set scores</strong>
                        <app-match-score-input
                          [teamALabel]="teamLabel(m.teamAId)"
                          [teamBLabel]="teamLabel(m.teamBId)"
                          [(value)]="scoreValue" />
                        <div class="score-prompt-actions">
                          <button type="button" class="btn btn-sm btn-primary"
                                  [disabled]="submitting() || !scoreValue()"
                                  (click)="confirmReport(m)">Confirm</button>
                          <button type="button" class="btn btn-sm"
                                  [disabled]="submitting()"
                                  (click)="cancelReport()">Cancel</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        }

        @if (reportError()) {
          <p class="error" style="margin-top:12px">{{ reportError() }}</p>
        }
      }
    </div>
  `
})
export class PublicMatchesComponent implements OnInit, OnDestroy {
  team        = signal<Team | null>(null);
  tournament  = signal<Tournament | null>(null);
  matches     = signal<Match[]>([]);
  loading     = signal(true);
  submitting  = signal(false);
  error       = signal('');
  reportError = signal('');

  scorePrompt = signal<Match | null>(null);
  scoreWon    = signal(false);
  scoreValue  = signal<string | null>(null);

  pin       = '';
  code      = '';
  private teamNames   = signal(new Map<number, string>());
  private signalRSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private publicService: PublicService,
    private signalR: PublicSignalRService
  ) {}

  ngOnInit(): void {
    this.pin  = this.route.snapshot.paramMap.get('pin')!;
    this.code = this.route.snapshot.paramMap.get('code')!;
    this.load();

    this.signalR.joinTournamentGroup(this.pin);
    this.signalRSub = this.signalR.tournamentMatchUpdated$.subscribe(msg => {
      if (msg.pin === this.pin && this.isDisplayedMatch(msg.matchId)) {
        this.reloadMatches();
      }
    });
  }

  ngOnDestroy(): void {
    this.signalR.leaveTournamentGroup(this.pin);
    this.signalRSub?.unsubscribe();
  }

  private load(): void {
    this.loading.set(true);
    this.publicService.getMyTeam(this.pin, this.code).subscribe({
      next: team => {
        this.team.set(team);
        forkJoin({
          matches:     this.publicService.getMyMatches(this.pin, this.code),
          allTeams:    this.publicService.getTeams(this.pin),
          tournament:  this.publicService.getTournament(this.pin)
        }).subscribe({
          next: ({ matches, allTeams, tournament }) => {
            this.matches.set(matches);
            this.tournament.set(tournament);
            this.teamNames.set(new Map(allTeams.map(t => [t.id, t.name])));
            this.loading.set(false);
          },
          error: err => { this.error.set(err.error?.detail ?? 'Failed to load matches.'); this.loading.set(false); }
        });
      },
      error: err => {
        this.error.set(err.error?.detail ?? 'Team not found. Check your PIN and registration code.');
        this.loading.set(false);
      }
    });
  }

  isPlayable(m: Match): boolean {
    const myId = this.team()?.id;
    return !m.result && !!m.teamAId && !!m.teamBId &&
           (m.teamAId === myId || m.teamBId === myId);
  }

  /** true=I won, false=I lost, null=not reported — for MY team's accept */
  myAcceptWon(m: Match): boolean | null {
    const isA   = m.teamAId === this.team()?.id;
    const raw   = isA ? m.acceptA : m.acceptB;
    if (!raw) return null;
    return isA ? raw === 'WonA' : raw === 'WonB';
  }

  /** true=I won, false=I lost (from my POV), null=not reported — for opponent's accept */
  oppAcceptWon(m: Match): boolean | null {
    const isA   = m.teamAId === this.team()?.id;
    const raw   = isA ? m.acceptB : m.acceptA;
    if (!raw) return null;
    return isA ? raw === 'WonA' : raw === 'WonB';
  }

  acceptWonLabel(won: boolean | null): string {
    if (won === null)  return '—';
    return won ? 'Won' : 'Lost';
  }

  acceptWonClass(won: boolean | null): string {
    if (won === null)  return 'accept-none';
    return won ? 'accept-won' : 'accept-lost';
  }

  teamLabel(id: number | null): string {
    if (id === null) return 'TBD';
    return this.teamNames().get(id) ?? `Team #${id}`;
  }

  formatTime(start: string | null): string {
    if (!start) return '—';
    return new Date(start).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' });
  }

  resultLabel(m: Match): string {
    if (!m.result) return 'Pending';
    const myId = this.team()?.id;
    if (m.result === 'WonA') return m.teamAId === myId ? 'Won' : 'Lost';
    if (m.result === 'WonB') return m.teamBId === myId ? 'Won' : 'Lost';
    return m.result;
  }

  setsLabel(m: Match): string {
    if (!m.sets || m.sets.length === 0) return '';
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(' ');
  }

  resultClass(m: Match): string {
    if (!m.result) return 'result-none';
    const myId = this.team()?.id;
    const won  = (m.result === 'WonA' && m.teamAId === myId) ||
                 (m.result === 'WonB' && m.teamBId === myId);
    return won ? 'result-won' : 'result-lost';
  }

  startReport(m: Match, won: boolean): void {
    this.reportError.set('');
    this.scoreValue.set(this.initialScoreValue(m));
    this.scoreWon.set(won);
    this.scorePrompt.set(m);
  }

  /** Formats the match's already-recorded sets as "6:4, 6:2, 7:6(2)" to prefill the score input. */
  private initialScoreValue(m: Match): string | null {
    if (!m.sets || m.sets.length === 0) return null;
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(', ');
  }

  cancelReport(): void {
    this.scorePrompt.set(null);
    this.scoreValue.set(null);
  }

  confirmReport(m: Match): void {
    const result = this.scoreValue();
    if (!result) return;

    this.submitting.set(true);
    this.reportError.set('');
    this.publicService.reportResult(this.pin, this.code, m.id, this.scoreWon(), result).subscribe({
      next: () => {
        this.submitting.set(false);
        this.scorePrompt.set(null);
        this.scoreValue.set(null);
        this.reloadMatches();
      },
      error: err => {
        this.submitting.set(false);
        this.reportError.set(err.error?.detail ?? 'Failed to report result.');
      }
    });
  }

  private isDisplayedMatch(matchId: number | null): boolean {
    return matchId === null || this.matches().some(m => m.id === matchId);
  }

  private reloadMatches(): void {
    this.publicService.getMyMatches(this.pin, this.code).subscribe({
      next: matches => this.matches.set(matches)
    });
  }
}

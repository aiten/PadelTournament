import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Match, MatchModify } from '../models/match.model';
import { Team } from '../models/team.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';
import { MatchScoreInputComponent } from '../shared/match-score-input.component';

@Component({
  selector: 'app-match-form',
  standalone: true,
  imports: [FormsModule, RouterModule, MatchScoreInputComponent],
  styles: [`
    .score-prompt { display: flex; flex-direction: column; gap: 10px; }
    .score-prompt-actions { display: flex; gap: 8px; }
    .winner-toggle { display: flex; gap: 8px; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <h2>Edit Match – Round {{ match().round }}, No {{ match().no }}</h2>
      <form (ngSubmit)="save()" #form="ngForm" class="form">
        <div class="form-group">
          <label>Team A</label>
          <select name="teamAId" [(ngModel)]="match().teamAId" class="form-control">
            <option [ngValue]="null">— none —</option>
            @for (t of teams(); track t.id) {
              <option [ngValue]="t.id">{{ t.name }}</option>
            }
          </select>
        </div>
        <div class="form-group">
          <label>Team B</label>
          <select name="teamBId" [(ngModel)]="match().teamBId" class="form-control">
            <option [ngValue]="null">— none —</option>
            @for (t of teams(); track t.id) {
              <option [ngValue]="t.id">{{ t.name }}</option>
            }
          </select>
        </div>
        <div class="form-group">
          <label>Start</label>
          <input type="datetime-local" name="start" [(ngModel)]="match().start" class="form-control" />
        </div>
        <div class="form-group">
          <label>Result</label>
          <div class="radio-group">
            @if (pendingWinner(); as winner) {
              <div class="score-prompt">
                @if (match().result) {
                  <div class="winner-toggle">
                    <button type="button" class="btn btn-sm" [class.btn-winner]="winner === 'WonA'"
                            (click)="pendingWinner.set('WonA')">{{ teamName(match().teamAId) }} wins</button>
                    <button type="button" class="btn btn-sm" [class.btn-winner]="winner === 'WonB'"
                            (click)="pendingWinner.set('WonB')">{{ teamName(match().teamBId) }} wins</button>
                  </div>
                }
                <strong>{{ teamName(winner === 'WonA' ? match().teamAId : match().teamBId) }} wins — enter the set scores (optional)</strong>
                <app-match-score-input
                  [teamALabel]="teamName(match().teamAId)"
                  [teamBLabel]="teamName(match().teamBId)"
                  [(value)]="scoreValue" />
                <div class="score-prompt-actions">
                  <button type="button" class="btn btn-primary" [disabled]="submitting()" (click)="confirmReport()">Confirm</button>
                  <button type="button" class="btn" [disabled]="submitting()" (click)="cancelReport()">Cancel</button>
                </div>
              </div>
            } @else if (match().result) {
              <span>{{ resultLabel() }}@if (setsLabel()) { — {{ setsLabel() }} }</span>
              <button type="button" class="btn btn-sm" (click)="startChangeResult()">Change Result</button>
            } @else if (match().teamAId && match().teamBId) {
              <button type="button" class="btn btn-winner" (click)="startReport('WonA')">
                {{ teamName(match().teamAId) }} wins
              </button>
              <button type="button" class="btn btn-winner" (click)="startReport('WonB')">
                {{ teamName(match().teamBId) }} wins
              </button>
            } @else {
              <label><input type="radio" name="result" [(ngModel)]="formResult" value="NoResult" /> No Result</label>
              <label><input type="radio" name="result" [(ngModel)]="formResult" value="WonA" /> Won A</label>
              <label><input type="radio" name="result" [(ngModel)]="formResult" value="WonB" /> Won B</label>
            }
          </div>
        </div>
        <div class="form-group">
          <label>Remark</label>
          <textarea name="remark" [(ngModel)]="match().remark" rows="2" class="form-control"></textarea>
        </div>
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid">Save</button>
          <a [routerLink]="['/tournaments', tournamentId, 'matches']" class="btn">Cancel</a>
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>
    </div>
  `
})
export class MatchFormComponent implements OnInit {
  match = signal<Match>({ id: 0, tournamentId: 0, round: 0, no: 0, teamAId: null, teamBId: null, start: null, nextMatchId: null, acceptA: null, acceptB: null, result: null, remark: null, sets: null });
  teams = signal<Team[]>([]);
  tournamentId = 0;
  error = signal('');
  formResult = 'NoResult';

  pendingWinner = signal<'WonA' | 'WonB' | null>(null);
  scoreValue    = signal<string | null>(null);
  submitting    = signal(false);

  constructor(
    private matchService: MatchService,
    private teamService: TeamService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    const id = +this.route.snapshot.paramMap.get('id')!;

    this.teamService.getAll(this.tournamentId).subscribe({
      next: teams => this.teams.set(teams)
    });

    this.matchService.getById(this.tournamentId, id).subscribe(m => {
      this.match.set({ ...m, start: m.start ? m.start.substring(0, 16) : null });
      this.formResult = m.result ?? 'NoResult';
    });
  }

  teamName(teamId: number | null): string {
    return this.teams().find(t => t.id === teamId)?.name ?? 'Team';
  }

  resultLabel(): string {
    const m = this.match();
    if (m.result === 'WonA') return `${this.teamName(m.teamAId)} won`;
    if (m.result === 'WonB') return `${this.teamName(m.teamBId)} won`;
    return '';
  }

  setsLabel(): string {
    const m = this.match();
    if (!m.sets || m.sets.length === 0) return '';
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(' ');
  }

  /** Formats the match's already-recorded sets as "6:4, 6:2, 7:6(2)" to prefill the score input. */
  private initialScoreValue(m: Match): string | null {
    if (!m.sets || m.sets.length === 0) return null;
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(', ');
  }

  startReport(winner: 'WonA' | 'WonB'): void {
    this.error.set('');
    this.scoreValue.set(null);
    this.pendingWinner.set(winner);
  }

  startChangeResult(): void {
    const m = this.match();
    this.error.set('');
    this.scoreValue.set(this.initialScoreValue(m));
    this.pendingWinner.set(m.result as 'WonA' | 'WonB');
  }

  cancelReport(): void {
    this.pendingWinner.set(null);
    this.scoreValue.set(null);
  }

  confirmReport(): void {
    const winner = this.pendingWinner();
    if (!winner) return;

    const m = this.match();
    this.submitting.set(true);
    this.matchService.setWinner(this.tournamentId, m.id, winner, this.scoreValue()).subscribe({
      next: () => this.router.navigate(['/tournaments', this.tournamentId, 'matches']),
      error: err => {
        this.submitting.set(false);
        this.error.set(err.error?.detail ?? 'Set winner failed.');
      }
    });
  }

  save(): void {
    const m = this.match();
    const dto: MatchModify = {
      teamAId:     m.teamAId,
      teamBId:     m.teamBId,
      start:       m.start || null,
      nextMatchId: m.nextMatchId,
      result:      this.formResult === 'NoResult' ? null : this.formResult,
      remark:      m.remark || null
    };
    this.matchService.update(this.tournamentId, m.id, dto).subscribe({
      next: () => this.router.navigate(['/tournaments', this.tournamentId, 'matches']),
      error: err => this.error.set(err.error?.detail ?? 'Save failed.')
    });
  }
}

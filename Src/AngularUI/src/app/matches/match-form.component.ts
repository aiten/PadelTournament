import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Match, MatchModify } from '../models/match.model';
import { Team } from '../models/team.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';

@Component({
  selector: 'app-match-form',
  standalone: true,
  imports: [FormsModule, RouterModule],
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
            @if (match().teamAId && match().teamBId && !match().result) {
              <button type="button" class="btn btn-winner" (click)="setWinner('WonA')">
                {{ teamName(match().teamAId) }} wins
              </button>
              <button type="button" class="btn btn-winner" (click)="setWinner('WonB')">
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
  match = signal<Match>({ id: 0, tournamentId: 0, round: 0, no: 0, teamAId: null, teamBId: null, start: null, nextMatchId: null, acceptA: null, acceptB: null, result: null, remark: null });
  teams = signal<Team[]>([]);
  tournamentId = 0;
  error = signal('');
  formResult = 'NoResult';

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

  setWinner(winner: 'WonA' | 'WonB'): void {
    const m = this.match();
    this.matchService.setWinner(this.tournamentId, m.id, winner).subscribe({
      next: () => this.router.navigate(['/tournaments', this.tournamentId, 'matches']),
      error: err => this.error.set(err.error?.detail ?? 'Set winner failed.')
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

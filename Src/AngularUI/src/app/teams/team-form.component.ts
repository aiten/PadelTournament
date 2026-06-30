import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Team } from '../models/team.model';
import { TeamService } from '../services/team.service';

@Component({
  selector: 'app-team-form',
  standalone: true,
  imports: [FormsModule, RouterModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <h2>{{ isNew ? 'Add Team' : 'Edit Team' }}</h2>
      <form (ngSubmit)="save()" #form="ngForm" class="form">
        <div class="form-group">
          <label>Player 1 *</label>
          <input name="player1" [(ngModel)]="team().player1" required maxlength="64" class="form-control" />
        </div>
        <div class="form-group">
          <label>Player 2</label>
          <input name="player2" [(ngModel)]="team().player2" maxlength="64" class="form-control" />
        </div>
        <div class="form-group">
          <label>Seed</label>
          <input type="number" name="seed" [(ngModel)]="team().seed" class="form-control"
                 [disabled]="team().startMatchPos != null" />
        </div>
        <div class="form-group">
          <label>Start Match Position</label>
          <input type="number" name="startMatchPos" [(ngModel)]="team().startMatchPos" class="form-control"
                 [disabled]="team().seed != null" />
        </div>
        @if (team().seed != null && team().startMatchPos != null) {
          <p class="error">Only one of Seed or Start Match Position may be set.</p>
        }
        @if (!isNew) {
          <div class="form-group">
            <label>Registration Date</label>
            <input [value]="formatDate(team().registrationDate)" class="form-control" disabled />
          </div>
          <div class="form-group">
            <label>Registration Code</label>
            <input [value]="team().registrationCode ?? ''" class="form-control" disabled />
          </div>
        }
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || (team().seed != null && team().startMatchPos != null)">Save</button>
          <a [routerLink]="['/tournaments', tournamentId, 'teams']" class="btn">Cancel</a>
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>
    </div>
  `
})
export class TeamFormComponent implements OnInit {
  team = signal<Team>({ id: 0, tournamentId: 0, player1: '', player2: null, name: '', seed: null, startMatchPos: null, registrationDate: '', registrationCode: null });
  tournamentId = 0;
  isNew = true;
  error = signal('');

  constructor(
    private service: TeamService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isNew = false;
      this.service.getById(this.tournamentId, +id).subscribe(t => this.team.set(t));
    } else {
      this.team.update(t => ({ ...t, tournamentId: this.tournamentId }));
    }
  }

  save(): void {
    const t = this.team();
    const done = () => this.router.navigate(['/tournaments', this.tournamentId, 'teams']);
    const fail = (err: any) => this.error.set(err.error?.detail ?? 'Save failed.');
    if (this.isNew) {
      this.service.create(this.tournamentId, { ...t, registrationDate: new Date().toISOString() }).subscribe({ next: done, error: fail });
    } else {
      this.service.update(this.tournamentId, t.id, t).subscribe({ next: done, error: fail });
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('de-AT');
  }
}

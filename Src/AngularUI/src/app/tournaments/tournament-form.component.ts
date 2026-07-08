import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Format, Tournament } from '../models/tournament.model';
import { TournamentService } from '../services/tournament.service';

@Component({
  selector: 'app-tournament-form',
  standalone: true,
  imports: [FormsModule, RouterModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <h2>{{ isNew ? 'New Tournament' : 'Edit Tournament' }}</h2>
      <form (ngSubmit)="save()" #form="ngForm" class="form">
        <div class="form-group">
          <label>Description *</label>
          <textarea name="description" [(ngModel)]="tournament().description" required rows="3" class="form-control"></textarea>
        </div>
        <div class="form-group">
          <label>From *</label>
          <input type="date" name="from" [(ngModel)]="tournament().from" required class="form-control" />
        </div>
        <div class="form-group">
          <label>To</label>
          <input type="date" name="to" [(ngModel)]="tournament().to" class="form-control" />
        </div>
        @if (tournament().from && tournament().to && tournament().from > tournament().to!) {
          <p class="error">End date must be after start date.</p>
        }
        <div class="form-group">
          <label>PIN</label>
          <input type="text" name="registrationPin" [(ngModel)]="tournament().registrationPin" maxlength="5" pattern="[0-9]{5}" class="form-control" />
        </div>
        <div class="form-group">
          <label>Format</label>
          <select name="format" [(ngModel)]="tournament().format" class="form-control">
            <option [ngValue]="null">-</option>
            @for (ct of formats; track ct) {
              <option [ngValue]="ct">{{ ct }}</option>
            }
          </select>
        </div>
        @if (tournament().format === Format.Tennis || tournament().format === Format.Padel) {
          <div class="form-group">
            <label>Best Of *</label>
            <input type="number" name="bestOf" [(ngModel)]="tournament().bestOf" required min="1" class="form-control" />
          </div>
          <div class="form-group">
            <label>Games To Win Set *</label>
            <input type="number" name="gamesToWinSet" [(ngModel)]="tournament().gamesToWinSet" required min="1" class="form-control" />
          </div>
          <div class="form-group">
            <label>Min Diff *</label>
            <input type="number" name="minDiff" [(ngModel)]="tournament().minDiff" required min="1" class="form-control" />
          </div>
          <div class="form-group form-check">
            <label>
              <input type="checkbox" name="noAdv" [(ngModel)]="tournament().noAdv" />
              No Advantage
            </label>
          </div>
        }
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || (tournament().from && tournament().to && tournament().from > tournament().to!)">Save</button>
          <a routerLink="/tournaments" class="btn">Cancel</a>
          @if (!isNew) {
            <a [routerLink]="['/tournaments', tournament().id, 'matches']" class="btn">Matches</a>
            <button type="button" class="btn btn-danger" (click)="confirmDelete()">Delete</button>
          }
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>
    </div>
  `
})
export class TournamentFormComponent implements OnInit {
  tournament = signal<Tournament>({
    id: 0,
    description: '',
    from: '',
    to: null,
    registrationPin: null,
    format: null,
    bestOf: 3,
    gamesToWinSet: 6,
    minDiff: 2,
    noAdv: false
  });

  isNew = true;
  error = signal('');

  readonly Format = Format;
  readonly formats = Object.values(Format);

  constructor(
    private service: TournamentService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isNew = false;
      this.service.getById(+id).subscribe(t => this.tournament.set(t));
    }
  }

  confirmDelete(): void {
    if (!confirm(`Delete tournament "${this.tournament().description}"? All teams and all matches will be deleted. This cannot be undone.`)) return;
    this.service.delete(this.tournament().id).subscribe({
      next: () => this.router.navigate(['/tournaments']),
      error: (err: any) => this.error.set(err.error?.detail ?? 'Delete failed.')
    });
  }

  save(): void {
    const t = this.tournament();
    if (t.to && t.from > t.to) {
      this.error.set('End date must be after start date.');
      return;
    }
    const payload = { ...t, to: t.to || null };
    const done = () => this.router.navigate(['/tournaments']);
    const fail = (err: any) => this.error.set(err.error?.detail ?? 'Save failed.');
    if (this.isNew) {
      this.service.create(payload).subscribe({ next: done, error: fail });
    } else {
      this.service.update(payload.id, payload).subscribe({ next: done, error: fail });
    }
  }
}

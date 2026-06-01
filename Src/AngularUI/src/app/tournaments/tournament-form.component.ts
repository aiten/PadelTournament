import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Tournament } from '../models/tournament.model';
import { TournamentService } from '../services/tournament.service';

@Component({
  selector: 'app-tournament-form',
  standalone: true,
  imports: [FormsModule, RouterModule],
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
          <label>PIN (100–999)</label>
          <input type="number" name="registrationPin" [(ngModel)]="tournament().registrationPin" min="100" max="999" class="form-control" />
        </div>
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
  tournament = signal<Tournament>({ id: 0, description: '', from: '', to: null, registrationPin: null });

  isNew = true;
  error = signal('');

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

import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Tournament } from '../models/tournament.model';
import { Format, PlayingFormat } from '../models/format.model';
import { TournamentService } from '../services/tournament.service';
import { FormatService } from '../services/format.service';

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
          <select name="format" [(ngModel)]="tournament().formatId" class="form-control">
            <option [ngValue]="null">-</option>
            @for (f of formats(); track f.id) {
              <option [ngValue]="f.id">{{ f.name }}</option>
            }
          </select>
        </div>
        @if (selectedFormat(); as sf) {
          <div class="form-group">
            <label>Playing Format</label>
            <input [value]="sf.playingFormat" class="form-control" disabled />
          </div>
          @if (sf.playingFormat === PlayingFormat.Tennis || sf.playingFormat === PlayingFormat.Padel) {
            <div class="form-group">
              <label>Best Of</label>
              <input [value]="sf.bestOf" class="form-control" disabled />
            </div>
            <div class="form-group">
              <label>Games To Win Set</label>
              <input [value]="sf.gamesToWinSet" class="form-control" disabled />
            </div>
            <div class="form-group">
              <label>Min Margin</label>
              <input [value]="sf.minMargin" class="form-control" disabled />
            </div>
            <div class="form-group form-check">
              <label>
                <input type="checkbox" [checked]="sf.noAdv" disabled />
                No Advantage
              </label>
            </div>
            <div class="form-group form-check">
              <label>
                <input type="checkbox" [checked]="sf.noTiebreak" disabled />
                No Tiebreak
              </label>
            </div>
          }
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
    formatId: null
  });

  isNew = true;
  error = signal('');
  formats = signal<Format[]>([]);

  readonly PlayingFormat = PlayingFormat;

  constructor(
    private service: TournamentService,
    private formatService: FormatService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.formatService.getAll().subscribe(f => this.formats.set(f));

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

  selectedFormat(): Format | null {
    return this.formats().find(f => f.id === this.tournament().formatId) ?? null;
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

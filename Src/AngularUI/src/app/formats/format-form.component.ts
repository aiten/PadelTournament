import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Format, PlayingFormat } from '../models/format.model';
import { FormatService } from '../services/format.service';

@Component({
  selector: 'app-format-form',
  standalone: true,
  imports: [FormsModule, RouterModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <h2>{{ isNew ? 'New Format' : 'Edit Format' }}</h2>
      <form (ngSubmit)="save()" #form="ngForm" class="form">
        <div class="form-group">
          <label>Name *</label>
          <input name="name" [(ngModel)]="format().name" required maxlength="50" class="form-control" />
        </div>
        <div class="form-group">
          <label>Playing Format *</label>
          <select name="playingFormat" [(ngModel)]="format().playingFormat" required class="form-control">
            <option [ngValue]="null">-</option>
            @for (pf of playingFormats; track pf) {
              <option [ngValue]="pf">{{ pf }}</option>
            }
          </select>
        </div>
        @if (format().playingFormat === PlayingFormat.Tennis || format().playingFormat === PlayingFormat.Padel) {
          <div class="form-group">
            <label>Best Of *</label>
            <input type="number" name="bestOf" [(ngModel)]="format().bestOf" required min="1" class="form-control" />
          </div>
          <div class="form-group">
            <label>Games To Win Set *</label>
            <input type="number" name="gamesToWinSet" [(ngModel)]="format().gamesToWinSet" required min="1" class="form-control" />
          </div>
          <div class="form-group">
            <label>Min Margin *</label>
            <input type="number" name="minMargin" [(ngModel)]="format().minMargin" required min="1" class="form-control" />
          </div>
          <div class="form-group form-check">
            <label>
              <input type="checkbox" name="noAdv" [(ngModel)]="format().noAdv" />
              No Advantage
            </label>
          </div>
          <div class="form-group form-check">
            <label>
              <input type="checkbox" name="noTiebreak" [(ngModel)]="format().noTiebreak" />
              No Tiebreak
            </label>
          </div>
        }
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid">Save</button>
          <a routerLink="/formats" class="btn">Cancel</a>
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>
    </div>
  `
})
export class FormatFormComponent implements OnInit {
  format = signal<Format>({
    id: 0,
    name: '',
    playingFormat: null,
    bestOf: 3,
    gamesToWinSet: 6,
    minMargin: 2,
    noAdv: false,
    noTiebreak: false
  });

  isNew = true;
  error = signal('');

  readonly PlayingFormat = PlayingFormat;
  readonly playingFormats = Object.values(PlayingFormat);

  constructor(
    private service: FormatService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isNew = false;
      this.service.getById(+id).subscribe(f => this.format.set(f));
    }
  }

  save(): void {
    const f = this.format();
    const done = () => this.router.navigate(['/formats']);
    const fail = (err: any) => this.error.set(err.error?.detail ?? 'Save failed.');
    if (this.isNew) {
      this.service.create(f).subscribe({ next: done, error: fail });
    } else {
      this.service.update(f.id, f).subscribe({ next: done, error: fail });
    }
  }
}

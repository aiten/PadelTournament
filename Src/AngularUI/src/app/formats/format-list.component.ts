import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Format } from '../models/format.model';
import { FormatService } from '../services/format.service';

@Component({
  selector: 'app-format-list',
  standalone: true,
  imports: [RouterModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Formats</h2>
        <a routerLink="/formats/new" class="btn btn-primary">+ New Format</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && formats().length > 0) {
        <table class="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Playing Format</th>
              <th>Best Of</th>
              <th>Games To Win Set</th>
              <th>Min Diff</th>
              <th>No Advantage</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (f of formats(); track f.id) {
              <tr>
                <td>{{ f.name }}</td>
                <td>{{ f.playingFormat }}</td>
                <td>{{ f.bestOf ?? '' }}</td>
                <td>{{ f.gamesToWinSet ?? '' }}</td>
                <td>{{ f.minDiff ?? '' }}</td>
                <td>{{ f.noAdv ? 'Yes' : 'No' }}</td>
                <td>
                  <a [routerLink]="['/formats', f.id]" class="btn btn-sm">Edit</a>
                  <button class="btn btn-sm btn-danger" (click)="deleteFormat(f.id, f.name)">Delete</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
      @if (!loading() && formats().length === 0) {
        <p class="empty">No formats found.</p>
      }
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </div>
  `
})
export class FormatListComponent implements OnInit {
  formats = signal<Format[]>([]);
  loading = signal(false);
  error = signal('');

  constructor(private service: FormatService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: data => { this.formats.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  deleteFormat(id: number, name: string): void {
    if (!confirm(`Delete format "${name}"?`)) return;
    this.service.delete(id).subscribe({
      next: () => this.formats.update(list => list.filter(f => f.id !== id)),
      error: (err: any) => this.error.set(err.error?.detail ?? 'Delete failed.')
    });
  }
}

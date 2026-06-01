import { Component, OnInit, signal, computed } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TournamentOverview } from '../models/tournament-overview.model';
import { TournamentService } from '../services/tournament.service';

type SortCol = 'description' | 'from' | 'to' | 'registrationPin' | 'teams' | 'finishedMatches';

@Component({
  selector: 'app-tournament-list',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    th.sortable { cursor: pointer; user-select: none; white-space: nowrap; }
    th.sortable:hover { background: #e2e8f0; }
    .sort-icon { margin-left: 4px; font-size: .8em; opacity: .5; }
    th.sort-active .sort-icon { opacity: 1; }
  `],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Tournaments</h2>
        <a routerLink="/tournaments/new" class="btn btn-primary">+ New Tournament</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && sortedTournaments().length > 0) {
        <table class="table">
          <thead>
            <tr>
              <th class="sortable" [class.sort-active]="sortCol() === 'description'" (click)="sort('description')">
                Description <span class="sort-icon">{{ sortIcon('description') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'from'" (click)="sort('from')">
                From <span class="sort-icon">{{ sortIcon('from') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'to'" (click)="sort('to')">
                To <span class="sort-icon">{{ sortIcon('to') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'registrationPin'" (click)="sort('registrationPin')">
                PIN <span class="sort-icon">{{ sortIcon('registrationPin') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'teams'" (click)="sort('teams')">
                Teams <span class="sort-icon">{{ sortIcon('teams') }}</span>
              </th>
<th class="sortable" [class.sort-active]="sortCol() === 'finishedMatches'" (click)="sort('finishedMatches')">
                Played <span class="sort-icon">{{ sortIcon('finishedMatches') }}</span>
              </th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (t of sortedTournaments(); track t.id) {
              <tr>
                <td>{{ t.description }}</td>
                <td>{{ formatDate(t.from) }}</td>
                <td>{{ t.to ? formatDate(t.to) : '' }}</td>
                <td>{{ t.registrationPin ?? '' }}</td>
                <td>{{ t.teams }}</td>
                <td>{{ t.finishedMatches }}</td>
                <td>
                  @if (t.matches === 0) { Team Registration }
                  @else if (t.finishedMatches >= t.matches) { Finished }
                  @else { Ongoing ({{ t.finishedMatches }}/{{ t.matches }}) }
                </td>
                <td>
                  <a [routerLink]="['/tournaments', t.id, 'teams']" class="btn btn-sm">Teams</a>
                  @if (t.matches > 0) {
                    <a [routerLink]="['/tournaments', t.id, 'matches']" class="btn btn-sm">Matches</a>
                    <a [routerLink]="['/tournaments', t.id, 'bracket']" class="btn btn-sm">Bracket</a>
                  }
                  @if (t.matches === 0) {
                    <a [routerLink]="['/tournaments', t.id, 'start']" class="btn btn-sm btn-primary"
                       [class.disabled]="t.teams < 2" [attr.title]="t.teams < 2 ? 'At least 2 teams required' : null">Start</a>
                  }
                  <a [routerLink]="['/tournaments', t.id]" class="btn btn-sm">Edit</a>
                  @if (t.teams === 0) {
                    <button class="btn btn-sm btn-danger" (click)="deleteTournament(t.id, t.description)">Delete</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
      @if (!loading() && tournaments().length === 0) {
        <p class="empty">No tournaments found.</p>
      }
    </div>
  `
})
export class TournamentListComponent implements OnInit {
  tournaments = signal<TournamentOverview[]>([]);
  loading = signal(false);
  sortCol = signal<SortCol>('from');
  sortAsc = signal(true);

  sortedTournaments = computed(() => {
    const col = this.sortCol();
    const asc = this.sortAsc();
    return this.tournaments().slice().sort((a, b) => {
      let cmp: number;
      switch (col) {
        case 'description':    cmp = a.description.localeCompare(b.description, undefined, { sensitivity: 'base' }); break;
        case 'from':           cmp = (a.from ?? '').localeCompare(b.from ?? ''); break;
        case 'to':             cmp = (a.to ?? '').localeCompare(b.to ?? ''); break;
        case 'registrationPin': cmp = (a.registrationPin ?? 0) - (b.registrationPin ?? 0); break;
        case 'teams':          cmp = a.teams - b.teams; break;
        case 'finishedMatches': cmp = a.finishedMatches - b.finishedMatches; break;
        default:               cmp = 0;
      }
      return asc ? cmp : -cmp;
    });
  });

  constructor(private service: TournamentService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: data => { this.tournaments.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); }
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

  deleteTournament(id: number, description: string): void {
    if (!confirm(`Delete tournament "${description}"?`)) return;
    this.service.delete(id).subscribe({
      next: () => this.tournaments.update(list => list.filter(t => t.id !== id)),
      error: () => alert('Delete failed.')
    });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const [y, m, d] = dateStr.split('-');
    return `${d}.${m}.${y}`;
  }
}

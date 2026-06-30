import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Team } from '../models/team.model';
import { TeamService } from '../services/team.service';

type SortCol = 'name' | 'seed' | 'startMatchPos' | 'registrationDate' | 'registrationCode';

@Component({
  selector: 'app-team-list',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    th.sortable { cursor: pointer; user-select: none; white-space: nowrap; }
    th.sortable:hover { background: #e2e8f0; }
    .sort-icon { margin-left: 4px; font-size: .8em; opacity: .5; }
    th.sort-active .sort-icon { opacity: 1; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Teams</h2>
        <div style="display:flex;gap:8px">
          <a [routerLink]="['/tournaments', tournamentId, 'teams', 'new']" class="btn btn-primary">+ Add Team</a>
          <a [routerLink]="['/tournaments', tournamentId, 'teams', 'bulk']" class="btn btn-secondary">Add Teams...</a>
          <a routerLink="/tournaments" class="btn">Back</a>
        </div>
      </div>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && sortedTeams().length > 0) {
        <table class="table">
          <thead>
            <tr>
              <th class="sortable" [class.sort-active]="sortCol() === 'name'" (click)="sort('name')">
                Name <span class="sort-icon">{{ sortIcon('name') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'seed'" (click)="sort('seed')">
                Seed <span class="sort-icon">{{ sortIcon('seed') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'startMatchPos'" (click)="sort('startMatchPos')">
                Start Pos <span class="sort-icon">{{ sortIcon('startMatchPos') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'registrationDate'" (click)="sort('registrationDate')">
                Registered <span class="sort-icon">{{ sortIcon('registrationDate') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'registrationCode'" (click)="sort('registrationCode')">
                Code <span class="sort-icon">{{ sortIcon('registrationCode') }}</span>
              </th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (t of sortedTeams(); track t.id) {
              <tr>
                <td>{{ t.name }}</td>
                <td>{{ t.seed ?? '' }}</td>
                <td>{{ t.startMatchPos ?? '' }}</td>
                <td>{{ formatDate(t.registrationDate) }}</td>
                <td>{{ t.registrationCode ?? '' }}</td>
                <td>
                  <a [routerLink]="['/tournaments', tournamentId, 'teams', t.id]" class="btn btn-sm">Edit</a>
                  <button class="btn btn-sm btn-danger" (click)="deleteTeam(t.id)">Delete</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
      @if (!loading() && teams().length === 0) {
        <p class="empty">No teams found.</p>
      }
    </div>
  `
})
export class TeamListComponent implements OnInit {
  tournamentId = 0;
  teams = signal<Team[]>([]);
  loading = signal(false);
  sortCol = signal<SortCol>('name');
  sortAsc = signal(true);

  sortedTeams = computed(() => {
    const col = this.sortCol();
    const asc = this.sortAsc();
    return this.teams().slice().sort((a, b) => {
      let cmp: number;
      switch (col) {
        case 'name':             cmp = a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }); break;
        case 'seed':             cmp = (a.seed ?? -1) - (b.seed ?? -1); break;
        case 'startMatchPos':    cmp = (a.startMatchPos ?? -1) - (b.startMatchPos ?? -1); break;
        case 'registrationDate': cmp = a.registrationDate.localeCompare(b.registrationDate); break;
        case 'registrationCode': cmp = (a.registrationCode ?? '').localeCompare(b.registrationCode ?? ''); break;
        default:                 cmp = 0;
      }
      return asc ? cmp : -cmp;
    });
  });

  constructor(private service: TeamService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    this.loading.set(true);
    this.service.getAll(this.tournamentId).subscribe({
      next: data => { this.teams.set(data); this.loading.set(false); },
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

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('de-AT');
  }

  deleteTeam(id: number): void {
    if (!confirm('Delete this team?')) return;
    this.service.delete(this.tournamentId, id).subscribe({
      next: () => this.teams.update(list => list.filter(t => t.id !== id)),
      error: () => alert('Delete failed.')
    });
  }
}

import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Team } from '../models/team.model';
import { TeamService } from '../services/team.service';
import { TournamentService } from '../services/tournament.service';

type SortCol = 'name' | 'registrationDate' | 'seed' | 'startMatchPos';

@Component({
  selector: 'app-tournament-start',
  standalone: true,
  imports: [FormsModule, RouterModule],
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
        <h2>Start Tournament</h2>
        <a routerLink="/tournaments" class="btn">Back</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && teams().length === 0) {
        <p class="empty">No teams registered yet.</p>
      }
      @if (!loading() && teams().length > 0) {
        <table class="table">
          <thead>
            <tr>
              <th class="sortable" [class.sort-active]="sortCol() === 'name'" (click)="sort('name')">
                Name <span class="sort-icon">{{ sortIcon('name') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'registrationDate'" (click)="sort('registrationDate')">
                Registration Date <span class="sort-icon">{{ sortIcon('registrationDate') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'seed'" (click)="sort('seed')">
                Seed <span class="sort-icon">{{ sortIcon('seed') }}</span>
              </th>
              <th class="sortable" [class.sort-active]="sortCol() === 'startMatchPos'" (click)="sort('startMatchPos')">
                Start Pos <span class="sort-icon">{{ sortIcon('startMatchPos') }}</span>
              </th>
            </tr>
          </thead>
          <tbody>
            @for (t of sortedTeams(); track t.id) {
              <tr>
                <td>{{ t.name }}</td>
                <td>{{ formatDate(t.registrationDate) }}</td>
                <td>
                  <input type="number" [(ngModel)]="t.seed" (blur)="saveTeam(t)"
                         min="1" class="form-control" style="width:80px"
                         [disabled]="t.startMatchPos != null" />
                </td>
                <td>
                  <input type="number" [(ngModel)]="t.startMatchPos" (blur)="saveTeam(t)"
                         min="1" class="form-control" style="width:80px"
                         [disabled]="t.seed != null" />
                </td>
              </tr>
            }
          </tbody>
        </table>
        <div class="form-actions">
          <button class="btn btn-primary" (click)="start()" [disabled]="starting()">
            {{ starting() ? 'Starting…' : 'Start Tournament' }}
          </button>
        </div>
      }
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </div>
  `
})
export class TournamentStartComponent implements OnInit {
  tournamentId = 0;
  teams = signal<Team[]>([]);
  loading = signal(false);
  starting = signal(false);
  error = signal('');
  sortCol = signal<SortCol>('name');
  sortAsc = signal(true);

  sortedTeams = computed(() => {
    const col = this.sortCol();
    const asc = this.sortAsc();
    return this.teams().slice().sort((a, b) => {
      let cmp: number;
      switch (col) {
        case 'name':             cmp = a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }); break;
        case 'registrationDate': cmp = a.registrationDate.localeCompare(b.registrationDate); break;
        case 'seed':             cmp = (a.seed ?? -1) - (b.seed ?? -1); break;
        case 'startMatchPos':    cmp = (a.startMatchPos ?? -1) - (b.startMatchPos ?? -1); break;
        default:                 cmp = 0;
      }
      return asc ? cmp : -cmp;
    });
  });

  constructor(
    private teamService: TeamService,
    private tournamentService: TournamentService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    this.loading.set(true);
    this.teamService.getAll(this.tournamentId).subscribe({
      next: data => { this.teams.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
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

  saveTeam(team: Team): void {
    this.teamService.update(this.tournamentId, team.id, team).subscribe({
      error: () => this.error.set(`Failed to save "${team.name}".`)
    });
  }

  start(): void {
    this.starting.set(true);
    this.error.set('');
    this.tournamentService.generateSchedule(this.tournamentId).subscribe({
      next: () => this.router.navigate(['/tournaments', this.tournamentId, 'matches']),
      error: err => {
        this.error.set(err.error?.detail ?? 'Failed to start tournament.');
        this.starting.set(false);
      }
    });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('de-AT');
  }
}

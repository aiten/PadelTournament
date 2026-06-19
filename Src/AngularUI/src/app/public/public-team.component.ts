import { Component, OnInit, signal } from '@angular/core';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { GlobalStateService } from '../services/global-state.service';
import { PublicService } from '../services/public.service';
import { Tournament } from '../models/tournament.model';
import { Team } from '../models/team.model';

@Component({
  selector: 'app-public-team',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    .info-bar {
      display: flex; gap: 1.5rem; flex-wrap: wrap;
      background: #f8fafc; border: 1px solid #e2e8f0;
      border-radius: 6px; padding: .65rem 1rem;
      margin-bottom: 1.25rem; font-size: .875rem;
    }
    .info-item { display: flex; flex-direction: column; gap: .1rem; }
    .info-label { font-size: .7rem; color: #94a3b8; text-transform: uppercase; letter-spacing: .04em; }
    .info-value { font-weight: 600; color: #1e293b; }
  `],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Your Team</h2>
        <a routerLink="/public" class="btn">Back</a>
      </div>

      @if (tournament()) {
        <div class="info-bar">
          <div class="info-item">
            <span class="info-label">Tournament</span>
            <span class="info-value">{{ tournament()!.description }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">From</span>
            <span class="info-value">{{ formatDate(tournament()!.from) }}</span>
          </div>
          @if (tournament()!.to) {
            <div class="info-item">
              <span class="info-label">To</span>
              <span class="info-value">{{ formatDate(tournament()!.to) }}</span>
            </div>
          }
          @if (team()) {
            <div class="info-item">
              <span class="info-label">Team</span>
              <span class="info-value">{{ team()!.name }}</span>
            </div>
          }
        </div>
      }

      <div class="form-actions" style="gap:.5rem;display:flex;flex-wrap:wrap;">
        <button type="button" class="btn btn-primary" (click)="viewMatches()">View My Matches</button>
        <button type="button" class="btn" (click)="viewBracket()">View Bracket</button>
      </div>
    </div>
  `
})
export class PublicTeamComponent implements OnInit {
  pin        = '';
  code       = '';
  tournament = signal<Tournament | null>(null);
  team       = signal<Team | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private globalState: GlobalStateService,
    private publicService: PublicService
  ) {}

  ngOnInit(): void {
    this.pin  = this.route.snapshot.paramMap.get('pin')!;
    this.code =  this.route.snapshot.paramMap.get('code')!;
    this.globalState.lastPin  = this.pin;
    this.globalState.lastCode = this.code;

    this.publicService.getTournament(this.pin).subscribe({
      next: t  => this.tournament.set(t),
      error: () => {}
    });

    this.publicService.getMyTeam(this.pin, this.code).subscribe({
      next: t  => this.team.set(t),
      error: () => {}
    });
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  }

  viewMatches(): void {
    this.router.navigate(['/public', this.pin, this.code, 'matches']);
  }

  viewBracket(): void {
    this.router.navigate(['/public', this.pin, 'bracket']);
  }
}

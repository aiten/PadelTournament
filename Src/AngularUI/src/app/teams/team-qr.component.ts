import { Component, OnInit, effect, signal, viewChild, ElementRef, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Team } from '../models/team.model';
import { TeamService } from '../services/team.service';
import { TournamentService } from '../services/tournament.service';
import { toCanvas } from 'qrcode';

@Component({
  selector: 'app-team-qr',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    .qr-wrap { margin-top: .5rem; }
    .qr-wrap canvas { display: block; border: 1px solid #e2e8f0; border-radius: 6px; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <h2>Team QR Code</h2>
      @if (loading()) {
        <p class="empty">Loading...</p>
      }
      @if (!loading() && team()) {
        <div class="form">
          <div class="form-group">
            <label>Team</label>
            <p>{{ team()!.name }}</p>
          </div>
          <div class="form-group">
            <label>Link</label>
            <p class="code-box">{{ publicUrl() }}</p>
          </div>
          <div class="form-group qr-wrap">
            <label>QR Code</label>
            <canvas #qrCanvas></canvas>
          </div>
          <div class="form-actions">
            <a [routerLink]="['/tournaments', tournamentId, 'teams']" class="btn">Back</a>
          </div>
        </div>
      }
      @if (!loading() && error()) {
        <p class="error">{{ error() }}</p>
      }
    </div>
  `
})
export class TeamQrComponent implements OnInit {
  tournamentId = 0;
  team = signal<Team | null>(null);
  loading = signal(false);
  error = signal('');
  publicUrl = signal('');

  qrCanvas = viewChild<ElementRef<HTMLCanvasElement>>('qrCanvas');

  constructor(
    private teamService: TeamService,
    private tournamentService: TournamentService,
    private route: ActivatedRoute
  ) {
    effect(() => {
      const canvas = this.qrCanvas();
      const url = this.publicUrl();
      if (canvas && url) {
        toCanvas(canvas.nativeElement, url, { width: 200, margin: 2 });
      }
    });
  }

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.teamService.getById(this.tournamentId, id).subscribe({
      next: team => {
        if (!team.registrationCode) {
          this.error.set('This team has no registration code yet.');
          this.loading.set(false);
          return;
        }
        this.tournamentService.getById(this.tournamentId).subscribe({
          next: tournament => {
            if (!tournament.registrationPin) {
              this.error.set('This tournament has no registration PIN.');
              this.loading.set(false);
              return;
            }
            this.publicUrl.set(`${window.location.origin}/public/${tournament.registrationPin}/${team.registrationCode}`);
            this.team.set(team);
            this.loading.set(false);
          },
          error: () => { this.error.set('Failed to load tournament.'); this.loading.set(false); }
        });
      },
      error: () => { this.error.set('Failed to load team.'); this.loading.set(false); }
    });
  }
}

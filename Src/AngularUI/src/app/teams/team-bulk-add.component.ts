import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TeamService } from '../services/team.service';
import { TournamentRegistrationResult } from '../models/registration.model';

@Component({
  selector: 'app-team-bulk-add',
  standalone: true,
  imports: [RouterModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Add Teams</h2>
        <a [routerLink]="['/tournaments', tournamentId, 'teams']" class="btn">Back</a>
      </div>
      @if (!registered()) {
        <div class="form-group">
          <label>One team per line — <code>TeamName;Seed</code> or just <code>TeamName</code></label>
          <textarea
            class="form-control"
            rows="12"
            [(ngModel)]="teamsText"
            placeholder="Donald;1&#10;Dagobert;2&#10;Tick/Trick/Track"
            style="font-family: monospace; width: 100%; resize: vertical; margin-top: 6px"
          ></textarea>
        </div>
        <div style="margin-top: 12px">
          <button class="btn btn-primary" (click)="submit()" [disabled]="submitting()">Register Teams</button>
        </div>
      }
      @if (error()) {
        <p class="error" style="margin-top: 12px">{{ error() }}</p>
      }
      @if (results().length > 0) {
        <h3 style="margin-top: 24px">Registered Teams</h3>
        <table class="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Registration Code</th>
            </tr>
          </thead>
          <tbody>
            @for (r of results(); track r.name) {
              <tr>
                <td>{{ r.name }}</td>
                <td>{{ r.registrationCode }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class TeamBulkAddComponent implements OnInit {
  tournamentId = 0;
  teamsText = '';
  submitting = signal(false);
  registered = signal(false);
  results = signal<TournamentRegistrationResult[]>([]);
  error = signal('');

  constructor(private service: TeamService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
  }

  submit(): void {
    if (!this.teamsText.trim()) return;
    this.submitting.set(true);
    this.error.set('');
    this.results.set([]);
    this.service.registerBulk(this.tournamentId, this.teamsText).subscribe({
      next: data => { this.results.set(data); this.registered.set(true); this.submitting.set(false); },
      error: err => {
        this.error.set(err?.error?.detail ?? 'Registration failed.');
        this.submitting.set(false);
      }
    });
  }
}

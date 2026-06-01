import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RegistrationService } from '../services/registration.service';
import { TournamentRegistrationRequest } from '../models/registration.model';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="page">
      <h2>Register for Tournament</h2>
      <form (ngSubmit)="register()" #form="ngForm" class="form">
        <div class="form-group">
          <label>Name *</label>
          <input name="name" [(ngModel)]="name" required class="form-control" />
        </div>
        <div class="form-group">
          <label>PIN *</label>
          <input type="number" name="pin" [(ngModel)]="pin" required min="100" max="999" class="form-control" />
        </div>
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Registering…' : 'Register' }}
          </button>
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </form>
    </div>
  `
})
export class RegisterFormComponent implements OnInit {
  name = '';
  pin: number | null = null;
  loading = signal(false);
  error = signal('');

  constructor(
    private service: RegistrationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const p = this.route.snapshot.queryParamMap.get('pin');
    if (p !== null) this.pin = +p;
  }

  register(): void {
    this.loading.set(true);
    this.error.set('');
    const req: TournamentRegistrationRequest = {
      name: this.name,
      pin: this.pin!
    };
    this.service.register(req).subscribe({
      next: result => {
        this.service.result.set(result);
        this.router.navigate(['/registration/result']);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.detail ?? 'Registration failed.');
      }
    });
  }
}

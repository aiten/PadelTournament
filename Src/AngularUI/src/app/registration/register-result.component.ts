import { Component, AfterViewInit, ViewChild, ElementRef, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RegistrationService } from '../services/registration.service';
import { toCanvas } from 'qrcode';

@Component({
  selector: 'app-register-result',
  standalone: true,
  imports: [RouterModule],
  styles: [`
    .qr-wrap { margin-top: .5rem; }
    .qr-wrap canvas { display: block; border: 1px solid #e2e8f0; border-radius: 6px; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    @if (result()) {
      <div class="page">
        <h2>Registration Successful</h2>
        <div class="form">
          <div class="form-group">
            <label>Name</label>
            <p>{{ result()!.name }}</p>
          </div>
          <div class="form-group">
            <label>PIN</label>
            <p>{{ result()!.pin }}</p>
          </div>
          <div class="form-group">
            <label>Registration Code</label>
            <p class="code-box">{{ result()!.registrationCode }}</p>
          </div>
          <div class="form-actions">
            <a [routerLink]="['/public', result()!.pin, result()!.registrationCode]" class="btn btn-primary">Go to my team</a>
          </div>
          <div class="form-group qr-wrap">
            <label>QR Code</label>
            <canvas #qrCanvas></canvas>
          </div>
        </div>
      </div>
    } @else {
      <div class="page">
        <p class="empty">No registration result. <a routerLink="/registration">Go back</a>.</p>
      </div>
    }
  `
})
export class RegisterResultComponent implements AfterViewInit {
  result;

  @ViewChild('qrCanvas') qrCanvas!: ElementRef<HTMLCanvasElement>;

  constructor(private service: RegistrationService) {
    this.result = service.result;
  }

  ngAfterViewInit(): void {
    if (!this.result() || !this.qrCanvas) return;
    const r = this.result()!;
    const url = `${window.location.origin}/public/${r.pin}/${r.registrationCode}`;
    toCanvas(this.qrCanvas.nativeElement, url, { width: 200, margin: 2 });
  }
}

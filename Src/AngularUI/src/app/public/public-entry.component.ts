import { Component, signal, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { GlobalStateService } from '../services/global-state.service';
import { toCanvas } from 'qrcode';

@Component({
  selector: 'app-public-entry',
  standalone: true,
  imports: [FormsModule, RouterModule],
  template: `
    <div class="page">
      <h2>Tournament Public Area</h2>
      <form class="form">
        <div class="form-group">
          <label>Tournament PIN *</label>
          <input type="number" name="pin" [(ngModel)]="pin" (ngModelChange)="renderQr()" required class="form-control"
                 placeholder="e.g. 123" />
        </div>
        <div class="form-group">
          <label>Registration Code <span style="font-weight:400;color:#64748b">(optional — for your team's matches)</span></label>
          <input name="code" [(ngModel)]="code" class="form-control"
                 placeholder="Your registration code" />
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
        <div class="form-actions" style="gap:.5rem;display:flex;flex-wrap:wrap;">
          <button type="button" class="btn btn-primary" [disabled]="!pin" (click)="viewBracket()">
            View Bracket
          </button>
          <button type="button" class="btn" [disabled]="!pin || !code.trim()" (click)="viewMatches()">
            View My Matches
          </button>
        </div>
      </form>
      <div style="margin-top:1.5rem; display:flex; flex-direction:column; gap:.75rem; align-items:flex-start;">
        <button type="button" class="btn btn-primary" (click)="register()">Register as Team</button>
        <canvas #qrCanvas style="border:1px solid #e2e8f0; border-radius:6px; display:block;"></canvas>
      </div>
    </div>
  `
})
export class PublicEntryComponent implements AfterViewInit {
  pin: number | null = null;
  code = '';
  error = signal('');

  @ViewChild('qrCanvas') qrCanvas!: ElementRef<HTMLCanvasElement>;

  constructor(private router: Router, private globalState: GlobalStateService) {
    if (globalState.lastPin)  this.pin  = globalState.lastPin;
    if (globalState.lastCode) this.code = globalState.lastCode;
  }

  private saveState(): void {
    this.globalState.lastPin  = this.pin ?? 0;
    this.globalState.lastCode = this.code.trim();
  }

  viewBracket(): void {
    if (!this.pin) { this.error.set('Please enter the tournament PIN.'); return; }
    this.error.set('');
    this.saveState();
    this.router.navigate(['/public', this.pin, 'bracket']);
  }

  viewMatches(): void {
    if (!this.pin || !this.code.trim()) {
      this.error.set('Please enter both PIN and registration code.');
      return;
    }
    this.error.set('');
    this.saveState();
    this.router.navigate(['/public', this.pin, this.code.trim(), 'matches']);
  }

  ngAfterViewInit(): void {
    this.renderQr();
  }

  renderQr(): void {
    const url = this.pin
      ? `${window.location.origin}/registration?pin=${this.pin}`
      : `${window.location.origin}/registration`;
    toCanvas(this.qrCanvas.nativeElement, url, { width: 200, margin: 2 });
  }

  register(): void {
    this.saveState();
    const extras = this.pin ? { queryParams: { pin: this.pin } } : {};
    this.router.navigate(['/registration'], extras);
  }
}

import { Component, OnInit, input, model, signal, ChangeDetectionStrategy } from '@angular/core';

interface SetScore {
  scoreA: number;
  scoreB: number;
  tieBreak: number | null;
}

function emptySet(): SetScore {
  return { scoreA: 0, scoreB: 0, tieBreak: null };
}

function parseValue(value: string | null): SetScore[] {
  if (!value) return [emptySet()];

  const sets = value.split(',').map(part => {
    const m = part.trim().match(/^(\d+)[:\-](\d+)(?:\((\d+)\))?$/);
    if (!m) return emptySet();
    return { scoreA: +m[1], scoreB: +m[2], tieBreak: m[3] !== undefined ? +m[3] : null };
  });

  return sets.length ? sets : [emptySet()];
}

/** A completed set can never end 0:0, so that's treated as "not entered yet". */
function formatValue(sets: SetScore[]): string | null {
  const complete = sets.filter(s => !(s.scoreA === 0 && s.scoreB === 0));
  if (!complete.length) return null;

  return complete
    .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreak !== null ? `(${s.tieBreak})` : ''}`)
    .join(', ');
}

@Component({
  selector: 'app-match-score-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .score-input { display: flex; flex-direction: column; gap: 10px; }
    .teams-header { display: flex; align-items: center; gap: 10px; padding: 0 4px; }
    .team-col { flex: 1; text-align: center; font-size: .85rem; font-weight: 600; color: #334155;
                min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .sep-spacer { width: 1.2rem; flex: 0 0 auto; }
    .set-card { border: 1px solid #e2e8f0; border-radius: 10px; padding: 10px 12px; display: flex; flex-direction: column; gap: 8px; }
    .set-card-header { display: flex; justify-content: space-between; align-items: center; }
    .set-title { font-size: .85rem; color: #64748b; font-weight: 600; }
    .remove-btn { border: none; background: none; color: #94a3b8; font-size: 1.1rem; padding: 4px 10px; line-height: 1; }
    .score-row { display: flex; align-items: center; gap: 10px; }
    .score-col { flex: 1; display: flex; justify-content: center; }
    .sep { flex: 0 0 auto; width: 1.2rem; text-align: center; font-weight: 700; color: #64748b; font-size: 1.1rem; }
    .stepper { display: flex; align-items: center; border: 1px solid #cbd5e1; border-radius: 8px; overflow: hidden; }
    .stepper-btn { width: 42px; height: 42px; border: none; background: #f1f5f9; font-size: 1.3rem; line-height: 1;
                   color: #334155; touch-action: manipulation; }
    .stepper-btn:active:not(:disabled) { background: #e2e8f0; }
    .stepper-btn:disabled { color: #cbd5e1; background: #f8fafc; }
    .stepper-value { min-width: 2.6rem; text-align: center; font-size: 1.15rem; font-weight: 700; }
    .tiebreak-row { display: flex; align-items: center; justify-content: flex-end; gap: 10px;
                    padding-top: 6px; border-top: 1px dashed #e2e8f0; }
    .tb-label { font-size: .8rem; color: #64748b; }
    .tb-link { border: none; background: none; color: #2563eb; font-size: .85rem; padding: 4px 0;
               text-decoration: underline; margin-left: auto; white-space: nowrap; }
    .set-actions { display: flex; justify-content: center; }
    .add-set-btn { padding: 8px 18px; }
  `],
  template: `
    <div class="score-input">
      <div class="teams-header">
        <span class="team-col">{{ teamALabel() }}</span>
        <span class="sep-spacer"></span>
        <span class="team-col">{{ teamBLabel() }}</span>
      </div>

      @for (set of sets(); track $index) {
        <div class="set-card">
          <div class="set-card-header">
            <span class="set-title">Set {{ $index + 1 }}</span>
            @if (sets().length > 1) {
              <button type="button" class="remove-btn" (click)="removeSet($index)" aria-label="Remove set">✕</button>
            }
          </div>

          <div class="score-row">
            <div class="score-col">
              <div class="stepper">
                <button type="button" class="stepper-btn" (click)="dec($index, 'scoreA')" [disabled]="set.scoreA <= 0">−</button>
                <span class="stepper-value">{{ set.scoreA }}</span>
                <button type="button" class="stepper-btn" (click)="inc($index, 'scoreA')" [disabled]="set.scoreA >= maxScore()">+</button>
              </div>
            </div>
            <span class="sep">:</span>
            <div class="score-col">
              <div class="stepper">
                <button type="button" class="stepper-btn" (click)="dec($index, 'scoreB')" [disabled]="set.scoreB <= 0">−</button>
                <span class="stepper-value">{{ set.scoreB }}</span>
                <button type="button" class="stepper-btn" (click)="inc($index, 'scoreB')" [disabled]="set.scoreB >= maxScore()">+</button>
              </div>
            </div>
            @if (!hasTieBreak(set)) {
              <button type="button" class="tb-link" (click)="addTieBreak($index)">+ Tiebreak</button>
            }
          </div>

          @if (hasTieBreak(set)) {
            <div class="tiebreak-row">
              <span class="tb-label">Tiebreak</span>
              <div class="stepper">
                <button type="button" class="stepper-btn" (click)="decTieBreak($index)" [disabled]="(set.tieBreak ?? 0) <= 0">−</button>
                <span class="stepper-value">{{ set.tieBreak ?? 0 }}</span>
                <button type="button" class="stepper-btn" (click)="incTieBreak($index)" [disabled]="(set.tieBreak ?? 0) >= maxTieBreak()">+</button>
              </div>
              <button type="button" class="remove-btn" (click)="removeTieBreak($index)" aria-label="Remove tiebreak">✕</button>
            </div>
          }
        </div>
      }

      <div class="set-actions">
        @if (sets().length < maxSets()) {
          <button type="button" class="btn btn-sm add-set-btn" (click)="addSet()">+ Add set</button>
        }
      </div>
    </div>
  `
})
export class MatchScoreInputComponent implements OnInit {
  teamALabel   = input('Team A');
  teamBLabel   = input('Team B');
  maxSets      = input(5);
  maxScore     = input(20);
  maxTieBreak  = input(30);

  /** Formatted score string, e.g. "6:4, 6:3, 7:6(2)" — null while incomplete. */
  value = model<string | null>(null);

  sets = signal<SetScore[]>([emptySet()]);

  ngOnInit(): void {
    this.sets.set(parseValue(this.value()));
  }

  hasTieBreak(s: SetScore): boolean {
    return s.tieBreak !== null;
  }

  addTieBreak(index: number): void {
    const next = this.sets().map((s, i) => i === index ? { ...s, tieBreak: 0 } : s);
    this.commit(next);
  }

  removeTieBreak(index: number): void {
    const next = this.sets().map((s, i) => i === index ? { ...s, tieBreak: null } : s);
    this.commit(next);
  }

  inc(index: number, field: 'scoreA' | 'scoreB'): void {
    this.updateScore(index, field, 1);
  }

  dec(index: number, field: 'scoreA' | 'scoreB'): void {
    this.updateScore(index, field, -1);
  }

  incTieBreak(index: number): void {
    this.updateTieBreak(index, 1);
  }

  decTieBreak(index: number): void {
    this.updateTieBreak(index, -1);
  }

  addSet(): void {
    if (this.sets().length >= this.maxSets()) return;
    this.commit([...this.sets(), emptySet()]);
  }

  removeSet(index: number): void {
    const next = this.sets().filter((_, i) => i !== index);
    this.commit(next.length ? next : [emptySet()]);
  }

  private updateScore(index: number, field: 'scoreA' | 'scoreB', delta: number): void {
    const next = this.sets().map((s, i) => i === index
      ? { ...s, [field]: this.clamp(s[field] + delta, 0, this.maxScore()) }
      : s);
    this.commit(next);
  }

  private updateTieBreak(index: number, delta: number): void {
    const next = this.sets().map((s, i) => i === index
      ? { ...s, tieBreak: this.clamp((s.tieBreak ?? 0) + delta, 0, this.maxTieBreak()) }
      : s);
    this.commit(next);
  }

  private clamp(val: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, val));
  }

  private commit(next: SetScore[]): void {
    this.sets.set(next);
    this.value.set(formatValue(next));
  }
}

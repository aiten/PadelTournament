import { Component, OnDestroy, OnInit, signal, computed, ChangeDetectionStrategy, HostListener, Renderer2 } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { Match } from '../models/match.model';
import { Team } from '../models/team.model';
import { Tournament } from '../models/tournament.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';
import { PublicSignalRService } from '../services/signalr.service';
import { TournamentService } from '../services/tournament.service';
import { MatchScoreInputComponent } from '../shared/match-score-input.component';

const SCORE_W = 54;
const CARD_W = 220 + SCORE_W;
const CARD_H = 76;
const UNIT_H = 100;
const CONN_W = 60;
const COL_W  = CARD_W + CONN_W;

interface MatchPos {
  match: Match;
  top: number;
  left: number;
}

interface Line {
  x1: number; y1: number;
  x2: number; y2: number;
}

function cardTop(ri: number, ni: number): number {
  const factor = Math.pow(2, ri);
  return Math.round(UNIT_H * (ni * factor + (factor - 1) / 2));
}

@Component({
  selector: 'app-match-bracket',
  standalone: true,
  imports: [RouterModule, MatchScoreInputComponent],
  styles: [`
    .page         { padding: 0 24px; }
    .bracket-scroll {
      overflow: auto;
      height: calc(100vh - 130px);
    }
    .bracket-outer  { position: relative; }
    .bracket-wrap   { position: relative; transform-origin: top left; }
    .match-card {
      position: absolute;
      width: ${CARD_W}px;
      display: flex;
      align-items: stretch;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      background: #fff;
      box-sizing: border-box;
      box-shadow: 0 1px 3px rgba(0,0,0,.08);
    }
    .match-card-main {
      flex: 1 1 auto;
      min-width: 0;
      padding: 8px 10px;
    }
    .match-card-score {
      flex: 0 0 ${SCORE_W}px;
      width: ${SCORE_W}px;
      border-left: 1px solid #e2e8f0;
      padding: 8px 4px;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      gap: 2px;
    }
    .score-row {
      font-size: .72rem;
      color: #475569;
      white-space: nowrap;
    }
    .match-label { font-size: .72rem; color: #94a3b8; margin-bottom: 4px; }
    .team-row {
      font-size: .85rem;
      padding: 3px 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .team-row.winner { font-weight: 700; color: #16a34a; }
    .team-row.loser  { color: #94a3b8; }
    .team-row.tbd    { color: #cbd5e1; font-style: italic; }
    .winner-btns { margin-top: 5px; display: flex; gap: 4px; flex-wrap: wrap; }
    .winner-btns.compact .btn { padding: 1px 6px; font-size: .7rem; }
    .score-prompt-panel {
      max-width: 420px;
      margin: 24px auto 0;
      padding: 20px;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      background: #fff;
      display: flex;
      flex-direction: column;
      gap: 14px;
    }
    .score-prompt-title { font-size: 1rem; font-weight: 600; color: #334155; }
    .score-prompt-actions { display: flex; gap: 8px; justify-content: flex-end; }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Tournament Bracket</h2>
        <a [routerLink]="['/tournaments', tournamentId, 'matches']" class="btn">Back to Matches</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading…</p>
      } @else if (scorePrompt(); as promptMatch) {
        <div class="score-prompt-panel">
          <div class="score-prompt-title">
            {{ teamName(pendingWinner() === 'WonA' ? promptMatch.teamAId : promptMatch.teamBId) }} wins — enter the set scores (optional)
          </div>
          <app-match-score-input
            [teamALabel]="teamName(promptMatch.teamAId)"
            [teamBLabel]="teamName(promptMatch.teamBId)"
            [(value)]="scoreValue" />
          @if (reportError()) {
            <p class="error">{{ reportError() }}</p>
          }
          <div class="score-prompt-actions">
            <button type="button" class="btn" [disabled]="submitting()" (click)="cancelReport()">Cancel</button>
            <button type="button" class="btn btn-primary" [disabled]="submitting()" (click)="confirmReport(promptMatch)">Confirm</button>
          </div>
        </div>
      } @else if (positions().length === 0) {
        <p class="empty">No matches found.</p>
      } @else {
        <div class="bracket-scroll">
          <div class="bracket-outer" [style.width.px]="scaledWidth()" [style.height.px]="scaledHeight()">
            <div class="bracket-wrap" [style.width.px]="totalWidth()" [style.height.px]="totalHeight()"
                 [style.transform]="'scale(' + scale() + ')'">
              <svg [attr.width]="totalWidth()" [attr.height]="totalHeight()"
                   style="position:absolute;top:0;left:0;pointer-events:none;overflow:visible;">
                @for (l of lines(); track $index) {
                  <line [attr.x1]="l.x1" [attr.y1]="l.y1" [attr.x2]="l.x2" [attr.y2]="l.y2"
                        stroke="#cbd5e1" stroke-width="1.5" />
                }
              </svg>

              @for (e of positions(); track e.match.id) {
                <div class="match-card" [style.top.px]="e.top" [style.left.px]="e.left">
                  <div class="match-card-main">
                    @if (!(e.match.round === 1 && e.match.teamAId && e.match.teamBId && !e.match.result)) {
                      <div class="match-label">{{ matchLabel(e.match) }}</div>
                    }
                    <div class="team-row"
                         [class.winner]="e.match.result === 'WonA'"
                         [class.loser]="e.match.result === 'WonB'"
                         [class.tbd]="!e.match.teamAId">
                      {{ teamName(e.match.teamAId) }}
                    </div>
                    <div class="team-row"
                         [class.winner]="e.match.result === 'WonB'"
                         [class.loser]="e.match.result === 'WonA'"
                         [class.tbd]="!e.match.teamBId">
                      {{ teamName(e.match.teamBId) }}
                    </div>
                    @if (e.match.teamAId && e.match.teamBId && !e.match.result) {
                      <div class="winner-btns" [class.compact]="e.match.round === 1">
                        <button type="button" class="btn btn-sm btn-winner" (click)="startReport(e.match, 'WonA')">
                          {{ teamName(e.match.teamAId) }} wins
                        </button>
                        <button type="button" class="btn btn-sm btn-winner" (click)="startReport(e.match, 'WonB')">
                          {{ teamName(e.match.teamBId) }} wins
                        </button>
                      </div>
                    }
                  </div>
                  @if (setRows(e.match).length > 0) {
                    <div class="match-card-score">
                      @for (row of setRows(e.match); track $index) {
                        <div class="score-row">{{ row }}</div>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class MatchBracketComponent implements OnInit, OnDestroy {
  tournamentId = 0;
  tournamentPin = '';
  matches = signal<Match[]>([]);
  teams   = signal<Team[]>([]);
  loading = signal(true);

  scorePrompt   = signal<Match | null>(null);
  pendingWinner = signal<'WonA' | 'WonB' | null>(null);
  scoreValue    = signal<string | null>(null);
  submitting    = signal(false);
  reportError   = signal('');

  containerWidth = signal(window.innerWidth);

  @HostListener('window:resize')
  onResize(): void {
    this.containerWidth.set(window.innerWidth);
  }

  scale = computed(() => {
    const tw = this.totalWidth();
    const cw = this.containerWidth();
    if (tw <= 0 || cw <= 0) return 1;
    return cw > tw ? cw / tw : 1;
  });

  scaledWidth  = computed(() => Math.round(this.totalWidth()  * this.scale()));
  scaledHeight = computed(() => Math.round(this.totalHeight() * this.scale()));

  private mainEl: HTMLElement | null = null;
  private signalRSub?: Subscription;

  constructor(
    private matchService: MatchService,
    private teamService: TeamService,
    private tournamentService: TournamentService,
    private route: ActivatedRoute,
    private signalR: PublicSignalRService,
    private renderer: Renderer2
  ) {}

  ngOnInit(): void {
    this.mainEl = document.querySelector('main');
    if (this.mainEl) this.renderer.addClass(this.mainEl, 'bracket-fullwidth');

    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    this.tournamentService.getById(this.tournamentId).subscribe({
      next: tournament => {
        this.tournamentPin = tournament.registrationPin ?? '';
        this.signalR.joinTournamentGroup(this.tournamentPin);
      }
    });
    this.teamService.getAll(this.tournamentId).subscribe(t => this.teams.set(t));
    this.loadMatches();

    this.signalRSub = new Subscription();
    this.signalRSub.add(this.signalR.tournamentMatchUpdated$.subscribe(({ pin }) => {
      if (pin === this.tournamentPin) {
        this.loadMatches();
      }
    }));
    this.signalRSub.add(this.signalR.reconnected$.subscribe(() => this.loadMatches()));
  }

  ngOnDestroy(): void {
    if (this.mainEl) this.renderer.removeClass(this.mainEl, 'bracket-fullwidth');
    if (this.tournamentPin) {
      this.signalR.leaveTournamentGroup(this.tournamentPin);
    }
    this.signalRSub?.unsubscribe();
  }

  private loadMatches(): void {
    this.loading.set(true);
    this.matchService.getAll(this.tournamentId).subscribe({
      next: data => { this.matches.set(data); this.loading.set(false); },
      error: ()   => { this.loading.set(false); }
    });
  }

  hideByes = computed(() => {
    const round1 = this.matches().filter(m => m.round === 1);
    const byeCount = round1.filter(m => !m.teamAId || !m.teamBId).length;
    return byeCount > round1.length / 2;
  });

  positions = computed<MatchPos[]>(() => {
    const hideByes = this.hideByes();
    return this.matches()
      .filter(m => !(hideByes && m.round === 1 && (!m.teamAId || !m.teamBId)))
      .map(m => ({
        match: m,
        top:  cardTop(m.round - 1, m.no - 1),
        left: (m.round - 1) * COL_W
      }));
  });

  maxRound = computed(() => Math.max(0, ...this.matches().map(m => m.round)));

  totalWidth = computed(() => this.maxRound() * COL_W - CONN_W + 2);

  totalHeight = computed(() => {
    const round1Count = this.matches().filter(m => m.round === 1).length;
    return Math.max(UNIT_H, round1Count * UNIT_H);
  });

  lines = computed<Line[]>(() => {
    const result: Line[] = [];
    const hideByes = this.hideByes();
    const rounds = [...new Set(this.matches().map(m => m.round))].sort((a, b) => a - b);

    for (const round of rounds) {
      if (round === this.maxRound()) continue;

      const ri       = round - 1;
      const midX     = ri * COL_W + CARD_W + CONN_W / 2;
      const nextLeft = (ri + 1) * COL_W;
      const cardRight = ri * COL_W + CARD_W;

      const inRound = this.matches()
        .filter(m => m.round === round)
        .sort((a, b) => a.no - b.no);

      if (round === 1 && hideByes) {
        for (const m of inRound) {
          if (!m.teamAId || !m.teamBId) continue;
          const ni            = m.no - 1;
          const centerY       = cardTop(ri, ni) + CARD_H / 2;
          const parentCenterY = cardTop(ri + 1, Math.floor(ni / 2)) + CARD_H / 2;
          result.push({ x1: cardRight, y1: centerY, x2: nextLeft, y2: parentCenterY });
        }
      } else {
        for (const m of inRound) {
          const ni      = m.no - 1;
          const centerY = cardTop(ri, ni) + CARD_H / 2;

          result.push({ x1: cardRight, y1: centerY, x2: midX, y2: centerY });

          if (ni % 2 === 0) {
            const sibCenterY    = cardTop(ri, ni + 1) + CARD_H / 2;
            const parentCenterY = (centerY + sibCenterY) / 2;
            result.push({ x1: midX, y1: centerY,       x2: midX,     y2: sibCenterY    });
            result.push({ x1: midX, y1: parentCenterY,  x2: nextLeft, y2: parentCenterY });
          }
        }
      }
    }
    return result;
  });

  matchLabel(match: Match): string {
    if (match.round === this.maxRound())
      return 'Final';

    if (match.round === this.maxRound()-1)
      return `Semifinal ${match.no}`;

    return `Round ${match.round} · Match ${match.no}`;
  }

  teamName(id: number | null): string {
    if (id === null) return 'TBD';
    return this.teams().find(t => t.id === id)?.name ?? `#${id}`;
  }

  setRows(match: Match): string[] {
    if (!match.sets || match.sets.length === 0) return [];
    return [...match.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`);
  }

  /** Formats the match's already-recorded sets as "6:4, 6:2, 7:6(2)" to prefill the score input. */
  private initialScoreValue(m: Match): string | null {
    if (!m.sets || m.sets.length === 0) return null;
    return [...m.sets]
      .sort((a, b) => a.no - b.no)
      .map(s => `${s.scoreA}:${s.scoreB}${s.tieBreakPoints !== null ? `(${s.tieBreakPoints})` : ''}`)
      .join(', ');
  }

  startReport(m: Match, winner: 'WonA' | 'WonB'): void {
    this.reportError.set('');
    this.scoreValue.set(this.initialScoreValue(m));
    this.pendingWinner.set(winner);
    this.scorePrompt.set(m);
  }

  cancelReport(): void {
    this.scorePrompt.set(null);
    this.pendingWinner.set(null);
    this.scoreValue.set(null);
  }

  confirmReport(m: Match): void {
    const winner = this.pendingWinner();
    if (!winner) return;

    this.submitting.set(true);
    this.reportError.set('');
    this.matchService.setWinner(this.tournamentId, m.id, winner, this.scoreValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.scorePrompt.set(null);
        this.pendingWinner.set(null);
        this.scoreValue.set(null);
        this.loadMatches();
      },
      error: err => {
        this.submitting.set(false);
        this.reportError.set(err.error?.detail ?? 'Set winner failed.');
      }
    });
  }
}

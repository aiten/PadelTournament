import { Component, OnInit, signal, computed } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Match } from '../models/match.model';
import { Team } from '../models/team.model';
import { MatchService } from '../services/match.service';
import { TeamService } from '../services/team.service';

const CARD_W = 220;
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
  imports: [RouterModule],
  styles: [`
    .bracket-scroll { overflow: auto; padding-bottom: 24px; }
    .bracket-wrap { position: relative; }
    .match-card {
      position: absolute;
      width: ${CARD_W}px;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      background: #fff;
      padding: 8px 10px;
      box-sizing: border-box;
      box-shadow: 0 1px 3px rgba(0,0,0,.08);
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
  `],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Tournament Bracket</h2>
        <a [routerLink]="['/tournaments', tournamentId, 'matches']" class="btn">Back to Matches</a>
      </div>
      @if (loading()) {
        <p class="empty">Loading…</p>
      } @else if (positions().length === 0) {
        <p class="empty">No matches found.</p>
      } @else {
        <div class="bracket-scroll">
          <div class="bracket-wrap" [style.width.px]="totalWidth()" [style.height.px]="totalHeight()">
            <svg [attr.width]="totalWidth()" [attr.height]="totalHeight()"
                 style="position:absolute;top:0;left:0;pointer-events:none;overflow:visible;">
              @for (l of lines(); track $index) {
                <line [attr.x1]="l.x1" [attr.y1]="l.y1" [attr.x2]="l.x2" [attr.y2]="l.y2"
                      stroke="#cbd5e1" stroke-width="1.5" />
              }
            </svg>

            @for (e of positions(); track e.match.id) {
              <div class="match-card" [style.top.px]="e.top" [style.left.px]="e.left">
                @if (!(e.match.round === 1 && e.match.teamAId && e.match.teamBId && !e.match.result)) {
                  <div class="match-label">Round {{ e.match.round }} · Match {{ e.match.no }}</div>
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
                    <button type="button" class="btn btn-sm btn-winner" (click)="setWinner(e.match, 'WonA')">
                      {{ teamName(e.match.teamAId) }} wins
                    </button>
                    <button type="button" class="btn btn-sm btn-winner" (click)="setWinner(e.match, 'WonB')">
                      {{ teamName(e.match.teamBId) }} wins
                    </button>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class MatchBracketComponent implements OnInit {
  tournamentId = 0;
  matches = signal<Match[]>([]);
  teams   = signal<Team[]>([]);
  loading = signal(true);

  constructor(
    private matchService: MatchService,
    private teamService: TeamService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.tournamentId = +this.route.snapshot.paramMap.get('tournamentId')!;
    this.teamService.getAll(this.tournamentId).subscribe(t => this.teams.set(t));
    this.loadMatches();
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

  totalWidth = computed(() => {
    const maxRound = Math.max(0, ...this.matches().map(m => m.round));
    return maxRound * COL_W - CONN_W + 2;
  });

  totalHeight = computed(() => {
    const round1Count = this.matches().filter(m => m.round === 1).length;
    return Math.max(UNIT_H, round1Count * UNIT_H);
  });

  lines = computed<Line[]>(() => {
    const result: Line[] = [];
    const hideByes = this.hideByes();
    const rounds = [...new Set(this.matches().map(m => m.round))].sort((a, b) => a - b);
    const maxRound = Math.max(...rounds);

    for (const round of rounds) {
      if (round === maxRound) continue;

      const ri       = round - 1;
      const midX     = ri * COL_W + CARD_W + CONN_W / 2;
      const nextLeft = (ri + 1) * COL_W;
      const cardRight = ri * COL_W + CARD_W;

      const inRound = this.matches()
        .filter(m => m.round === round)
        .sort((a, b) => a.no - b.no);

      if (round === 1 && hideByes) {
        for (const m of inRound) {
          if (!m.teamAId || !m.teamBId) continue; // bye — no line drawn
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

  teamName(id: number | null): string {
    if (id === null) return 'TBD';
    return this.teams().find(t => t.id === id)?.name ?? `#${id}`;
  }

  setWinner(m: Match, winner: 'WonA' | 'WonB'): void {
    this.matchService.setWinner(this.tournamentId, m.id, winner).subscribe({
      next:  () => this.loadMatches(),
      error: err => alert(err.error?.detail ?? 'Set winner failed.')
    });
  }
}

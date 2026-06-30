import { Component, OnInit, OnDestroy, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subscription, forkJoin } from 'rxjs';
import { GlobalStateService } from '../services/global-state.service';
import { Match } from '../models/match.model';
import { Team } from '../models/team.model';
import { PublicService } from '../services/public.service';
import { PublicSignalRService } from '../services/signalr.service';

const CARD_W = 220;
const CARD_H = 76;
const UNIT_H = 100;
const CONN_W = 60;
const COL_W  = CARD_W + CONN_W;

interface MatchPos { match: Match; top: number; left: number; }
interface Line { x1: number; y1: number; x2: number; y2: number; }

function cardTop(ri: number, ni: number): number {
  const factor = Math.pow(2, ri);
  return Math.round(UNIT_H * (ni * factor + (factor - 1) / 2));
}

@Component({
  selector: 'app-public-bracket',
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
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Tournament Bracket</h2>
        <button type="button" class="btn" (click)="goBack()">Back</button>
      </div>
      @if (loading()) {
        <p class="empty">Loading…</p>
      } @else if (error()) {
        <p class="error">{{ error() }}</p>
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
                <div class="match-label">Round {{ e.match.round }} · Match {{ e.match.no }}</div>
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
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class PublicBracketComponent implements OnInit, OnDestroy {
  matches = signal<Match[]>([]);
  teams   = signal<Team[]>([]);
  loading = signal(true);
  error   = signal('');
  teamNames = computed(() => new Map(this.teams().map(t => [t.id, t.name])));

  private pin = '';
  private signalRSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicService: PublicService,
    private globalState: GlobalStateService,
    private signalR: PublicSignalRService
  ) {}

  goBack(): void {
    if (this.globalState.lastCode) {
      this.router.navigate(['/public', this.pin, this.globalState.lastCode]);
    } else {
      this.router.navigate(['/public']);
    }
  }

  ngOnInit(): void {
    this.pin = this.route.snapshot.paramMap.get('pin')!;

    this.signalR.joinTournamentGroup(this.pin);
    this.signalRSub = new Subscription();
    this.signalRSub.add(this.signalR.tournamentMatchUpdated$.subscribe(msg => {
      if (msg.pin === this.pin) {
        this.reloadData();
      }
    }));
    this.signalRSub.add(this.signalR.tournamentTeamUpdated$.subscribe(msg => {
      if (msg.pin === this.pin) {
        this.reloadData();
      }
    }));

    this.reloadData();
  }

  ngOnDestroy(): void {
    this.signalR.leaveTournamentGroup(this.pin);
    this.signalRSub?.unsubscribe();
  }

  private reloadData(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      matches: this.publicService.getMatches(this.pin),
      teams: this.publicService.getTeams(this.pin)
    }).subscribe({
      next: ({ matches, teams }) => {
        this.matches.set(matches);
        this.teams.set(teams);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.detail ?? 'Tournament not found.');
        this.loading.set(false);
      }
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
      const ri        = round - 1;
      const midX      = ri * COL_W + CARD_W + CONN_W / 2;
      const nextLeft  = (ri + 1) * COL_W;
      const cardRight = ri * COL_W + CARD_W;
      const inRound   = this.matches().filter(m => m.round === round).sort((a, b) => a.no - b.no);

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
            result.push({ x1: midX, y1: centerY,       x2: midX,     y2: sibCenterY     });
            result.push({ x1: midX, y1: parentCenterY,  x2: nextLeft, y2: parentCenterY  });
          }
        }
      }
    }
    return result;
  });

  teamName(id: number | null): string {
    if (id === null) return 'TBD';
    return this.teamNames().get(id) ?? `#${id}`;
  }
}

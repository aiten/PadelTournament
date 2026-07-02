export interface GameResult {
  no: number;
  server: string | null;
  points: string;
}

export interface SetResult {
  no: number;
  scoreA: number;
  scoreB: number;
  tieBreakPoints: number | null;
  gameResults: GameResult[];
}

export interface Match {
  id: number;
  tournamentId: number;
  round: number;
  no: number;
  teamAId: number | null;
  teamBId: number | null;
  start: string | null;
  nextMatchId: number | null;
  result: string | null;
  acceptA: string | null;
  acceptB: string | null;
  remark: string | null;
  sets: SetResult[] | null;
}

export interface MatchModify {
  teamAId: number | null;
  teamBId: number | null;
  start: string | null;
  nextMatchId: number | null;
  result: string | null;
  remark: string | null;
}

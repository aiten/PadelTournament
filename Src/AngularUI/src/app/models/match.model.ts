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
}

export interface MatchModify {
  teamAId: number | null;
  teamBId: number | null;
  start: string | null;
  nextMatchId: number | null;
  result: string | null;
  remark: string | null;
}

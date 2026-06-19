export interface TournamentOverview {
  id: number;
  description: string;
  from: string;
  to: string | null;
  registrationPin?: string | null;
  teams: number;
  matches: number;
  finishedMatches: number;
}

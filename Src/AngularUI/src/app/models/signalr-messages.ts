export interface TournamentUpdatedMessage {
  tournamentId: number;
}

export interface TournamentMatchUpdatedMessage {
  pin: string;
  matchId: number | null;
}

export interface TournamentTeamUpdatedMessage {
  pin: string;
}

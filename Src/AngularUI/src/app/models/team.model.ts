export interface Team {
  id: number;
  tournamentId: number;
  player1: string;
  player2: string | null;
  name: string;
  seed: number | null;
  startMatchPos: number | null;
  registrationDate: string;
  registrationCode: string | null;
}

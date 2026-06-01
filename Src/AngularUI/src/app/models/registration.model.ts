export interface TournamentRegistrationRequest {
  name: string;
  pin: number;
}

export interface TournamentRegistrationResult {
  name: string;
  pin: number;
  registrationCode: string;
}

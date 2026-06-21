export interface TournamentRegistrationRequest {
  name: string;
  pin: string;
}

export interface TournamentRegistrationResult {
  name: string;
  pin: string;
  registrationCode: string;
}

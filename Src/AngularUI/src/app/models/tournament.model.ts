export interface Tournament {
  id: number;
  description: string;
  from: string;
  to: string | null;
  registrationPin: number | null;
}

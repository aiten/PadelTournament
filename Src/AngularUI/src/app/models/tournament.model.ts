export enum Format {
  Tennis = 'Tennis',
  Padel = 'Padel',
  Soccer = 'Soccer'
}

export interface Tournament {
  id: number;
  description: string;
  from: string;
  to: string | null;
  registrationPin: string | null;
  format: Format | null;
  bestOf: number;
  gamesToWinSet: number;
  minDiff: number;
  noAdv: boolean;
}

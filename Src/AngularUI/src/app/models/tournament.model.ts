export enum CountType {
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
  countType: CountType | null;
  bestOf: number;
  gamesToWinSet: number;
  minDiff: number;
  noAdv: boolean;
}

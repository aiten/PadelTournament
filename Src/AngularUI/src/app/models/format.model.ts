export enum PlayingFormat {
  Tennis = 'Tennis',
  Padel = 'Padel',
  Soccer = 'Soccer'
}

export interface Format {
  id: number;
  name: string;
  playingFormat: PlayingFormat | null;
  bestOf: number | null;
  gamesToWinSet: number | null;
  minMargin: number | null;
  noAdv: boolean;
  noTiebreak: boolean;
}

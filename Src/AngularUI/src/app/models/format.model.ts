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
  minDiff: number | null;
  noAdv: boolean;
}

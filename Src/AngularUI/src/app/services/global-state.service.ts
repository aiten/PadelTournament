import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GlobalStateService {
  lastPin  = 0;
  lastCode = '';
}

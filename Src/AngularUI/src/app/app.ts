import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MenuComponent } from './menu/menu.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule, MenuComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <app-menu></app-menu>
     <main>
      <router-outlet />
    </main>
  `
})
export class App {}

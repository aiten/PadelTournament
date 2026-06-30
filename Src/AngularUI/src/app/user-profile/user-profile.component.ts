import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { User } from '../models/user.model';
import Keycloak from 'keycloak-js';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-user-profile',
  templateUrl: 'user-profile.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: [`user-profile.component.css`]
})
export class UserProfileComponent implements OnInit {
  private readonly keycloak = inject(Keycloak);

  user: User | undefined;

  async ngOnInit() {
    if (this.keycloak?.authenticated) {
      const profile = await this.keycloak.loadUserProfile();

      console.log('User Profile:', profile);
      console.log(await this.keycloak.hasResourceRole(environment.roles.admin));
      console.log(await this.keycloak.hasResourceRole(environment.roles.user));
      console.log(await this.keycloak.hasResourceRole(environment.roles.viewProfile));

      this.user = {
        name: `${profile?.firstName} ${profile.lastName}`,
        email: profile?.email,
        username: profile?.username
      };
    }
  }
}

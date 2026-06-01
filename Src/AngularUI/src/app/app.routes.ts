import { Routes } from '@angular/router';
import { ForbiddenComponent } from './forbidden/forbidden.component';
import { environment } from '../environments/environment';
import { UserProfileComponent } from './user-profile/user-profile.component';

import { canActivateAuthRole } from './guards/auth-role.guard';
import { TournamentListComponent } from './tournaments/tournament-list.component';
import { TournamentFormComponent } from './tournaments/tournament-form.component';
import { TeamListComponent } from './teams/team-list.component';
import { TeamFormComponent } from './teams/team-form.component';
import { MatchListComponent } from './matches/match-list.component';
import { MatchFormComponent } from './matches/match-form.component';
import { MatchBracketComponent } from './matches/match-bracket.component';
import { TournamentStartComponent } from './tournaments/tournament-start.component';
import { TeamBulkAddComponent } from './teams/team-bulk-add.component';
import { RegisterFormComponent } from './registration/register-form.component';
import { RegisterResultComponent } from './registration/register-result.component';
import { PublicEntryComponent } from './public/public-entry.component';
import { PublicTeamComponent } from './public/public-team.component';
import { PublicMatchesComponent } from './public/public-matches.component';
import { PublicBracketComponent } from './public/public-bracket.component';

export const routes: Routes = [
  { path: '', redirectTo: '/public', pathMatch: 'full' },
  { path: 'tournaments', component: TournamentListComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/new', component: TournamentFormComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:id', component: TournamentFormComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/start', component: TournamentStartComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/teams', component: TeamListComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/teams/new', component: TeamFormComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/teams/bulk', component: TeamBulkAddComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/teams/:id', component: TeamFormComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/matches', component: MatchListComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/matches/:id', component: MatchFormComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'tournaments/:tournamentId/bracket', component: MatchBracketComponent, canActivate: [canActivateAuthRole], data: { role: environment.roles.admin } },
  { path: 'public', component: PublicEntryComponent },
  { path: 'public/:pin/bracket', component: PublicBracketComponent },
  { path: 'public/:pin/:code', component: PublicTeamComponent },
  { path: 'public/:pin/:code/matches', component: PublicMatchesComponent },
  { path: 'registration', component: RegisterFormComponent },
  { path: 'registration/result', component: RegisterResultComponent },
  { path: 'profile', component: UserProfileComponent,  canActivate: [canActivateAuthRole],    data: { role: environment.roles.viewProfile }  },
  { path: 'forbidden', component: ForbiddenComponent },
  { path: '**', redirectTo: '/public' }

];

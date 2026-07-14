import { Route } from "@angular/router";
import { LoginComponent } from "./login/login.component";
import { SignUpComponent } from "./sign-up/sign-up.component";

import { SettingsComponent } from "./settings/settings.component";
import { authGuard } from "../../core/guards/auth-guard";

export const accountRoutes: Route[] = [
    {path: 'login', component: LoginComponent},
    {path: 'register', component: SignUpComponent},
    {path: 'sign-up', component: SignUpComponent},
    {path: 'settings', component: SettingsComponent, canActivate: [authGuard]}
]
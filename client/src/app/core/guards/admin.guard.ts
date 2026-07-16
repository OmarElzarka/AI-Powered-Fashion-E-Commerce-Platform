import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account.service';
import { SnackbarService } from '../services/snackbar.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  if (accountService.isAdmin()) {
    return true;
  } else {
    snackbar.error('You do not have permission to access this area.');
    router.navigate(['/']);
    return false;
  }
};

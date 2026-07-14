import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { SnackbarService } from '../services/snackbar.service';
import { AccountService } from '../services/account.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);
  const accountService = inject(AccountService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 400) {
        if (err.error.errors) {
          const modalStateErrors = [];
          for (const key in err.error.errors) {
            if (err.error.errors[key]) {
              modalStateErrors.push(err.error.errors[key])
            }
          }
          throw modalStateErrors.flat();
        } else {
          snackbar.error(err.error.title || err.error)
        }
      }
      if (err.status === 401) {
        const isAuthReq = req.url.includes('refresh-token') || req.url.includes('login') || req.url.includes('register') || req.url.includes('logout');
        if (!isAuthReq) {
            const hasRefreshToken = accountService.getRefreshToken();
            if (hasRefreshToken) {
                return accountService.refreshToken().pipe(
                    switchMap(res => {
                        const clonedRequest = req.clone({
                            setHeaders: {
                                Authorization: `Bearer ${res.accessToken}`
                            }
                        });
                        return next(clonedRequest);
                    }),
                    catchError(refreshErr => {
                        accountService.logout().subscribe();
                        router.navigateByUrl('/account/login');
                        snackbar.error('Session expired, please log in again.');
                        return throwError(() => refreshErr);
                    })
                );
            } else {
                // If the user has no refresh token but got a 401 on a non-auth request,
                // we clear any partial state, redirect, and maybe show an error.
                accountService.logout().subscribe();
                router.navigateByUrl('/account/login');
                snackbar.error('Please log in to continue.');
            }
        } else {
            // For auth requests (like login), we just show the error returned by the server.
            snackbar.error(err.error.title || err.error || 'Authentication failed');
        }
      }
      if (err.status === 403) {
        snackbar.error('Forbidden');
      }
      if (err.status === 404) {
        router.navigateByUrl('/not-found');
      }
      if (err.status === 500) {
        const navigationExtras: NavigationExtras = {state: {error: err.error}};
        router.navigateByUrl('/server-error', navigationExtras);
      }
      return throwError(() => err)
    })
  )
};

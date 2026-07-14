import { inject, Injectable } from '@angular/core';
import { forkJoin, of, tap, catchError, switchMap } from 'rxjs';
import { CartService } from './cart.service';
import { AccountService } from './account.service';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class InitService {
  private cartService = inject(CartService);
  private accountService = inject(AccountService);
  private signalrService = inject(SignalrService);

  init() {
    const token = this.accountService.getAccessToken();
    const user$ = token ? this.accountService.getUserInfo().pipe(
        tap(user => {
          if (user) this.signalrService.createHubConnection()
        }),
        catchError(() => of(null))
      ) : of(null);

    return forkJoin({
      user: user$,
      cart: user$.pipe(
        switchMap(() => {
          const cartId = localStorage.getItem('cart_id');
          return cartId ? this.cartService.getCart(cartId) : of(null);
        })
      )
    })
  }
}

import { inject, Injectable } from '@angular/core';
import { forkJoin, of, tap, catchError } from 'rxjs';
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
    const cartId = localStorage.getItem('cart_id');
    const cart$ = cartId ? this.cartService.getCart(cartId) : of(null);
    const token = this.accountService.getAccessToken();
    const user$ = token ? this.accountService.getUserInfo().pipe(
        tap(user => {
          if (user) this.signalrService.createHubConnection()
        }),
        catchError(() => of(null))
      ) : of(null);

    return forkJoin({
      cart: cart$,
      user: user$
    })
  }
}

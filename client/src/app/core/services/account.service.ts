import { computed, inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { User, Address } from '../../shared/models/user';
import { map, tap, catchError } from 'rxjs/operators';
import { SignalrService } from './signalr.service';
import { of, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.baseUrl;
  private http = inject(HttpClient);
  private signalrService = inject(SignalrService);
  currentUser = signal<User | null>(null);
  isAdmin = computed(() => {
    const roles = this.currentUser()?.roles;
    return Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin'
  })

  login(values: any) {
    return this.http.post<{accessToken: string, refreshToken: string}>(this.baseUrl + 'account/login-jwt', values).pipe(
      tap(res => {
        localStorage.setItem('token', res.accessToken);
        localStorage.setItem('refreshToken', res.refreshToken);
        this.signalrService.createHubConnection();
      })
    )
  }

  register(values: any) {
    return this.http.post(this.baseUrl + 'account/register', values);
  }

  getUserInfo() {
    return this.http.get<User>(this.baseUrl + 'account/user-info').pipe(
      map(user => {
        this.currentUser.set(user);
        return user;
      })
    );
  }

  logout() {
    return this.http.post(this.baseUrl + 'account/logout', {}).pipe(
      tap(() => {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        this.currentUser.set(null);
        this.signalrService.stopHubConnection();
      }),
      catchError(() => {
        // Fallback even if API logout fails
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        this.currentUser.set(null);
        this.signalrService.stopHubConnection();
        return of(null);
      })
    )
  }

  updateAddress(address: Address) {
    return this.http.post<Address>(this.baseUrl + 'account/address', address).pipe(
      tap(updatedAddress => {
        this.currentUser.update(user => {
          if (user) user.address = updatedAddress;
          return user;
        })
      })
    )
  }

  updateProfile(data: { firstName: string, lastName: string, phoneNumber: string }) {
    return this.http.post(this.baseUrl + 'account/profile', data).pipe(
      tap(() => {
        this.currentUser.update(user => {
          if (user) {
            user.firstName = data.firstName;
            user.lastName = data.lastName;
            user.phoneNumber = data.phoneNumber;
          }
          return user;
        })
      })
    )
  }

  updateLanguage(language: string) {
    return this.http.post(this.baseUrl + 'account/language', { language }).pipe(
      tap(() => {
        this.currentUser.update(user => {
          if (user) user.language = language;
          return user;
        })
      })
    )
  }

  deleteAccount() {
    return this.http.delete(this.baseUrl + 'account').pipe(
      tap(() => {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        this.currentUser.set(null);
        this.signalrService.stopHubConnection();
      })
    )
  }

  getAuthState() {
    return this.http.get<{ isAuthenticated: boolean }>(this.baseUrl + 'account/auth-status');
  }

  getAccessToken() {
    return localStorage.getItem('token');
  }

  getRefreshToken() {
    return localStorage.getItem('refreshToken');
  }

  refreshToken() {
    const token = this.getRefreshToken();
    if (!token) return throwError(() => new Error('No refresh token available'));

    return this.http.post<{accessToken: string, refreshToken: string}>(this.baseUrl + 'account/refresh-token', `"${token}"`, {
      headers: { 'Content-Type': 'application/json' }
    }).pipe(
      tap(res => {
        localStorage.setItem('token', res.accessToken);
        localStorage.setItem('refreshToken', res.refreshToken);
      })
    );
  }
}

import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, ReactiveFormsModule, ValidatorFn, Validators, AsyncValidatorFn } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { HttpClient } from '@angular/common/http';
import { map, switchMap, timer, take, startWith } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { NgClass } from '@angular/common';
import { COUNTRIES, PHONE_CODES, PhoneCode } from './country-data';

/* ── Password validators ──────────────────────── */
function hasUpperCase(): ValidatorFn {
  return (c: AbstractControl) => /[A-Z]/.test(c.value) ? null : { noUpperCase: true };
}
function hasLowerCase(): ValidatorFn {
  return (c: AbstractControl) => /[a-z]/.test(c.value) ? null : { noLowerCase: true };
}
function hasDigit(): ValidatorFn {
  return (c: AbstractControl) => /\d/.test(c.value) ? null : { noDigit: true };
}
function hasSpecialChar(): ValidatorFn {
  return (c: AbstractControl) => /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(c.value) ? null : { noSpecialChar: true };
}

function matchPassword(): ValidatorFn {
  return (group: AbstractControl) => {
    const pw = group.get('password')?.value;
    const cpw = group.get('confirmPassword')?.value;
    if (!cpw) return null; // don't show mismatch until user types
    return pw === cpw ? null : { mismatch: true };
  };
}

@Component({
  selector: 'app-sign-up',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule,
    MatAutocompleteModule, MatIconModule, MatSelectModule, RouterLink, NgClass
  ],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.scss'
})
export class SignUpComponent {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private snackbar = inject(SnackbarService);
  private http = inject(HttpClient);

  isLoading = false;
  showPassword = false;
  showConfirmPassword = false;
  serverErrors = signal<string[]>([]);

  // Country data
  countries = COUNTRIES;
  filteredCountries = COUNTRIES;
  phoneCodes = PHONE_CODES;
  selectedPhoneCode = signal<PhoneCode>(PHONE_CODES[0]);

  // Country search
  countrySearchCtrl = new FormControl('');

  emailExistsValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      return timer(500).pipe(
        switchMap(() => {
          if (!control.value) return [null];
          return this.http.get<boolean>(
            `${environment.baseUrl}account/check-email?email=${control.value}`
          ).pipe(map(res => res ? { emailExists: true } : null));
        }),
        take(1)
      );
    };
  }

  signUpForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email], [this.emailExistsValidator()]],
    password: ['', [Validators.required, Validators.minLength(8), hasUpperCase(), hasLowerCase(), hasDigit(), hasSpecialChar()]],
    confirmPassword: ['', Validators.required],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\d{6,15}$/)]],
    line1: ['', Validators.required],
    line2: [''],
    city: ['', Validators.required],
    country: ['', Validators.required],
    postalCode: ['', Validators.required]
  }, { validators: matchPassword() });

  constructor() {
    // Filter countries as user types in the country field
    this.signUpForm.get('country')!.valueChanges.pipe(
      startWith('')
    ).subscribe(val => {
      const q = (val || '').toLowerCase();
      this.filteredCountries = this.countries.filter(c => c.toLowerCase().includes(q));
    });
  }

  selectCountry(country: string) {
    this.signUpForm.get('country')!.setValue(country);
    // Try to match phone code
    const code = this.phoneCodes.find(p => p.country === country);
    if (code) this.selectedPhoneCode.set(code);
  }

  selectPhoneCode(code: PhoneCode) {
    this.selectedPhoneCode.set(code);
  }

  /* ── Password strength helpers ─────── */
  get pw() { return this.signUpForm.get('password')!; }
  get pwRules() {
    return [
      { label: 'At least 8 characters', valid: !this.pw.hasError('minlength') && this.pw.value!.length >= 8 },
      { label: 'One uppercase letter', valid: !this.pw.hasError('noUpperCase') },
      { label: 'One lowercase letter', valid: !this.pw.hasError('noLowerCase') },
      { label: 'One number', valid: !this.pw.hasError('noDigit') },
      { label: 'One special character', valid: !this.pw.hasError('noSpecialChar') },
    ];
  }
  get pwStrength(): number {
    return this.pwRules.filter(r => r.valid).length;
  }

  /* ── Field validation helpers ────────── */
  isFieldValid(name: string): boolean {
    const ctrl = this.signUpForm.get(name);
    return !!ctrl && ctrl.valid && ctrl.dirty;
  }
  isFieldInvalid(name: string): boolean {
    const ctrl = this.signUpForm.get(name);
    return !!ctrl && ctrl.invalid && ctrl.touched;
  }

  togglePassword() { this.showPassword = !this.showPassword; }
  toggleConfirmPassword() { this.showConfirmPassword = !this.showConfirmPassword; }

  onSubmit() {
    if (this.signUpForm.invalid) {
      this.signUpForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.serverErrors.set([]);

    // Build payload — strip confirmPassword, prepend phone code
    const formVal = { ...this.signUpForm.value };
    delete (formVal as any).confirmPassword;
    formVal.phoneNumber = `${this.selectedPhoneCode().dialCode}${formVal.phoneNumber}`;

    this.accountService.register(formVal).subscribe({
      next: () => {
        this.snackbar.success('Account created successfully! Please log in.');
        this.router.navigateByUrl('/account/login');
      },
      error: (err) => {
        this.isLoading = false;
        if (Array.isArray(err)) {
          this.serverErrors.set(err);
        } else if (err?.error?.errors) {
          const msgs: string[] = [];
          for (const key in err.error.errors) {
            if (err.error.errors[key]) {
              msgs.push(...(Array.isArray(err.error.errors[key]) ? err.error.errors[key] : [err.error.errors[key]]));
            }
          }
          this.serverErrors.set(msgs);
        } else if (typeof err?.error === 'string') {
          this.serverErrors.set([err.error]);
        } else {
          this.serverErrors.set(['Registration failed. Please try again.']);
        }
      }
    });
  }
}

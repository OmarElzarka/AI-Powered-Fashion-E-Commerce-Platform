import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  public accountService = inject(AccountService);
  private snackbar = inject(SnackbarService);
  private router = inject(Router);

  isSavingProfile = false;
  isSavingLanguage = false;
  isDeleting = false;
  showDeleteConfirm = false;

  profileForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phoneNumber: ['', Validators.pattern(/^\+?\d{6,15}$/)]
  });

  languageForm = this.fb.group({
    language: ['en', Validators.required]
  });

  languages = [
    { code: 'en', name: 'English' },
    { code: 'fr', name: 'Français' },
    { code: 'es', name: 'Español' },
    { code: 'de', name: 'Deutsch' },
    { code: 'ar', name: 'العربية' }
  ];

  ngOnInit() {
    this.accountService.getUserInfo().subscribe({
      next: (user) => {
        if (user) {
          this.profileForm.patchValue({
            firstName: user.firstName,
            lastName: user.lastName,
            phoneNumber: user.phoneNumber || ''
          });
          
          this.languageForm.patchValue({
            language: user.language || 'en'
          });
        }
      }
    });
  }

  updateProfile() {
    if (this.profileForm.invalid) return;

    this.isSavingProfile = true;
    this.accountService.updateProfile(this.profileForm.value as any).subscribe({
      next: () => {
        this.snackbar.success('Profile updated successfully');
        this.isSavingProfile = false;
      },
      error: () => {
        this.snackbar.error('Failed to update profile');
        this.isSavingProfile = false;
      }
    });
  }

  updateLanguage() {
    if (this.languageForm.invalid) return;

    this.isSavingLanguage = true;
    const lang = this.languageForm.value.language || 'en';
    this.accountService.updateLanguage(lang).subscribe({
      next: () => {
        this.snackbar.success('Language preference updated');
        this.isSavingLanguage = false;
      },
      error: () => {
        this.snackbar.error('Failed to update language');
        this.isSavingLanguage = false;
      }
    });
  }

  confirmDelete() {
    this.showDeleteConfirm = true;
  }

  cancelDelete() {
    this.showDeleteConfirm = false;
  }

  deleteAccount() {
    this.isDeleting = true;
    this.accountService.deleteAccount().subscribe({
      next: () => {
        this.snackbar.success('Account deleted successfully');
        this.router.navigateByUrl('/');
      },
      error: () => {
        this.snackbar.error('Failed to delete account');
        this.isDeleting = false;
        this.showDeleteConfirm = false;
      }
    });
  }
}

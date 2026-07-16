import { Component, inject, OnInit } from '@angular/core';
import { AdminService, AdminUser } from '../../admin.service';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { SnackbarService } from '../../../../core/services/snackbar.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  private adminService = inject(AdminService);
  private snackbar = inject(SnackbarService);
  users: AdminUser[] = [];
  loading = true;

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers() {
    this.loading = true;
    this.adminService.getUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.error('Failed to load users');
      }
    });
  }

  deleteUser(id: string) {
    if (confirm('Are you sure you want to delete this user? This action cannot be undone.')) {
      this.adminService.deleteUser(id).subscribe({
        next: () => {
          this.snackbar.success('User deleted successfully');
          this.loadUsers();
        },
        error: (err) => {
          this.snackbar.error(err.error?.message || 'Failed to delete user');
        }
      });
    }
  }

  toggleAdminRole(user: AdminUser) {
    const isAdmin = user.roles.includes('Admin');
    const actionText = isAdmin ? 'revoke admin privileges from' : 'grant admin privileges to';
    
    if (confirm(`Are you sure you want to ${actionText} this user?`)) {
      this.adminService.updateUserRole(user.id, 'Admin', !isAdmin).subscribe({
        next: () => {
          this.snackbar.success(`Successfully ${isAdmin ? 'revoked' : 'granted'} admin privileges`);
          this.loadUsers();
        },
        error: (err) => {
          this.snackbar.error(err.error?.message || 'Failed to update user role');
        }
      });
    }
  }
}

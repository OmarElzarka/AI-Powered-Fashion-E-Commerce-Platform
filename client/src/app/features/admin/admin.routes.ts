import { Routes } from '@angular/router';
import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { AdminDashboardComponent } from './dashboard/admin-dashboard/admin-dashboard.component';
import { AdminProductsComponent } from './products/admin-products/admin-products.component';
import { AdminUsersComponent } from './users/admin-users/admin-users.component';
import { AdminOrdersComponent } from './orders/admin-orders/admin-orders.component';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AdminDashboardComponent },
      { path: 'products', component: AdminProductsComponent },
      { path: 'products/add', loadComponent: () => import('./products/admin-product-form/admin-product-form.component').then(c => c.AdminProductFormComponent) },
      { path: 'products/edit/:id', loadComponent: () => import('./products/admin-product-form/admin-product-form.component').then(c => c.AdminProductFormComponent) },
      { path: 'users', component: AdminUsersComponent },
      { path: 'orders', component: AdminOrdersComponent }
    ]
  }
];

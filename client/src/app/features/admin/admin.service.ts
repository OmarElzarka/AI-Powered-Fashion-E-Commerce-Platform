import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Order } from '../../shared/models/order';
import { User } from '../../shared/models/user';
import { Pagination } from '../../shared/models/pagination';
import { Product } from '../../shared/models/product';

export interface DashboardStats {
  totalProducts: number;
  totalUsers: number;
  totalOrders: number;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.baseUrl;
  private http = inject(HttpClient);

  getDashboardStats() {
    return this.http.get<DashboardStats>(this.baseUrl + 'admin/dashboard-stats');
  }

  getUsers() {
    return this.http.get<AdminUser[]>(this.baseUrl + 'admin/users');
  }

  getOrders(pageIndex: number, pageSize: number) {
    let params = new HttpParams()
      .append('pageIndex', pageIndex.toString())
      .append('pageSize', pageSize.toString());

    return this.http.get<Pagination<Order>>(this.baseUrl + 'admin/orders', { params });
  }

  // Uses existing ProductController endpoint protected by Admin role
  createProduct(product: any) {
    return this.http.post<Product>(this.baseUrl + 'products', product);
  }

  updateProduct(id: number, product: any) {
    return this.http.put(this.baseUrl + 'products/' + id, product);
  }

  deleteProduct(id: number) {
    return this.http.delete(this.baseUrl + 'products/' + id);
  }

  deleteUser(id: string) {
    return this.http.delete(this.baseUrl + 'admin/users/' + id);
  }

  updateUserRole(id: string, role: string, assign: boolean) {
    return this.http.post(this.baseUrl + `admin/users/${id}/roles?role=${role}&assign=${assign}`, {});
  }
}

import { Component, inject, OnInit } from '@angular/core';
import { AdminService } from '../../admin.service';
import { Order } from '../../../../shared/models/order';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, FormsModule, MatIcon],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.scss'
})
export class AdminOrdersComponent implements OnInit {
  private adminService = inject(AdminService);
  orders: Order[] = [];
  totalOrders = 0;
  loading = true;
  searchTerm = '';

  get filteredOrders() {
    if (!this.searchTerm) return this.orders;
    const lowerTerm = this.searchTerm.toLowerCase();
    return this.orders.filter(o => o.buyerEmail.toLowerCase().includes(lowerTerm));
  }

  ngOnInit(): void {
    // For now, load first 50 orders
    this.adminService.getOrders(1, 50).subscribe({
      next: (response) => {
        this.orders = response.data;
        this.totalOrders = response.count;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }
}

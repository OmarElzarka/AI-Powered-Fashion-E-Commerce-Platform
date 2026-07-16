import { Component, inject, OnInit } from '@angular/core';
import { AdminService } from '../../admin.service';
import { Order } from '../../../../shared/models/order';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.scss'
})
export class AdminOrdersComponent implements OnInit {
  private adminService = inject(AdminService);
  orders: Order[] = [];
  totalOrders = 0;
  loading = true;

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

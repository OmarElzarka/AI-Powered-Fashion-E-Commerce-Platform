import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../../../core/services/shop.service';
import { AdminService } from '../../admin.service';
import { Product } from '../../../../shared/models/product';
import { ShopParams } from '../../../../shared/models/shopParams';
import { CurrencyPipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '../../../../core/services/snackbar.service';
import { BackendImagePipe } from '../../../../shared/pipes/backend-image-pipe';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CurrencyPipe, MatIcon, RouterLink, BackendImagePipe],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.scss'
})
export class AdminProductsComponent implements OnInit {
  private shopService = inject(ShopService);
  private adminService = inject(AdminService);
  private dialog = inject(MatDialog);
  private snackbar = inject(SnackbarService);

  products: Product[] = [];
  shopParams = new ShopParams();
  totalCount = 0;
  loading = true;

  ngOnInit(): void {
    this.shopParams.pageSize = 50; // Load more per page in admin view
    this.loadProducts();
  }

  loadProducts() {
    this.loading = true;
    this.shopService.getProducts(this.shopParams).subscribe({
      next: (response) => {
        this.products = response.data;
        this.totalCount = response.count;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  onSearch(event: any) {
    this.shopParams.search = event.target.value;
    this.shopParams.pageNumber = 1;
    this.loadProducts();
  }

  onDelete(id: number, name: string) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Product', message: `Are you sure you want to delete ${name}?` }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.adminService.deleteProduct(id).subscribe({
          next: () => {
            this.snackbar.success('Product deleted successfully');
            this.loadProducts();
          },
          error: () => {
            this.snackbar.error('Failed to delete product');
          }
        });
      }
    });
  }
}

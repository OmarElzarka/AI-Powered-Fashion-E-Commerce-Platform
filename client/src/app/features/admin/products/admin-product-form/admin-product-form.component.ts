import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminService } from '../../admin.service';
import { ShopService } from '../../../../core/services/shop.service';
import { SnackbarService } from '../../../../core/services/snackbar.service';
import { Product } from '../../../../shared/models/product';

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './admin-product-form.component.html',
  styleUrl: './admin-product-form.component.scss'
})
export class AdminProductFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);
  private shopService = inject(ShopService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackbar = inject(SnackbarService);

  productForm!: FormGroup;
  isEditMode = false;
  productId?: number;
  loading = false;

  ngOnInit(): void {
    this.initForm();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.productId = +id;
      this.loadProduct(this.productId);
    }
  }

  initForm() {
    this.productForm = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      discountPercentage: [0, [Validators.min(0), Validators.max(100)]],
      brand: ['', Validators.required],
      category: ['', Validators.required],
      subCategory: [''],
      articleType: ['', Validators.required],
      gender: ['', Validators.required],
      baseColor: [''],
      season: [''],
      usage: [''],
      material: [''],
      pattern: [''],
      imageUrl: ['', Validators.required],
      tags: [''],
      isFeatured: [false],
      isNewArrival: [false],
      quantityInStock: [0, [Validators.required, Validators.min(0)]]
    });
  }

  loadProduct(id: number) {
    this.loading = true;
    this.shopService.getProduct(id).subscribe({
      next: (product: Product) => {
        this.productForm.patchValue({
          ...product,
          tags: product.tags ? product.tags.join(', ') : ''
        });
        this.loading = false;
      },
      error: () => {
        this.snackbar.error('Could not load product');
        this.router.navigate(['/admin/products']);
      }
    });
  }

  onSubmit() {
    if (this.productForm.invalid) return;

    this.loading = true;
    const formValue = this.productForm.value;
    
    // Convert comma-separated tags back to array
    const productData = {
      ...formValue,
      tags: formValue.tags ? formValue.tags.split(',').map((t: string) => t.trim()).filter((t: string) => t) : []
    };

    if (this.isEditMode && this.productId) {
      this.adminService.updateProduct(this.productId, productData).subscribe({
        next: () => {
          this.snackbar.success('Product updated successfully');
          this.router.navigate(['/admin/products']);
        },
        error: () => {
          this.snackbar.error('Problem updating product');
          this.loading = false;
        }
      });
    } else {
      this.adminService.createProduct(productData).subscribe({
        next: () => {
          this.snackbar.success('Product created successfully');
          this.router.navigate(['/admin/products']);
        },
        error: () => {
          this.snackbar.error('Problem creating product');
          this.loading = false;
        }
      });
    }
  }
}

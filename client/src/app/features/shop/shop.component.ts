import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ProductItemComponent } from "./product-item/product-item.component";
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuTrigger } from '@angular/material/menu';
import { MatListOption, MatSelectionList } from '@angular/material/list';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { ShopParams } from '../../shared/models/shopParams';
import { Pagination } from '../../shared/models/pagination';
import { FormsModule } from '@angular/forms';
import { EmptyStateComponent } from "../../shared/components/empty-state/empty-state.component";
import { ActivatedRoute } from '@angular/router';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-shop',
  imports: [
    ProductItemComponent,
    MatButton,
    MatIcon,
    MatMenu,
    MatSelectionList,
    MatListOption,
    MatMenuTrigger,
    MatPaginator,
    FormsModule,
    EmptyStateComponent,
    MatExpansionModule,
    MatCheckboxModule
  ],
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss'
})
export class ShopComponent implements OnInit {
  shopService = inject(ShopService);
  private route = inject(ActivatedRoute);
  
  products?: Pagination<Product>;
  sortOptions = [
    { name: 'Newest', value: 'newest' },
    { name: 'Price: Low-High', value: 'priceAsc' },
    { name: 'Price: High-Low', value: 'priceDesc' },
    { name: 'Top Rated', value: 'rating' },
    { name: 'Most Popular', value: 'popularity' },
  ];
  shopParams = new ShopParams();
  pageSizeOptions = [12, 24, 36, 48];
  
  isSidebarOpen = true;

  // Static Category Hierarchy
  categoryTree = [
    {
      name: 'Accessories',
      types: ['Wallets', 'Watches', 'Belts', 'Sunglasses', 'Handbags']
    },
    {
      name: 'Apparel',
      types: ['Tshirts', 'Jackets', 'Jeans', 'Dresses', 'Shirts', 'Sweaters']
    },
    {
      name: 'Footwear',
      types: ['Casual Shoes', 'Sports Shoes', 'Heels', 'Flats', 'Sandals']
    }
  ];

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['search']) this.shopParams.search = params['search'];
      if (params['genders']) this.shopParams.genders = [params['genders']];
      if (params['categories']) this.shopParams.categories = [params['categories']];
      if (params['isNewArrival']) this.shopParams.isNewArrival = params['isNewArrival'] === 'true';
      this.initialiseShop();
    });
  }

  initialiseShop() {
    this.shopService.getBrands();
    this.shopService.getCategories();
    this.shopService.getColors();
    this.shopService.getGenders();
    this.shopService.getArticleTypes();
    this.getProducts();
  }

  resetFilters() {
    this.shopParams = new ShopParams();
    this.getProducts();
  }

  onSearchChange() {
    this.shopParams.pageNumber = 1;
    this.getProducts();
  }

  getProducts() {
    this.shopService.getProducts(this.shopParams).subscribe({
      next: response => this.products = response,
      error: error => console.error(error)
    });
  }

  onSortChange(event: any) {
    this.shopParams.pageNumber = 1;
    const selectedOption = event.options[0];
    if (selectedOption) {
      this.shopParams.sort = selectedOption.value;
      this.getProducts();
    }
  }

  handlePageEvent(event: PageEvent) {
    this.shopParams.pageNumber = event.pageIndex + 1;
    this.shopParams.pageSize = event.pageSize;
    this.getProducts();
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleType(type: string) {
    if (this.shopParams.types.includes(type)) {
      this.shopParams.types = this.shopParams.types.filter(t => t !== type);
    } else {
      this.shopParams.types.push(type);
    }
    this.shopParams.pageNumber = 1;
    this.getProducts();
  }

  toggleBrand(brand: string) {
    if (this.shopParams.brands.includes(brand)) {
      this.shopParams.brands = this.shopParams.brands.filter(b => b !== brand);
    } else {
      this.shopParams.brands.push(brand);
    }
    this.shopParams.pageNumber = 1;
    this.getProducts();
  }

  toggleColor(color: string) {
    if (this.shopParams.colors.includes(color)) {
      this.shopParams.colors = this.shopParams.colors.filter(c => c !== color);
    } else {
      this.shopParams.colors.push(color);
    }
    this.shopParams.pageNumber = 1;
    this.getProducts();
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.shopParams.brands.length) count++;
    if (this.shopParams.categories.length) count++;
    if (this.shopParams.types.length) count++;
    if (this.shopParams.genders.length) count++;
    if (this.shopParams.colors.length) count++;
    if (this.shopParams.search) count++;
    return count;
  }
}

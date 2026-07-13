import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ProductItemComponent } from "./product-item/product-item.component";
import { MatDialog } from '@angular/material/dialog';
import { FiltersDialogComponent } from './filters-dialog/filters-dialog.component';
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
    EmptyStateComponent
  ],
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss'
})
export class ShopComponent implements OnInit {
  private shopService = inject(ShopService);
  private dialogService = inject(MatDialog);
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

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['search']) this.shopParams.search = params['search'];
      if (params['genders']) this.shopParams.genders = [params['genders']];
      if (params['categories']) this.shopParams.categories = [params['categories']];
      if (params['seasons']) this.shopParams.seasons = [params['seasons']];
      if (params['isNewArrival']) this.shopParams.isNewArrival = params['isNewArrival'] === 'true';
      this.initialiseShop();
    });
  }

  initialiseShop() {
    this.shopService.getBrands();
    this.shopService.getCategories();
    this.shopService.getColors();
    this.shopService.getSeasons();
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

  openFiltersDialog() {
    const dialogRef = this.dialogService.open(FiltersDialogComponent, {
      minWidth: '500px',
      data: {
        selectedBrands: this.shopParams.brands,
        selectedCategories: this.shopParams.categories,
        selectedGenders: this.shopParams.genders,
        selectedColors: this.shopParams.colors,
        selectedSeasons: this.shopParams.seasons,
      }
    });
    dialogRef.afterClosed().subscribe({
      next: result => {
        if (result) {
          this.shopParams.pageNumber = 1;
          this.shopParams.brands = result.selectedBrands || [];
          this.shopParams.categories = result.selectedCategories || [];
          this.shopParams.genders = result.selectedGenders || [];
          this.shopParams.colors = result.selectedColors || [];
          this.shopParams.seasons = result.selectedSeasons || [];
          this.getProducts();
        }
      },
    });
  }

  handlePageEvent(event: PageEvent) {
    this.shopParams.pageNumber = event.pageIndex + 1;
    this.shopParams.pageSize = event.pageSize;
    this.getProducts();
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.shopParams.brands.length) count++;
    if (this.shopParams.categories.length) count++;
    if (this.shopParams.genders.length) count++;
    if (this.shopParams.colors.length) count++;
    if (this.shopParams.seasons.length) count++;
    if (this.shopParams.search) count++;
    return count;
  }
}

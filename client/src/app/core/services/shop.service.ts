import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Pagination } from '../../shared/models/pagination';
import { Product } from '../../shared/models/product';
import { ShopParams } from '../../shared/models/shopParams';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ShopService {
  baseUrl = environment.baseUrl;
  private http = inject(HttpClient);
  brands: string[] = [];
  categories: string[] = [];
  colors: string[] = [];
  seasons: string[] = [];
  genders: string[] = [];
  articleTypes: string[] = [];

  getProducts(shopParams: ShopParams) {
    let params = new HttpParams();

    if (shopParams.brands.length > 0) {
      params = params.append('brands', shopParams.brands.join(','));
    }
    if (shopParams.categories.length > 0) {
      params = params.append('categories', shopParams.categories.join(','));
    }
    if (shopParams.genders.length > 0) {
      params = params.append('genders', shopParams.genders.join(','));
    }
    if (shopParams.colors.length > 0) {
      params = params.append('colors', shopParams.colors.join(','));
    }
    if (shopParams.seasons.length > 0) {
      params = params.append('seasons', shopParams.seasons.join(','));
    }
    if (shopParams.usages.length > 0) {
      params = params.append('usages', shopParams.usages.join(','));
    }
    if (shopParams.articleTypes.length > 0) {
      params = params.append('articleTypes', shopParams.articleTypes.join(','));
    }
    if (shopParams.patterns.length > 0) {
      params = params.append('patterns', shopParams.patterns.join(','));
    }
    if (shopParams.priceMin) {
      params = params.append('priceMin', shopParams.priceMin);
    }
    if (shopParams.priceMax) {
      params = params.append('priceMax', shopParams.priceMax);
    }
    if (shopParams.minRating) {
      params = params.append('minRating', shopParams.minRating);
    }
    if (shopParams.sort) {
      params = params.append('sort', shopParams.sort);
    }
    if (shopParams.search) {
      params = params.append('search', shopParams.search);
    }

    params = params.append('pageSize', shopParams.pageSize);
    params = params.append('pageIndex', shopParams.pageNumber);

    return this.http.get<Pagination<Product>>(this.baseUrl + 'products', { params });
  }

  getProduct(id: number) {
    return this.http.get<Product>(this.baseUrl + 'products/' + id);
  }

  getBrands() {
    if (this.brands.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/brands').subscribe({
      next: response => this.brands = response,
    });
  }

  getCategories() {
    if (this.categories.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/categories').subscribe({
      next: response => this.categories = response,
    });
  }

  getColors() {
    if (this.colors.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/colors').subscribe({
      next: response => this.colors = response,
    });
  }

  getSeasons() {
    if (this.seasons.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/seasons').subscribe({
      next: response => this.seasons = response,
    });
  }

  getGenders() {
    if (this.genders.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/genders').subscribe({
      next: response => this.genders = response,
    });
  }

  getArticleTypes() {
    if (this.articleTypes.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/article-types').subscribe({
      next: response => this.articleTypes = response,
    });
  }

  getFeatured(count: number = 8) {
    return this.http.get<Product[]>(this.baseUrl + 'products/featured?count=' + count);
  }

  getNewArrivals(count: number = 8) {
    return this.http.get<Product[]>(this.baseUrl + 'products/new-arrivals?count=' + count);
  }

  getTrending(count: number = 8) {
    return this.http.get<Product[]>(this.baseUrl + 'products/trending?count=' + count);
  }

  getSimilar(id: number, count: number = 6) {
    return this.http.get<Product[]>(this.baseUrl + 'products/' + id + '/similar?count=' + count);
  }
}

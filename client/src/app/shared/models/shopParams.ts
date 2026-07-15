export class ShopParams {
  brands: string[] = [];
  categories: string[] = [];
  subCategories: string[] = [];
  genders: string[] = [];
  colors: string[] = [];
  types: string[] = [];
  usages: string[] = [];
  articleTypes: string[] = [];
  patterns: string[] = [];
  priceMin?: number;
  priceMax?: number;
  minRating?: number;
  minDiscount?: number;
  isFeatured?: boolean;
  isNewArrival?: boolean;
  sort = 'newest';
  pageNumber = 1;
  pageSize = 12;
  search = '';
}
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShopService } from '../../core/services/shop.service';
import { CartService } from '../../core/services/cart.service';
import { ChatService } from '../../core/services/chat.service';
import { Product } from '../../shared/models/product';
import { CurrencyPipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';
import { BackendImagePipe } from '../../shared/pipes/backend-image-pipe';
import { ProductItemComponent } from '../shop/product-item/product-item.component';

@Component({
  selector: 'app-home',
  imports: [RouterLink, CurrencyPipe, MatIcon, MatButton, BackendImagePipe, ProductItemComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private shopService = inject(ShopService);
  private cartService = inject(CartService);
  private chatService = inject(ChatService);
  featuredProducts: Product[] = [];
  newArrivals: Product[] = [];
  trendingProducts: Product[] = [];

  categories = [
    { name: 'Apparel', icon: 'checkroom', query: { categories: 'Apparel' } },
    { name: 'Footwear', icon: 'hiking', query: { categories: 'Footwear' } },
    { name: 'Accessories', icon: 'watch', query: { categories: 'Accessories' } },
    { name: 'Personal Care', icon: 'spa', query: { categories: 'Personal Care' } },
  ];

  seasons = [
    { name: 'Spring', color: '#e8d5b7', textColor: '#0a0a0a' },
    { name: 'Summer', color: '#fef3c7', textColor: '#0a0a0a' },
    { name: 'Fall', color: '#c8a97e', textColor: '#fff' },
    { name: 'Winter', color: '#1a1a1a', textColor: '#fff' },
  ];

  ngOnInit() {
    this.loadFeatured();
    this.loadNewArrivals();
    this.loadTrending();
  }

  loadFeatured() {
    this.shopService.getFeatured(8).subscribe({
      next: products => this.featuredProducts = products
    });
  }

  loadNewArrivals() {
    this.shopService.getNewArrivals(8).subscribe({
      next: products => this.newArrivals = products
    });
  }

  loadTrending() {
    this.shopService.getTrending(8).subscribe({
      next: products => this.trendingProducts = products
    });
  }

  addToCart(product: Product) {
    this.cartService.addItemToCart(product, 1);
  }

  openAgent(event: Event) {
    event.preventDefault();
    this.chatService.openChatSubject.next(true);
  }
}

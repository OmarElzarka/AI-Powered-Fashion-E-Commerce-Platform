import { Component, inject } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { HeaderComponent } from "./layout/header/header.component";
import { FooterComponent } from "./layout/footer/footer.component";
import { ChatWidgetComponent } from './shared/components/chat-widget/chat-widget.component';

@Component({
  selector: 'app-root',
  imports: [HeaderComponent, FooterComponent, RouterOutlet, ChatWidgetComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent  {
  title = 'STYLÉ — AI-Powered Fashion';
  private router = inject(Router);

  get isShopPage(): boolean {
    // Exact match or sub-routes
    return this.router.url.split('?')[0] === '/shop' || this.router.url.startsWith('/shop/');
  }
}

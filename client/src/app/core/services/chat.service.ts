import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartService } from './cart.service';
import { Product } from '../../shared/models/product';

export interface ActionConfirmation {
  action: string;
  toolCallId: string;
  parameters: any;
}

export interface ChatMessage {
  text: string;
  isUser: boolean;
  products?: Product[];
  confirmation?: ActionConfirmation;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  private baseUrl = environment.baseUrl;
  
  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  public messages$ = this.messagesSubject.asObservable();
  
  private cartService = inject(CartService);

  public isTypingSubject = new BehaviorSubject<boolean>(false);
  public isTyping$ = this.isTypingSubject.asObservable();

  public openChatSubject = new BehaviorSubject<boolean>(false);
  public openChat$ = this.openChatSubject.asObservable();

  sendMessage(message: string) {
    if (!message.trim()) return;

    const currentMessages = this.messagesSubject.value;
    this.messagesSubject.next([...currentMessages, { text: message, isUser: true }]);
    this.isTypingSubject.next(true);

    const history = this.messagesSubject.value.map(m => ({
      role: m.isUser ? 'user' : 'assistant',
      text: m.text
    }));

    const cartId = this.cartService.cart()?.id || localStorage.getItem('cart_id') || '';

    this.http.post<{text: string, products: Product[], confirmation: ActionConfirmation}>(
      this.baseUrl + 'agent', 
      { history, cartId }
    ).subscribe({
      next: (res) => {
        const updatedMessages = this.messagesSubject.value;
        this.messagesSubject.next([...updatedMessages, { 
          text: res.text, 
          isUser: false,
          products: res.products?.length ? res.products : undefined,
          confirmation: res.confirmation || undefined
        }]);
        this.isTypingSubject.next(false);
      },
      error: (err) => {
        console.error('Chat API Error', err);
        const updatedMessages = this.messagesSubject.value;
        this.messagesSubject.next([...updatedMessages, { text: "Sorry, I'm having trouble connecting right now.", isUser: false }]);
        this.isTypingSubject.next(false);
      }
    });
  }

  confirmAction(confirmation: ActionConfirmation) {
    this.isTypingSubject.next(true);
    const cartId = this.cartService.cart()?.id || localStorage.getItem('cart_id') || '';
    
    this.http.post<{message: string, cartId?: string}>(
      `${this.baseUrl}agent/confirm?cartId=${cartId}`, 
      confirmation
    ).subscribe({
      next: (res) => {
        if (res.cartId) {
          localStorage.setItem('cart_id', res.cartId);
          this.cartService.getCart(res.cartId).subscribe();
        }

        // Clear the confirmation from the last message so it doesn't show buttons anymore
        const currentMessages = this.messagesSubject.value;
        if (currentMessages.length > 0) {
           const lastMsg = currentMessages[currentMessages.length - 1];
           lastMsg.confirmation = undefined;
        }
        
        // Add the confirmation response to the chat
        this.messagesSubject.next([...currentMessages, { text: res.message, isUser: false }]);
        this.isTypingSubject.next(false);
      },
      error: (err) => {
        console.error('Confirmation Error', err);
        this.isTypingSubject.next(false);
      }
    });
  }

  cancelAction() {
    // Clear the confirmation
    const currentMessages = this.messagesSubject.value;
    if (currentMessages.length > 0) {
        const lastMsg = currentMessages[currentMessages.length - 1];
        lastMsg.confirmation = undefined;
        this.messagesSubject.next([...currentMessages]);
    }
    this.sendMessage("I cancelled the action. What's next?");
  }
}

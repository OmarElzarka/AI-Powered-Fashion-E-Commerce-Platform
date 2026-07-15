import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartService } from './cart.service';

export interface ChatMessage {
  text: string;
  isUser: boolean;
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

  sendMessage(message: string, mode: 'chat' | 'agent' = 'chat') {
    if (!message.trim()) return;

    // Add user message to state
    const currentMessages = this.messagesSubject.value;
    this.messagesSubject.next([...currentMessages, { text: message, isUser: true }]);
    
    // Set typing indicator
    this.isTypingSubject.next(true);

    // Call API
    const endpoint = mode === 'agent' ? 'agent' : 'chat';
    const payload: any = { message };
    
    if (mode === 'agent') {
      payload.cartId = this.cartService.cart()?.id || localStorage.getItem('cart_id');
    }

    this.http.post<{response: string}>(this.baseUrl + endpoint, payload).subscribe({
      next: (res) => {
        const updatedMessages = this.messagesSubject.value;
        this.messagesSubject.next([...updatedMessages, { text: res.response, isUser: false }]);
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
}

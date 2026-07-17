import { Component, ElementRef, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../../core/services/chat.service';
import { BackendImagePipe } from '../../pipes/backend-image-pipe';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, BackendImagePipe, MatIcon],
  templateUrl: './chat-widget.component.html',
  styleUrls: ['./chat-widget.component.scss']
})
export class ChatWidgetComponent implements OnInit {
  isOpen = false;
  userInput = '';
  chatService = inject(ChatService);
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;
  private previousMessageCount = 0;

  ngOnInit() {
    this.chatService.openChat$.subscribe(open => {
      if (open) {
        this.isOpen = true;
        setTimeout(() => {
          this.previousMessageCount = this.getMessagesCount();
          this.scrollToBottom();
        }, 100);
      }
    });

    this.chatService.messages$.subscribe(messages => {
      if (!this.isOpen) return;
      if (messages.length === 0) return;
      
      if (messages.length > this.previousMessageCount) {
        const lastMsg = messages[messages.length - 1];
        
        setTimeout(() => {
           try {
             const elements = this.scrollContainer.nativeElement.querySelectorAll('.message-element');
             const lastEl = elements[elements.length - 1] as HTMLElement;
             if (lastEl) {
               if (lastMsg.isUser) {
                 this.scrollToBottom();
               } else {
                 this.scrollToElementTop(lastEl);
               }
             }
           } catch(e) {}
        }, 50);
      }
      this.previousMessageCount = messages.length;
    });

    this.chatService.isTyping$.subscribe(isTyping => {
      if (isTyping && this.isOpen) {
        setTimeout(() => this.scrollToBottom(), 50);
      }
    });
  }

  private getMessagesCount(): number {
    try {
      return this.scrollContainer.nativeElement.querySelectorAll('.message-element').length;
    } catch {
      return 0;
    }
  }

  scrollToElementTop(element: HTMLElement) {
    try {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch(err) { }
  }

  welcomePrompts = [
    "Create a black winter outfit for a man.",
    "Recommend a casual summer outfit under $100.",
    "Show me the best formal shoes.",
    "Find a matching watch for this outfit.",
    "Recommend clothes based on my previous purchases.",
    "Help me choose an outfit for a wedding."
  ];

  toggleChat() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      setTimeout(() => {
        this.previousMessageCount = this.getMessagesCount();
        this.scrollToBottom();
      }, 100);
    } else {
      this.previousMessageCount = 0;
    }
  }

  sendWelcomePrompt(prompt: string) {
    this.userInput = prompt;
    this.sendMessage();
  }

  sendMessage() {
    if (this.userInput.trim()) {
      this.chatService.sendMessage(this.userInput);
      this.userInput = '';
      // Scroll handling is now managed by handleScroll when elements are added
    }
  }

  scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTo({
        top: this.scrollContainer.nativeElement.scrollHeight,
        behavior: 'smooth'
      });
    } catch(err) { }
  }

  confirmAction(confirmation: any) {
    this.chatService.confirmAction(confirmation);
  }

  cancelAction() {
    this.chatService.cancelAction();
  }

  addRecommendedToCart(productId: number) {
    this.chatService.confirmAction({
      action: 'AddToCart',
      toolCallId: 'ui-direct',
      parameters: { productId, quantity: 1 }
    });
  }

  onImageError(event: any) {
    event.target.src = 'assets/images/placeholder.png';
  }
}

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

  ngOnInit() {
    this.chatService.openChat$.subscribe(open => {
      if (open) {
        this.isOpen = true;
        setTimeout(() => this.scrollToBottom(), 100);
      }
    });
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
      setTimeout(() => this.scrollToBottom(), 100);
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
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
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

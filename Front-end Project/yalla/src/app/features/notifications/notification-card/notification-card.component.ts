import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Notification } from '../../../core/models/feed.models';

interface NotificationIcon {
  name: string;
  colorClass: string;
}

@Component({
  selector: 'app-notification-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-card.component.html',
  styleUrl: './notification-card.component.scss',
})
export class NotificationCardComponent {
  @Input({ required: true }) notification!: Notification;
  @Output() select = new EventEmitter<void>();

  // GUESSED against the mockup's four examples (like / comment / group-add / mention).
  // `type` is just `string` on the model - confirm the real values your backend sends
  // and adjust these cases (and the default) to match.
  get icon(): NotificationIcon {
    switch (this.notification.type) {
      case 'PostLike':
        return { name: 'favorite', colorClass: 'text-error' };
      case 'PostComment':
        return { name: 'chat_bubble', colorClass: 'text-primary' };
      case 'GroupInvite':
      case 'GroupAdd':
        return { name: 'group_add', colorClass: 'text-gold-metallic' };
      case 'Mention':
        return { name: 'alternate_email', colorClass: 'text-secondary' };
      default:
        return { name: 'notifications', colorClass: 'text-on-surface-variant' };
    }
  }

  onClick(): void {
    this.select.emit();
  }
}

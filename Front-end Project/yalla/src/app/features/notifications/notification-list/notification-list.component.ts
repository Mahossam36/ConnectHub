import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Notification } from '../../../core/models/feed.models';
import { NotificationCardComponent } from '../notification-card/notification-card.component';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule,NotificationCardComponent],
  templateUrl: './notification-list.component.html',
  styleUrl: './notification-list.component.scss',
})
export class NotificationListComponent {
  @Input() notifications: Notification[] = [];
  @Output() selectNotification = new EventEmitter<Notification>();
}

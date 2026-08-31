import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Notification } from '../../../core/models/feed.models';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { NotificationsHeaderComponent } from '../notifications-header/notifications-header.component';
import { NotificationListComponent } from '../notification-list/notification-list.component';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, NotificationsHeaderComponent, NotificationListComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent implements OnInit {
  notifications = signal<Notification[]>([]);
  unreadCount = signal<number>(0);

  constructor(private readonly feedApi: FeedApiService) {}

  ngOnInit(): void {
    this.feedApi.getNotifications().subscribe((feed) => {
      this.notifications.set(feed.items);
      this.unreadCount.set(feed.unreadCount);
    });
  }

  onSelectNotification(notification: Notification): void {
    if (!notification.isRead) {
      this.feedApi.markNotificationAsRead(notification.id).subscribe(() => {
        this.notifications.update((items) =>
          items.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n))
        );
        this.unreadCount.update((count) => Math.max(0, count - 1));
      });
    }
    // TODO: navigate to notification.targetUrl (e.g. this.router.navigateByUrl(...)) if present.
  }
}

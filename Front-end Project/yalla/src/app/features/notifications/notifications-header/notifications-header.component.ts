import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-notifications-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications-header.component.html',
  styleUrl: './notifications-header.component.scss',
})
export class NotificationsHeaderComponent {
  @Input() title = 'Notifications';
  @Input() subtitle = '';
}

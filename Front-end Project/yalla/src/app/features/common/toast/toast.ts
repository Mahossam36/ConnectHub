import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastComponent {
  @Input() type: ToastType = 'info';
  @Input() title = '';
  @Input() message = '';
  @Input() visible = false;
  @Input() dismissible = true;

  get icon(): string {
    switch (this.type) {
      case 'success':
        return '✓';

      case 'error':
        return '×';

      case 'warning':
        return '!';

      case 'info':
      default:
        return 'i';
    }
  }

  close(): void {
    this.visible = false;
  }
}

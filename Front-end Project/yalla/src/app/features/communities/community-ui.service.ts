import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CommunityUiService {
  readonly createDialogOpen = signal(false);

  openCreateDialog(): void { this.createDialogOpen.set(true); }
  closeCreateDialog(): void { this.createDialogOpen.set(false); }
}

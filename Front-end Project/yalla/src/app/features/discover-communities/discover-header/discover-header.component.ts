import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
// TODO: replace the raw <input> in the template with your shared search component
// (src/app/features/common/search) if its API fits - selector/inputs unknown from here.
// import { Search } from '../../../../common/search/search';

@Component({
  selector: 'app-discover-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './discover-header.component.html',
  styleUrl: './discover-header.component.scss'
})
export class DiscoverHeaderComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() searchPlaceholder = 'Search communities...';
  @Output() searchChange = new EventEmitter<string>();

  onSearchInput(value: string): void {
    this.searchChange.emit(value);
  }
}

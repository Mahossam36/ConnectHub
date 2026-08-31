import { Component, EventEmitter, Output } from '@angular/core';

import { RouterLink, RouterLinkActive } from '@angular/router';

import { SearchComponent, SearchResult } from '../../search/search';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SearchComponent],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.scss'],
})
export class NavbarComponent {
  @Output()
  menuClicked = new EventEmitter<void>();

  @Output()
  notificationClicked = new EventEmitter<void>();

  @Output()
  profileClicked = new EventEmitter<void>();

  @Output()
  logoutClicked = new EventEmitter<void>();

  @Output()
  searchRequested = new EventEmitter<string>();

  @Output()
  searchResultSelected = new EventEmitter<SearchResult>();

  @Output()
  searchCleared = new EventEmitter<void>();

  onSearch(query: string): void {
    this.searchRequested.emit(query);
  }

  onSearchResultSelected(result: SearchResult): void {
    this.searchResultSelected.emit(result);
  }

  onSearchCleared(): void {
    this.searchCleared.emit();
  }
}

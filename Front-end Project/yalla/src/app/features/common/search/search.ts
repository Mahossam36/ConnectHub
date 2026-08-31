import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

export type SearchVariant = 'navbar' | 'page';

export type SearchResultType = 'category' | 'tag' | 'community';

export interface SearchResult {
  id: string;
  type: SearchResultType;
  name: string;
  description?: string;
  imageUrl?: string;
  memberCount?: number;
  slug?: string;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './search.html',
  styleUrls: ['./search.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchComponent {
  @Input()
  variant: SearchVariant = 'navbar';

  @Input()
  placeholder = 'Search Yalla...';

  @Input()
  results: SearchResult[] = [];

  @Input()
  loading = false;

  @Input()
  disabled = false;

  @Output()
  search = new EventEmitter<string>();

  @Output()
  resultSelected = new EventEmitter<SearchResult>();

  @Output()
  cleared = new EventEmitter<void>();

  query = '';

  isFocused = false;

  get showDropdown(): boolean {
    return (
      this.isFocused && (this.query.trim().length > 0 || this.loading || this.results.length > 0)
    );
  }

  get categories(): SearchResult[] {
    return this.results.filter((result) => result.type === 'category');
  }

  get tags(): SearchResult[] {
    return this.results.filter((result) => result.type === 'tag');
  }

  get communities(): SearchResult[] {
    return this.results.filter((result) => result.type === 'community');
  }

  get hasResults(): boolean {
    return this.results.length > 0;
  }

  onFocus(): void {
    this.isFocused = true;
  }

  onBlur(): void {
    /*
     * Small delay allows clicks on dropdown results
     * to finish before the dropdown disappears.
     */
    setTimeout(() => {
      this.isFocused = false;
    }, 150);
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.query = input.value;

    const value = this.query.trim();

    if (!value) {
      this.cleared.emit();
      return;
    }

    this.search.emit(value);
  }

  clearSearch(): void {
    this.query = '';

    this.cleared.emit();

    setTimeout(() => {
      const input = document.querySelector('.search-input') as HTMLInputElement | null;

      input?.focus();
    });
  }

  selectResult(result: SearchResult): void {
    this.resultSelected.emit(result);

    this.isFocused = false;
  }

  trackByResult(_index: number, result: SearchResult): string {
    return result.id;
  }
}

import { Component, EventEmitter, OnDestroy, OnInit, Output, inject } from '@angular/core';

import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, takeUntil } from 'rxjs';

import { SearchComponent, SearchResult } from '../../search/search';
import { FeedApiService } from '../../../../core/services/feed-api.service';
import { CategoryItem, TagItem } from '../../../../core/models/feed.models';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SearchComponent],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.scss'],
})
export class NavbarComponent implements OnInit, OnDestroy {
  private readonly feedApi = inject(FeedApiService);
  private readonly router = inject(Router);

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

  // =====================================================
  // SEARCH STATE
  // =====================================================

  results: SearchResult[] = [];
  loading = false;

  private categories: CategoryItem[] = [];
  private tags: TagItem[] = [];

  private readonly search$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    // Categories/tags rarely change — fetch once and filter client-side
    // on every keystroke instead of hitting the API each time.
    this.feedApi.getCategories().subscribe({
      next: (items) => (this.categories = items),
    });

    this.feedApi.getTags().subscribe({
      next: (items) => (this.tags = items),
    });

    this.search$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((query) => {
          const trimmed = query.trim();

          if (!trimmed) {
            this.loading = false;
            return of<SearchResult[]>([]);
          }

          this.loading = true;

          const matchedCategories: SearchResult[] = this.filterLocal(this.categories, trimmed).map(
            (category) => ({
              id: category.id,
              type: 'category',
              name: category.name,
              description: category.description ?? undefined,
            }),
          );

          const matchedTags: SearchResult[] = this.filterLocal(this.tags, trimmed).map((tag) => ({
            id: tag.id,
            type: 'tag',
            name: tag.name,
          }));

          return this.feedApi.searchCommunities(trimmed).pipe(
            map((communities): SearchResult[] => {
              const matchedCommunities: SearchResult[] = communities.map((community) => ({
                id: community.id,
                type: 'community',
                name: community.name,
                description: community.description ?? undefined,
                imageUrl: community.coverImageUrl ?? undefined,
                memberCount: community.memberCount,
              }));

              return [...matchedCategories, ...matchedTags, ...matchedCommunities];
            }),
            catchError(() => of<SearchResult[]>([...matchedCategories, ...matchedTags])),
          );
        }),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (combined) => {
          this.results = combined;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        },
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private filterLocal<T extends { name: string }>(items: T[], query: string): T[] {
    const normalized = query.toLowerCase();
    return items.filter((item) => item.name.toLowerCase().includes(normalized)).slice(0, 5);
  }

  onSearch(query: string): void {
    this.searchRequested.emit(query);
    this.search$.next(query);
  }

  onSearchResultSelected(result: SearchResult): void {
    this.searchResultSelected.emit(result);

    if (result.type === 'community') {
      this.router.navigate(['/community', result.id]);
    } else if (result.type === 'category' || result.type === 'tag') {
      this.router.navigate(['/discover'], { queryParams: { search: result.name } });
    }
  }

  onSearchCleared(): void {
    this.searchCleared.emit();
    this.results = [];
    this.loading = false;
  }
}

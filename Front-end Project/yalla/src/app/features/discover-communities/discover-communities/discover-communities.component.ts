import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Community, CategoryItem, JoinState } from '../../../core/models/feed.models';
import { DiscoverHeaderComponent } from '../discover-header/discover-header.component';
import { CategoryFilterComponent } from '../category-filter/category-filter.component';
import { FeaturedCommunityCardComponent } from '../featured-community-card/featured-community-card.component';
import { CommunityGridComponent } from '../community-grid/community-grid.component';
import { CreateCommunityCtaComponent } from '../create-community-cta/create-community-cta.component';
import { FeedApiService } from '../../../core/services/feed-api.service';

@Component({
  selector: 'app-discover-communities',
  standalone: true,
  imports: [
    CommonModule,
    DiscoverHeaderComponent,
    CategoryFilterComponent,
    FeaturedCommunityCardComponent,
    CommunityGridComponent,
    CreateCommunityCtaComponent,
  ],
  templateUrl: './discover-communities.component.html',
  styleUrl: './discover-communities.component.scss',
})
export class DiscoverCommunitiesComponent implements OnInit {
  categories = signal<CategoryItem[]>([]);
  selectedCategoryId = signal<string>('all');
  featuredCommunity = signal<Community | null>(null);
  communities = signal<Community[]>([]);
  searchTerm = signal<string>('');
  joinStates = signal<Record<string, JoinState>>({});

  constructor(private readonly feedApi: FeedApiService) {}

  ngOnInit(): void {
    this.feedApi.getCategories().subscribe((categories) => this.categories.set(categories));
    this.loadCommunities();
  }

  onSearchChange(term: string): void {
    this.searchTerm.set(term);
    this.loadCommunities();
  }

  onCategorySelected(categoryId: string): void {
    this.selectedCategoryId.set(categoryId);
    this.loadCommunities();
  }

  onJoinCommunity(communityId: string): void {
    this.joinStates.update((s) => ({ ...s, [communityId]: 'joining' }));
    this.feedApi.joinCommunity(communityId).subscribe({
      next: () => this.joinStates.update((s) => ({ ...s, [communityId]: 'joined' })),
      error: () => this.joinStates.update((s) => ({ ...s, [communityId]: 'none' })),
    });
  }

  onLoadMore(): void {
    this.feedApi
      .getCommunities(this.searchTerm(), this.communities().length + 12)
      .subscribe((communities) => {
        this.communities.set(this.filterByCategory(communities));
      });
  }

  onCreateCommunity(): void {
    // TODO: navigate to the create-community flow.
  }

  private loadCommunities(): void {
    this.feedApi.getCommunities(this.searchTerm()).subscribe((communities) => {
      this.communities.set(this.filterByCategory(communities));
    });
  }

  private filterByCategory(communities: Community[]): Community[] {
    const categoryId = this.selectedCategoryId();
    return categoryId === 'all'
      ? communities
      : communities.filter((c) => c.category?.id === categoryId);
  }
}

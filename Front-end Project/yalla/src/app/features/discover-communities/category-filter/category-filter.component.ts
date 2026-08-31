import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CategoryItem } from '../../../core/models/feed.models';

@Component({
  selector: 'app-category-filter',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-filter.component.html',
  styleUrl: './category-filter.component.scss',
})
export class CategoryFilterComponent {
  @Input() heading = 'Explore by category';
  @Input() categories: CategoryItem[] = [];
  @Input() selectedCategoryId = 'all';
  @Output() categorySelected = new EventEmitter<string>();

  onSelect(categoryId: string): void {
    this.categorySelected.emit(categoryId);
  }
}

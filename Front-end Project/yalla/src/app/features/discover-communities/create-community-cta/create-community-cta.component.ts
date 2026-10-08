
import {
  Component,
  EventEmitter,
  Input,
  Output,
  HostListener,
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface CommunityOption {
  id: string;
  name: string;
  isNew?: boolean;
}

@Component({
  selector: 'app-create-community-cta',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
  ],
  templateUrl: './create-community-cta.component.html',
  styleUrl: './create-community-cta.component.scss',
})
export class CreateCommunityCtaComponent {

  @Input()
  title = "Can't find your community?";

  @Input()
  description =
    'Create a community and bring people together around your interests.';

  /*
   * These should come from the API.
   */
  @Input()
  categories: CommunityOption[] = [];

  @Input()
  tags: CommunityOption[] = [];

  @Output()
  create = new EventEmitter<void>();

  @Output()
  categoryCreated = new EventEmitter<string>();

  @Output()
  tagCreated = new EventEmitter<string>();


  // =====================================================
  // SEARCH STATE
  // =====================================================

  categorySearch = '';
  tagSearch = '';

  categoryDropdownOpen = false;
  tagDropdownOpen = false;


  // =====================================================
  // CREATE STATE
  // =====================================================

  creatingCategory = false;
  creatingTag = false;

  newCategoryName = '';
  newTagName = '';


  // =====================================================
  // SELECTION
  // =====================================================

  selectedCategory: CommunityOption | null = null;

  selectedTags: CommunityOption[] = [];


  // =====================================================
  // FILTERED CATEGORIES
  // =====================================================

  get filteredCategories(): CommunityOption[] {

    const search =
      this.categorySearch.trim().toLowerCase();

    if (!search) {
      return this.categories;
    }

    return this.categories.filter(category =>
      category.name
        .toLowerCase()
        .includes(search)
    );
  }


  // =====================================================
  // FILTERED TAGS
  // =====================================================

  get filteredTags(): CommunityOption[] {

    const search =
      this.tagSearch.trim().toLowerCase();

    if (!search) {
      return this.tags;
    }

    return this.tags.filter(tag =>
      tag.name
        .toLowerCase()
        .includes(search)
    );
  }


  // =====================================================
  // CATEGORY SEARCH
  // =====================================================

  onCategorySearch(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.categorySearch = input.value;

    this.categoryDropdownOpen = true;
  }


  // =====================================================
  // TAG SEARCH
  // =====================================================

  onTagSearch(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.tagSearch = input.value;

    this.tagDropdownOpen = true;
  }


  // =====================================================
  // CATEGORY EXISTS
  // =====================================================

  categoryExists(value: string): boolean {

    const normalized =
      value.trim().toLowerCase();

    return this.categories.some(
      category =>
        category.name.toLowerCase() === normalized
    );
  }


  // =====================================================
  // TAG EXISTS
  // =====================================================

  tagExists(value: string): boolean {

    const normalized =
      value.trim().toLowerCase();

    return this.tags.some(
      tag =>
        tag.name.toLowerCase() === normalized
    );
  }


  // =====================================================
  // START CREATE CATEGORY
  // =====================================================

  startCreateCategory(event?: Event): void {

    event?.stopPropagation();

    this.categoryDropdownOpen = false;

    this.tagDropdownOpen = false;

    /*
     * If the user searched something first,
     * use that value inside the create field.
     */
    this.newCategoryName =
      this.categorySearch.trim();

    this.categorySearch = '';

    this.creatingCategory = true;
  }


  // =====================================================
  // CANCEL CREATE CATEGORY
  // =====================================================

  cancelCreateCategory(): void {

    this.creatingCategory = false;

    this.newCategoryName = '';

  }


  // =====================================================
  // CREATE CATEGORY
  // =====================================================

  createCategory(): void {

    const name =
      this.newCategoryName.trim();

    if (!name) {
      return;
    }

    /*
     * Prevent duplicates.
     */
    if (this.categoryExists(name)) {

      const existing =
        this.categories.find(
          category =>
            category.name.toLowerCase() ===
            name.toLowerCase()
        );

      if (existing) {
        this.selectedCategory = existing;
      }

      this.cancelCreateCategory();

      return;
    }


    /*
     * Temporary client-side option.
     *
     * The parent should save the actual
     * category to the database.
     */
    const category: CommunityOption = {
      id: `new-category-${Date.now()}`,
      name,
      isNew: true,
    };


    /*
     * Add immediately to local list.
     */
    this.categories = [
      ...this.categories,
      category,
    ];


    /*
     * Automatically select it.
     */
    this.selectedCategory = category;


    /*
     * Reset create mode.
     */
    this.creatingCategory = false;

    this.newCategoryName = '';


    /*
     * Tell parent to POST it to the API.
     */
    this.categoryCreated.emit(name);
  }


  // =====================================================
  // SELECT CATEGORY
  // =====================================================

  selectCategory(
    category: CommunityOption
  ): void {

    this.selectedCategory = category;

    this.categorySearch = '';

    this.categoryDropdownOpen = false;
  }


  // =====================================================
  // REMOVE CATEGORY
  // =====================================================

  removeCategory(): void {

    this.selectedCategory = null;
  }


  // =====================================================
  // START CREATE TAG
  // =====================================================

  startCreateTag(event?: Event): void {

    event?.stopPropagation();

    this.tagDropdownOpen = false;

    this.categoryDropdownOpen = false;

    /*
     * Preserve searched text.
     */
    this.newTagName =
      this.tagSearch.trim();

    this.tagSearch = '';

    this.creatingTag = true;
  }


  // =====================================================
  // CANCEL CREATE TAG
  // =====================================================

  cancelCreateTag(): void {

    this.creatingTag = false;

    this.newTagName = '';
  }


  // =====================================================
  // TOGGLE TAG
  // =====================================================

  toggleTag(tag: CommunityOption): void {

    const exists =
      this.isTagSelected(tag);

    if (exists) {

      this.removeTag(tag);

      return;
    }

    this.selectedTags = [
      ...this.selectedTags,
      tag,
    ];
  }


  // =====================================================
  // IS TAG SELECTED
  // =====================================================

  isTagSelected(
    tag: CommunityOption
  ): boolean {

    return this.selectedTags.some(
      selected =>
        selected.id === tag.id
    );
  }


  // =====================================================
  // CREATE TAG
  // =====================================================

  createTag(): void {

    const name =
      this.newTagName.trim();

    if (!name) {
      return;
    }


    /*
     * Prevent duplicates.
     */
    if (this.tagExists(name)) {

      const existing =
        this.tags.find(
          tag =>
            tag.name.toLowerCase() ===
            name.toLowerCase()
        );

      if (
        existing &&
        !this.isTagSelected(existing)
      ) {

        this.selectedTags = [
          ...this.selectedTags,
          existing,
        ];
      }

      this.cancelCreateTag();

      return;
    }


    /*
     * Temporary client-side option.
     */
    const tag: CommunityOption = {
      id: `new-tag-${Date.now()}`,
      name,
      isNew: true,
    };


    /*
     * Add to local list.
     */
    this.tags = [
      ...this.tags,
      tag,
    ];


    /*
     * Automatically select new tag.
     */
    this.selectedTags = [
      ...this.selectedTags,
      tag,
    ];


    /*
     * Reset create mode.
     */
    this.creatingTag = false;

    this.newTagName = '';


    /*
     * Tell parent to POST it to the API.
     */
    this.tagCreated.emit(name);
  }


  // =====================================================
  // REMOVE TAG
  // =====================================================

  removeTag(
    tag: CommunityOption
  ): void {

    this.selectedTags =
      this.selectedTags.filter(
        selected =>
          selected.id !== tag.id
      );
  }


  // =====================================================
  // CLEAR SEARCH
  // =====================================================

  clearCategorySearch(): void {

    this.categorySearch = '';

    this.categoryDropdownOpen = true;
  }


  clearTagSearch(): void {

    this.tagSearch = '';

    this.tagDropdownOpen = true;
  }


  // =====================================================
  // CLOSE DROPDOWNS
  // =====================================================

  @HostListener(
    'document:click',
    ['$event']
  )
  onDocumentClick(event: MouseEvent): void {

    const target =
      event.target as HTMLElement;

    if (
      !target.closest('.search-select')
    ) {

      this.categoryDropdownOpen = false;

      this.tagDropdownOpen = false;
    }
  }
}


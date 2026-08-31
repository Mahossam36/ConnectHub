import {
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { BffApiService } from '../../../core/services/bff-api.service';
import { ApiPost, Attachment as FeedAttachment, CreatePostRequest } from '../../../core/models/feed.models';

interface UserProfile {
  id: string;
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  email: string | null;
  bio: string | null;
  avatarUrl: string | null;
  isActive: boolean;
  createdAt: string;
}

interface Category {
  id: string;
  name: string | null;
}

type GroupRole = 'Member' | 'Admin' | 'Owner';

interface Group {
  id: string;
  name: string | null;
  description: string | null;
  coverImageUrl: string | null;
  category: Category;
  tags: Array<{
    id: string;
    name: string | null;
  }> | null;
  memberCount: number;
  currentUserRole: GroupRole;
  createdAt: string;
}

type Attachment = FeedAttachment;

type PostResponse = ApiPost;

interface PagedGroupsResponse {
  items: Group[] | null;
  total: number;
  skip: number;
  take: number;
}

@Component({
  selector: 'app-create-post',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-post.html',
  styleUrls: ['./create-post.scss'],
})
export class CreatePostComponent implements OnInit, OnDestroy {
  @ViewChild('fileInput')
  private fileInput?: ElementRef<HTMLInputElement>;

  @ViewChild('contentEditor')
  private contentEditor?: ElementRef<HTMLDivElement>;

  @Output()
  readonly closed = new EventEmitter<void>();

  @Output()
  readonly postCreated = new EventEmitter<PostResponse>();

  currentUser: UserProfile | null = null;

  groups: Group[] = [];
  selectedGroupId = '';

  content = '';

  selectedFiles: File[] = [];
  uploadedAttachments: Attachment[] = [];

  isDragging = false;
  isLoading = false;
  isLoadingGroups = false;
  isUploading = false;

  errorMessage = '';

  private readonly subscriptions = new Subscription();

  constructor(
    private readonly feedApi: FeedApiService,
    private readonly bffApi: BffApiService
  ) {}

  ngOnInit(): void {
    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadInitialData(): void {
    this.errorMessage = '';
    this.isLoadingGroups = true;

    const userRequest = this.bffApi.get<UserProfile>('/api/Users/me').pipe(
      catchError((error) => {
        this.errorMessage = error?.message ?? 'Unable to load your profile.';

        return of(null);
      })
    );

    const groupsRequest = this.bffApi
      .get<PagedGroupsResponse>('/api/Groups?skip=0&take=100')
      .pipe(
        catchError((error) => {
          this.errorMessage = error?.message ?? 'Unable to load your communities.';

          return of(null);
        }),
        finalize(() => {
          this.isLoadingGroups = false;
        })
      );

    this.subscriptions.add(
      forkJoin({
        user: userRequest,
        groups: groupsRequest,
      }).subscribe({
        next: ({ user, groups }) => {
          this.currentUser = user;

          this.groups = (groups?.items ?? []).filter(
            (group): group is Group =>
              !!group &&
              typeof group.id === 'string' &&
              group.id.length > 0 &&
              typeof group.name === 'string'
          );

          this.selectFirstAvailableGroup();
        },
      })
    );
  }

  private selectFirstAvailableGroup(): void {
    if (this.selectedGroupId) {
      return;
    }

    const firstPostableGroup = this.groups.find((group) =>
      this.canCreatePostInGroup(group)
    );

    if (firstPostableGroup) {
      this.selectedGroupId = firstPostableGroup.id;
    }
  }

  canCreatePostInGroup(group: Group): boolean {
    return (
      group.currentUserRole === 'Member' ||
      group.currentUserRole === 'Admin' ||
      group.currentUserRole === 'Owner'
    );
  }

  openFilePicker(): void {
    if (this.isUploading || this.isLoading) {
      return;
    }

    this.fileInput?.nativeElement.click();
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files?.length) {
      return;
    }

    this.addFiles(Array.from(input.files));

    /*
     * Reset the input so selecting the same file again
     * triggers change correctly.
     */
    input.value = '';
  }

  onDragEnter(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.isUploading || this.isLoading) {
      return;
    }

    this.isDragging = true;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.isUploading || this.isLoading) {
      return;
    }

    this.isDragging = true;

    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    const currentTarget = event.currentTarget as HTMLElement | null;
    const relatedTarget = event.relatedTarget as Node | null;

    /*
     * Do not remove the drag state while moving between
     * children inside the upload box.
     */
    if (currentTarget && relatedTarget && currentTarget.contains(relatedTarget)) {
      return;
    }

    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.isDragging = false;

    if (this.isUploading || this.isLoading) {
      return;
    }

    const files = event.dataTransfer?.files;

    if (!files?.length) {
      return;
    }

    /*
     * IMPORTANT:
     * Dropping files only processes the dropped files.
     * It NEVER opens the native file picker.
     */
    this.addFiles(Array.from(files));
  }

  removeFile(index: number): void {
    if (index < 0 || index >= this.selectedFiles.length) {
      return;
    }

    this.selectedFiles.splice(index, 1);

    /*
     * If the file was already uploaded, delete the corresponding
     * attachment from the BFF as well.
     */
    const attachment = this.uploadedAttachments[index];

    if (attachment?.id) {
      this.deleteAttachment(attachment.id);
    }
  }

  private addFiles(files: File[]): void {
    const validFiles = files.filter((file) => this.isSupportedFile(file));

    if (!validFiles.length) {
      this.errorMessage =
        'Please select a supported image, video, or document file.';
      return;
    }

    const existingKeys = new Set(
      this.selectedFiles.map(
        (file) => `${file.name}-${file.size}-${file.lastModified}`
      )
    );

    for (const file of validFiles) {
      const key = `${file.name}-${file.size}-${file.lastModified}`;

      if (!existingKeys.has(key)) {
        this.selectedFiles.push(file);
        existingKeys.add(key);
      }
    }

    this.errorMessage = '';
  }

  private isSupportedFile(file: File): boolean {
    const supportedTypes = [
      'image/',
      'video/',
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/vnd.ms-excel',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'text/plain',
      'text/csv',
    ];

    return supportedTypes.some((type) =>
      type.endsWith('/')
        ? file.type.startsWith(type)
        : file.type === type
    );
  }

  formatBold(): void {
    this.executeEditorCommand('bold');
  }

  formatItalic(): void {
    this.executeEditorCommand('italic');
  }

  formatUnderline(): void {
    this.executeEditorCommand('underline');
  }

  formatUnorderedList(): void {
    this.executeEditorCommand('insertUnorderedList');
  }

  formatOrderedList(): void {
    this.executeEditorCommand('insertOrderedList');
  }

  private executeEditorCommand(command: string): void {
    const editor = this.contentEditor?.nativeElement;

    if (!editor) {
      return;
    }

    editor.focus();

    document.execCommand(command, false);

    this.syncContentFromEditor();
  }

  onEditorInput(): void {
    this.syncContentFromEditor();
  }

  private syncContentFromEditor(): void {
    const editor = this.contentEditor?.nativeElement;

    if (!editor) {
      return;
    }

    this.content = editor.innerHTML.trim();
  }

  get plainTextContent(): string {
    const container = document.createElement('div');
    container.innerHTML = this.content;

    return container.textContent?.trim() ?? '';
  }

  async publishPost(): Promise<void> {
    this.syncContentFromEditor();

    if (!this.selectedGroupId) {
      this.errorMessage = 'Please select a community.';
      return;
    }

    if (!this.plainTextContent) {
      this.errorMessage = 'Please write something before publishing.';
      return;
    }

    if (this.isLoading || this.isUploading) {
      return;
    }

    this.errorMessage = '';
    this.isLoading = true;

    try {
      const attachmentIds = await this.uploadSelectedFiles();

      const payload: CreatePostRequest = {
        content: this.content,
        attachmentIds,
      };

      const post = await this.feedApi
        .createPost(this.selectedGroupId, payload)
        .toPromise();

      if (!post) {
        throw new Error('The server did not return the created post.');
      }

      this.postCreated.emit(post);
      this.resetForm();
    } catch (error) {
      const err = error as { message?: string };

      this.errorMessage = err?.message ?? 'Unable to create the post.';
    } finally {
      this.isLoading = false;
    }
  }

  private uploadSelectedFiles(): Promise<string[]> {
    if (!this.selectedFiles.length) {
      return Promise.resolve([]);
    }

    this.isUploading = true;

    const uploadRequests = this.selectedFiles.map((file) => {
      const formData = new FormData();

      formData.append('file', file);

      return this.feedApi.uploadAttachment(file);
    });

    return forkJoin(uploadRequests)
      .pipe(
        finalize(() => {
          this.isUploading = false;
        })
      )
      .toPromise()
      .then((attachments) => {
        const uploaded = attachments ?? [];

        this.uploadedAttachments = uploaded;

        return uploaded
          .map((attachment) => attachment.id)
          .filter((id): id is string => !!id);
      });
  }

  private deleteAttachment(attachmentId: string): void {
    this.bffApi
      .delete<void>(`/api/Attachments/${attachmentId}`)
      .pipe(
        catchError(() => {
          /*
           * The post form should remain usable even if cleanup
           * of an uploaded attachment fails.
           */
          return of(undefined);
        })
      )
      .subscribe();
  }

  getFilePreview(file: File): string | null {
    if (!file.type.startsWith('image/')) {
      return null;
    }

    return URL.createObjectURL(file);
  }

  isImage(file: File): boolean {
    return file.type.startsWith('image/');
  }

  isVideo(file: File): boolean {
    return file.type.startsWith('video/');
  }

  getUserDisplayName(): string {
    if (!this.currentUser) {
      return '';
    }

    return (
      this.currentUser.displayName ||
      `${this.currentUser.firstName ?? ''} ${this.currentUser.lastName ?? ''}`.trim() ||
      'User'
    );
  }

  close(): void {
    if (this.isLoading || this.isUploading) {
      return;
    }

    this.closed.emit();
  }

  private resetForm(): void {
    this.content = '';
    this.selectedGroupId = '';
    this.selectedFiles = [];
    this.uploadedAttachments = [];
    this.isDragging = false;
    this.errorMessage = '';

    if (this.contentEditor) {
      this.contentEditor.nativeElement.innerHTML = '';
    }

    this.selectFirstAvailableGroup();
  }
}

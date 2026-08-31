import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-create-community-cta',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './create-community-cta.component.html',
  styleUrl: './create-community-cta.component.scss'
})
export class CreateCommunityCtaComponent {
  @Input() title = "Can't find your community?";
  @Input() description = 'Create a community and bring people together around your interests.';
  @Output() create = new EventEmitter<void>();
}

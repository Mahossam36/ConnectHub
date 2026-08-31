import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { NavbarComponent } from '../features/common/navbar/navbar/navbar';
import { SidePanelComponent } from '../features/common/side-panel/side-panel';
import { FixedSidePanelComponent } from '../features/common/fixed-side-panel/fixed-side-panel';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SidePanelComponent, FixedSidePanelComponent],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayoutComponent {
  sidePanelOpen = false;
}

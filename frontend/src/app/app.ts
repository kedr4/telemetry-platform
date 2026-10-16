import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TelemetryHub } from './core/realtime/telemetry-hub';
import { ConnectionStatus } from './shared/ui/connection-status';

@Component({
  selector: 'app-root',
  imports: [
    ConnectionStatus,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly navigation = [
    { path: '/', label: 'Overview', icon: 'dashboard', exact: true },
    { path: '/readings', label: 'Readings', icon: 'table_rows', exact: false },
  ];

  constructor() {
    // Live updates run for the whole session, across pages.
    inject(TelemetryHub).start();
  }
}

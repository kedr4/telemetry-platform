import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  type ResourceRef,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * A titled card that renders the state of the data behind it: a spinner while loading for the first time,
 * an error with a retry button, and the projected content once data is there (with a thin progress bar
 * while it refreshes in the background, so content never flickers away).
 */
@Component({
  selector: 'app-panel',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-card appearance="outlined" class="panel">
      <mat-card-header>
        <mat-card-title>{{ heading() }}</mat-card-title>
        @if (subheading()) {
          <mat-card-subtitle>{{ subheading() }}</mat-card-subtitle>
        }
        <div class="panel-actions"><ng-content select="[panelActions]" /></div>
      </mat-card-header>

      <div class="panel-progress">
        @if (refreshing()) {
          <mat-progress-bar mode="indeterminate" aria-label="Refreshing" />
        }
      </div>

      <mat-card-content>
        @if (resource().error() && !resource().hasValue()) {
          <div class="panel-state panel-error" role="alert">
            <mat-icon>cloud_off</mat-icon>
            <p>{{ errorMessage() }}</p>
            <button mat-stroked-button type="button" (click)="resource().reload()">
              <mat-icon>refresh</mat-icon> Retry
            </button>
          </div>
        } @else if (resource().hasValue()) {
          <ng-content />
        } @else {
          <div class="panel-state" aria-busy="true">
            <mat-progress-spinner mode="indeterminate" diameter="36" aria-label="Loading" />
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  styleUrl: './panel.scss',
})
export class Panel {
  readonly heading = input.required<string>();
  readonly subheading = input<string>();
  readonly resource = input.required<ResourceRef<unknown>>();

  protected readonly refreshing = computed(
    () => this.resource().isLoading() && this.resource().hasValue(),
  );

  protected readonly errorMessage = computed(() => {
    const error = this.resource().error();
    return error instanceof Error && error.message
      ? `Could not load data: ${error.message}`
      : 'Could not load data.';
  });
}

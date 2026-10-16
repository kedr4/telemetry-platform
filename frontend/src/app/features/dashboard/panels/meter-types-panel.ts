import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TelemetryApi } from '../../../core/telemetry/telemetry-api';
import { rangeStart, timeRange, type TimeRangeId } from '../../../core/telemetry/time-range';
import { Chart } from '../../../shared/ui/chart';
import { Panel } from '../../../shared/ui/panel';
import { distributionChart } from '../charts';

/** How the stored readings split across meter types. */
@Component({
  selector: 'app-meter-types-panel',
  imports: [Chart, Panel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-panel heading="Readings by meter type" [subheading]="subheading()" [resource]="data">
      @if (data.value()?.length) {
        <app-chart [options]="chart()" label="Share of readings per meter type" height="300px" />
        <p class="total">{{ total() }} readings</p>
      } @else {
        <p class="empty">No readings in this time range yet.</p>
      }
    </app-panel>
  `,
  styles: `
    .empty {
      text-align: center;
      padding: 48px 0;
      color: var(--mat-sys-on-surface-variant);
    }
    .total {
      text-align: center;
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class MeterTypesPanel {
  readonly range = input.required<TimeRangeId>();
  readonly refreshTick = input(0);

  private readonly api = inject(TelemetryApi);

  protected readonly subheading = computed(() => `Last ${timeRange(this.range()).label}`);

  protected readonly data = rxResource({
    params: () => ({ range: this.range(), tick: this.refreshTick() }),
    stream: ({ params }) =>
      this.api.aggregates({ groupBy: ['TYPE'], filter: { from: rangeStart(params.range) } }),
  });

  protected readonly chart = computed(() => distributionChart(this.data.value() ?? []));
  protected readonly total = computed(() =>
    (this.data.value() ?? []).reduce((sum, aggregate) => sum + aggregate.count, 0),
  );
}

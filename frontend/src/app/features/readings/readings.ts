import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import type { MeterType, ReadingFilterInput } from '../../core/graphql/generated';
import { describeReading, fromFragment } from '../../core/telemetry/live-reading';
import { METER_TYPES } from '../../core/telemetry/meter-catalog';
import { TelemetryApi } from '../../core/telemetry/telemetry-api';
import { Panel } from '../../shared/ui/panel';
import { CursorPager } from './cursor-pager';

export const PAGE_SIZES = [25, 50, 100] as const;

/** Converts a `datetime-local` input value (local time, no zone) to an ISO timestamp. */
export function localInputToIso(value: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

/** Browse and filter stored readings, newest first. */
@Component({
  selector: 'app-readings',
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    Panel,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './readings.html',
  styleUrl: './readings.scss',
})
export class Readings {
  private readonly api = inject(TelemetryApi);

  protected readonly meterTypes = Object.values(METER_TYPES);
  protected readonly pageSizes = PAGE_SIZES;
  protected readonly columns = ['recordedAt', 'location', 'type', 'value'];

  protected readonly locations = signal<string[]>([]);
  protected readonly types = signal<MeterType[]>([]);
  protected readonly from = signal('');
  protected readonly to = signal('');
  protected readonly pageSize = signal<number>(PAGE_SIZES[0]);

  protected readonly availableLocations = rxResource({ stream: () => this.api.locations() });

  protected readonly filter = computed<ReadingFilterInput>(() => ({
    locations: this.locations().length ? this.locations() : null,
    types: this.types().length ? this.types() : null,
    from: localInputToIso(this.from()),
    to: localInputToIso(this.to()),
  }));

  /** Back to the first page whenever the filter or page size changes. */
  protected readonly pager = linkedSignal({
    source: () => ({ filter: this.filter(), pageSize: this.pageSize() }),
    computation: () => CursorPager.first(),
  });

  protected readonly page = rxResource({
    params: () => ({ filter: this.filter(), first: this.pageSize(), after: this.pager().after }),
    stream: ({ params }) => this.api.readings(params.filter, params.first, params.after),
  });

  protected readonly rows = computed(() =>
    (this.page.value()?.nodes ?? []).map((node) => {
      const reading = fromFragment(node);
      return { id: node.id, reading, description: describeReading(reading) };
    }),
  );

  protected readonly rangeLabel = computed(() => {
    const page = this.page.value();
    if (!page || page.nodes.length === 0) {
      return 'No readings';
    }
    const start = this.pager().pageIndex * this.pageSize() + 1;
    return `${start}–${start + page.nodes.length - 1} of ${page.totalCount}`;
  });

  protected readonly trackRow = (_: number, row: { id: number }) => row.id;

  protected typeLabel(type: MeterType): string {
    return METER_TYPES[type].label;
  }

  protected typeIcon(type: MeterType): string {
    return METER_TYPES[type].icon;
  }

  protected nextPage(): void {
    const endCursor = this.page.value()?.pageInfo.endCursor;
    if (endCursor) {
      this.pager.update((pager) => pager.next(endCursor));
    }
  }

  protected previousPage(): void {
    this.pager.update((pager) => pager.previous());
  }

  protected clearFilters(): void {
    this.locations.set([]);
    this.types.set([]);
    this.from.set('');
    this.to.set('');
  }
}

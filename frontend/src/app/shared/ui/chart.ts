import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { echarts, type EChartsCoreOption } from './echarts';

type ChartInstance = ReturnType<typeof echarts.init>;

const DARK_SCHEME = '(prefers-color-scheme: dark)';

/**
 * Thin Apache ECharts wrapper: renders the given options, follows the container size and the
 * light/dark color scheme, and disposes the chart with the component.
 */
@Component({
  selector: 'app-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '',
  host: { role: 'img', '[attr.aria-label]': 'label()', '[style.height]': 'height()' },
  styles: ':host { display: block; width: 100%; }',
})
export class Chart {
  readonly options = input.required<EChartsCoreOption>();
  readonly label = input.required<string>();
  readonly height = input('300px');

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly chart = signal<ChartInstance | undefined>(undefined);

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      const scheme = window.matchMedia(DARK_SCHEME);
      const create = () => {
        this.chart()?.dispose();
        this.chart.set(echarts.init(this.host.nativeElement, scheme.matches ? 'dark' : undefined));
      };
      create();

      const resize = new ResizeObserver(() => this.chart()?.resize());
      resize.observe(this.host.nativeElement);
      scheme.addEventListener('change', create);

      destroyRef.onDestroy(() => {
        scheme.removeEventListener('change', create);
        resize.disconnect();
        this.chart()?.dispose();
      });
    });

    effect(() => {
      const chart = this.chart();
      chart?.setOption({ backgroundColor: 'transparent', ...this.options() }, { notMerge: true });
    });
  }
}

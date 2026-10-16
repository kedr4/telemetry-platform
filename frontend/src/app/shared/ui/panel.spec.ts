import { ChangeDetectionStrategy, Component, signal, type ResourceRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Panel } from './panel';

/** A hand-driven stand-in for a resource, to put the panel into each state. */
function fakeResource() {
  const value = signal<string | undefined>(undefined);
  const loading = signal(true);
  const error = signal<Error | undefined>(undefined);
  const reload = vi.fn(() => true);
  const resource = {
    value,
    isLoading: loading,
    error,
    hasValue: () => value() !== undefined,
    reload,
  } as unknown as ResourceRef<unknown>;
  return { resource, value, loading, error, reload };
}

@Component({
  imports: [Panel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-panel heading="Data" [resource]="resource"
    ><p class="content">loaded</p></app-panel
  >`,
})
class Host {
  readonly state = fakeResource();
  readonly resource = this.state.resource;
}

describe('Panel', () => {
  async function render() {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    return {
      fixture,
      state: fixture.componentInstance.state,
      element: fixture.nativeElement as HTMLElement,
    };
  }

  it('shows a spinner while loading for the first time', async () => {
    const { element } = await render();

    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();
    expect(element.querySelector('.content')).toBeNull();
  });

  it('shows the content once data is there, with a progress bar while refreshing', async () => {
    const { fixture, state, element } = await render();

    state.value.set('data');
    await fixture.whenStable();
    expect(element.querySelector('.content')).not.toBeNull();
    expect(element.querySelector('mat-progress-bar')).not.toBeNull();

    state.loading.set(false);
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).toBeNull();
  });

  it('shows the error with a retry button that reloads', async () => {
    const { fixture, state, element } = await render();

    state.loading.set(false);
    state.error.set(new Error('Gateway unavailable'));
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Gateway unavailable');
    element.querySelector<HTMLButtonElement>('button')?.click();
    expect(state.reload).toHaveBeenCalled();
  });
});

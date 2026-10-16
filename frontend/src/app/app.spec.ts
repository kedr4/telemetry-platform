import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { TelemetryHub } from './core/realtime/telemetry-hub';

describe('App', () => {
  const hub = {
    start: vi.fn(),
    status: signal('connected'),
    lastUpdate: signal<Date | null>(null),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), { provide: TelemetryHub, useValue: hub }],
    });
  });

  it('shows the navigation and the live status', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const links = [...element.querySelectorAll('nav .nav-label')].map((label) => label.textContent);
    expect(links).toEqual(['Overview', 'Readings']);
    expect(element.querySelector('[role="status"]')?.textContent).toContain('Live');
  });

  it('starts live updates', () => {
    TestBed.createComponent(App);

    expect(hub.start).toHaveBeenCalled();
  });
});

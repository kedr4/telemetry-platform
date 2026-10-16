import { TestBed } from '@angular/core/testing';
import type { RoomSummary } from '../../core/telemetry/live-reading';
import { RoomCard } from './room-card';

describe('RoomCard', () => {
  async function render(room: RoomSummary) {
    const fixture = TestBed.createComponent(RoomCard);
    fixture.componentRef.setInput('room', room);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const at = '2026-10-01T12:00:00Z';

  it('shows the latest values of every meter in the room', async () => {
    const element = await render({
      location: 'Kitchen',
      updatedAt: at,
      energy: { location: 'Kitchen', type: 'ENERGY', recordedAt: at, energy: 604.88 },
      airQuality: {
        location: 'Kitchen',
        type: 'AIR_QUALITY',
        recordedAt: at,
        co2: 1300,
        pm25: 8,
        humidity: 41,
      },
      motion: { location: 'Kitchen', type: 'MOTION', recordedAt: at, motionDetected: true },
    });

    const text = element.textContent ?? '';
    expect(text).toContain('Kitchen');
    expect(text).toContain('604.9 kWh');
    expect(text).toContain('1300 ppm');
    expect(text).toContain('41 %');
    expect(text).toContain('Detected');
    expect(element.querySelector('.level-poor')).not.toBeNull(); // CO2 1300
    expect(element.querySelector('.level-good')).not.toBeNull(); // PM2.5 8
  });

  it('shows dashes for meters that have not reported', async () => {
    const element = await render({ location: 'Attic', updatedAt: at });

    expect(element.querySelectorAll('dd')[0].textContent?.trim()).toBe('–');
    expect(element.querySelector('.level')).toBeNull();
  });
});

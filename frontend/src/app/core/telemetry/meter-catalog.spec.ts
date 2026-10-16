import { co2Level, formatMetric, pm25Level } from './meter-catalog';

describe('meter catalog', () => {
  it.each([
    [400, 'good'],
    [799, 'good'],
    [800, 'moderate'],
    [1199, 'moderate'],
    [1200, 'poor'],
  ] as const)('rates %i ppm CO2 as %s', (ppm, level) => {
    expect(co2Level(ppm)).toBe(level);
  });

  it.each([
    [12, 'good'],
    [13, 'moderate'],
    [35, 'moderate'],
    [36, 'poor'],
  ] as const)('rates %i µg/m³ PM2.5 as %s', (value, level) => {
    expect(pm25Level(value)).toBe(level);
  });

  it('formats values with the metric unit and precision', () => {
    expect(formatMetric('ENERGY', 604.876)).toBe('604.9 kWh');
    expect(formatMetric('CO2', 543.4)).toBe('543 ppm');
    expect(formatMetric('HUMIDITY', 68)).toBe('68 %');
  });

  it('shows motion shares as percentages', () => {
    expect(formatMetric('MOTION', 2 / 3)).toBe('67 %');
  });

  it('shows a dash for missing values', () => {
    expect(formatMetric('ENERGY', null)).toBe('–');
    expect(formatMetric('ENERGY', undefined)).toBe('–');
    expect(formatMetric('ENERGY', Number.NaN)).toBe('–');
  });
});

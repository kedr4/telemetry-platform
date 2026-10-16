import type { MeterType, Metric } from '../graphql/generated';

export interface MeterTypeDefinition {
  readonly id: MeterType;
  readonly label: string;
  readonly icon: string;
}

export interface MetricDefinition {
  readonly id: Metric;
  readonly label: string;
  readonly unit: string;
  readonly decimals: number;
  readonly meterType: MeterType;
}

export const METER_TYPES: Readonly<Record<MeterType, MeterTypeDefinition>> = {
  ENERGY: { id: 'ENERGY', label: 'Energy', icon: 'bolt' },
  AIR_QUALITY: { id: 'AIR_QUALITY', label: 'Air quality', icon: 'air' },
  MOTION: { id: 'MOTION', label: 'Motion', icon: 'directions_walk' },
};

export const METRICS: Readonly<Record<Metric, MetricDefinition>> = {
  ENERGY: { id: 'ENERGY', label: 'Energy', unit: 'kWh', decimals: 1, meterType: 'ENERGY' },
  CO2: { id: 'CO2', label: 'CO₂', unit: 'ppm', decimals: 0, meterType: 'AIR_QUALITY' },
  PM25: { id: 'PM25', label: 'PM2.5', unit: 'µg/m³', decimals: 0, meterType: 'AIR_QUALITY' },
  HUMIDITY: { id: 'HUMIDITY', label: 'Humidity', unit: '%', decimals: 0, meterType: 'AIR_QUALITY' },
  MOTION: { id: 'MOTION', label: 'Motion share', unit: '%', decimals: 0, meterType: 'MOTION' },
};

/** Metrics that make sense as continuous values on a chart. */
export const CHARTABLE_METRICS: readonly MetricDefinition[] = [
  METRICS.ENERGY,
  METRICS.CO2,
  METRICS.PM25,
  METRICS.HUMIDITY,
];

export type AirQualityLevel = 'good' | 'moderate' | 'poor';

/** Indoor CO₂: below 800 ppm is fresh air, above 1200 ppm calls for ventilation. */
export function co2Level(ppm: number): AirQualityLevel {
  if (ppm < 800) {
    return 'good';
  }
  return ppm < 1200 ? 'moderate' : 'poor';
}

/** PM2.5 (24 h exposure guideline breakpoints): up to 12 good, up to 35 moderate. */
export function pm25Level(microgramsPerCubicMeter: number): AirQualityLevel {
  if (microgramsPerCubicMeter <= 12) {
    return 'good';
  }
  return microgramsPerCubicMeter <= 35 ? 'moderate' : 'poor';
}

/** Formats a metric value with its unit; motion values (0–1 shares) are shown as percentages. */
export function formatMetric(metric: Metric, value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '–';
  }

  const definition = METRICS[metric];
  const shown = metric === 'MOTION' ? value * 100 : value;
  return `${shown.toFixed(definition.decimals)} ${definition.unit}`;
}

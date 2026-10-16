/** Internal type. DO NOT USE DIRECTLY. */
type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
/** Internal type. DO NOT USE DIRECTLY. */
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
/** What readings are grouped by when aggregating. Dimensions can be combined. */
export type AggregationDimension =
  | 'LOCATION'
  | 'TIME'
  | 'TYPE';

/** What to aggregate and how to group it. */
export type AggregationInput = {
  /** Time bucket width; required when grouping by TIME. */
  bucket?: TimeBucket | null | undefined;
  /** Readings to include. */
  filter?: ReadingFilterInput | null | undefined;
  /** Dimensions to group by; combine TIME and LOCATION for one series per room. */
  groupBy: Array<AggregationDimension>;
  /** Metric to compute statistics of; omit to count readings. */
  metric?: Metric | null | undefined;
};

export type MeterType =
  | 'AIR_QUALITY'
  | 'ENERGY'
  | 'MOTION';

/** A numeric quantity that can be aggregated. */
export type Metric =
  /** CO2 concentration (ppm). */
  | 'CO2'
  /** Energy consumption (kWh). */
  | 'ENERGY'
  /** Relative humidity (%). */
  | 'HUMIDITY'
  /** Motion detected, as 1 (yes) or 0 (no): its average is the share of readings with motion. */
  | 'MOTION'
  /** PM2.5 concentration (µg/m³). */
  | 'PM25';

/** Restricts readings; omitted fields do not restrict. */
export type ReadingFilterInput = {
  /** Inclusive lower bound of the reading time. */
  from?: string | null | undefined;
  /** Locations to include. */
  locations?: Array<string> | null | undefined;
  /** Exclusive upper bound of the reading time. */
  to?: string | null | undefined;
  /** Meter types to include. */
  types?: Array<MeterType> | null | undefined;
};

/** Width of the time intervals readings are grouped into. */
export type TimeBucket =
  | 'DAY'
  | 'HOUR'
  | 'MINUTE';

type ReadingFields_AirQualityReading_Fragment = { __typename: 'AirQualityReading', co2: number, pm25: number, humidity: number, id: number, location: string, type: MeterType, recordedAt: string };

type ReadingFields_EnergyReading_Fragment = { __typename: 'EnergyReading', energy: number, id: number, location: string, type: MeterType, recordedAt: string };

type ReadingFields_MotionReading_Fragment = { __typename: 'MotionReading', motionDetected: boolean, id: number, location: string, type: MeterType, recordedAt: string };

export type ReadingFieldsFragment =
  | ReadingFields_AirQualityReading_Fragment
  | ReadingFields_EnergyReading_Fragment
  | ReadingFields_MotionReading_Fragment
;

export type LocationsQueryVariables = Exact<{ [key: string]: never; }>;


export type LocationsQuery = { locations: Array<string> };

export type LatestReadingsQueryVariables = Exact<{
  filter?: ReadingFilterInput | null | undefined;
}>;


export type LatestReadingsQuery = { latestReadings: Array<
    | { __typename: 'AirQualityReading', co2: number, pm25: number, humidity: number, id: number, location: string, type: MeterType, recordedAt: string }
    | { __typename: 'EnergyReading', energy: number, id: number, location: string, type: MeterType, recordedAt: string }
    | { __typename: 'MotionReading', motionDetected: boolean, id: number, location: string, type: MeterType, recordedAt: string }
  > };

export type AggregatesQueryVariables = Exact<{
  input: AggregationInput;
}>;


export type AggregatesQuery = { aggregates: Array<{ location: string | null, type: MeterType | null, bucketStart: string | null, count: number, average: number | null, minimum: number | null, maximum: number | null, sum: number | null }> };

export type ReadingsQueryVariables = Exact<{
  filter?: ReadingFilterInput | null | undefined;
  first: number;
  after?: string | null | undefined;
}>;


export type ReadingsQuery = { readings: { totalCount: number, pageInfo: { hasNextPage: boolean, endCursor: string | null }, nodes: Array<
      | { __typename: 'AirQualityReading', co2: number, pm25: number, humidity: number, id: number, location: string, type: MeterType, recordedAt: string }
      | { __typename: 'EnergyReading', energy: number, id: number, location: string, type: MeterType, recordedAt: string }
      | { __typename: 'MotionReading', motionDetected: boolean, id: number, location: string, type: MeterType, recordedAt: string }
    > } };

export const ReadingFieldsFragmentDoc = {"kind":"Document","definitions":[{"kind":"FragmentDefinition","name":{"kind":"Name","value":"ReadingFields"},"typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"Reading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"__typename"}},{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"location"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"recordedAt"}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"EnergyReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"energy"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"AirQualityReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"co2"}},{"kind":"Field","name":{"kind":"Name","value":"pm25"}},{"kind":"Field","name":{"kind":"Name","value":"humidity"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"MotionReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"motionDetected"}}]}}]}}]} as unknown as DocumentNode<ReadingFieldsFragment, unknown>;
export const LocationsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Locations"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"locations"}}]}}]} as unknown as DocumentNode<LocationsQuery, LocationsQueryVariables>;
export const LatestReadingsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"LatestReadings"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"filter"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"ReadingFilterInput"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"latestReadings"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"filter"},"value":{"kind":"Variable","name":{"kind":"Name","value":"filter"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"FragmentSpread","name":{"kind":"Name","value":"ReadingFields"}}]}}]}},{"kind":"FragmentDefinition","name":{"kind":"Name","value":"ReadingFields"},"typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"Reading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"__typename"}},{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"location"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"recordedAt"}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"EnergyReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"energy"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"AirQualityReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"co2"}},{"kind":"Field","name":{"kind":"Name","value":"pm25"}},{"kind":"Field","name":{"kind":"Name","value":"humidity"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"MotionReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"motionDetected"}}]}}]}}]} as unknown as DocumentNode<LatestReadingsQuery, LatestReadingsQueryVariables>;
export const AggregatesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Aggregates"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"AggregationInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"aggregates"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"location"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"bucketStart"}},{"kind":"Field","name":{"kind":"Name","value":"count"}},{"kind":"Field","name":{"kind":"Name","value":"average"}},{"kind":"Field","name":{"kind":"Name","value":"minimum"}},{"kind":"Field","name":{"kind":"Name","value":"maximum"}},{"kind":"Field","name":{"kind":"Name","value":"sum"}}]}}]}}]} as unknown as DocumentNode<AggregatesQuery, AggregatesQueryVariables>;
export const ReadingsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Readings"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"filter"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"ReadingFilterInput"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"first"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"after"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"readings"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"filter"},"value":{"kind":"Variable","name":{"kind":"Name","value":"filter"}}},{"kind":"Argument","name":{"kind":"Name","value":"first"},"value":{"kind":"Variable","name":{"kind":"Name","value":"first"}}},{"kind":"Argument","name":{"kind":"Name","value":"after"},"value":{"kind":"Variable","name":{"kind":"Name","value":"after"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"totalCount"}},{"kind":"Field","name":{"kind":"Name","value":"pageInfo"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"hasNextPage"}},{"kind":"Field","name":{"kind":"Name","value":"endCursor"}}]}},{"kind":"Field","name":{"kind":"Name","value":"nodes"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"FragmentSpread","name":{"kind":"Name","value":"ReadingFields"}}]}}]}}]}},{"kind":"FragmentDefinition","name":{"kind":"Name","value":"ReadingFields"},"typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"Reading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"__typename"}},{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"location"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"recordedAt"}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"EnergyReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"energy"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"AirQualityReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"co2"}},{"kind":"Field","name":{"kind":"Name","value":"pm25"}},{"kind":"Field","name":{"kind":"Name","value":"humidity"}}]}},{"kind":"InlineFragment","typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"MotionReading"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"motionDetected"}}]}}]}}]} as unknown as DocumentNode<ReadingsQuery, ReadingsQueryVariables>;
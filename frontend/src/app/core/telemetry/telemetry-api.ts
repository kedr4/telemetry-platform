import { Injectable, inject } from '@angular/core';
import type { OperationVariables, TypedDocumentNode } from '@apollo/client/core';
import { Apollo } from 'apollo-angular';
import { Observable, map } from 'rxjs';
import {
  AggregatesDocument,
  LatestReadingsDocument,
  LocationsDocument,
  ReadingsDocument,
  type AggregatesQuery,
  type AggregationInput,
  type ReadingFilterInput,
  type ReadingsQuery,
} from '../graphql/generated';
import { fromFragment, type LiveReading } from './live-reading';

export type Aggregate = AggregatesQuery['aggregates'][number];
export type ReadingsPage = ReadingsQuery['readings'];

/** Typed access to the gateway's GraphQL API. */
@Injectable({ providedIn: 'root' })
export class TelemetryApi {
  private readonly apollo = inject(Apollo);

  locations(): Observable<string[]> {
    return this.query(LocationsDocument, {}).pipe(map((data) => data.locations));
  }

  latestReadings(filter?: ReadingFilterInput): Observable<LiveReading[]> {
    return this.query(LatestReadingsDocument, { filter }).pipe(
      map((data) => data.latestReadings.map(fromFragment)),
    );
  }

  aggregates(input: AggregationInput): Observable<Aggregate[]> {
    return this.query(AggregatesDocument, { input }).pipe(map((data) => data.aggregates));
  }

  readings(
    filter: ReadingFilterInput,
    first: number,
    after: string | null,
  ): Observable<ReadingsPage> {
    return this.query(ReadingsDocument, { filter, first, after }).pipe(
      map((data) => data.readings),
    );
  }

  private query<TData, TVariables extends OperationVariables>(
    query: TypedDocumentNode<TData, TVariables>,
    variables: TVariables,
  ): Observable<TData> {
    return this.apollo.query<TData, TVariables>({ query, variables }).pipe(
      map((result) => {
        if (result.data === undefined) {
          throw new Error('The server returned no data.');
        }
        return result.data;
      }),
    );
  }
}

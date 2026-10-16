import { inject, type EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { InMemoryCache } from '@apollo/client/core';
import { provideApollo } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';

export const GRAPHQL_PATH = '/graphql';

/**
 * Apollo Client for the gateway. The gateway is reached on the same origin (proxied by the dev server and
 * by nginx), so no CORS is involved. Telemetry changes constantly, so queries always go to the network;
 * the cache still needs to know the Reading interface's implementations to resolve fragments.
 */
export function provideTelemetryApollo(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideHttpClient(withFetch()),
    provideApollo(() => ({
      link: inject(HttpLink).create({ uri: GRAPHQL_PATH }),
      cache: new InMemoryCache({
        possibleTypes: { Reading: ['EnergyReading', 'AirQualityReading', 'MotionReading'] },
        typePolicies: { Aggregate: { keyFields: false } },
      }),
      defaultOptions: {
        query: { fetchPolicy: 'network-only' },
      },
    })),
  ]);
}

import { expect, type APIRequestContext } from '@playwright/test';
import { urls } from '../playwright.config';

export interface GraphQLResponse<T> {
  data: T | null;
  errors?: { message: string; extensions?: Record<string, unknown> }[];
}

export async function graphql<T>(
  request: APIRequestContext,
  query: string,
  variables: Record<string, unknown> = {},
): Promise<GraphQLResponse<T>> {
  const response = await request.post(`${urls.gateway}/graphql`, { data: { query, variables } });
  expect(response.ok(), `GraphQL HTTP status ${response.status()}`).toBeTruthy();
  return (await response.json()) as GraphQLResponse<T>;
}

/** Rooms known to the platform; fails if nothing has been ingested yet. */
export async function locations(request: APIRequestContext): Promise<string[]> {
  const response = await request.get(`${urls.gateway}/api/v1/locations`);
  expect(response.ok()).toBeTruthy();
  const result = (await response.json()) as string[];
  expect(result.length, 'the platform has ingested at least one batch').toBeGreaterThan(0);
  return result;
}

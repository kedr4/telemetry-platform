import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Overview · Smart Home Telemetry',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'readings',
    title: 'Readings · Smart Home Telemetry',
    loadComponent: () => import('./features/readings/readings').then((m) => m.Readings),
  },
  { path: '**', redirectTo: '' },
];

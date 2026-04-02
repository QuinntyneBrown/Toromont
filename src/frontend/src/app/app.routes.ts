import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component')
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component')
      },
      {
        path: 'equipment',
        loadComponent: () => import('./features/equipment/equipment-list.component')
      },
      {
        path: 'equipment/:id',
        loadComponent: () => import('./features/equipment/equipment-detail.component')
      },
      {
        path: 'service',
        loadComponent: () => import('./features/service/work-orders.component')
      },
      {
        path: 'parts',
        loadComponent: () => import('./features/parts/parts-catalog.component')
      },
      {
        path: 'parts/cart',
        loadComponent: () => import('./features/parts/cart.component')
      },
      {
        path: 'telemetry',
        loadComponent: () => import('./features/telemetry/telemetry-dashboard.component')
      },
      {
        path: 'telemetry/:equipmentId',
        loadComponent: () => import('./features/telemetry/telemetry-dashboard.component')
      },
      {
        path: 'ai-insights',
        loadComponent: () => import('./features/ai-insights/ai-insights.component')
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports.component')
      },
      {
        path: 'admin/users',
        loadComponent: () => import('./features/users/user-management.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'settings/notifications',
        loadComponent: () => import('./features/settings/notification-preferences.component')
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

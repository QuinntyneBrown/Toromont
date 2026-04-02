import { ApplicationConfig, APP_INITIALIZER, provideBrowserGlobalErrorListeners, isDevMode, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  MSAL_INSTANCE,
  MSAL_GUARD_CONFIG,
  MSAL_INTERCEPTOR_CONFIG,
  MsalService,
  MsalGuard,
  MsalBroadcastService
} from '@azure/msal-angular';
import {
  PublicClientApplication,
  InteractionType,
  BrowserCacheLocation
} from '@azure/msal-browser';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { tenantInterceptor } from './core/interceptors/tenant.interceptor';

export function msalInstanceFactory() {
  // In dev mode with placeholder credentials, create a minimal config
  // that won't attempt to contact Azure AD
  const clientId = 'YOUR_CLIENT_ID';
  return new PublicClientApplication({
    auth: {
      clientId,
      authority: `https://login.microsoftonline.com/common`,
      redirectUri: window.location.origin
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage
    },
    system: {
      loggerOptions: {
        logLevel: 0 // Error only
      }
    }
  });
}

export function msalGuardConfigFactory() {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: ['user.read']
    }
  };
}

export function msalInterceptorConfigFactory() {
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap: new Map<string, string[]>([
      ['/api/*', ['api://fleet-hub-api/.default']]
    ])
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      ...(isDevMode() ? [] : [withInterceptors([authInterceptor, tenantInterceptor])])
    ),
    {
      provide: MSAL_INSTANCE,
      useFactory: msalInstanceFactory
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: msalGuardConfigFactory
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: msalInterceptorConfigFactory
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    {
      provide: APP_INITIALIZER,
      useFactory: () => () => Promise.resolve(),
      multi: true
    }
  ]
};

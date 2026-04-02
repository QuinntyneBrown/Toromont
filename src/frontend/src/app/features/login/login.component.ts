import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ButtonsModule],
  template: `
    <div class="login-container">
      <!-- Left Panel: Branding -->
      <div class="login-branding">
        <div class="branding-content">
          <div class="logo-section">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#FFCD11" stroke-width="2">
              <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/><path d="M15 18H9"/><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"/><circle cx="17" cy="18" r="2"/><circle cx="7" cy="18" r="2"/>
            </svg>
            <span class="brand-name">TOROMONT</span>
          </div>
          <h1 class="logo-text">FLEET HUB</h1>
          <p class="description">Equipment Fleet Management &amp; Service Portal</p>
          <p class="tagline">
            Manage your heavy equipment fleet, schedule service, track
            work orders, and get AI-powered insights — all in one place.
          </p>
        </div>
      </div>

      <!-- Right Panel: Sign In -->
      <div class="login-form-panel">
        <div class="login-card">
          <h2 class="sign-in-title">Welcome Back</h2>
          <p class="sign-in-subtitle">Sign in to access your fleet dashboard</p>

          <button class="ms-login-btn" (click)="onLogin()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--status-info)" stroke-width="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
            <span>Sign in with Microsoft</span>
          </button>

          <div class="divider-row">
            <span class="divider-line"></span>
            <span class="divider-text">Secured by Microsoft Entra ID</span>
            <span class="divider-line"></span>
          </div>
          <p class="copyright-text">&copy; {{ currentYear }} Toromont Industries Ltd.</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      min-height: 100vh;
      height: 100vh;
      overflow: hidden;
    }

    .login-branding {
      width: 600px;
      min-width: 600px;
      background: var(--surface-inverse);
      color: var(--foreground-inverse);
      display: flex;
      flex-direction: column;
      justify-content: center;
      padding: 80px 60px;
    }

    .branding-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .logo-section {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .brand-name {
      font-size: 14px;
      font-weight: 600;
      color: var(--foreground-inverse);
      letter-spacing: 2px;
    }

    .logo-text {
      font-size: 48px;
      font-weight: 800;
      margin: 0;
      color: var(--foreground-inverse);
    }

    .description {
      font-size: 18px;
      color: var(--foreground-secondary);
    }

    .tagline {
      font-size: 14px;
      line-height: 1.6;
      color: var(--sidebar-text);
      max-width: 420px;
    }

    .login-form-panel {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--surface-secondary);
    }

    .login-card {
      background: var(--surface-secondary);
      border-radius: var(--radius-lg);
      padding: 40px;
      width: 400px;
      border: 1px solid var(--border-subtle);
      text-align: center;
    }

    .login-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 24px;
    }

    .sign-in-title {
      font-size: 24px;
      font-weight: 700;
      margin: 0;
      color: var(--foreground-primary);
    }

    .sign-in-subtitle {
      font-size: 14px;
      color: var(--foreground-secondary);
      margin: 0;
      text-align: center;
    }

    .ms-login-btn {
      width: 320px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      background: var(--surface-secondary);
      border: 1px solid var(--border-strong);
      border-radius: var(--radius-sm);
      cursor: pointer;
      font-size: 15px;
      font-weight: 600;
      color: var(--foreground-primary);
      font-family: var(--font-body);
    }

    .ms-login-btn:hover {
      background: var(--surface-hover);
    }

    .divider-row {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 320px;
    }

    .divider-line {
      flex: 1;
      height: 1px;
      background: var(--border-subtle);
    }

    .divider-text {
      font-size: 11px;
      color: var(--foreground-disabled);
      white-space: nowrap;
    }

    .copyright-text {
      margin: 0;
      font-size: 12px;
      color: var(--foreground-disabled);
    }

    @media (max-width: 960px) {
      .login-container {
        flex-direction: column;
      }

      .login-branding {
        width: 100%;
        min-width: unset;
        padding: 32px 24px;
        min-height: auto;
      }

      .login-form-panel {
        padding: 32px 16px;
      }

      .login-card {
        width: 100%;
        max-width: 420px;
      }
    }
  `]
})
export default class LoginComponent {
  private authService = inject(AuthService);
  currentYear = new Date().getFullYear();

  onLogin(): void {
    this.authService.login();
  }
}

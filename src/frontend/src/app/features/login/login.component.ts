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
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
              <rect width="48" height="48" rx="8" fill="#FFCD11"/>
              <path d="M14 16h20v4H14zM14 24h14v4H14zM14 32h8v4H14z" fill="#1a1a2e"/>
            </svg>
            <h1 class="logo-text">FLEET <span class="logo-accent">HUB</span></h1>
          </div>
          <h2 class="tagline">Intelligent Fleet Management</h2>
          <p class="description">
            Monitor your entire fleet in real-time. Track equipment health, predict maintenance needs,
            and optimize operations with AI-powered insights — all from a single platform.
          </p>
          <div class="features">
            <div class="feature-item">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#FFCD11" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
              <span>Real-time Telemetry Monitoring</span>
            </div>
            <div class="feature-item">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#FFCD11" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
              <span>AI-Powered Predictive Maintenance</span>
            </div>
            <div class="feature-item">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#FFCD11" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
              <span>Comprehensive Reporting & Analytics</span>
            </div>
          </div>
        </div>
        <div class="branding-footer">
          <span>&copy; {{ currentYear }} Toromont Industries. All rights reserved.</span>
        </div>
      </div>

      <!-- Right Panel: Sign In -->
      <div class="login-form-panel">
        <div class="login-card">
          <h2 class="sign-in-title">Sign In</h2>
          <p class="sign-in-subtitle">Access your fleet management dashboard</p>

          <button kendoButton
                  [themeColor]="'primary'"
                  class="ms-login-btn"
                  (click)="onLogin()">
            <svg width="20" height="20" viewBox="0 0 21 21" class="me-2">
              <rect x="1" y="1" width="9" height="9" fill="#f25022"/>
              <rect x="11" y="1" width="9" height="9" fill="#7fba00"/>
              <rect x="1" y="11" width="9" height="9" fill="#00a4ef"/>
              <rect x="11" y="11" width="9" height="9" fill="#ffb900"/>
            </svg>
            Sign in with Microsoft
          </button>

          <p class="terms-text">
            By signing in, you agree to the
            <a href="javascript:void(0)">Terms of Service</a> and
            <a href="javascript:void(0)">Privacy Policy</a>.
          </p>
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
      gap: 16px;
    }

    .logo-text {
      font-size: 48px;
      font-weight: 800;
      letter-spacing: 2px;
      margin: 0;
      color: #ffffff;
    }

    .logo-accent {
      color: #FFCD11;
    }

    .tagline {
      font-size: 18px;
      font-weight: 400;
      margin: 12px 0 0;
      color: var(--foreground-secondary);
    }

    .description {
      font-size: 15px;
      line-height: 1.7;
      color: rgba(255, 255, 255, 0.65);
      max-width: 460px;
    }

    .features {
      display: flex;
      flex-direction: column;
      gap: 14px;
      margin-top: 12px;
    }

    .feature-item {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 14px;
      color: rgba(255, 255, 255, 0.8);
    }

    .branding-footer {
      font-size: 12px;
      color: rgba(255, 255, 255, 0.4);
    }

    .login-form-panel {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--surface-secondary);
    }

    .login-card {
      background: #ffffff;
      border-radius: var(--radius-lg);
      padding: 40px;
      width: 400px;
      border: 1px solid var(--border-subtle);
      text-align: center;
    }

    .sign-in-title {
      font-size: 26px;
      font-weight: 700;
      margin: 0 0 8px;
      color: var(--foreground-primary);
    }

    .sign-in-subtitle {
      font-size: 14px;
      color: #6b7280;
      margin: 0 0 32px;
    }

    .ms-login-btn {
      width: 100%;
      padding: 12px 24px;
      font-size: 15px;
      font-weight: 600;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      border-radius: 8px;
    }

    .terms-text {
      margin-top: 24px;
      font-size: 12px;
      color: #9ca3af;
    }

    .terms-text a {
      color: #3b82f6;
      text-decoration: none;
    }

    .terms-text a:hover {
      text-decoration: underline;
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

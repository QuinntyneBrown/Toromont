import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ButtonsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export default class LoginComponent {
  private authService = inject(AuthService);
  currentYear = new Date().getFullYear();

  onLogin(): void {
    this.authService.login();
  }
}


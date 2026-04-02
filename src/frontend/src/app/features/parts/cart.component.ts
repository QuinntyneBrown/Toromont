import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogsModule } from '@progress/kendo-angular-dialog';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, OrderLineItem } from '../../core/models';

interface CartItem {
  id: string;
  partId: string;
  quantity: number;
  addedAt: string;
  part: {
    id: string;
    partNumber: string;
    name: string;
    description: string;
    price: number;
    category: string;
    availability: string;
  };
}

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    InputsModule, ButtonsModule, DialogsModule
  ],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export default class CartComponent implements OnInit {
  cartItems: CartItem[] = [];
  loading = true;
  submitting = false;
  showConfirmation = false;
  orderNumber = '';

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadCart();
  }

  get subtotal(): number {
    return this.cartItems.reduce((sum, item) => sum + (item.part?.price || 0) * item.quantity, 0);
  }

  get totalItems(): number {
    return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  loadCart(): void {
    this.loading = true;
    this.api.get<CartItem[]>('/cart').subscribe({
      next: (res) => {
        this.cartItems = Array.isArray(res) ? res : [];
        this.loading = false;
      },
      error: () => {
        this.cartItems = [];
        this.loading = false;
      }
    });
  }

  onQuantityInputChange(item: CartItem, event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = parseInt(input.value, 10);
    if (isNaN(val) || val < 1) {
      item.quantity = 1;
    } else {
      item.quantity = val;
    }
    this.api.put<any>(`/cart/items/${item.id}`, { quantity: item.quantity }).subscribe({
      error: (err) => {
        console.error('Failed to update quantity', err);
      }
    });
  }

  onQuantityChange(item: CartItem): void {
    if (item.quantity < 1) item.quantity = 1;
    this.api.put<any>(`/cart/items/${item.id}`, { quantity: item.quantity }).subscribe({
      error: (err) => {
        console.error('Failed to update quantity', err);
      }
    });
  }

  removeItem(item: CartItem): void {
    this.api.delete<any>(`/cart/items/${item.id}`).subscribe({
      next: () => {
        this.cartItems = this.cartItems.filter(i => i.id !== item.id);
      },
      error: (err) => {
        console.error('Failed to remove item', err);
      }
    });
  }

  submitOrder(): void {
    this.submitting = true;
    this.api.post<any>('/orders', {}).subscribe({
      next: (res) => {
        this.submitting = false;
        this.orderNumber = res?.orderNumber || ('PO-' + Math.random().toString(36).substring(2, 10).toUpperCase());
        this.showConfirmation = true;
        this.cartItems = [];
      },
      error: (err) => {
        this.submitting = false;
        console.error('Failed to submit order', err);
      }
    });
  }

  onConfirmationClose(): void {
    this.showConfirmation = false;
    this.router.navigate(['/parts']);
  }
}


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
  template: `
    <div class="container-fluid py-3">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="fw-bold mb-0">Shopping Cart</h2>
        <a routerLink="/parts" class="btn btn-outline-secondary">&larr; Continue Shopping</a>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && cartItems.length === 0" class="card">
        <div class="card-body text-center py-5">
          <div style="font-size:3rem;color:var(--border-strong);">&#128722;</div>
          <h5 class="text-muted mt-3">Your cart is empty</h5>
          <p class="text-muted">Browse the parts catalog to add items to your cart.</p>
          <a routerLink="/parts" class="btn btn-warning fw-semibold mt-2">Browse Parts</a>
        </div>
      </div>

      <!-- Cart Items -->
      <div *ngIf="cartItems.length > 0">
        <div class="card mb-3">
          <div class="table-responsive" data-testid="cart-items">
            <table class="table table-hover mb-0 align-middle">
              <thead class="table-light">
                <tr>
                  <th>Part</th>
                  <th style="width:100px">Unit Price</th>
                  <th style="width:140px">Quantity</th>
                  <th style="width:110px" class="text-end">Line Total</th>
                  <th style="width:60px"></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of cartItems" data-testid="cart-item">
                  <td>
                    <div class="fw-semibold">{{ item.part?.name }}</div>
                    <div class="text-muted small">{{ item.part?.partNumber }}</div>
                  </td>
                  <td>{{ item.part?.price | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td>
                    <input type="number" class="form-control form-control-sm"
                           data-testid="quantity-input"
                           [value]="item.quantity"
                           min="1" max="999" step="1"
                           style="width:100px"
                           (change)="onQuantityInputChange(item, $event)">
                  </td>
                  <td class="text-end fw-semibold">{{ ((item.part?.price || 0) * item.quantity) | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td class="text-center">
                    <button class="btn btn-sm btn-outline-danger" data-testid="remove-item-btn" (click)="removeItem(item)" title="Remove">
                      &times;
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Summary -->
        <div class="row justify-content-end">
          <div class="col-md-4">
            <div class="card">
              <div class="card-body">
                <div class="d-flex justify-content-between mb-2">
                  <span class="text-muted">Subtotal ({{ totalItems }} items)</span>
                  <span class="fw-bold" data-testid="cart-subtotal">{{ subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between mb-3">
                  <span class="fw-bold fs-5">Total</span>
                  <span class="fw-bold fs-5">{{ subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
                </div>
                <button class="btn btn-warning fw-semibold w-100"
                        data-testid="submit-order-btn"
                        [disabled]="submitting"
                        (click)="submitOrder()">
                  {{ submitting ? 'Submitting...' : 'Submit Order' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Order Confirmation Dialog -->
      <div *ngIf="showConfirmation" data-testid="order-confirmation" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-dialog-centered" style="max-width:400px">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Order Submitted</h5>
              <button type="button" class="btn-close" (click)="showConfirmation = false"></button>
            </div>
            <div class="modal-body text-center py-3">
              <div style="font-size:3rem;color:var(--status-success);">&#10003;</div>
              <h5 class="mt-2">Order Submitted Successfully</h5>
              <p class="text-muted">Your order has been placed and is being processed.</p>
              <p class="fw-semibold" data-testid="order-number">{{ orderNumber }}</p>
            </div>
            <div class="modal-footer">
              <button class="btn btn-primary" (click)="onConfirmationClose()">OK</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
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

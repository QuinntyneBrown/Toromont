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
  partNumber: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
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
          <div style="font-size:3rem;color:#d1d5db;">&#128722;</div>
          <h5 class="text-muted mt-3">Your cart is empty</h5>
          <p class="text-muted">Browse the parts catalog to add items to your cart.</p>
          <a routerLink="/parts" class="btn btn-warning fw-semibold mt-2">Browse Parts</a>
        </div>
      </div>

      <!-- Cart Items -->
      <div *ngIf="cartItems.length > 0">
        <div class="card mb-3">
          <div class="table-responsive">
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
                <tr *ngFor="let item of cartItems">
                  <td>
                    <div class="fw-semibold">{{ item.description }}</div>
                    <div class="text-muted small">{{ item.partNumber }}</div>
                  </td>
                  <td>{{ item.unitPrice | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td>
                    <kendo-numerictextbox
                      [(value)]="item.quantity"
                      [min]="1"
                      [max]="999"
                      [step]="1"
                      [format]="'n0'"
                      [style.width]="'100px'"
                      (valueChange)="onQuantityChange(item)">
                    </kendo-numerictextbox>
                  </td>
                  <td class="text-end fw-semibold">{{ (item.unitPrice * item.quantity) | currency:'USD':'symbol':'1.2-2' }}</td>
                  <td class="text-center">
                    <button class="btn btn-sm btn-outline-danger" (click)="removeItem(item)" title="Remove">
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
                  <span class="fw-bold">{{ subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between mb-3">
                  <span class="fw-bold fs-5">Total</span>
                  <span class="fw-bold fs-5">{{ subtotal | currency:'USD':'symbol':'1.2-2' }}</span>
                </div>
                <button class="btn btn-warning fw-semibold w-100"
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
      <kendo-dialog *ngIf="showConfirmation" title="Order Submitted" (close)="showConfirmation = false" [width]="400">
        <div class="text-center py-3">
          <div style="font-size:3rem;color:#10b981;">&#10003;</div>
          <h5 class="mt-2">Order Submitted Successfully</h5>
          <p class="text-muted">Your order has been placed and is being processed.</p>
        </div>
        <kendo-dialog-actions>
          <button kendoButton themeColor="primary" (click)="onConfirmationClose()">OK</button>
        </kendo-dialog-actions>
      </kendo-dialog>
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

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadCart();
  }

  get subtotal(): number {
    return this.cartItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  }

  get totalItems(): number {
    return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  loadCart(): void {
    this.loading = true;
    this.api.get<ApiResponse<CartItem[]>>('/cart').subscribe({
      next: (res) => {
        this.cartItems = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.cartItems = [];
        this.loading = false;
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
      next: () => {
        this.submitting = false;
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

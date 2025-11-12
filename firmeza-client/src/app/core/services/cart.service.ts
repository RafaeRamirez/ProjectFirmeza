import { Injectable } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartItem, CartTotals } from '../models/cart.model';
import { Product } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly storageKey = 'firmeza.client.cart';
  private readonly itemsSubject = new BehaviorSubject<CartItem[]>(this.loadItems());
  readonly items$ = this.itemsSubject.asObservable();

  readonly totalItems$ = this.items$.pipe(
    map((items) => items.reduce((acc, curr) => acc + curr.quantity, 0))
  );

  readonly totals$ = this.items$.pipe(map((items) => this.calculateTotals(items)));

  add(product: Product, quantity = 1): void {
    const items = [...this.itemsSubject.value];
    const index = items.findIndex((i) => i.product.id === product.id);
    if (index >= 0) {
      const item = items[index];
      const updated = Math.min(item.quantity + quantity, product.stock);
      items[index] = { ...item, quantity: updated };
    } else {
      items.push({ product, quantity: Math.min(quantity, product.stock) });
    }
    this.persist(items);
  }

  updateQuantity(productId: string, quantity: number): void {
    const items = this.itemsSubject.value.map((item) =>
      item.product.id === productId
        ? { ...item, quantity: this.getValidatedQuantity(quantity, item.product.stock) }
        : item
    );
    this.persist(items);
  }

  remove(productId: string): void {
    const items = this.itemsSubject.value.filter((item) => item.product.id !== productId);
    this.persist(items);
  }

  clear(): void {
    this.persist([]);
  }

  snapshot(): CartItem[] {
    return [...this.itemsSubject.value];
  }

  private calculateTotals(items: CartItem[]): CartTotals {
    const subtotal = items.reduce((sum, item) => sum + item.product.unitPrice * item.quantity, 0);
    const taxes = +(subtotal * environment.taxRate).toFixed(2);
    const total = +(subtotal + taxes).toFixed(2);
    return { subtotal, taxes, total };
  }

  private getValidatedQuantity(quantity: number, stock: number): number {
    if (quantity < 1) {
      return 1;
    }
    return Math.min(quantity, stock);
  }

  private persist(items: CartItem[]): void {
    this.itemsSubject.next(items);
    localStorage.setItem(this.storageKey, JSON.stringify(items));
  }

  private loadItems(): CartItem[] {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw) as CartItem[];
      return parsed.filter((item) => item.quantity > 0);
    } catch {
      return [];
    }
  }
}

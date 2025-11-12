import { Product } from './product.model';

export interface CartItem {
  product: Product;
  quantity: number;
}

export interface CartTotals {
  subtotal: number;
  taxes: number;
  total: number;
}

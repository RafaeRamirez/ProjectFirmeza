export interface SaleRequest {
  customerId?: string;
  items: SaleItemRequest[];
}

export interface SaleItemRequest {
  productId: string;
  quantity: number;
}

export interface SaleResponse {
  id: string;
  total: number;
  createdAt: string;
}

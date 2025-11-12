export interface Product {
  id: string;
  name: string;
  unitPrice: number;
  stock: number;
  isActive: boolean;
}

export interface ProductQuery {
  search?: string;
  onlyAvailable?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

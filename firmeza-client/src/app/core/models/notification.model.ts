export type RequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface ProductRequestNotification {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  note?: string | null;
  status: RequestStatus;
  responseMessage?: string | null;
  requestedAt: string;
  processedAt?: string | null;
  saleId?: string | null;
}

export interface ProductRequestError {
  productId: string;
  message: string;
}

export interface ProductRequestBatchResponse {
  requests: ProductRequestNotification[];
  errors: ProductRequestError[];
}

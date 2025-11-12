import { firstValueFrom, filter } from 'rxjs';
import { CartService } from './cart.service';
import { Product } from '../models/product.model';

describe('CartService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('should calculate subtotal, taxes and total', async () => {
    const service = new CartService();
    const cement: Product = { id: '1', name: 'Cemento Portland', unitPrice: 120, stock: 50, isActive: true };
    const truck: Product = { id: '2', name: 'Mixer', unitPrice: 800, stock: 10, isActive: true };

    service.add(cement, 2);
    service.add(truck, 1);

    const totals = await firstValueFrom(
      service.totals$.pipe(filter((value) => value.total > 0))
    );

    expect(totals.subtotal).toBeCloseTo(1040, 2);
    expect(totals.taxes).toBeCloseTo(166.4, 2);
    expect(totals.total).toBeCloseTo(1206.4, 2);
  });
});

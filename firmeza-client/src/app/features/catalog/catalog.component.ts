import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.scss'
})
export class CatalogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  constructor(
    private readonly products: ProductService,
    private readonly cart: CartService,
    private readonly notifications: NotificationService
  ) {}

  loading = false;
  errorMessage = '';
  catalog: Product[] = [];

  filters = this.fb.group({
    search: [''],
    onlyAvailable: [true]
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.errorMessage = '';

    const params = {
      search: this.filters.value.search ?? undefined,
      onlyAvailable: this.filters.value.onlyAvailable ?? undefined,
      page: 1,
      pageSize: 30
    };

    this.products.search(params).subscribe({
      next: (response) => {
        this.loading = false;
        this.catalog = response.items;
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage =
          typeof error?.error === 'string' ? error.error : error?.message ?? 'No se pudo cargar el catálogo.';
      }
    });
  }

  clearFilters(): void {
    this.filters.reset({ search: '', onlyAvailable: true });
    this.loadProducts();
  }

  addToCart(product: Product): void {
    if (!product.isActive || product.stock <= 0) {
      return;
    }
    this.cart.add(product, 1);
    this.notifications.show({
      type: 'success',
      text: `${product.name} se agregó al carrito.`
    });
  }
}

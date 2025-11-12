import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router } from '@angular/router';
import { AppComponent } from './app.component';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { NotificationService } from './core/services/notification.service';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        { provide: AuthService, useValue: { authState$: of(null), logout: () => {} } },
        { provide: CartService, useValue: { totalItems$: of(0) } },
        { provide: NotificationService, useValue: { message$: of(null), clear: () => {} } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});

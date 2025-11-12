import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { UiMessage } from '../models/ui-message.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly messageSubject = new BehaviorSubject<UiMessage | null>(null);
  readonly message$ = this.messageSubject.asObservable();

  show(message: UiMessage): void {
    this.messageSubject.next(message);
  }

  clear(): void {
    this.messageSubject.next(null);
  }
}

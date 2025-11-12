export type UiMessageType = 'success' | 'info' | 'danger';

export interface UiMessage {
  type: UiMessageType;
  text: string;
}

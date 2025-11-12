export const environment = {
  production: true,
  /**
   * When the Angular container runs alongside the API within docker-compose
   * we can reference the service name directly.
   */
  apiBaseUrl: 'http://firmeza.api:8080/api',
  taxRate: 0.16
};

export const environment = {
  production: true,
  /**
   * When building locally we still default to the API running on localhost.
   * Use the docker configuration (see angular.json) to point to the compose network host.
   */
  apiBaseUrl: 'http://localhost:5053/api',
  taxRate: 0.16
};

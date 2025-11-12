# Firmeza Client (Angular)

SPA that powers the self-service customer portal for Firmeza. Clients can register, log in with JWT, browse the catalog, manage a persistent shopping cart, and confirm purchases that trigger the API email workflow.

## Available npm Scripts

| Command | Description |
| --- | --- |
| `npm start` | Runs `ng serve` (http://localhost:4200) using the development environment (API on `http://localhost:5053/api`). |
| `npm run build` | Production build that targets `dist/firmeza-client/browser`. |
| `npm run build -- --configuration docker` | Uses `environment.docker.ts` so the app can call the API through the docker-compose network (`http://firmeza.api:8080/api`). |
| `npm test` | Launches Karma + Jasmine unit tests (includes the cart total calculation test). |

## Environment Files
- `src/environments/environment.development.ts`: local dev defaults.
- `src/environments/environment.ts`: base production settings (still pointing to localhost).
- `src/environments/environment.docker.ts`: used inside the Docker image.

## Features
- Bootstrap 5 layout with responsive navigation, authenticated header, and global notifications.
- Login + registration forms with validation, helpful error messages, and automatic redirect to the catalog.
- Catalog with quick filters and add-to-cart buttons that honor stock availability.
- Cart view with quantity controls, subtotal/tax breakdown (16%), and checkout button that persists sales via the API and informs the user about the emailed receipt.
- JWT guard + interceptor ensure that only authenticated users access catalog/cart routes and every protected request carries the bearer token.

## Docker
The project ships a multi-stage `Dockerfile` plus an nginx config so it can be composed alongside the API:
```bash
docker build -t firmeza-client --build-arg NG_BUILD_CONFIGURATION=docker .
docker run -p 4200:80 firmeza-client
```

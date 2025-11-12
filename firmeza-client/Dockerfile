# Stage 1: build the Angular app
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
ARG NG_BUILD_CONFIGURATION=docker
RUN npm run build -- --configuration ${NG_BUILD_CONFIGURATION}

# Stage 2: serve with nginx
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/firmeza-client/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

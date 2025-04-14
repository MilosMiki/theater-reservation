FROM node:18-alpine

WORKDIR /app

# Copy package files first for better caching
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy application code
COPY . .

# Use host.docker.internal for Docker compatibility
ENV SERVICES_PLAYS=http://host.docker.internal:3000
ENV SERVICES_RESERVATIONS=http://host.docker.internal:5000
ENV SERVICES_USERS=http://host.docker.internal:8080

EXPOSE 4000

CMD ["node", "web-gateway.js"]
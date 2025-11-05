# Dockerfile.dev
FROM node:20-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm install

# Copy everything
COPY . .

# Environment variables (can be overridden at runtime)
ENV VITE_API_BASE_URL=http://ec2-54-147-56-151.compute-1.amazonaws.com
ENV VITE_API_PORT=8080

# Expose dev server port (Vite default 5173 or adjust as needed)
EXPOSE 5173

CMD ["npm", "run", "dev"]
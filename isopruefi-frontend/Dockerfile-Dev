# Use the latest LTS version of Node.js
FROM node:24-alpine
 
# Set the working directory inside the container
WORKDIR /app
 
# Copy package.json
COPY package.json .
 
# Install dependencies
RUN npm install
 
# Copy the rest of your application files
COPY . .

# Define the command to run your app
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]


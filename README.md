# Theater Reservation Microservices

## Overview

This project implements a theater reservation system using microservices. It consists of three independent services built following Clean Architecture principles. Each service handles a specific domain and operates independently, ensuring scalability, maintainability, and flexibility in technology choices.

## Microservices

### 1. Users Service (Authentication & Reservation History)

**Responsibilities:**

-   User registration and authentication
-   Secure token-based authentication
-   Managing user profiles
-   Fetching reservation history for authenticated users

**Exposed APIs (gRPC):**

-   `RegisterUser`: Register a new user
-   `AuthenticateUser`: Authenticate and obtain access tokens
-   `GetUserDetails`: Retrieve user details
-   `GetUserReservations`: Fetch reservation history of the logged-in user

**Note:** This service uses gRPC for communication.

### 2. Repertoire Service (Plays Management)

**Responsibilities:**

-   Storing and managing details of plays (title, duration, description, cast, etc.)
-   Allowing users to browse available plays
-   Enabling administrators to add, update, and delete plays
-   Providing availability details for each play

**Exposed APIs (REST):**

-   `GET /plays`: Fetch details of available plays
-   `GET /plays/{playId}/schedule`: Retrieve play schedules and availability
-   `POST /plays`: Create a new play (admin)
-   `PUT /plays/{playId}`: Update an existing play (admin)
-   `DELETE /plays/{playId}`: Delete a play (admin)

**Note:** This service uses a REST API for communication.

### 3. Reservation Service (Booking & Availability)

**Responsibilities:**

-   Handling seat reservations
-   Checking play availability before confirming reservations
-   Processing reservation requests asynchronously
-   Sending reservation confirmations to users
-   Broadcasting reservation events for other services (e.g., notifying the Users Service)

**Exposed APIs:**

-   `POST /reservations`: Reserve seats for a play
-   `DELETE /reservations/{reservationId}`: Cancel an existing reservation
-   `GET /reservations/{reservationId}`: Retrieve reservation details

**Inter-Service Communication:**

-   **Users ↔ Reservation:** The Users Service fetches reservation history from the Reservation Service.
-   **Users ↔ Repertoire:** The Users Service retrieves play details from the Repertoire Service.
-   **Repertoire ↔ Reservation:** The Reservation Service checks play availability before confirming reservations using REST API.
-   **Reservation ↔ Users:** The Reservation Service publishes reservation events, which the Users Service consumes via a message broker.
-   **Users ↔ Users:** The Users Service uses gRPC for its own internal communication.

## Frontend

A web application will be built for users to interact with the system. Users will be able to:

-   Register and log in
-   Browse available plays
-   Make reservations
-   View their reservation history

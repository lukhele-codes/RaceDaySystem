# RaceDay API Endpoint Plan

This plan covers every endpoint the Part 2 RESTful API (C#/.NET) will expose.
It is grouped by resource area as required: Authentication, User Profile,
Events, Categories, Event Enrolments, and Results.

Base route prefix: `/api`

---

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Registers a new user as either Organiser or Participant | None (public) | `{ "fullName": "string", "email": "string", "password": "string", "role": "Organiser \| Participant" }` | `201 Created` – `{ "userId": int, "email": "string", "role": "string" }`; `400 Bad Request` if email already exists or validation fails |
| POST | `/api/auth/login` | Authenticates a user and issues a JWT | None (public) | `{ "email": "string", "password": "string" }` | `200 OK` – `{ "token": "string", "userId": int, "role": "string", "expiresAt": "datetime" }`; `401 Unauthorized` on bad credentials |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the currently authenticated user's profile | Organiser, Participant | None | `200 OK` – `{ "userId", "fullName", "email", "role", "createdAt" }` |
| PUT | `/api/users/me` | Updates the current user's own profile details | Organiser, Participant | `{ "fullName": "string", "email": "string" }` | `200 OK` – updated profile object; `400 Bad Request` on invalid data |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Lists all upcoming events (browsable by anyone) | Organiser, Participant | None | `200 OK` – array of event summaries |
| GET | `/api/events/{id}` | Returns full detail for a single event, including its categories | Organiser, Participant | None | `200 OK` – event object with nested categories; `404 Not Found` |
| POST | `/api/events` | Creates a new event | Organiser | `{ "eventName", "eventDate", "location", "description" }` | `201 Created` – created event object; `400 Bad Request` |
| PUT | `/api/events/{id}` | Updates an existing event owned by the caller | Organiser (owner only) | `{ "eventName", "eventDate", "location", "description" }` | `200 OK` – updated event; `403 Forbidden` if not owner; `404 Not Found` |
| DELETE | `/api/events/{id}` | Deletes an event owned by the caller | Organiser (owner only) | None | `204 No Content`; `403 Forbidden`; `404 Not Found` |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Lists all categories for a given event | Organiser, Participant | None | `200 OK` – array of categories |
| POST | `/api/events/{eventId}/categories` | Adds a new category (e.g. "10km") to an event | Organiser (owner only) | `{ "categoryName", "distanceKm", "maxParticipants", "entryFee" }` | `201 Created` – created category; `403 Forbidden`; `404 Not Found` |
| PUT | `/api/categories/{id}` | Updates a category | Organiser (owner only) | `{ "categoryName", "distanceKm", "maxParticipants", "entryFee" }` | `200 OK`; `403 Forbidden`; `404 Not Found` |
| DELETE | `/api/categories/{id}` | Deletes a category | Organiser (owner only) | None | `204 No Content`; `403 Forbidden`; `404 Not Found` |

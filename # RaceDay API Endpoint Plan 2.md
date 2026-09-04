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

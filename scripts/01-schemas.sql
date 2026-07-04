-- Each microservice owns its own PostgreSQL schema inside the shared "schoolerp"
-- database (simplest topology for local/dev; production can split into one
-- database per service instead). No service is granted access to another's schema.
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS student;
CREATE SCHEMA IF NOT EXISTS staff;
CREATE SCHEMA IF NOT EXISTS attendance;
CREATE SCHEMA IF NOT EXISTS academic;
CREATE SCHEMA IF NOT EXISTS examination;
CREATE SCHEMA IF NOT EXISTS fee;
CREATE SCHEMA IF NOT EXISTS communication;
CREATE SCHEMA IF NOT EXISTS library;
CREATE SCHEMA IF NOT EXISTS transport;
CREATE SCHEMA IF NOT EXISTS notification;
CREATE SCHEMA IF NOT EXISTS reporting;

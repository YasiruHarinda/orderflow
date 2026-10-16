-- Runs once, when the PostgreSQL container starts with an empty data volume.
-- Database per service: Orders and Inventory never share tables.
CREATE DATABASE ordersdb;
CREATE DATABASE inventorydb;

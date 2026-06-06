-- ============================================================
-- Reset completo de la base de datos AgentSapienxa
-- Borra todas las tablas y el historial de migraciones EF.
-- Al reiniciar la app, las migraciones se aplican desde cero
-- y el seeder recrea los datos iniciales.
-- ============================================================

DROP TABLE IF EXISTS leads                CASCADE;
DROP TABLE IF EXISTS courses              CASCADE;
DROP TABLE IF EXISTS leads_enrollments    CASCADE;
DROP TABLE IF EXISTS payment_validations  CASCADE;
DROP TABLE IF EXISTS payment_methods      CASCADE;
DROP TABLE IF EXISTS conversation_history CASCADE;
DROP TABLE IF EXISTS agent_config           CASCADE;
DROP TABLE IF EXISTS admin_users            CASCADE;
DROP TABLE IF EXISTS sales_agents           CASCADE;
DROP TABLE IF EXISTS instructors            CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

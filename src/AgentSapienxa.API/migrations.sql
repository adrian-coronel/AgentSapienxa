CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE agent_config (
    id uuid NOT NULL,
    agent_key text NOT NULL,
    name text NOT NULL,
    description text,
    system_prompt text NOT NULL,
    model text NOT NULL,
    temperature numeric NOT NULL,
    max_tokens integer,
    memory_window integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_agent_config" PRIMARY KEY (id)
);

CREATE TABLE conversation_history (
    id uuid NOT NULL,
    session_id text NOT NULL,
    role text NOT NULL,
    content text NOT NULL,
    tool_calls text,
    tokens_in integer,
    tokens_out integer,
    model text,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_conversation_history" PRIMARY KEY (id)
);

CREATE TABLE instructors (
    id uuid NOT NULL,
    instructor_name text NOT NULL,
    email text,
    phone_number text,
    profile_picture text,
    expertise text,
    instructor_summary text,
    CONSTRAINT "PK_instructors" PRIMARY KEY (id)
);

CREATE TABLE leads_enrollments (
    id uuid NOT NULL,
    lead uuid NOT NULL,
    course uuid NOT NULL,
    status text NOT NULL,
    observation text,
    total_cost text,
    payment_method uuid,
    voucher text,
    sale_agent uuid,
    CONSTRAINT "PK_leads_enrollments" PRIMARY KEY (id)
);

CREATE TABLE payment_methods (
    id uuid NOT NULL,
    name text NOT NULL,
    description text,
    image text,
    limit_amount text NOT NULL,
    CONSTRAINT "PK_payment_methods" PRIMARY KEY (id)
);

CREATE TABLE payment_validations (
    id uuid NOT NULL,
    enrollment_id uuid NOT NULL,
    voucher_detail text,
    voucher_url text,
    status text NOT NULL,
    observation text,
    requested_by text,
    resolved_by text,
    requested_at timestamp with time zone NOT NULL,
    resolved_at timestamp with time zone,
    CONSTRAINT "PK_payment_validations" PRIMARY KEY (id)
);

CREATE TABLE sales_agents (
    id uuid NOT NULL,
    agent_name text NOT NULL,
    email text,
    phone_number text,
    lead_classification_summary text,
    CONSTRAINT "PK_sales_agents" PRIMARY KEY (id)
);

CREATE TABLE courses (
    id uuid NOT NULL,
    code text,
    title text NOT NULL,
    short_description text,
    features text,
    details text,
    syllabus text,
    projects text,
    link text,
    instructors uuid,
    cost text NOT NULL,
    places text,
    available_places text,
    start_date date,
    CONSTRAINT "PK_courses" PRIMARY KEY (id),
    CONSTRAINT "FK_courses_instructors_instructors" FOREIGN KEY (instructors) REFERENCES instructors (id) ON DELETE SET NULL
);

CREATE TABLE leads (
    id uuid NOT NULL,
    lead_name text,
    email text,
    phone_number text NOT NULL,
    contact_method text,
    status text NOT NULL,
    sales_agent uuid,
    CONSTRAINT "PK_leads" PRIMARY KEY (id),
    CONSTRAINT "FK_leads_sales_agents_sales_agent" FOREIGN KEY (sales_agent) REFERENCES sales_agents (id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_agent_config_agent_key" ON agent_config (agent_key);

CREATE INDEX "IX_conversation_history_session_id_created_at" ON conversation_history (session_id, created_at);

CREATE INDEX "IX_courses_instructors" ON courses (instructors);

CREATE UNIQUE INDEX "IX_leads_phone_number" ON leads (phone_number);

CREATE INDEX "IX_leads_sales_agent" ON leads (sales_agent);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260427223150_InitialMigration', '9.0.16');

CREATE TABLE admin_users (
    id uuid NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    password_hash text NOT NULL,
    role text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_admin_users" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX "IX_admin_users_email" ON admin_users (email);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260525010504_AddAdminUsers', '9.0.16');

ALTER TABLE leads_enrollments ALTER COLUMN total_cost TYPE numeric(18,2) USING total_cost::numeric;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260525015018_FixTotalCostToNumeric', '9.0.16');

COMMIT;


-- ============================================================
--  Seed Data — AgentSapienxa / Datapath
--  Ejecutar contra la BD después de aplicar migraciones.
--  Orden: instructors → courses → sales_agents → payment_methods → leads
-- ============================================================

-- ── Instructores ────────────────────────────────────────────
INSERT INTO instructors (id, instructor_name, email, phone_number, profile_picture, expertise, instructor_summary) VALUES
(
    'a1b2c3d4-0001-0000-0000-000000000001',
    'Carlos Mendoza',
    'carlos.mendoza@datapath.pe',
    '+51987001001',
    NULL,
    'Data Science, Machine Learning, Python',
    'Ingeniero de datos con 8 años de experiencia en empresas del sector financiero y retail. Docente certificado en Databricks y Google Cloud.'
),
(
    'a1b2c3d4-0001-0000-0000-000000000002',
    'Lucía Torres',
    'lucia.torres@datapath.pe',
    '+51987001002',
    NULL,
    'Desarrollo Web, React, Node.js, TypeScript',
    'Desarrolladora full-stack con experiencia en startups peruanas. Ha liderado equipos técnicos en proyectos de e-commerce y fintech.'
),
(
    'a1b2c3d4-0001-0000-0000-000000000003',
    'Rodrigo Castillo',
    'rodrigo.castillo@datapath.pe',
    '+51987001003',
    NULL,
    'Cloud AWS, DevOps, Infraestructura como Código',
    'AWS Solutions Architect certificado. Consultor de infraestructura cloud para empresas medianas en Latinoamérica.'
);

-- ── Cursos ──────────────────────────────────────────────────
INSERT INTO courses (id, code, title, short_description, features, details, syllabus, projects, link, instructors, cost, places, available_places, start_date) VALUES
(
    'b2c3d4e5-0002-0000-0000-000000000001',
    'DS-101',
    'Data Science con Python',
    'Aprende a analizar datos, crear modelos de machine learning y visualizar resultados usando Python y sus principales librerías.',
    '- 80 horas de contenido
- Clases en vivo los sábados
- Acceso de por vida a grabaciones
- Certificado al finalizar
- Mentoría personalizada',
    'Curso intensivo orientado a profesionales que quieren hacer la transición hacia el análisis de datos. No se requiere experiencia previa en programación.',
    'Módulo 1: Python para datos (NumPy, Pandas)
Módulo 2: Visualización (Matplotlib, Seaborn, Plotly)
Módulo 3: Machine Learning (Scikit-learn)
Módulo 4: Proyecto final con dataset real',
    'Proyecto 1: Análisis exploratorio de ventas retail
Proyecto 2: Modelo de predicción de churn bancario
Proyecto Final: Dashboard interactivo con insights de negocio',
    'https://datapath.pe/cursos/data-science-python',
    'a1b2c3d4-0001-0000-0000-000000000001',
    '890',
    '25',
    '8',
    '2026-06-07'
),
(
    'b2c3d4e5-0002-0000-0000-000000000002',
    'FW-201',
    'Desarrollo Web Full Stack',
    'Domina React en el frontend y Node.js con Express en el backend. Construye aplicaciones web completas listas para producción.',
    '- 120 horas de contenido
- Proyectos reales desde el primer módulo
- Preparación para entrevistas técnicas
- Comunidad privada de egresados
- Certificado verificable',
    'Programa diseñado para quienes buscan trabajar como desarrolladores web o mejorar sus habilidades actuales. Incluye despliegue en la nube.',
    'Módulo 1: HTML, CSS, JavaScript moderno (ES6+)
Módulo 2: React + TypeScript
Módulo 3: Node.js + Express + REST APIs
Módulo 4: Bases de datos (PostgreSQL + MongoDB)
Módulo 5: Despliegue (Vercel, Railway, Docker básico)',
    'Proyecto 1: Landing page responsive
Proyecto 2: CRUD con autenticación JWT
Proyecto Final: Aplicación full-stack desplegada en producción',
    'https://datapath.pe/cursos/fullstack-web',
    'a1b2c3d4-0001-0000-0000-000000000002',
    '1050',
    '20',
    '5',
    '2026-06-14'
),
(
    'b2c3d4e5-0002-0000-0000-000000000003',
    'CL-301',
    'Cloud en AWS para Desarrolladores',
    'Aprende a diseñar, desplegar y escalar aplicaciones en Amazon Web Services. Preparación incluida para la certificación AWS Cloud Practitioner.',
    '- 60 horas de contenido
- Laboratorios prácticos en AWS real
- Simulacros del examen de certificación
- Soporte por Discord
- Certificado del curso + guía para AWS CLF-C02',
    'Ideal para desarrolladores y administradores de sistemas que quieren adoptar la nube. Los laboratorios usan cuentas AWS reales con créditos incluidos.',
    'Módulo 1: Fundamentos de la nube y AWS
Módulo 2: Cómputo (EC2, Lambda, ECS)
Módulo 3: Almacenamiento (S3, RDS, DynamoDB)
Módulo 4: Redes y seguridad (VPC, IAM, CloudFront)
Módulo 5: Monitoreo y costos (CloudWatch, Cost Explorer)',
    'Lab 1: Despliegue de app web en EC2
Lab 2: API serverless con Lambda + API Gateway
Lab Final: Arquitectura de 3 capas en AWS',
    'https://datapath.pe/cursos/aws-developers',
    'a1b2c3d4-0001-0000-0000-000000000003',
    '750',
    '30',
    '12',
    '2026-06-21'
),
(
    'b2c3d4e5-0002-0000-0000-000000000004',
    'AI-401',
    'Inteligencia Artificial Aplicada',
    'Construye aplicaciones con IA usando LLMs, APIs de OpenAI y LangChain. Del prompt engineering al despliegue de agentes autónomos.',
    '- 70 horas de contenido
- Acceso a APIs de OpenAI durante el curso
- Proyectos con casos de uso reales de negocio
- Comunidad activa de egresados
- Certificado al finalizar',
    'Curso de vanguardia para profesionales que quieren integrar inteligencia artificial en sus productos y servicios. No requiere conocimientos previos de IA.',
    'Módulo 1: Fundamentos de LLMs y Prompt Engineering
Módulo 2: API de OpenAI (GPT-4, Embeddings, Whisper)
Módulo 3: LangChain y cadenas de razonamiento
Módulo 4: Agentes autónomos con herramientas
Módulo 5: RAG (Retrieval-Augmented Generation)',
    'Proyecto 1: Chatbot de atención al cliente
Proyecto 2: Buscador semántico con embeddings
Proyecto Final: Agente autónomo para automatización de tareas',
    'https://datapath.pe/cursos/ia-aplicada',
    'a1b2c3d4-0001-0000-0000-000000000001',
    '990',
    '25',
    '10',
    '2026-07-05'
);

-- ── Agentes de ventas ────────────────────────────────────────
INSERT INTO sales_agents (id, agent_name, email, phone_number, lead_classification_summary) VALUES
(
    'c3d4e5f6-0003-0000-0000-000000000001',
    'María Quispe',
    'maria.quispe@datapath.pe',
    '+51987002001',
    'Especialista en leads de data science y analytics. Alta tasa de cierre con perfiles de finanzas y administración.'
),
(
    'c3d4e5f6-0003-0000-0000-000000000002',
    'Jorge Huamán',
    'jorge.huaman@datapath.pe',
    '+51987002002',
    'Especialista en leads de desarrollo web y cloud. Enfocado en perfiles técnicos y recién egresados.'
);

-- ── Métodos de pago ──────────────────────────────────────────
INSERT INTO payment_methods (id, name, description, image, limit_amount) VALUES
(
    'd4e5f6a7-0004-0000-0000-000000000001',
    'BCP — Transferencia o Depósito',
    'Cuenta corriente en soles: 191-12345678-0-12. CCI: 002-191-00123456780-12. Titular: Datapath SAC. RUC: 20612345678.',
    NULL,
    '5000'
),
(
    'd4e5f6a7-0004-0000-0000-000000000002',
    'Yape',
    'Yape al número +51 987 000 100. Nombre: Datapath SAC. Indica el código del curso en el concepto.',
    NULL,
    '500'
),
(
    'd4e5f6a7-0004-0000-0000-000000000003',
    'Plin',
    'Plin al número +51 987 000 101. Nombre: Datapath SAC. Indica el código del curso en el concepto.',
    NULL,
    '500'
);

-- ── Leads de prueba ──────────────────────────────────────────
INSERT INTO leads (id, lead_name, email, phone_number, contact_method, status, sales_agent) VALUES
(
    'e5f6a7b8-0005-0000-0000-000000000002',
    'Sofía Ramírez',
    'sofia.ramirez@gmail.com',
    '+51911222333',
    'WhatsApp',
    'New',
    NULL
);

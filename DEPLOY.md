# Guía de Deploy - AgentSapienxa API

Esta documento contiene los pasos para deployar la aplicación AgentSapienxa API en un VPS usando Docker.

## Requisitos Previos

- Docker instalado en el VPS
- Docker Compose instalado
- Traefik corriendo en una red externa llamada `proxy-network`
- Certbot/Let's Encrypt configurado en Traefik
- Git instalado (para clonar el repositorio)

## Pasos de Deploy Manual

### 1. Preparar el VPS

```bash
# Conectarse al VPS
ssh user@tu-vps-ip

# Crear directorio para el proyecto
mkdir -p ~/projects
cd ~/projects

# Crear la red de Traefik si no existe
docker network create proxy-network 2>/dev/null || true
```

### 2. Clonar el Repositorio

```bash
# Clonar el proyecto
git clone https://github.com/tu-usuario/AgentSapienxa.git
cd AgentSapienxa

# Cambiar a rama main (o la rama de producción)
git checkout main
```

### 3. Configurar Variables de Entorno

```bash
# Copiar el archivo de ejemplo y editarlo
cp .env.example .env

# Editar el archivo con tus valores de producción
nano .env
```

**Variables críticas que DEBEN completarse:**

- `CONNECTION_STRING`: Connection string a tu base de datos PostgreSQL
- `OPENAI_API_KEY`: API Key de OpenRouter
- `META_VERIFY_TOKEN`: Token de verificación de webhook de Meta/WhatsApp
- `META_APP_SECRET`: App secret de Meta
- `META_PHONE_NUMBER_ID`: ID del número de teléfono de WhatsApp
- `META_ACCESS_TOKEN`: Access token de Meta

### 4. Construir y Ejecutar con Docker Compose

```bash
# Construir la imagen (primera vez)
docker-compose -f docker-compose.prod.yml build

# Levantar el servicio
docker-compose -f docker-compose.prod.yml up -d

# Verificar que está corriendo
docker-compose -f docker-compose.prod.yml ps

# Ver logs
docker-compose -f docker-compose.prod.yml logs -f agentsapienxa-api
```

### 5. Verificar Que Todo Funciona

```bash
# Esperar 10-15 segundos a que el servicio inicie
sleep 15

# Verificar health check (esto debería responder 200)
curl -i http://localhost:8080/health

# O desde el VPS con el dominio (si Traefik está configurado)
curl -i https://api.dludena.digital/health
```

## Operaciones Comunes

### Detener el Servicio

```bash
docker-compose -f docker-compose.prod.yml down
```

### Reiniciar el Servicio

```bash
docker-compose -f docker-compose.prod.yml restart agentsapienxa-api
```

### Ver Logs en Tiempo Real

```bash
docker-compose -f docker-compose.prod.yml logs -f agentsapienxa-api
```

### Ejecutar Comandos Dentro del Contenedor

```bash
# Acceder a la shell del contenedor
docker-compose -f docker-compose.prod.yml exec agentsapienxa-api /bin/bash

# O ejecutar un comando directo
docker-compose -f docker-compose.prod.yml exec agentsapienxa-api dotnet --version
```

## Actualizar a Última Versión

```bash
cd ~/projects/AgentSapienxa

# Obtener cambios del repositorio
git pull origin main

# Reconstruir la imagen
docker-compose -f docker-compose.prod.yml build --no-cache

# Reiniciar el servicio
docker-compose -f docker-compose.prod.yml up -d

# Verificar que está funcionando
docker-compose -f docker-compose.prod.yml logs -f agentsapienxa-api
```

## Troubleshooting

### El servicio no inicia

```bash
# Ver logs detallados
docker-compose -f docker-compose.prod.yml logs agentsapienxa-api

# Verificar que las variables de entorno están correctas
docker-compose -f docker-compose.prod.yml config
```

### Error de conexión a base de datos

- Verificar que la `CONNECTION_STRING` en `.env` es correcta
- Asegurarse de que la base de datos está corriendo y accesible
- Verificar firewall y reglas de seguridad

### Health check fallando

- Verificar que el puerto 8080 está accesible internamente
- Revisar logs del contenedor
- Asegurarse de que la aplicación tiene un endpoint `/health`

### Traefik no redirige al servicio

- Verificar que la red `proxy-network` existe: `docker network ls | grep proxy-network`
- Verificar labels en el docker-compose.prod.yml
- Ver logs de Traefik para errores de configuración

## Backup

### Backup de Base de Datos

```bash
# Crear backup con pg_dump (si usas PostgreSQL)
docker exec <postgres-container-id> pg_dump -U postgres agentsapienxa > backup.sql

# O desde dentro de la aplicación mediante script de respaldo
```

### Backup del Directorio .env

```bash
# El .env está en .gitignore, así que guarda una copia segura
cp .env ~/.backups/agentsapienxa-.env.backup.$(date +%Y%m%d)
```

## Monitoreo y Mantenimiento

### Limpiar Docker

```bash
# Limpiar imágenes no usadas
docker image prune -a

# Limpiar volúmenes no usados
docker volume prune

# Ver tamaño de imágenes
docker images --format "{{.Repository}}:{{.Tag}}\t{{.Size}}"
```

### Ver Recursos de Contenedores

```bash
docker stats agentsapienxa-api
```

## Configuración de CI/CD (Futuro)

Una vez que todo esté funcionando, se recomienda:

1. Configurar un workflow de GitHub Actions para builds automáticos
2. Implementar push automático de imágenes a Docker Hub o GitHub Container Registry
3. Automatizar el deploy en el VPS al hacer push a `main`

## Contacto y Soporte

Para problemas o preguntas, contactar al equipo de DevOps.

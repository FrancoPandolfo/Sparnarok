# ⚔️ Sparnarok

**Sparnarok** es un gestor de flujos de trabajo gamificado que reemplaza el tradicional tablero Kanban por un sistema de progresión basado en mecánicas de juegos de rol (RPG). Convierte las tareas rutinarias en *Quests*, permitiendo a los usuarios ganar experiencia (XP) y subir de nivel en disciplinas técnicas a través de un árbol de habilidades.

## 📁 Estructura del Proyecto

El proyecto está diseñado bajo una arquitectura modular y orientada a dominio (DDD ligero).

```text
Sparnarok/
├── .github/workflows/    # Pipelines de CI (Validación PRs y E2E)
├── Backend/              # Solución .NET (C#)
│   ├── Sparnarok.Api/
│   ├── Sparnarok.Core/
│   ├── Sparnarok.Application/
│   ├── Sparnarok.Infrastructure/
│   ├── Sparnarok.Tests.Unit/
│   └── Sparnarok.Tests.E2E/
└── Frontend/             # App de React (Vite + TypeScript)
    └── src/
```

## 🛠️ Requisitos Previos

- **Docker** y **Docker Compose** (para levantar PostgreSQL fácilmente).
- **.NET SDK 8.0** o superior.
- **Node.js 20.x** o superior y **npm**.

## 🚀 Entorno de Desarrollo Local

### 1. Levantar la Base de Datos
Puedes levantar una instancia de PostgreSQL utilizando Docker.
```bash
docker run --name sparnarok-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=password -e POSTGRES_DB=sparnarok -p 5432:5432 -d postgres:15
```

### 2. Iniciar el Backend (API)
Navega a la carpeta del host de la API y arranca la aplicación. La API conectará con la base de datos (asegúrate de que los strings de conexión en `appsettings.json` apunten a tu localhost).
```bash
cd Backend/Sparnarok.Api
dotnet run
```

### 3. Iniciar el Frontend (UI)
Navega a la carpeta del frontend, instala las dependencias y arranca el servidor de Vite.
```bash
cd Frontend
npm install
npm run dev
```

## 🧪 Ejecución de Pruebas

### Tests Unitarios (Backend)
```bash
cd Backend
dotnet test Sparnarok.Tests.Unit/Sparnarok.Tests.Unit.csproj
```

### Tests E2E (Playwright - Backend)
Si necesitas instalar los navegadores primero:
```bash
cd Backend/Sparnarok.Tests.E2E
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install
```
Ejecutar E2E:
```bash
cd Backend
dotnet test Sparnarok.Tests.E2E/Sparnarok.Tests.E2E.csproj
```

### Tests de Componente (Frontend)
```bash
cd Frontend
npm run test
```

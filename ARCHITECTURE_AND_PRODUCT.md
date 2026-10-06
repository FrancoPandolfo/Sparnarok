# 🏛️ Architecture and Product Vision (Sparnarok)

Este documento define los lineamientos estratégicos de producto, la experiencia de usuario (UX) y la escalabilidad técnica para convertir Sparnarok en un SaaS exitoso.

## 🎨 1. Experiencia de Usuario (UI/UX) y Dinamismo
Sparnarok no es una herramienta de gestión tradicional; es un juego. La interfaz debe transmitir progreso, recompensa y fluidez.

- **Stack Visual Frontend**:
  - **Tailwind CSS**: Para utilidad y consistencia de diseño ágil.
  - **Shadcn/UI**: Como base de componentes accesibles y altamente personalizables.
  - **Framer Motion**: Obligatorio para animaciones de estado (ganancia de XP, level-ups, micro-interacciones al completar una Quest).
- **Optimistic Updates**: Todas las interacciones principales (ej. cambiar el estado de una Quest a Completada) deben reflejarse inmediatamente en el estado del cliente (UI) asumiendo éxito, revertiendo los cambios solo si el servidor falla. Esto garantiza una sensación de inmediatez y ritmo de juego.

## 🏢 2. Estructura para Monetización (SaaS B2B/B2C)
El sistema debe estar diseñado para escalar financieramente y organizativamente desde el día uno.

- **Arquitectura Multi-Tenant**: Absolutamente todas las entidades principales (Quests, Skills, Users, Rewards) deben estar aisladas por un `WorkspaceId` o `TenantId`.
  - El Backend debe asegurar el filtrado de datos por Tenant utilizando **Global Query Filters** en Entity Framework Core.
- **Modelo Freemium**: 
  - **Capa Gratuita**: Diseñar la base para establecer límites (ej. 5 usuarios por Workspace, 50 Quests activas mensuales).
  - La arquitectura debe soportar *Feature Flags* para habilitar funcionalidades Premium basadas en el plan del Tenant.
  - Módulos Aislados: Preparar el terreno para integraciones de pago (Stripe/Paddle) aislando toda lógica de facturación en un `BillingService` o módulo de dominio separado.

## ⚙️ 3. Escalabilidad del Backend
Para soportar múltiples tenants y cálculos complejos sin degradar el rendimiento:

- **Performance y Lecturas**:
  - Toda lista (Quests, Rank de Usuarios, Skills) debe implementar **Paginación obligatoria** por defecto. No devolver colecciones enteras.
  - Arquitectura orientada a **Caché (Redis)** para endpoints costosos, particularmente la renderización del Árbol de Habilidades y cálculos totales de nivel del usuario.
- **Validación y Robustez**:
  - Inyección de dependencias estricta.
  - Aislamiento de las entidades de DB usando **DTOs fuertemente tipados**.
  - **Validación de entrada agresiva**: Utilizar `FluentValidation` en la capa de Aplicación/API antes de procesar reglas de negocio o tocar la base de datos.

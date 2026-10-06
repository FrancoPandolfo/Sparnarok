# 🤖 Reglas e Instrucciones para Agentes de IA

Si eres un asistente de IA (ej. Gemini, Copilot, Cursor, Claude) ayudando a desarrollar **Sparnarok**, debes adherirte estrictamente a las siguientes directivas de arquitectura, calidad y flujo de trabajo:

## 1. Arquitectura y Código Limpio (Backend C#)
- **DDD Ligero**: Mantén la separación de responsabilidades. `Sparnarok.Core` nunca debe tener referencias a Entity Framework ni a la capa web. La lógica de negocio pura va en *Core*, los flujos de orquestación en *Application*, persistencia en *Infrastructure* y el host/controladores en *Api*.
- **Inyección de Dependencias**: Usa la abstracción a través de interfaces (ej. `IQuestService`).
- **Nombres Claros y Asíncronos**: Usa el sufijo `Async` para métodos que retornen `Task` o `Task<T>`. 

## 2. Internacionalización Obligatoria (Frontend React)
- **¡No uses textos hardcodeados!** Todo texto renderizado en la interfaz debe ser extraído utilizando el hook `useTranslation()` de `react-i18next`.
- Al agregar una nueva funcionalidad, debes actualizar los archivos JSON de traducciones (`Frontend/src/locales/en.json` y `es.json`) en la misma iteración.

## 3. Pruebas y Cobertura (TDD / BDD)
- **Unit & Component Tests**: Por cada nueva funcionalidad o componente agregado, DEBES crear o actualizar los tests correspondientes en xUnit (Backend) y Vitest/RTL (Frontend). Usa patrones como *Arrange-Act-Assert*.
- **Mocks**: En el backend, mockea repositorios o dependencias externas utilizando Moq o in-memory databases. En el frontend, utiliza proveedores falsos (ej. `I18nextProvider`) para testear estado.
- **End-to-End (E2E)**: Si tu cambio afecta el flujo transversal de la aplicación (ej. flujo de Login, completar una Quest), debes sugerir o escribir directamente los tests correspondientes en `Sparnarok.Tests.E2E` con Playwright.

## 4. Mantenimiento del Proyecto
- **Actualización Documental**: Si añades un nuevo comando, variable de entorno obligatoria o cambias drásticamente la infraestructura, debes actualizar el `README.md` y `.env.example` según corresponda.
- Evita incluir configuraciones locales o secretos reales en los repositorios.

## Resumen del Comportamiento Esperado
*Evalúa el impacto de tu código, aísla la lógica, pruébalo automáticamente y tradúcelo. La calidad del código en Sparnarok debe ser Legendaria (Rango S).*

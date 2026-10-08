---
description: Obligatory pre-push checklist for all frontend and backend code
---

# Pre-Push Validation Rule

Whenever you are about to commit and push code, you MUST always run the following verifications locally to ensure the CI/CD pipeline does not break:

1. **Frontend (Vite/TypeScript/React)**:
   Always run these commands inside the `Frontend` directory:
   - `npm run test -- --run`
   - `npm run lint`
   - `npm run build` (or `tsc -b && vite build`)

2. **Backend (.NET/xUnit)**:
   Always run these commands inside the `Backend` directory:
   - `dotnet test`
   - `dotnet build`

Do NOT push code until all these checks pass successfully with 0 errors.

export const getTitleForLevel = (level: number): string => {
  if (level <= 4) return "Junior / Neófito";
  if (level <= 9) return "Semi-Senior / Adepto";
  if (level <= 14) return "Senior / Veterano";
  if (level <= 19) return "Staff / Maestro";
  return "Principal / Leyenda";
};

export const SENIORITY_PRESETS = {
  Trainee: { Backend: 100, Frontend: 100, Arquitectura: 0, DevOps: 0 },
  Junior: { Backend: 400, Frontend: 400, Arquitectura: 0, DevOps: 100 },
  SemiSenior: { Backend: 900, Frontend: 800, Arquitectura: 200, DevOps: 300 },
  Senior: { Backend: 1500, Frontend: 1200, Arquitectura: 800, DevOps: 600 },
  Staff: { Backend: 2500, Frontend: 2000, Arquitectura: 1500, DevOps: 1200 }
};

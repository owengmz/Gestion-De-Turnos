-- ============================================================
-- Datos de prueba - Gestor de Turnos Medicos
-- Requisitos: schema.sql ejecutado y la aplicacion iniciada una vez
-- (crea los usuarios). Se puede ejecutar mas de una vez sin duplicar.
-- Orden: obras sociales, pacientes, especialidades, profesionales,
-- horarios de atencion y turnos.
-- ============================================================

-- ---------- Obras sociales (5) ----------
INSERT INTO obra_social (nombre, descripcion, estado)
SELECT v.nombre, v.descripcion, true
FROM (VALUES
  ('OSDE', 'Cobertura prepaga nacional'),
  ('Swiss Medical', 'Cobertura prepaga premium'),
  ('Galeno', 'Cobertura prepaga regional'),
  ('PAMI', 'Cobertura para jubilados'),
  ('OSEP', 'Obra social provincial')
) AS v(nombre, descripcion)
WHERE NOT EXISTS (
  SELECT 1 FROM obra_social o WHERE lower(o.nombre) = lower(v.nombre)
);

-- ---------- Pacientes (11) ----------
INSERT INTO paciente
  (dni, nombre, apellido, fecha_nacimiento, telefono, email, direccion, numero_afiliado, estado, obra_social_id)
SELECT
  v.dni, v.nombre, v.apellido, v.fecha_nacimiento, v.telefono, v.email, v.direccion,
  v.numero_afiliado, v.estado,
  (SELECT o.id FROM obra_social o WHERE lower(o.nombre) = lower(v.obra))
FROM (VALUES
  ('40000001', 'Lucia',    'Fernandez', DATE '1985-03-12', '2664100001', 'lucia.fernandez@correo.com',  'Av. Illia 120, San Luis',      'OS-0001', true,  'OSDE'),
  ('40000002', 'Martin',   'Gomez',     DATE '1990-07-25', '2664100002', 'martin.gomez@correo.com',     'Rivadavia 450, San Luis',      'SW-0002', true,  'Swiss Medical'),
  ('40000003', 'Carolina', 'Diaz',      DATE '1978-11-02', '2664100003', 'carolina.diaz@correo.com',    'San Martin 88, Juana Koslay',  'GA-0003', true,  'Galeno'),
  ('40000004', 'Roberto',  'Sosa',      DATE '1950-01-30', '2664100004', NULL,                          'Colon 310, San Luis',          'PA-0004', true,  'PAMI'),
  ('40000005', 'Elena',    'Ruiz',      DATE '1948-09-14', '2664100005', 'elena.ruiz@correo.com',       'Lavalle 75, San Luis',         'PA-0005', true,  'PAMI'),
  ('40000006', 'Diego',    'Morales',   DATE '1995-05-19', '2664100006', 'diego.morales@correo.com',    'Belgrano 640, La Punta',       'EP-0006', true,  'OSEP'),
  ('40000007', 'Sofia',    'Herrera',   DATE '2001-12-08', '2664100007', 'sofia.herrera@correo.com',    'Pringles 205, San Luis',       'OS-0007', true,  'OSDE'),
  ('40000008', 'Javier',   'Castro',    DATE '1982-04-22', '2664100008', NULL,                          'Mitre 150, San Luis',          'EP-0008', false, 'OSEP'),
  ('40000009', 'Valeria',  'Acosta',    DATE '1992-08-03', '2664100009', 'valeria.acosta@correo.com',   'Ejercito de los Andes 90',     NULL,      true,  NULL),
  ('40000010', 'Pablo',    'Navarro',   DATE '1987-06-17', '2664100010', 'pablo.navarro@correo.com',    'Sarmiento 330, San Luis',      NULL,      true,  NULL),
  ('40000011', 'Nicolas',  'Gomez',     DATE '1999-02-10', '2664100011', 'nicolas.gomez@correo.com',    'Ayacucho 410, San Luis',       NULL,      true,  NULL)
) AS v(dni, nombre, apellido, fecha_nacimiento, telefono, email, direccion, numero_afiliado, estado, obra)
WHERE NOT EXISTS (SELECT 1 FROM paciente p WHERE p.dni = v.dni);

-- ---------- Especialidades (6) ----------
INSERT INTO especialidad (nombre, descripcion, estado)
SELECT v.nombre, v.descripcion, true
FROM (VALUES
  ('Clinica medica', 'Atencion general del adulto'),
  ('Pediatria', 'Atencion de ninos y adolescentes'),
  ('Cardiologia', 'Corazon y sistema circulatorio'),
  ('Traumatologia', 'Huesos, articulaciones y lesiones'),
  ('Dermatologia', 'Enfermedades de la piel'),
  ('Ginecologia', 'Salud de la mujer')
) AS v(nombre, descripcion)
WHERE NOT EXISTS (
  SELECT 1 FROM especialidad e WHERE e.nombre = v.nombre
);

-- ---------- Profesionales (12, dos por especialidad) ----------
INSERT INTO profesional (nombre, apellido, dni, matricula, telefono, email, estado, especialidad_id)
SELECT v.nombre, v.apellido, v.dni, v.matricula, v.telefono, v.email, true, e.id
FROM (VALUES
  ('Laura',   'Fernandez', '30111001', 'MP-1001', '2664100001', 'laura.fernandez@clinica.test',   'Clinica medica'),
  ('Martin',  'Gomez',     '30111002', 'MP-1002', '2664100002', 'martin.gomez@clinica.test',      'Clinica medica'),
  ('Sofia',   'Ramirez',   '30111003', 'MP-1003', '2664100003', 'sofia.ramirez@clinica.test',     'Pediatria'),
  ('Diego',   'Torres',    '30111004', 'MP-1004', '2664100004', 'diego.torres@clinica.test',      'Pediatria'),
  ('Carlos',  'Herrera',   '30111005', 'MP-1005', '2664100005', 'carlos.herrera@clinica.test',    'Cardiologia'),
  ('Valeria', 'Molina',    '30111006', 'MP-1006', '2664100006', 'valeria.molina@clinica.test',    'Cardiologia'),
  ('Pablo',   'Suarez',    '30111007', 'MP-1007', '2664100007', 'pablo.suarez@clinica.test',      'Traumatologia'),
  ('Andrea',  'Castro',    '30111008', 'MP-1008', '2664100008', 'andrea.castro@clinica.test',     'Traumatologia'),
  ('Julieta', 'Romero',    '30111009', 'MP-1009', '2664100009', 'julieta.romero@clinica.test',    'Dermatologia'),
  ('Facundo', 'Ortiz',     '30111010', 'MP-1010', '2664100010', 'facundo.ortiz@clinica.test',     'Dermatologia'),
  ('Mariana', 'Acosta',    '30111011', 'MP-1011', '2664100011', 'mariana.acosta@clinica.test',    'Ginecologia'),
  ('Lucia',   'Benitez',   '30111012', 'MP-1012', '2664100012', 'lucia.benitez@clinica.test',     'Ginecologia')
) AS v(nombre, apellido, dni, matricula, telefono, email, especialidad)
JOIN especialidad e ON e.nombre = v.especialidad
WHERE NOT EXISTS (
  SELECT 1 FROM profesional p WHERE p.dni = v.dni
);

-- ---------- Horarios de atencion: lunes a viernes de 8 a 12 ----------
INSERT INTO horario_atencion (dia_semana, hora_desde, hora_hasta, estado, profesional_id)
SELECT dia.dia_semana, '08:00'::time, '12:00'::time, true, p.id
FROM profesional p
CROSS JOIN (
  VALUES ('Lunes'), ('Martes'), ('Miercoles'), ('Jueves'), ('Viernes')
) AS dia(dia_semana)
WHERE p.estado = true
  AND NOT EXISTS (
    SELECT 1 FROM horario_atencion h
    WHERE h.profesional_id = p.id
      AND h.dia_semana = dia.dia_semana
  );

-- ---------- Turnos (21) ----------
-- Fechas relativas a hoy, corridas a dia habil, dentro del horario 8 a 12.
-- Requiere al menos un usuario (se usa el de menor id como creador).
WITH usr AS (
  SELECT min(id) AS id FROM usuario
),
datos (pac_dni, prof_dni, dias, hora, motivo, estado) AS (
  VALUES
  -- Fernandez: 5 turnos
  ('40000001', '30111001', -20, time '09:00', 'Control general',        'Atendido'),
  ('40000001', '30111005', -12, time '10:30', 'Control cardiologico',   'Atendido'),
  ('40000001', '30111001',  -5, time '09:30', 'Seguimiento',            'Cancelado'),
  ('40000001', '30111005',   3, time '11:00', 'Resultados de estudios', 'Pendiente'),
  ('40000001', '30111001',  10, time '09:00', 'Control general',        'Pendiente'),
  -- Gomez Martin: 4 turnos
  ('40000002', '30111003', -15, time '08:30', 'Control pediatrico',     'Atendido'),
  ('40000002', '30111004',  -7, time '09:00', 'Vacunacion',             'Atendido'),
  ('40000002', '30111003',   2, time '10:00', 'Control pediatrico',     'Pendiente'),
  ('40000002', '30111004',   9, time '08:30', 'Consulta por fiebre',    'Pendiente'),
  -- Diaz: 3 turnos
  ('40000003', '30111007', -10, time '10:00', 'Dolor de rodilla',       'Atendido'),
  ('40000003', '30111008',   4, time '10:30', 'Control de fractura',    'Pendiente'),
  ('40000003', '30111007',  11, time '11:00', 'Rehabilitacion',         'Pendiente'),
  -- Sosa: 3 turnos
  ('40000004', '30111009',  -9, time '10:00', 'Consulta por alergia',   'Atendido'),
  ('40000004', '30111010',  -3, time '10:30', 'Control de lunares',     'Cancelado'),
  ('40000004', '30111009',   6, time '11:30', 'Control de lunares',     'Pendiente'),
  -- Ruiz: 2 turnos
  ('40000005', '30111011', -14, time '09:00', 'Control anual',          'Atendido'),
  ('40000005', '30111012',   8, time '09:30', 'Control anual',          'Pendiente'),
  -- Morales: 2 turnos
  ('40000006', '30111002',  -6, time '11:00', 'Dolor de cabeza',        'Atendido'),
  ('40000006', '30111002',   5, time '11:30', 'Seguimiento',            'Pendiente'),
  -- Herrera: 1 turno
  ('40000007', '30111006',   7, time '09:30', 'Control de presion',     'Pendiente'),
  -- Castro: 1 turno
  ('40000008', '30111004', -18, time '11:00', 'Control pediatrico',     'Atendido')
  -- Acosta, Navarro y Gomez Nicolas: sin turnos (informe de pacientes sin turnos)
),
calc AS (
  SELECT d.*,
         (current_date + d.dias) AS base
  FROM datos d
),
final AS (
  SELECT c.*,
         c.base + CASE extract(dow FROM c.base)::int
                    WHEN 6 THEN 2
                    WHEN 0 THEN 1
                    ELSE 0
                  END AS fecha
  FROM calc c
)
INSERT INTO turno (fecha, hora, motivo, estado, observaciones, paciente_id, profesional_id,
                   usuario_creador_id, fecha_creacion)
SELECT f.fecha, f.hora, f.motivo, f.estado, NULL, pa.id, pro.id, usr.id, now()
FROM final f
JOIN paciente pa ON pa.dni = f.pac_dni
JOIN profesional pro ON pro.dni = f.prof_dni
CROSS JOIN usr
WHERE usr.id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM turno t
    WHERE t.paciente_id = pa.id
      AND t.profesional_id = pro.id
      AND t.fecha = f.fecha
      AND t.hora = f.hora
  );
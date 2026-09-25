-- ============================================================
-- GESTOR DE TURNOS MEDICOS - Esquema de base de datos (PostgreSQL)
-- Convenciones: snake_case singular, <entidad>_id como FK,
-- enums como varchar con check constraint, fechas de auditoria
-- con DEFAULT now()
-- ============================================================

-- ---------- Roles y usuarios (reemplaza tablas de Identity) ----------

CREATE TABLE rol (
    id              serial PRIMARY KEY,
    nombre          varchar(50) NOT NULL UNIQUE   -- Administrador, Recepcionista, Profesional
);

CREATE TABLE usuario (
    id                  serial PRIMARY KEY,
    nombre              varchar(100) NOT NULL,
    apellido            varchar(100) NOT NULL,
    email               varchar(150) NOT NULL UNIQUE,
    password_hash       varchar(300) NOT NULL,
    avatar_path         varchar(300),
    activo              boolean NOT NULL DEFAULT true,
    fecha_creacion      timestamp NOT NULL DEFAULT now(),
    fecha_ultimo_acceso timestamp
);

CREATE TABLE usuario_rol (
    usuario_id  int NOT NULL REFERENCES usuario(id) ON DELETE CASCADE,
    rol_id      int NOT NULL REFERENCES rol(id) ON DELETE RESTRICT,
    PRIMARY KEY (usuario_id, rol_id)
);

-- ---------- Catalogos ----------

CREATE TABLE especialidad (
    id          serial PRIMARY KEY,
    nombre      varchar(100) NOT NULL,
    descripcion varchar(300),
    estado      boolean NOT NULL DEFAULT true
);

CREATE TABLE consultorio (
    id          serial PRIMARY KEY,
    numero      varchar(20) NOT NULL,
    piso        varchar(20),
    descripcion varchar(300),
    estado      boolean NOT NULL DEFAULT true
);

CREATE TABLE obra_social (
    id          serial PRIMARY KEY,
    nombre      varchar(100) NOT NULL,
    descripcion varchar(300),
    estado      boolean NOT NULL DEFAULT true
);

-- ---------- Entidades principales ----------

-- Tabla fija segun definicion del equipo (Nehuen) - no modificar
CREATE TABLE profesional (
    id              serial PRIMARY KEY,
    nombre          varchar(100) NOT NULL,
    apellido        varchar(100) NOT NULL,
    dni             varchar(20) NOT NULL UNIQUE,
    matricula       varchar(50) NOT NULL UNIQUE,
    telefono        varchar(50),
    email           varchar(150),
    estado          boolean NOT NULL DEFAULT true,
    especialidad_id int NOT NULL REFERENCES especialidad(id) ON DELETE RESTRICT
);

-- Tabla fija segun definicion del equipo (Nehuen) - no modificar
CREATE TABLE paciente (
    id               serial PRIMARY KEY,
    dni              varchar(20) NOT NULL UNIQUE,
    nombre           varchar(100) NOT NULL,
    apellido         varchar(100) NOT NULL,
    fecha_nacimiento date,
    telefono         varchar(50),
    email            varchar(150),
    direccion        varchar(300),
    numero_afiliado  varchar(50),
    estado           boolean NOT NULL DEFAULT true,
    obra_social_id   int REFERENCES obra_social(id) ON DELETE RESTRICT
);

-- Tabla fija segun definicion del equipo (Nehuen) - no modificar
CREATE TABLE horario_atencion (
    id              serial PRIMARY KEY,
    dia_semana      varchar(20) NOT NULL
        CHECK (dia_semana IN ('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')),
    hora_desde      time NOT NULL,
    hora_hasta      time NOT NULL,
    estado          boolean NOT NULL DEFAULT true,
    profesional_id  int NOT NULL REFERENCES profesional(id) ON DELETE CASCADE,
    CHECK (hora_hasta > hora_desde)
);

-- Tabla fija segun definicion del equipo (Nehuen) - no modificar
CREATE TABLE turno (
    id                      serial PRIMARY KEY,
    fecha                   date NOT NULL,
    hora                    time NOT NULL,
    motivo                  varchar(300),
    estado                  varchar(20) NOT NULL DEFAULT 'Pendiente'
        CHECK (estado IN ('Pendiente','Confirmado','Atendido','Cancelado','Ausente')),
    observaciones           varchar(500),
    paciente_id             int NOT NULL REFERENCES paciente(id) ON DELETE RESTRICT,
    profesional_id          int NOT NULL REFERENCES profesional(id) ON DELETE RESTRICT,
    consultorio_id          int NOT NULL REFERENCES consultorio(id) ON DELETE RESTRICT,
    usuario_creador_id      int NOT NULL REFERENCES usuario(id) ON DELETE RESTRICT,
    usuario_modificador_id  int REFERENCES usuario(id) ON DELETE RESTRICT,
    fecha_creacion          timestamp NOT NULL DEFAULT now(),
    fecha_modificacion      timestamp
);

-- Tabla fija segun definicion del equipo (Nehuen) - no modificar
CREATE TABLE pago (
    id                  serial PRIMARY KEY,
    monto               numeric(10,2) NOT NULL,
    fecha               timestamp NOT NULL DEFAULT now(),
    concepto            varchar(200) NOT NULL,
    estado              varchar(20) NOT NULL DEFAULT 'Registrado'
        CHECK (estado IN ('Registrado','Anulado')),
    turno_id            int NOT NULL REFERENCES turno(id) ON DELETE RESTRICT,
    usuario_creador_id  int NOT NULL REFERENCES usuario(id) ON DELETE RESTRICT,
    usuario_anulador_id int REFERENCES usuario(id) ON DELETE RESTRICT
);

-- ---------- Indices para las busquedas via AJAX ----------

CREATE INDEX idx_paciente_dni ON paciente(dni);
CREATE INDEX idx_paciente_apellido ON paciente(apellido);
CREATE INDEX idx_profesional_apellido ON profesional(apellido);
CREATE INDEX idx_turno_fecha ON turno(fecha);
CREATE INDEX idx_turno_estado ON turno(estado);

-- ---------- Roles iniciales ----------

INSERT INTO rol (nombre) VALUES ('Administrador'), ('Recepcionista'), ('Profesional');

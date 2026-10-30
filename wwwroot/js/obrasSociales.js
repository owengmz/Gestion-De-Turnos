// aplicacion Vue de obras sociales
const { createApp } = Vue;

createApp({
  // estado de la pagina
  data() {
    return {
      registros: [],
      buscar: '',
      pagina: 1,
      registrosPorPagina: 3,
      totalRegistros: 0,
      totalPaginas: 0,
      tienePaginaAnterior: false,
      tienePaginaSiguiente: false,
      cargando: false,
      error: '',
      mensajeOk: '',
      temporizador: null,
      // formulario del modal (id 0 = alta)
      modalAbierto: false,
      form: { id: 0, nombre: '', descripcion: '' },
      errorForm: '',
      guardando: false,
      // lo define la vista segun el rol del usuario
      esAdmin: window.esAdmin === true
    };
  },

  // primera carga al abrir la pagina
  mounted() {
    this.cargar();
  },

  methods: {
    // pide la pagina actual al servidor
    async cargar() {
      this.cargando = true;
      this.error = '';

      try {
        const params = new URLSearchParams({
          buscar: this.buscar,
          pagina: this.pagina,
          registrosPorPagina: this.registrosPorPagina
        });

        const resp = await fetch('/ObrasSociales/Listar?' + params.toString());
        if (!resp.ok) {
          throw new Error('No se pudo cargar el listado');
        }

        const datos = await resp.json();
        this.registros = datos.registros;
        this.pagina = datos.pagina;
        this.totalRegistros = datos.totalRegistros;
        this.totalPaginas = datos.totalPaginas;
        this.tienePaginaAnterior = datos.tienePaginaAnterior;
        this.tienePaginaSiguiente = datos.tienePaginaSiguiente;
      } catch (e) {
        this.error = e.message;
      } finally {
        this.cargando = false;
      }
    },

    // espera 300 ms despues de la ultima tecla antes de buscar
    buscarConRetraso() {
      clearTimeout(this.temporizador);
      this.temporizador = setTimeout(() => {
        this.pagina = 1;
        this.cargar();
      }, 300);
    },

    // cambia de pagina si esta dentro del rango
    irAPagina(n) {
      if (n < 1 || n > this.totalPaginas) {
        return;
      }
      this.pagina = n;
      this.cargar();
    },

    // envia un POST y devuelve siempre un objeto { ok, mensaje }
    async enviar(url, datos) {
      const opciones = { method: 'POST' };
      if (datos) {
        opciones.headers = { 'Content-Type': 'application/json' };
        opciones.body = JSON.stringify(datos);
      }

      const resp = await fetch(url, opciones);

      let respuesta = null;
      try {
        respuesta = await resp.json();
      } catch (e) {
        respuesta = null;
      }

      // sin JSON: sesion vencida o sin permisos (el servidor redirige)
      if (!respuesta) {
        return { ok: false, mensaje: 'No se pudo completar la operacion (sesion vencida o sin permisos)' };
      }
      return respuesta;
    },

    // muestra un aviso de exito unos segundos
    mostrarOk(texto) {
      this.mensajeOk = texto;
      setTimeout(() => { this.mensajeOk = ''; }, 4000);
    },

    // abre el modal vacio para dar de alta
    abrirNuevo() {
      this.form = { id: 0, nombre: '', descripcion: '' };
      this.errorForm = '';
      this.modalAbierto = true;
    },

    // abre el modal con los datos de la fila elegida
    abrirEditar(o) {
      this.form = { id: o.id, nombre: o.nombre, descripcion: o.descripcion || '' };
      this.errorForm = '';
      this.modalAbierto = true;
    },

    cerrarModal() {
      this.modalAbierto = false;
    },

    // valida en el cliente y luego llama a Crear o Editar
    async guardar() {
      this.errorForm = '';

      const nombre = this.form.nombre.trim();
      const descripcion = (this.form.descripcion || '').trim();

      if (!nombre) {
        this.errorForm = 'El nombre es obligatorio';
        return;
      }
      if (nombre.length > 100) {
        this.errorForm = 'El nombre no puede superar los 100 caracteres';
        return;
      }
      if (descripcion.length > 300) {
        this.errorForm = 'La descripcion no puede superar los 300 caracteres';
        return;
      }

      this.guardando = true;
      const url = this.form.id ? '/ObrasSociales/Editar' : '/ObrasSociales/Crear';
      const r = await this.enviar(url, { id: this.form.id, nombre: nombre, descripcion: descripcion });
      this.guardando = false;

      if (!r.ok) {
        this.errorForm = r.mensaje;
        return;
      }

      this.cerrarModal();
      this.mostrarOk(r.mensaje);
      await this.cargar();
    },

    // activa o desactiva (baja logica)
    async cambiarEstado(o) {
      this.error = '';
      const r = await this.enviar('/ObrasSociales/CambiarEstado/' + o.id);

      if (!r.ok) {
        this.error = r.mensaje;
        return;
      }

      this.mostrarOk(r.mensaje);
      await this.cargar();
    },

    // elimina (baja fisica, solo administrador)
    async eliminar(o) {
      if (!confirm('Eliminar la obra social "' + o.nombre + '"?')) {
        return;
      }

      this.error = '';
      const r = await this.enviar('/ObrasSociales/Eliminar/' + o.id);

      if (!r.ok) {
        this.error = r.mensaje;
        return;
      }

      // si era el unico registro de la pagina, volvemos a la anterior
      if (this.registros.length === 1 && this.pagina > 1) {
        this.pagina--;
      }

      this.mostrarOk(r.mensaje);
      await this.cargar();
    }
  }
}).mount('#app');
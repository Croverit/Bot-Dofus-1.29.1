﻿using Bot_Dofus_1._29._1.Comun.Network;
using Bot_Dofus_1._29._1.Forms;
using Bot_Dofus_1._29._1.Otros;
using Bot_Dofus_1._29._1.Otros.Entidades.Stats;
using Bot_Dofus_1._29._1.Otros.Scripts;
using Bot_Dofus_1._29._1.Protocolo.Enums;
using Bot_Dofus_1._29._1.Protocolo.Extensiones;
using Bot_Dofus_1._29._1.Utilidades.Configuracion;
using Bot_Dofus_1._29._1.Utilidades.Logs;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Bot_Dofus_1._29._1.Interfaces
{
    public partial class UI_Principal : UserControl
    {
        private CuentaConf configuracion_cuenta;
        private Cuenta cuenta;

        public UI_Principal(CuentaConf _configuracion_cuenta)
        {
            InitializeComponent();
            configuracion_cuenta = _configuracion_cuenta;
            desconectarOconectarToolStripMenuItem.Text = "Conectar";
        }

        private void UI_Principal_Load(object sender, EventArgs e)
        {
            escribir_mensaje("[" + DateTime.Now.ToString("HH:mm:ss") + "] -> [INFORMACIÓN] Bot creado por Alvaro totalmente gratuito, http://www.salesprendes.com versión: " + Application.ProductVersion, LogTipos.ERROR.ToString("X"));
        }

        private void eliminarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string nombre_cuenta = configuracion_cuenta.nombre_cuenta;
            if (Principal.get_Paginas_Cuentas_Cargadas().ContainsKey(nombre_cuenta))
            {
                desconectar_Cuenta();
                Principal.get_Paginas_Cuentas_Cargadas()[nombre_cuenta].contenido.Dispose();
                Principal.get_Paginas_Cuentas_Cargadas().Remove(nombre_cuenta);
            }
        }

        private void cambiar_Tab_Imagen(Image image)
        {
            if (Principal.paginas_cuentas_cargadas.ContainsKey(configuracion_cuenta.nombre_cuenta))
            {
                Principal.paginas_cuentas_cargadas[configuracion_cuenta.nombre_cuenta].cabezera.propiedad_Imagen = image;
            }
        }

        private void button_limpiar_consola_Click(object sender, EventArgs e) => textbox_logs.Clear();

        private void desconectarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (desconectarOconectarToolStripMenuItem.Text.Equals("Conectar"))
            {
                if (cuenta == null)
                {
                    cuenta = new Cuenta(configuracion_cuenta);

                    while (tabControl_principal.TabPages.Count > 2)
                        tabControl_principal.TabPages.RemoveAt(2);

                    cargar_Eventos_Login();
                    cuenta.evento_fase_socket += cargar_Eventos_Debugger;
                    desconectarOconectarToolStripMenuItem.Text = "Desconectar";
                }
            }
            else if (desconectarOconectarToolStripMenuItem.Text.Equals("Desconectar"))
            {
                desconectar_Cuenta();
                desconectarOconectarToolStripMenuItem.Text = "Conectar";
            }
        }

        private void desconectar_Cuenta()
        {
            if (cuenta != null)
            {
                if (cuenta.conexion != null)
                {
                    cuenta.conexion.paquete_recibido -= debugger.paquete_Recibido;
                    cuenta.conexion.paquete_enviado -= debugger.paquete_Enviado;
                    cuenta.conexion.socket_informacion -= get_Mensajes_Socket_Informacion;
                }

                cuenta.Dispose();
                cuenta = null;
                cambiar_Todos_Controles_Chat(false);

                for (int i = 2; i < tabControl_principal.TabPages.Count; i++)
                {
                    tabControl_principal.TabPages[i].Enabled = false;
                }
            }
        }

        private void cargar_Eventos_Debugger(ClienteAbstracto socket)
        {
            switch (cuenta.Estado_Socket)
            {
                case EstadoSocket.CAMBIANDO_A_JUEGO:
                    socket.paquete_recibido += debugger.paquete_Recibido;
                    socket.paquete_enviado += debugger.paquete_Enviado;
                    socket.socket_informacion += get_Mensajes_Socket_Informacion;
                break;

                case EstadoSocket.PERSONAJE_SELECCIONADO:
                    cuenta.pelea_extension.configuracion.cargar();
                    agregar_Tab_Pagina("Personaje", new UI_Personaje(cuenta), 2);
                    agregar_Tab_Pagina("Inventario", new UI_Inventario(cuenta), 3);
                    agregar_Tab_Pagina("Mapa", new UI_Mapa(cuenta), 4);
                    agregar_Tab_Pagina("Combates", new UI_Pelea(cuenta), 5);
                    cambiar_Todos_Controles_Chat(true);
                    cuenta.personaje.socket_canal_personaje += socket_Evento_Chat;
                    cuenta.personaje.caracteristicas_actualizadas += personaje_Caracteristicas_Actualizadas;
                    cuenta.personaje.pods_actualizados += personaje_Pods_Actualizados;

                    cuenta.script = new ManejadorScript(cuenta);
                    cuenta.script.evento_script_cargado += evento_Scripts_Cargado;
                    cuenta.script.evento_script_iniciado += evento_Scripts_Iniciado;
                    cuenta.script.evento_script_detenido += evento_Scripts_Detenido;
                break;
            }
        }

        private void personaje_Caracteristicas_Actualizadas()
        {
            BeginInvoke((Action)(() =>
            {
                CaracteristicasInformacion caracteristicas = cuenta.personaje.caracteristicas;

                progresBar_vitalidad.valor_Maximo = caracteristicas.vitalidad_maxima;
                progresBar_vitalidad.Valor = caracteristicas.vitalidad_actual;

                progresBar_energia.valor_Maximo = caracteristicas.maxima_energia;
                progresBar_energia.Valor = caracteristicas.energia_actual;

                progresBar_experiencia.Text = cuenta.personaje.nivel.ToString();
                progresBar_experiencia.Valor = cuenta.personaje.porcentaje_experiencia;

                label_kamas_principal.Text = caracteristicas.kamas.ToString("0,0");
            }));
        }

        private void personaje_Pods_Actualizados()
        {
            BeginInvoke((Action)(() =>
            {
                progresBar_pods.valor_Maximo = cuenta.personaje.inventario.pods_maximos;
                progresBar_pods.Valor = cuenta.personaje.inventario.pods_actuales;
            }));
        }

        private void cambiar_Todos_Controles_Chat(bool estado_botones)
        {
            BeginInvoke((Action)(() =>
            {
                tableLayout_Canales.Controls.OfType<CheckBox>().ToList().ForEach(checkbox => checkbox.Enabled = estado_botones);
                textBox_enviar_consola.Enabled = estado_botones;
                cargarScriptToolStripMenuItem.Enabled = estado_botones;
            }));
        }

        private void cargar_Eventos_Login()
        {
            if (cuenta != null)
            {
                cuenta.conexion.paquete_recibido += debugger.paquete_Recibido;
                cuenta.conexion.paquete_enviado += debugger.paquete_Enviado;
                cuenta.conexion.socket_informacion += get_Mensajes_Socket_Informacion;

                cuenta.evento_estado_cuenta += eventos_Estados_Cuenta;
                cuenta.logger.log_evento += (mensaje, color) => escribir_mensaje(mensaje.ToString(), color);
            }
        }

        private void eventos_Estados_Cuenta()
        {
            switch (cuenta.Estado_Cuenta)
            {
                case EstadoCuenta.DESCONECTADO:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_rojo);
                break;

                case EstadoCuenta.CONECTANDO:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_naranja);
                    break;

                default:
                    cambiar_Tab_Imagen(Properties.Resources.circulo_verde);
                    break;
            }
            if (cuenta != null && Principal.paginas_cuentas_cargadas.ContainsKey(configuracion_cuenta.nombre_cuenta))
                Principal.paginas_cuentas_cargadas[configuracion_cuenta.nombre_cuenta].cabezera.propiedad_Estado = cuenta.Estado_Cuenta.cadena_Amigable();
        }

        private void agregar_Tab_Pagina(string nombre, UserControl control, int imagen_index)
        {
            tabControl_principal.BeginInvoke((Action)(() =>
            {
                control.Dock = DockStyle.Fill;
                TabPage nueva_pagina = new TabPage(nombre)
                {
                    ImageIndex = imagen_index
                };
                nueva_pagina.Controls.Add(control);
                tabControl_principal.TabPages.Add(nueva_pagina);
            }));
        }

        private void socket_Evento_Chat()
        {
            BeginInvoke((Action)(() =>
            {
                canal_informaciones.Checked = cuenta.personaje.canales.Contains("i");
                canal_general.Checked = cuenta.personaje.canales.Contains("*");
                canal_privado.Checked = cuenta.personaje.canales.Contains("#$p");
                canal_gremio.Checked = cuenta.personaje.canales.Contains("%");
                canal_alineamiento.Checked = cuenta.personaje.canales.Contains("!");
                canal_reclutamiento.Checked = cuenta.personaje.canales.Contains("?");
                canal_comercio.Checked = cuenta.personaje.canales.Contains(":");
                canal_incarnam.Checked = cuenta.personaje.canales.Contains("^");
            }));
        }

        private void canal_CheckedChanged(object sender, EventArgs e)
        {
            if (cuenta.personaje != null && cuenta.Estado_Cuenta != EstadoCuenta.CONECTANDO)
            {
                CheckBox control = sender as CheckBox;
                switch (control.Name)
                {
                    case "canal_informaciones":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+i" : "cC-i");
                    break;

                    case "canal_general":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+*" : "cC-*");
                    break;

                    case "canal_privado":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+#$p" : "cC-#$p");
                    break;

                    case "canal_gremio":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+%" : "cC-%");
                    break;

                    case "canal_alineamiento":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+!" : "cC-!");
                    break;

                    case "canal_reclutamiento":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+?" : "cC-?");
                    break;

                    case "canal_comercio":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+:" : "cC-:");
                    break;

                    case "canal_incarnam":
                        cuenta.conexion.enviar_Paquete(control.Checked ? "cC+^" : "cC-^");
                    break;
                }
            }
        }

        private void textBox_enviar_consola_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox_enviar_consola.TextLength > 0 && textBox_enviar_consola.TextLength < 255)
            {
                if (cuenta.personaje != null && cuenta.Estado_Cuenta != EstadoCuenta.CONECTANDO)
                {
                    switch (textBox_enviar_consola.Text)
                    {
                        case "/mapid":
                            escribir_mensaje(cuenta.personaje.mapa.id.ToString(), "0040FF");
                        break;

                        case "/cellid":
                            escribir_mensaje(cuenta.personaje.celda.id.ToString(), "0040FF");
                        break;

                        default:
                            cuenta.conexion.enviar_Paquete("BM*|" + textBox_enviar_consola.Text + "|");
                        break;
                    }
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    textBox_enviar_consola.Clear();
                }
            }
        }

        private void cargarScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Selecciona el script para el bot";
                    ofd.Filter = "Extension (.lua) | *.lua";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        cuenta.script.get_Desde_Archivo(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                escribir_mensaje("[" + DateTime.Now.ToString("HH:mm:ss") + "] -> [Script] " + ex.Message, LogTipos.ERROR.ToString("X"));
            }
        }

        private void iniciarScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cuenta != null)
            {
                if (!cuenta.script.activado)
                    cuenta.script.activar_Script();
                else
                    cuenta.script.detener_Script();
            }
        }

        #region Eventos Logger Mensajes
        private void get_Mensajes_Socket_Informacion(object error) => escribir_mensaje("[" + DateTime.Now.ToString("HH:mm:ss") + "] [Conexión] " + error, LogTipos.PELIGRO.ToString("X"));

        private void escribir_mensaje(string mensaje, string color)
        {
            if (!IsHandleCreated)
                return;

            textbox_logs.BeginInvoke((Action)(() =>
            {
                textbox_logs.Select(textbox_logs.TextLength, 0);
                textbox_logs.SelectionColor = ColorTranslator.FromHtml("#" + color);
                textbox_logs.AppendText(mensaje + Environment.NewLine);
                textbox_logs.ScrollToCaret();
            }));
        }
        #endregion

        #region Eventos Scripts
        private void evento_Scripts_Cargado(string nombre)
        {
            cuenta.logger.log_informacion("SCRIPT", $"'{nombre}' Cargado.");
            BeginInvoke((Action)(() =>
            {
                ScriptTituloStripMenuItem.Text = $"{nombre.Truncar(16)}";
                iniciarScriptToolStripMenuItem.Enabled = true;
            }));
        }

        private void evento_Scripts_Iniciado()
        {
            cuenta.logger.log_informacion("SCRIPT", "Iniciado.");
            BeginInvoke((Action)(() =>
            {
                cargarScriptToolStripMenuItem.Enabled = false;
                iniciarScriptToolStripMenuItem.Image = Properties.Resources.boton_stop;
            }));
        }

        private void evento_Scripts_Detenido(string motivo)
        {
            if (string.IsNullOrEmpty(motivo))
                cuenta.logger.log_informacion("SCRIPT", "Detenido.");
            else
                cuenta.logger.log_informacion("SCRIPT", $"Detenido {motivo}");

            BeginInvoke((Action)(() =>
            {
                iniciarScriptToolStripMenuItem.Image = Properties.Resources.boton_play;
                cargarScriptToolStripMenuItem.Enabled = true;
                ScriptTituloStripMenuItem.Text = "-";
            }));
        }
        #endregion
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bot_Dofus_1._29._1.Comun.Network;

namespace Bot_Dofus_1._29._1.Comun.Frames.Transporte
{
    public class PaqueteRecibido
    {
        public List<PaqueteDatos> metodos { get; private set; }
        private bool esta_iniciado { get; set; }

        public PaqueteRecibido()
        {
            metodos = new List<PaqueteDatos>();
            esta_iniciado = false;
        }

        public void Inicializar()
        {
            if (!esta_iniciado)
            {
                Assembly assembly = typeof(Frame).GetTypeInfo().Assembly;

                foreach (MethodInfo tipo in assembly.GetTypes().SelectMany(x => x.GetMethods()).Where(m => m.GetCustomAttributes(typeof(PaqueteAtributo), false).Length > 0).ToArray())
                {
                    PaqueteAtributo atributo = tipo.GetCustomAttributes(typeof(PaqueteAtributo), true)[0] as PaqueteAtributo;
                    Type tipo_string = Type.GetType(tipo.DeclaringType.FullName);

                    object instancia = Activator.CreateInstance(tipo_string, null);
                    metodos.Add(new PaqueteDatos(instancia, atributo.paquete, tipo));
                }
                esta_iniciado = true;
            }
        }

        public void Recibir(ClienteAbstracto cliente, string paquete)
        {
            if (!esta_iniciado)
                Inicializar();

            foreach (PaqueteDatos metodo in metodos)
            {
                if (paquete.StartsWith(metodo.nombre_paquete))
                {
                    metodo.metodo.Invoke(metodo.instancia, new object[] { cliente, paquete });
                    break;
                }  
            }
        }
    }
}
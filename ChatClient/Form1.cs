using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        // Contiene el nombre de usuario
        private string UserName = "desconocido";

        //Streams implementan flujos de escritura y lectura
        private StreamWriter swSender;
        private StreamReader srReceiver;
        

        //TcpClient provee conexiones por TCP
        private TcpClient tcpServer;


        // Actualiza la Form con mensajes desde otro hilo
        private delegate void ActualizaLogDeMensajes(string strMessage);


        //Establece el estado de "desconectado" desde otro hilo
        private delegate void CierraConexion(string strReason);


        private Thread thrMessaging;

        //provee una direccion IP
        private IPAddress ipAddr;

        private bool conectado;

        public Form1()
        {

            InitializeComponent();
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            
            if (conectado == false)
            {
                inicializaConexion();
            }
            else 
            {
                cierraConexion("Desconectado por el usuario.");
            }
        }

        private void inicializaConexion()
        {
            // Convierte la ip capturada a un formato adecuado para la asignacion a un
            // objeto IPAddress
            ipAddr = IPAddress.Parse(txtIp.Text);

            // Inicia una nueva conexion TCP hacia el servidor
            tcpServer = new TcpClient();
            // el puerto usado es el 1986
            tcpServer.Connect(ipAddr, 777);

            // esta bandera nos ayuda a saber cuando estamos conectados
            conectado = true;

            // Asignamos el contenido de nuestro textbox a una variable string
            // para hacer el codigo un poco mas intuitivo.
            UserName = txtUser.Text;

            // Se activan y desactivan los campos apropiados
            txtIp.Enabled = false;
            txtUser.Enabled = false;
            txtMessage.Enabled = true;
            btnSend.Enabled = true;
            btnConnect.Text = "Desconecta";

            // Se envia el usuario al servidor
            swSender = new StreamWriter(tcpServer.GetStream());
            swSender.WriteLine(txtUser.Text);
            swSender.Flush();

             // Se inicia un hilo para recibir los mensajes
            thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
            thrMessaging.Start();
        }

        private void ReceiveMessages()
        {
            // Se recibe respuestas desde el servidor
            srReceiver = new StreamReader(tcpServer.GetStream());
            string ConResponse = srReceiver.ReadLine();

            // Si el primer caracter de la respuesta es un 1, asumiremos que la conexion
            // fue exitosa

            if (ConResponse[0] == '1')
            {
                // Se actualiza la form para ver quien se ha conectado
                this.Invoke(new ActualizaLogDeMensajes(this.UpdateLog), new object[] { "Conectado de Manera Exitosa!" });
            }
            else // Si el servidor no nos mando el 1 como primer respuesta, entonces algo sucedio!.. demonios!!
            {
                string Reason = "No Conectado: ";
                // Veamos que fue lo que sucedio.. de acuerdo con el formato del mensaje
                // la razon inicia en el 3er caracter.
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                // Actualizamos la form con la razon por la cual no pudimos conectarnos..
                this.Invoke(new CierraConexion(this.cierraConexion), new object[] { Reason });
                // return... 
                return;
            }
            // Mientras estemos conectados al servidor, leemos lo proveniente del servidor
            while (conectado)
            {
                // mostramos los mensajes en el textBox Log
                this.Invoke(new ActualizaLogDeMensajes(this.UpdateLog), new object[] { srReceiver.ReadLine() });
            }
        }

        // Este metodo es llamado desde un hilo diferente para actualizar el textbox Log
        private void UpdateLog(string strMessage)
        {
            // Se agrega el texto al textBox Log
            txtLog.AppendText(strMessage + "\r\n");
        }

        // Cierra la conexion actual
        private void cierraConexion(string Reason)
        {
            thrMessaging.Abort();
            // Mostramos el porque estamos cerrando la conexion
            txtLog.AppendText(Reason + "\r\n");
            // Activa y desactiva los controles correspondientes en el Form.
            txtIp.Enabled = true;
            txtUser.Enabled = true;
            txtMessage.Enabled = false;
            btnSend.Enabled = false;
            btnConnect.Text = "Conectar";

            // Cierra conexiones, streams, etc..
            conectado = false;
            swSender.Close();
            srReceiver.Close();
            tcpServer.Close();

        }

        // Envia el mensaje tecleado al servidor.
        private void enviaMensaje()
        {
            if (txtMessage.Lines.Length >= 1)
            {
                swSender.WriteLine(txtMessage.Text);
                swSender.Flush();
                txtMessage.Lines = null;
            }
            txtMessage.Text = "";
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            enviaMensaje();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Se valida la tecla Enter
            if (e.KeyChar == (char)13)
            {
                enviaMensaje();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // antes de salir hay que desconectarse
            if (conectado == true)
            {
                thrMessaging.Abort();
                // Cierra conexiones, streams, etc..
                conectado = false;
                swSender.Close();
                srReceiver.Close();
                tcpServer.Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //averiguemos la ip de la computadora local
            String strHostName = Dns.GetHostName();

            
            // Usamos el hostname para averiguar la IP..
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
           for (int i = 0; i < addr.Length; i++)
            {
                txtIp.Text = txtIp.Text + addr[i].ToString(); 
            }
           txtIp.Text = "127.0.0.1";



        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Parse the server's IP address out of the TextBox
            IPAddress ipAddr = IPAddress.Parse(txtIp.Text);
            // Create a new instance of the ChatServer object
            ChatServer mainServer = new ChatServer(ipAddr);
            // Hook the StatusChanged event handler to mainServer_StatusChanged
            ChatServer.StatusChanged += new StatusChangedEventHandler(mainServer_StatusChanged);
            // Start listening for connections
            mainServer.StartListening();
            // Show that we started to listen for connections
            txtLog.AppendText("Monitoring for connections...\r\n");
        }
    }
}
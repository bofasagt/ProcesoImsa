using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace ProcesoImsa
{
    public partial class frmIMSA : Form
    {
        DB.Crypto encriptacion = new DB.Crypto();
        DB.Conexion conexion = new DB.Conexion();
        System.Timers.Timer aTimer = new System.Timers.Timer();
        DataTable dtFacturas = new System.Data.DataTable();
        List<string> items = new List<string>();
        public frmIMSA()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
            ConectarWS();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(ConectarWS);
            aTimer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["TiempoHiloSinCAE"]) * 1000;
            aTimer.Enabled = true;
            aTimer.Start();
        }
        public void ConectarWS(object sender = null, System.Timers.ElapsedEventArgs e = null)
        {
            try
            {
                var Facturas = ObtenerFacturas();
                foreach (var factura in Facturas)
                {
                    gt.com.imsa.handheldws.wsServicioFarmaciaImsa WSIMSA = new gt.com.imsa.handheldws.wsServicioFarmaciaImsa();
                    WSIMSA.Url = "https://handheldws.imsa.com.gt:4443/wsIMSAFarmacia/wsServicioFarmaciaImsa.asmx?wsdl";
                    gt.com.imsa.handheldws.clsFarmaciaImsaRequest RequestImsa = new gt.com.imsa.handheldws.clsFarmaciaImsaRequest();
                    System.Net.ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
                    System.Net.CredentialCache CredencialCache = new System.Net.CredentialCache();
                    System.Net.NetworkCredential NetCredencial = new System.Net.NetworkCredential("FCVERDE", "F@rm@c1$CV3rd3", "MAGDALENA");
                    CredencialCache.Add(new Uri(WSIMSA.Url), "Basic", NetCredencial);
                    WSIMSA.Credentials = CredencialCache;
                    gt.com.imsa.handheldws.clsEncabezado[] ListaEncabezado = new gt.com.imsa.handheldws.clsEncabezado[1];
                    gt.com.imsa.handheldws.clsDetalle[] ListaDetalle = new gt.com.imsa.handheldws.clsDetalle[factura.Detalle.Productos.Count];
                    var Encabezado = new gt.com.imsa.handheldws.clsEncabezado();
                    int ContadorProd = 0;
                    Encabezado.cod_empleado = factura.Encabezado.datos.cod_empleado;
                    Encabezado.codigo_transaccion = factura.Encabezado.datos.codigo_transaccion;
                    Encabezado.fecha = factura.Encabezado.datos.fecha;
                    Encabezado.total = factura.Encabezado.datos.total;
                    Encabezado.pagosye = factura.Encabezado.datos.pagosye;
                    foreach (var producto in factura.Detalle.Productos)
                    {
                        var Detalle = new gt.com.imsa.handheldws.clsDetalle();
                        Detalle.cod_empleado = producto.cod_empleado;
                        Detalle.codigo_transaccion = producto.codigo_transaccion;
                        Detalle.correlativo = producto.correlativo;
                        Detalle.descripcion = producto.descripcion;
                        Detalle.cantidad = producto.cantidad;
                        Detalle.monto = producto.monto;
                        ListaDetalle[ContadorProd++] = Detalle;
                    }
                    ListaEncabezado[0] = Encabezado;
                    RequestImsa.ClsEncabezado = ListaEncabezado;
                    RequestImsa.ClsDetalle = ListaDetalle;
                    gt.com.imsa.handheldws.clsFarmaciaImsaResponse ResponseImsa = WSIMSA.setConsumoMedicamento(RequestImsa);
                    var Query = "[VENTAS].[SP_PROC_IMSA] '<PROCESADO><U_SERIE>" + factura.Serie + "</U_SERIE><U_NUMERO>" +
                            factura.Numero + "</U_NUMERO><RESPUESTA><![CDATA[<RESPUESTA><cod_error> " + ResponseImsa.cod_error + " </cod_error><descripcion> " + ResponseImsa.descripcion + " </descripcion></RESPUESTA >]]></RESPUESTA ></PROCESADO > '";
                    System.Data.SqlClient.SqlConnection CadenaConexion =
                        new System.Data.SqlClient.SqlConnection(encriptacion.DecryptCadena(
                            ConfigurationManager.ConnectionStrings["conexion"].ToString(), "bofasa1$"));
                    conexion.ConexionSQL = CadenaConexion;
                    dtFacturas = conexion.LlenarDataTableSQL(Query);
                    String ItemRespuesta = "Factura: " + factura.Serie + factura.Numero + " Respuesta: " + ResponseImsa.descripcion;
                    items.Add(ItemRespuesta);
                }
            }
            catch (Exception ex)
            {
                items.Add(ex.Message);
            }
            lstRespuesta.DataSource = items;
        }

        public List<FACTURA> ObtenerFacturas()
        {
            try
            {
                List<FACTURA> facturas = new List<FACTURA>();
                var Query = "[VENTAS].[SP_PROC_IMSA] '<VERIFICA_PENDIENTES/>'";
                System.Data.SqlClient.SqlConnection CadenaConexion =
                   new System.Data.SqlClient.SqlConnection(encriptacion.DecryptCadena(
                       ConfigurationManager.ConnectionStrings["conexion"].ToString(), "bofasa1$"));
                conexion.ConexionSQL = CadenaConexion;
                dtFacturas = conexion.LlenarDataTableSQL(Query);
                foreach (DataRow factura in dtFacturas.Rows)
                {
                    facturas.Add((FACTURA)GenerarObjFactura(factura["BITAC_REQ"].ToString(), factura["U_SERIE"].ToString(), (int)factura["U_NUMERO"]));
                }
                return facturas;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static Object GenerarObjFactura(string xml, string serie, int numero)
        {
            Object Factura;
            try
            {
                ENCABEZADO Encabezado = new ENCABEZADO();
                DATOS Datos = new DATOS();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlNodeList ENCABEZADO = doc.DocumentElement.SelectNodes("/FACTURA/ENCABEZADO/DATOS");
                foreach (XmlNode DATOS in ENCABEZADO)
                {
                    Datos.cod_empleado = DATOS["cod_empleado"].InnerText;
                    Datos.codigo_transaccion = DATOS["codigo_transaccion"].InnerText;
                    Datos.fecha = DATOS["fecha"].InnerText;
                    Datos.total = DATOS["total"].InnerText;
                    Datos.pagosye = DATOS["pagosye"].InnerText;
                }
                Encabezado.datos = Datos;
                DETALLE Detalle = new DETALLE();
                XmlNodeList DETALLE = doc.DocumentElement.SelectNodes("/FACTURA/DETALLE/PRODUCTOS");
                foreach (XmlNode PRODUCTOS in DETALLE)
                {
                    PRODUCTOS Producto = new PRODUCTOS();
                    Producto.cod_empleado = PRODUCTOS["cod_empleado"].InnerText;
                    Producto.codigo_transaccion = PRODUCTOS["codigo_transaccion"].InnerText;
                    Producto.correlativo = PRODUCTOS["correlativo"].InnerText;
                    Producto.descripcion = PRODUCTOS["descripcion"].InnerText;
                    Producto.cantidad = PRODUCTOS["cantidad"].InnerText;
                    Producto.monto = PRODUCTOS["monto"].InnerText;
                    Detalle.InsertarProductos(Producto);
                }
                Detalle.ObtenerProductos();
                Factura = new FACTURA(Encabezado, Detalle, serie, numero);
            }
            catch
            {
                return null;
            }

            return Factura;
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
       (
           int nLeftRect,
           int nTopRect,
           int nRightRect,
           int nBottomRect,
           int nWidthEllipse,
           int nHeightEllipse
        );
        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private void frmIMSA_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void lstRespuesta_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void lstRespuesta_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
    public class TrustAllCertificatePolicy : System.Net.ICertificatePolicy
    {
        public TrustAllCertificatePolicy()
        { }
        public bool CheckValidationResult(System.Net.ServicePoint sp,
            X509Certificate cert, System.Net.WebRequest req, int problem)
        {
            return true;
        }
        public static bool TrustAllCertificateCallback(object sender,
    X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors errors)
        {
            return true;
        }
    }
}

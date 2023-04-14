using SQL_Server_Job_Monitor.CiscoWebDialer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime;

namespace SQL_Server_Job_Monitor
{
    internal class Program
    {

        public static List<string> jobList = new List<string>();
        static void Main(string[] args)
        {

            // 2 tentativas 
            for (int i = 0; i < 2; i++)
            {
                ConnectDB();
                Thread.Sleep(300000);//Aguarda 5 minutos
            }
        }

        public static void WriteLog(string log)
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/SQL Server Job Monitor Log.log";
            StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8);
            writer.WriteLine(DateTime.Now.ToString() + " " + log);
            writer.Close();

        }

        public static void ConnectDB()
        {
            WriteLog("Iniciando conexão com o banco de dados.");

            SqlConnection connection;
            string connetionParameters = @"Data Source=sv-dc2-sql05,1434\DW;Initial Catalog=DW_AUDITORIA_SAPBO;User ID=dwpaqueta;Password=paqu3t4";
            connection = new SqlConnection(connetionParameters);
            connection.Open();

            WriteLog("Estado da conexão com o banco de dados: " + connection.State.ToString());

            SqlCommand eventQuery = connection.CreateCommand();
            eventQuery.CommandText = "SELECT TOP 1000 " +
                "[NOME]," +
                "[DATA_INICIO]," +
                "[STATUS]," +
                "[TURNO_PESSOA] " +
                "FROM [DW_AUDITORIA_SAPBO].[dbo].[PAINEL_MONITORAMENTO_JOBS_NOVO] " +
                "WHERE NOME IN " +
                "('BI - Varejo Comercial MP - Dumond Capodarte'," +
                "'BI - Varejo Logistica'," +
                "'BI - Industria Comercial Cota'," +
                "'BI - Industria Comercial'," +
                "'BI - Varejo Comercial - Vendedores'," +
                "'BI - Industria Producao'," +
                "'BI - Varejo - Microindicadores'," +
                "'BI - Industria Estoque'," +
                "'BI - Painel Indicadores Corporativos'," +
                "'BI - Corporativo Contabil'," +
                "'BI - Corporativo RH'," +
                "'BI - Industria Faturamento'," +
                "'BI - Varejo Recargas'," +
                "'BI - Industria Controladoria'," +
                "'BI - Industria Compras'," +
                "'BI - Varejo - Auditoria'," +
                "'BI - Industria Desenvolvimento Produto'," +
                "'BI - Varejo Fornecedores'," +
                "'BI - Varejo Comparativo Comercial'," +
                "'BI - Varejo Periodo Fiscal'," +
                "'BI - Varejo Virtual'," +
                "'BI - Varejo Vendas Previsao e Estoque - Tamanho'," +
                "'BI - Varejo Preco Compra'," +
                "'BI - Varejo Escalonador - Manual'," +
                "'BI - Industria Depreciacao'," +
                "'BI - Varejo Pedido Empenho'," +
                "'BI - Varejo Remanejo'," +
                "'BI - Varejo - Credsystem - Volume Negocio Completo'," +
                "'BI - Urbanismo Comercial'," +
                "'BI - Jobs Falha Teste')" +
                "AND [STATUS] = 0"; // STATUS 0 == FALHA
            SqlDataReader reader = eventQuery.ExecuteReader();
            CheckJobs(reader);
            connection.Close();

            WriteLog("Conexão com o banco de dados encerrada.");

        }

        public static void Log(string date, string time, string name, string job)
        {
            WriteLog("Iniciando gravação na tabela de logs");
            SqlConnection connection;
            string connetionParameters = @"Data Source=sv-dc2-sql05,1434\DW;Initial Catalog=DW_AUDITORIA_SAPBO;User ID=dwpaqueta;Password=paqu3t4";
            connection = new SqlConnection(connetionParameters);
            connection.Open();

            SqlCommand logQuery = connection.CreateCommand();
            logQuery.CommandText = $"INSERT INTO [dbo].[LOG_MONITORAMENTO_JOBS] ([DATA] ,[TIME] ,[PLANTONISTA],[JOB]) VALUES('{date}', '{time}', '{name}' ,'{job}')";
            logQuery.ExecuteNonQuery();
            connection.Close();
        }

        public static void CheckJobs(SqlDataReader reader)
        {
            WriteLog("Iniciando checagem dos Jobs");
            if (jobList.Count > 0)
            {
                while (reader.Read())
                {
                    if (reader["STATUS"].ToString() == "0" && jobList.Contains(reader["NOME"].ToString()))
                    {

                        WriteLog("Job " + reader["NOME"].ToString() + " com erro");

                        string dest = reader["TURNO_PESSOA"].ToString();
                        Log(DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), reader["TURNO_PESSOA"].ToString(), reader["NOME"].ToString());

                        jobList.Remove(reader["NOME"].ToString());

                        WriteLog("Iniciando chamada para o plantonista");

                        MakeCall(dest);
                    }
                }
            }
            else
            {
                while (reader.Read())
                {
                    if (reader["STATUS"].ToString() == "0")
                    {
                        WriteLog("Primeiro loop de checagem, Job adicionado à lista auxiliar");
                        jobList.Add(reader["NOME"].ToString());
                    }
                }
            }

        }

        public static void MakeCall(string dest)
        {

            var number = "";
            switch (dest)
            {
                case "ALEXSANDRE":
                    number = "70009454";
                    break;
                case "MATEUS":
                    number = "70009311";
                    break;
                case "VITOR":
                    number = "70999253";
                    break;
            }

            WriteLog($"Ligando para o plantonista {dest}, número: {number}");

            CiscoWebDialer.WebdialerSoapServiceClient client = new CiscoWebDialer.WebdialerSoapServiceClient();
            //https://10.22.3.59/webdialer/services/WebdialerSoapService?wsdl

            Credential cred = new Credential();
            UserProfile profile = new UserProfile();
            cred.userID = "userhelpdesk";
            cred.password = "TelecomPKT";

            profile.deviceName = "SEP847BEBE7DB3F";
            profile.user = "userhelpdesk";
            profile.lineNumber = "70009748";

            ServicePointManager.ServerCertificateValidationCallback
            += new RemoteCertificateValidationCallback(AllwaysGoodCertificate);

            client.makeCallSoap(cred, number, profile);

            WriteLog("Estado da conexão com o WebService: " + client.State.ToString());

            //Mantém a ligação por 1 minuto
            Thread.Sleep(60000);

            client.endCallSoap(cred, profile);
            client.Close();

            WriteLog("Estado da conexão com o WebService: " + client.State.ToString());

        }

        // Utiliza um certificado de confiança para a conexão com a WebService
        private static bool AllwaysGoodCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }
    }
}
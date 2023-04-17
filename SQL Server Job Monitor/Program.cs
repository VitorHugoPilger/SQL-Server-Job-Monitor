using SQL_Server_Job_Monitor.CiscoWebDialer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

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
            string connetionParameters = @"Data Source=SERVER;Initial Catalog=DB;User ID=USER;Password=PASSWORD";
            connection = new SqlConnection(connetionParameters);
            connection.Open();

            WriteLog("Estado da conexão com o banco de dados: " + connection.State.ToString());

            SqlCommand eventQuery = connection.CreateCommand();
            eventQuery.CommandText = "SELECT TOP 1000 " +
                "[NOME]," +
                "[DATA_INICIO]," +
                "[STATUS]," +
                "[TURNO_PESSOA] " +
                "FROM [TABELA DE JOBS] " +
                "WHERE NOME IN " +
                "('JOBS_NAME')" +
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
            string connetionParameters = @"Data Source=SERVER;Initial Catalog=DB;User ID=USER;Password=PASSWORD";
            connection = new SqlConnection(connetionParameters);
            connection.Open();

            SqlCommand logQuery = connection.CreateCommand();
            logQuery.CommandText = $"INSERT INTO [TABELA_LOG] ([DATA] ,[TIME] ,[PLANTONISTA],[JOB]) VALUES('{date}', '{time}', '{name}' ,'{job}')";
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
                case "PLANTONISTA1":
                    number = "RAMAL";
                    break;
                case "PLANTONISTA2":
                    number = "RAMAL";
                    break;
                case "PLANTONISTA3":
                    number = "RAMAL";
                    break;
            }

            WriteLog($"Ligando para o plantonista {dest}, número: {number}");

            CiscoWebDialer.WebdialerSoapServiceClient client = new CiscoWebDialer.WebdialerSoapServiceClient();
            //https://IP/webdialer/services/WebdialerSoapService?wsdl


            Credential cred = new Credential();
            UserProfile profile = new UserProfile();
            cred.userID = "USER";
            cred.password = "PASSWORD";

            profile.deviceName = "MAC";
            profile.user = "USER";
            profile.lineNumber = "RAMAL";

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
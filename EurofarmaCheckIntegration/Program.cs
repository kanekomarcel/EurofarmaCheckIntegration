using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace EurofarmaCheckIntegration
{
    class Program
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private OracleConnection _con;
        
        static void Main(string[] args)
        {
            log.Info("Application started");

            String processedFolder = ConfigurationManager.AppSettings["ProcessedFolder"];
            String successfulFolder = ConfigurationManager.AppSettings["SuccessfulFolder"];
            String regexFileName = ConfigurationManager.AppSettings["RegexFileName"];

            Regex r = new Regex(regexFileName, RegexOptions.IgnoreCase);
            Program p = new Program();
            //p.InitializeDBConnection();

            foreach (string file in Directory.GetFiles(processedFolder, "*.txt"))
            {
                
                Match m = r.Match(file.Replace(processedFolder + "\\", ""));
                if (m.Success)
                {
                    log.Info("File " + file + " found. Starting process.");
                    String readText = File.ReadAllText(file);

                    String[] fileContent = readText.Split(';');        
                    p.checkLink(fileContent[0],fileContent[1]);
                }
                //string contents = File.ReadAllText(file);
            }
        }

        private void InitializeDBConnection()
        {
            String connectionString = "User Id={0};Password={1};Data Source={2};";
            String dbUser = ConfigurationManager.AppSettings["dbUser"];
            String dbPass = ConfigurationManager.AppSettings["dbPass"];
            String dbSource = "mytestDB";

            _con = new OracleConnection();
            _con.ConnectionString = string.Format(connectionString, dbUser, dbPass, dbSource);
            _con.Open();
        }

        private void checkLink(String parentLot, String childLot)
        {
            OracleConnection con = null;
            OracleCommand cmd = null;
            OracleDataReader reader = null;
            try
            {
                //Database connection
                String dbUser = ConfigurationManager.AppSettings["dbUser"];
                String dbPass = ConfigurationManager.AppSettings["dbPass"];
                String dbSource = "mytestDB";
                string constr = "user id = " + dbUser + "; password = " + dbPass + "; data source = " + dbSource + ";";
                con = new OracleConnection(constr);
                con.Open();

                //check for link


                //if there is no link, create it

                //Get next increment
                int increment = 0;
                cmd = new OracleCommand("select IDS.nextval from dual", con);
                cmd.CommandType = System.Data.CommandType.Text;
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    increment = reader.GetInt16(0); //Just example
                }

                //

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace.ToString());
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                if (con != null && con.State == System.Data.ConnectionState.Open)
                    con.Close();
                con.Dispose();
            }
        }
    }
}

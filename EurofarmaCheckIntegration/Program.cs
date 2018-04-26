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

        //private OracleConnection _con;
        
        static void Main(string[] args)
        {
            log.Info("Application started");

            String processedFolder = ConfigurationManager.AppSettings["ProcessedFolder"];
            String successfulFolder = ConfigurationManager.AppSettings["SuccessfulFolder"];
            String QueueFolder = ConfigurationManager.AppSettings["QueueFolder"];
            String regexFileName = ConfigurationManager.AppSettings["LinkedBatchRegexFileName"];
            String inspectionRegexFileName = ConfigurationManager.AppSettings["InspectionRegexFileName"];

            Regex r = new Regex(regexFileName, RegexOptions.IgnoreCase);
            Regex r_insp = new Regex(inspectionRegexFileName, RegexOptions.IgnoreCase);
            Program p = new Program();
            //p.InitializeDBConnection();

            //Process ILPA files - linked batches
            foreach (string file in Directory.GetFiles(processedFolder, "*.csv"))
            {
                Boolean moveFile = false;

                //process linked batch
                Match m = r.Match(file.Replace(processedFolder + "\\", ""));
                if (m.Success)
                {
                    log.Info("File " + file + " found. Starting process.");
                    //String readText = File.ReadAllText(file);

                    System.IO.StreamReader readFile = new System.IO.StreamReader(file);
                    String line;
                    
                    int cont = 0; //ignore header
                    while ((line = readFile.ReadLine()) != null) 
                    {
                        if (cont > 0) {
                            String[] fileContent = line.Replace("\"","").Split(';');        
                            if (p.checkBatchLink(fileContent[0],fileContent[6])) 
                            {
                                moveFile = true;
                            }
                        } else {
                            cont++;
                        }
                    }
                    readFile.Close();
                    
                }

                //process inspection files
                Match m_insp = r_insp.Match(file.Replace(processedFolder + "\\", ""));
                if (m_insp.Success) 
                {
                    log.Info("File " + file + " found. Starting process.");
                    //String readText = File.ReadAllText(file);

                    System.IO.StreamReader readFile = new System.IO.StreamReader(file);
                    String line;
                    int cont = 0; //ignore header
                    while ((line = readFile.ReadLine()) != null) 
                    {
                        if (cont > 0) {
                            String[] fileContent = line.Replace("\"","").Split(';');        
                            if (p.checkInspection(fileContent[0])) 
                            {
                                //
                                moveFile = true;
                            }
                        } else {
                            cont++;
                        }
                    }
                    readFile.Close();
                }
                //string contents = File.ReadAllText(file);

                if (moveFile) {
                    Directory.Move(file, QueueFolder + "\\" + file.Replace(processedFolder + "\\", ""));
                } else {
                    Directory.Move(file, successfulFolder + "\\" + file.Replace(processedFolder + "\\", ""));
                }
            }
        }

        /*
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
        */

        //returns true if no relationship is found
        private Boolean checkBatchLink(String parentLot, String childLot)
        {
            OracleConnection con = null;
            OracleCommand cmd = null;
            OracleDataReader reader = null;

            Boolean ret = false;
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
                cmd = new OracleCommand("select * from linked_batches where a_parent_batch like '%" + parentLot + "%' and a_linked_batch like '%" + childLot + "%'", con);
                cmd.CommandType = System.Data.CommandType.Text;
                reader = cmd.ExecuteReader();

                Boolean hasLink = reader.HasRows;

                //if there is no link, create it
                if (!hasLink) {
                    
                    /*
                    //Criar novo registro na linked_batches
                    //Get next increment
                    int increment = 0;
                    cmd = new OracleCommand("select IDS.nextval from dual", con);
                    cmd.CommandType = System.Data.CommandType.Text;
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        increment = reader.GetInt16(0); //Just example
                    }

                    if (increment > 0) {
                    //Create new link
                        //cmd = new OracleCommand("insert into fake_linked_batch values (ID, ID_PARENT_LOT, ID_CHILD_LOT) values (" + increment + ", '" + parentLot + "','" + childLot + "')");
                        //cmd.CommandType = System.Data.CommandType.Text;
                        //reader = cmd.ExecuteReader();

                        var commandText = "insert into fake_linked_batch (ID, ID_PARENT_LOT, ID_CHILD_LOT) values (" + increment + ", '" + parentLot + "', '" + childLot + "')";
                        //using (oracleConnection con = new oracleConnection(connect))
                        OracleCommand command = new OracleCommand(commandText, con);
                        
                        //command.Parameters.Add(new OracleParameter("inc", increment));
                        //command.Parameters.Add(new OracleParameter("parentLot", parentLot));
                        //command.Parameters.Add(new OracleParameter("childLot", childLot));
                        //command.Connection.Open();
                        command.ExecuteNonQuery();
                        //command.Connection.Close();
                        log.Info("Relacionamento entre lotes " + parentLot + " e " + childLot + " criado com sucesso");
                        
                    }
                    */
                    //mover arquivos de volta para processamento
                    ret = true;
                    

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace.ToString());
                log.Error("Erro na criação do relacionamento entre lotes " + parentLot + " e " + childLot + ". " + e.Message);
                    
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                if (con != null && con.State == System.Data.ConnectionState.Open)
                    con.Close();
                con.Dispose();
                
            }

            return ret;
        }

        //returns true if no relationship is found
        private Boolean checkInspection(String parentLot)
        {
            OracleConnection con = null;
            OracleCommand cmd = null;
            OracleDataReader reader = null;

            Boolean ret = false;
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
                cmd = new OracleCommand("select * from inspection_detail where a_batch_inspection like '%" + parentLot + "%' and inspection_type = '04'", con);
                cmd.CommandType = System.Data.CommandType.Text;
                reader = cmd.ExecuteReader();

                Boolean hasLink = reader.HasRows;

                //if there is no link, create it
                if (!hasLink) {
                   
                    ret = true;
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace.ToString());
                log.Error("Erro na criação da inspeção do lote " + parentLot + ". " + e.Message);
                    
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                if (con != null && con.State == System.Data.ConnectionState.Open)
                    con.Close();
                con.Dispose();
                
            }

            return ret;
        }
    }
}

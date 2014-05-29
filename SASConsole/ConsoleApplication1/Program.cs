using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
//using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitcode = 0;
            SasServer activeSession = new SasServer();

            //string code = ReadFileFromAssembly("");

            string path = args[0];
            string readCode = "proc setinit; run;";

            // This text is added only once to the file. 
            if (File.Exists(path))
            {
                readCode = File.ReadAllText(path);
            }
       
            //connect to sas server
              try
                {
                    activeSession.Connect();
                }
                catch (Exception ex)
                {
                   Console.WriteLine(ex.Message);
                }

            //run sas code
            if (activeSession != null && activeSession.Workspace != null)
            {
                activeSession.Workspace.LanguageService.Submit(readCode);
            }


            PopulateDatasetList(activeSession, "work");

            // when code is complete, check error/warning
            bool hasErrors = false, hasWarnings = false;
            StringBuilder log = new StringBuilder();
            Array carriage, lineTypes, lines;
            do
            {
                activeSession.Workspace.LanguageService.FlushLogLines(1000,
                    out carriage,
                    out lineTypes,
                    out lines);
                for (int i = 0; i < lines.GetLength(0); i++)
                {
                    SAS.LanguageServiceLineType pre =
                        (SAS.LanguageServiceLineType)lineTypes.GetValue(i);
                    Console.WriteLine(lines.GetValue(i));
                    switch (pre)
                    {
                        case SAS.LanguageServiceLineType.LanguageServiceLineTypeError:
                            hasErrors = true;
                            break;
                        case SAS.LanguageServiceLineType.LanguageServiceLineTypeWarning:
                            hasWarnings = true;
                            break;
                        default:
                            break;
                    }
                    log.Append(string.Format("{0}{1}", lines.GetValue(i) as string, Environment.NewLine));
                  }
                }
            while (lines != null && lines.Length > 0);

            using (StreamWriter outfile = new StreamWriter("log.txt"))
            {
                outfile.Write(log.ToString());
            }

            if (hasWarnings && hasErrors)
            { Console.WriteLine("Program complete - has ERRORS and WARNINGS"); exitcode = 4; }
            else if (hasErrors)
            { Console.WriteLine("Program complete - has ERRORS"); exitcode = 4; }
            else if (hasWarnings)
            { Console.WriteLine("Program complete - has WARNINGS"); exitcode = 0; }
            else
            { Console.WriteLine("Program complete - no warnings or errors!"); exitcode = 0; }


            return exitcode;

        }

        //string ReadFileFromAssembly(string filename)
        //{
        //    string filecontents = String.Empty;
        //    Assembly assem = System.Reflection.Assembly.GetCallingAssembly();
        //    Stream stream = assem.GetManifestResourceStream(filename);
        //    if (stream != null)
        //    {
        //        StreamReader tr = new StreamReader(stream);
        //        filecontents = tr.ReadToEnd();
        //        tr.Close();
        //        stream.Close();
        //    }

        //    return filecontents;
        //}

        static void PopulateDatasetList(SasServer ss,string libname)
        {
           
                ADODB.Recordset adorecordset = new ADODB.RecordsetClass();
                ADODB.Connection adoconnect = new ADODB.ConnectionClass();

                try
                {
                    adoconnect.Open("Provider=sas.iomprovider.1; SAS Workspace ID=" + ss.Workspace.UniqueIdentifier, "", "", 0);
                    // use the SASHELP.VMEMBER view to get names of all of the datasets and views in the specified library
                    string selectclause = "select memname, memtype from sashelp.vmember where libname='" + libname + "' and memtype in ('DATA', 'VIEW')";
                    adorecordset.Open(selectclause, adoconnect, ADODB.CursorTypeEnum.adOpenForwardOnly, ADODB.LockTypeEnum.adLockReadOnly, (int)ADODB.CommandTypeEnum.adCmdText);

                    OleDbDataAdapter da = new OleDbDataAdapter();
                    DataSet ds = new DataSet();
                    da.Fill(ds, adorecordset, "data");

                }
                catch
                {

                }

                finally
                {
                    adoconnect.Close();
                }

        }

        static Boolean AddExcelRow(String strFilePath)
        {

        if (!File.Exists(strFilePath)) return false;
        String strExcelConn = "Provider=Microsoft.Jet.OLEDB.4.0;"+ "Data Source=" + strFilePath + ";"+ "Extended Properties='Excel 8.0;HDR=Yes'";
        OleDbConnection connExcel = new OleDbConnection(strExcelConn);
        OleDbCommand cmdExcel = new OleDbCommand();

            try
            {
            cmdExcel.Connection = connExcel;

    //Check if the Sheet Exists
            connExcel.Open();
            DataTable dtExcelSchema;
            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            connExcel.Close();

            DataRow[] dr = dtExcelSchema.Select("TABLE_NAME = 'tblData'");

     //if not Create the Sheet

           if (dr == null || dr.Length == 0)
            {
            cmdExcel.CommandText = "CREATE TABLE [tblData]" + "(ID varchar(10), Name varchar(50));";
            connExcel.Open();
            cmdExcel.ExecuteNonQuery();
            connExcel.Close();
            }

     connExcel.Open();

     //Add New Row to Excel File

            cmdExcel.CommandText = "INSERT INTO [tblData] (ID, Name)" + " values ('1', 'MAK')";
            cmdExcel.ExecuteNonQuery();
            connExcel.Close();
            return true;
        }

        catch
        {
        return false;
        }

        finally
        {
        cmdExcel.Dispose();
        connExcel.Dispose();
        }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApplication1
{
    public class SasServer
    {
        /// <summary>
        /// Name of the server ("SASApp")
        /// </summary>
        public string Name { get {return "Test";} }
        /// <summary>
        /// Node name of the server
        /// </summary>
        public string Host { get { return "prdsasad02"; } }
        /// <summary>
        /// Port (number), such as 8591
        /// </summary>
        public string Port { get { return "8591"; }  }
        /// <summary>
        /// User ID that can connect to a Workspace
        /// </summary>
        public string UserId { get { return "cwang"; } }
        /// <summary>
        /// Password to connect to the Workspace 
        /// </summary>
        public string Password { get { return ""; }  }
        /// <summary>
        /// Whether to use Local (COM) connection instead of IOM Bridge
        /// </summary>
  
        // Use the ObjectKeeper, which keeps track of SAS Workspaces
        // We need this so that the OLE DB provider can find the workspace to
        // connect to if/when the user opens a data set to view
        internal static SASObjectManager.ObjectKeeper objectKeeper =
            new SASObjectManager.ObjectKeeper();

        /// <summary>
        /// Property for the SAS Workspace connection.
        /// Will connect if needed.
        /// </summary>
        public SAS.Workspace Workspace
        {
            get
            {
                if (_workspace == null)
                    Connect();
                
                if (_workspace!=null)
                    return _workspace;
                else
                    throw new Exception("Could not connect to SAS Workspace");
            }
        }

        /// <summary>
        /// Is a Workspace connected?
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _workspace != null;     
            }
        }

        /// <summary>
        /// Close the Workspace if connected
        /// </summary>
        public void Close()
        {
            if (IsConnected) _workspace.Close();
            
            // clear out the ObjectKeeper
            objectKeeper.RemoveAllObjects();

            _workspace = null;
        }

        /// <summary>
        /// Connect to a SAS Workspace
        /// </summary>
        public void Connect()
        {
            if (_workspace != null)
                try
                {
                    Close();
                }
                catch { }
                finally
                { 
                    _workspace = null; 
                }

                // Connect using the IOM Bridge (TCP) for remote server
                SASObjectManager.IObjectFactory2 obObjectFactory = 
                    new SASObjectManager.ObjectFactoryMulti2();
                SASObjectManager.ServerDef obServer = 
                    new SASObjectManager.ServerDef();
                obServer.MachineDNSName = Host;
                obServer.Protocol = SASObjectManager.Protocols.ProtocolBridge;
                obServer.Port = Convert.ToInt32(Port);
                obServer.ClassIdentifier = "440196d4-90f0-11d0-9f41-00a024bb830c";

                // handle the case where there is no UserID or PW, and try IWA
                // Doc on integrated Windows auth in SAS: http://support.sas.com/documentation/cdl/en/bisecag/63082/HTML/default/viewer.htm#n0go7pclcqr6q2n1bxfuk46hmtdy.htm
                if (string.IsNullOrEmpty(UserId))
                {
                    obServer.BridgeSecurityPackage = "Negotiate";
                }

                _workspace = (SAS.Workspace)obObjectFactory.CreateObjectByServer(
                    Name, true, 
                    obServer,
                    // if trying IWA, pass null in
                    // otherwise try supplied credentials
                    string.IsNullOrEmpty(UserId) ? null : UserId,
                    string.IsNullOrEmpty(Password) ? null : Password);

                objectKeeper.AddObject(1, Name, _workspace);

           
        }

        private SAS.Workspace _workspace = null;


    }
}

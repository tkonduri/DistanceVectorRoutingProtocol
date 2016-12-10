/*
 * Developed by: Tejaswi Konduri
 * Course: Computer Communication and Networks
 * Department of Computer Science, UNC Charlotte
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DVRouting
{
    class Program
    {
        static int iUdpPort;
        static string hostName;
        static string szInputFilePath;
        const string LOCALHOST = "127.0.0.1";
        const int TIMEOUT = 15000;
        static Dictionary<string, float> szFlMap = new Dictionary<string, float>();
        
        /// <summary>
        /// List of destination port numbers...
        /// </summary>
        static List<int> portList = new List<int>();
        
        /// <summary>
        /// Map of nodes and their next hop hosts...
        /// </summary>
        static Dictionary<string, string> nextHopMap = new Dictionary<string, string>();
        
        /// <summary>
        /// Command line output after each send operation...
        /// </summary>
        /// <param name="iCount">Output message count</param>
        void commandLineOutput(int iCount)
        {
            int iCnt = 0;
            
            try
            {
                Console.WriteLine("Output Number " + iCount);

                String currentLine = "";
                using (StreamReader sr = new StreamReader(szInputFilePath))
                {
                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        ///The first line contains the number of neighboring nodes, so skipping that...
                        if (iCnt > 0)
                        {
                            string[] words = currentLine.Split(' ');
                            Console.WriteLine("Shortest path " + hostName + "-" + words[0] + ": the next hop is " + words[3] + " and the cost is " + words[1]);
                        }
                        ++iCnt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }

        string getTheNextHop(string key)
        {
            string szNextHop = "", line = "";
            try
            {
                ///File operations...
                FileStream fileStream = new FileStream(szInputFilePath, FileMode.Open, FileAccess.Read);

                ///Reading the input file...
                using (var sr = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith(key))
                        {
                            string[] words = line.Split(' ');
                            szNextHop = words[3];
                            return szNextHop;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured : " + ex.Message);
            }
            return szNextHop;
        }

        Dictionary<string, float> getLatestIpFileData()
        {
            Dictionary<string, float> inpFileMap = new Dictionary<string, float>();
            
            string line;
            var list = new List<string>();
            float fCost = 0;
            int iCnt = 0;

            try
            {
                inpFileMap.Clear();
                ///File operations...
                FileStream fileStream = new FileStream(szInputFilePath, FileMode.Open, FileAccess.Read);

                ///Reading the input file...
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (iCnt > 0)
                        {
                            string[] words = line.Split(' ');
                            float.TryParse(words[1], out fCost);
                            inpFileMap.Add(words[0], fCost);
                        }
                        ++iCnt;
                    }
                }

                fileStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured (getLatestIpFileData): " + ex.Message);
            }

            return inpFileMap;
        }
        
        string reCalcInputString(string szLine)
        {
            Dictionary<string, float> latestIpFileMap = new Dictionary<string, float>();
            float fInputCost = 0, fFileCost = 0;
            string szRetString = "", szNextHop = "";

            try
            {
                string[] inputString = szLine.Split(' ');
                float.TryParse(inputString[1], out fInputCost);
                
                latestIpFileMap = getLatestIpFileData();
                latestIpFileMap.TryGetValue(inputString[3], out fFileCost);

                ///read the input fie to get the next hop info...
                szNextHop = getTheNextHop(inputString[3]);

                szRetString = inputString[0] + " " + (fInputCost + fFileCost).ToString() + " " + inputString[2] + " " + szNextHop + "\n"; 
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }

            return szRetString;
        }

        /// <summary>
        /// Compare and operate on the received message from the neighbor...
        /// </summary>
        /// <param name="szRcvData">Received data</param>
        void compareRecMessage(string szRcvData)
        {
            var list = new List<string>();
            var newNodeList = new List<string>();
            string szRcvHost = "";
            float fValue, fHostValue;
            Dictionary<string, float> rcvdMsgMap = new Dictionary<string, float>();
            Dictionary<string, float> latestIpFileMap = new Dictionary<string, float>();
            
            string line;
            int iCnt = 0;
            float fCost;

            try
            {
                #region Reading_And_Storing_Received_Text
                ///Reading the received text...                
                using (StringReader sr = new StringReader(szRcvData))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (iCnt == 0)
                        {
                            szRcvHost = line;
                        }
                        else
                        {
                            string[] words = line.Split(' ');
                            float.TryParse(words[1], out fCost);
                            rcvdMsgMap.Add(words[0], fCost);
                        }

                        ++iCnt;
                    }
                }
                #endregion

                #region Checking if the node is already present in the input file, if not adding to a temporary list

                //Recompute szFlMap before the foreach loop, to get the updated input file...
                latestIpFileMap = getLatestIpFileData();
                
                newNodeList.Clear();
                foreach (var key in rcvdMsgMap.Keys)
                {
                    if ((latestIpFileMap.ContainsKey(key) == false) && (key != hostName))
                    {
                        rcvdMsgMap.TryGetValue(key, out fValue);
                        ///Adding the newly discovered nodes to a list, which can later be used to write to the input file...
                        newNodeList.Add(key + " " + fValue.ToString() + " " + "000" + " " + szRcvHost);
                    }
                }
                #endregion

                #region add new host entry to the input file from the temporary list
                if (newNodeList.Count > 0)
                {
                    foreach (var szLine in newNodeList)
                    {
                        string readText = File.ReadAllText(szInputFilePath);
                        ///recalculate the new string to be added (szLine)
                        string szLine1 = reCalcInputString(szLine);
                        File.WriteAllText(szInputFilePath, readText + szLine1);
                    }
                    newNodeList.Clear();
                }
                #endregion

                #region re-compute the cost of already existing hosts in the input file

                latestIpFileMap = getLatestIpFileData();
                foreach (var key in rcvdMsgMap.Keys)
                {
                    //If the input file has the key and also has the host that sent the message
                    if ((latestIpFileMap.ContainsKey(key) == true) && (latestIpFileMap.ContainsKey(szRcvHost) == true) && (key != hostName))
                    {
                        //Get the value of the key
                        rcvdMsgMap.TryGetValue(key, out fValue);

                        //Get the value of the receiver host key
                        latestIpFileMap.TryGetValue(szRcvHost, out fHostValue);

                        float f2 = 0;                        
                        rcvdMsgMap.TryGetValue(key, out f2);

                        float fCurrentValue;
                        latestIpFileMap.TryGetValue(key, out fCurrentValue);
                        if (fCurrentValue > (fHostValue + fValue))
                        {
                            //Update the value inside the input file...
                            
                            String fileContents = "", currentLine = "";
                            using (StreamReader sr = new StreamReader(szInputFilePath))
                            {
                                while ((currentLine = sr.ReadLine()) != null)
                                {
                                    if (currentLine.StartsWith(key))
                                    {
                                        string[] words = currentLine.Split(' ');
                                        float fMinCost = fHostValue + fValue;
                                        ///Check in the input file and see if the next hop of c is c, if not write the value of the next hop in place of szRcvHost in the line below...
                                        string szNextHop = getTheNextHop(szRcvHost);

                                        if (0 == string.Compare(szRcvHost, szNextHop))
                                        {
                                            ///both are equal
                                            currentLine = words[0] + " " + fMinCost.ToString() + " " + words[2] + " " + szRcvHost;
                                        }
                                        else
                                        {
                                            currentLine = words[0] + " " + fMinCost.ToString() + " " + words[2] + " " + szNextHop;
                                        }
                                    }
                                    fileContents += currentLine + "\n";
                                }
                            }
                            using (StreamWriter sw = new StreamWriter(szInputFilePath))
                            {
                                sw.Write(fileContents);
                            }
                        }

                        /// Reverse case...
                        if (fHostValue > (f2 + fValue))
                        {
                            //Update the value inside the input file...

                            String fileContents = "", currentLine = "";
                            using (StreamReader sr = new StreamReader(szInputFilePath))
                            {
                                while ((currentLine = sr.ReadLine()) != null)
                                {
                                    if (currentLine.StartsWith(hostName))
                                    {
                                        string[] words = currentLine.Split(' ');
                                        float fMinCost = f2 + fValue;
                                        currentLine = words[0] + " " + fMinCost.ToString() + " " + words[2] + " " + key;
                                    }
                                    fileContents += currentLine + "\n";
                                }
                            }
                            using (StreamWriter sw = new StreamWriter(szInputFilePath))
                            {
                                sw.Write(fileContents);
                            }
                        }             
                    }
                }
                #endregion                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured (compareRecMessage) : " + ex.Message);
            }
        }

        /// <summary>
        /// Adding additional information in the input file regarding the next hop host
        /// </summary>
        /// <param name="szInputFilePath">Input file path</param>
        /// <param name="lines">lines in the input file</param>
        /// <param name="iNoOfNeighbors">No. of neighboring nodes/hosts</param>
        void addNextHopInInputFile(string szInputFilePath, string[] lines, int iNoOfNeighbors)
        {
            try
            {
                FileStream fileStreamW = new FileStream(szInputFilePath, FileMode.Open, FileAccess.Write);

                ///Writing to the output file (adding the next hop information)...
                using (var streamWriter = new StreamWriter(fileStreamW, Encoding.UTF8))
                {
                    streamWriter.WriteLine(iNoOfNeighbors);

                    for (int j = 1; j <= iNoOfNeighbors; j++)
                    {
                        string[] words = lines[j].Split(' ');
                        streamWriter.WriteLine(words[0] + " " + words[1] + " " + words[2] + " " + words[0]);
                    }
                }

                fileStreamW.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }

        ///Reading the input file
        Dictionary<string, float> readingAndUpdatingInputFile(String szInputFilePath)
        {
            Dictionary<string, float> szFlMap = new Dictionary<string, float>();

            try
            {
                string[] lines;
                string line;
                var list = new List<string>();
                int iNoOfNeighbors = 0;
                float fCost = 0;
                int iDestPort = 0;
                
                ///Extracting the host name
                int pFrom = szInputFilePath.LastIndexOf(@"\") + @"\".Length;
                int pTo = szInputFilePath.LastIndexOf(".dat");

                hostName = szInputFilePath.Substring(pFrom, pTo - pFrom);
                Console.WriteLine("!!!!! Local Host Name = {0} !!!!!", hostName);
                
                ///File operations...
                FileStream fileStream = new FileStream(szInputFilePath, FileMode.Open, FileAccess.Read);

                ///Reading the input file...
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // process the line
                        Console.WriteLine(line);                        
                        list.Add(line);
                    }
                }

                ///Reading the first line from the input file...
                lines = list.ToArray();
                int.TryParse(lines[0], out iNoOfNeighbors);
                Console.WriteLine("\n\nCount = " + iNoOfNeighbors);

                ///Writing the file (Adding the next hop information to the input file)
                addNextHopInInputFile(szInputFilePath, lines, iNoOfNeighbors);

                ///filling the dictionary
                for (int j = 1; j <= iNoOfNeighbors; j++)
                {
                    string[] words = lines[j].Split(' ');
                    float.TryParse(words[1], out fCost);
                    szFlMap.Add(words[0], fCost);

                    ///Adding UDP ports to the list
                    int.TryParse(words[2], out iDestPort);
                    portList.Add(iDestPort);
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return szFlMap;
        }

        void printInputFile(Dictionary<string, float> szFlMap)
        {
            try
            {
                ///Printing the dictionary content
                Console.WriteLine("Displaying the dictionary...");
                foreach (KeyValuePair<string, float> keyValue in szFlMap)
                {
                    Console.WriteLine("{0} -> {1}", keyValue.Key, keyValue.Value);
                }

                Console.WriteLine("Neighboring nodes UDP port numbers: ");
                foreach (int elem in portList)
                {
                    Console.WriteLine("Port # = " + elem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        Dictionary<string, float> fetchLatestFileContents()
        {
            Dictionary<string, float> szFlMap = new Dictionary<string, float>();
            portList.Clear();
            szFlMap.Clear();

            try
            {
                string[] lines;
                string line;
                var list = new List<string>();
                int iNoOfNeighbors = 0;
                float fCost = 0;
                int iDestPort = 0;
                int iCnt = 0;

                list.Clear();

                ///File operations...
                FileStream fileStream = new FileStream(szInputFilePath, FileMode.Open, FileAccess.Read);

                ///Reading the input file...
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // process the line
                        list.Add(line);
                    }
                }

                ///Reading the first line from the input file...
                lines = list.ToArray();
                int.TryParse(lines[0], out iNoOfNeighbors);

                ///filling the dictionary                
                foreach (var CurrentLine in list)
                {
                    if (iCnt > 0)
                    {
                        string[] words = CurrentLine.Split(' ');
                        float.TryParse(words[1], out fCost);
                        szFlMap.Add(words[0], fCost);

                        if (iCnt <= iNoOfNeighbors)
                        {
                            ///Adding UDP ports to the list
                            int.TryParse(words[2], out iDestPort);
                            portList.Add(iDestPort);
                        }
                    }
                    ++iCnt;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return szFlMap;
        }

        void sendReceiveFunction()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, iUdpPort);

            ///Receiver UDP client
            UdpClient udpClient = new UdpClient(ipep);
            
            ///Sender UDP client
            UdpClient udpClientB = null;
            int iCount = 1;
            
            string szSendMessage = "";
            Byte[] sendBytes;

            Program ob = new Program();

            try
            {
                while (true)
                {
                    szFlMap = fetchLatestFileContents();
                    szSendMessage = hostName + "\n";
                    foreach (KeyValuePair<string, float> keyValue in szFlMap)
                    {
                        szSendMessage += keyValue.Key + " " + keyValue.Value + "\n";
                    }
                    
                    // Sends a message to a different host using optional hostname and port parameters.
                    sendBytes = Encoding.ASCII.GetBytes(szSendMessage);
                    udpClientB = new UdpClient();

                    foreach (int iUdpDestPort in portList)
                    {
                        udpClientB.Send(sendBytes, sendBytes.Length, LOCALHOST, iUdpDestPort);

                        string sendData = Encoding.ASCII.GetString(sendBytes);
                        Console.WriteLine("Content sent to Port # (" + iUdpDestPort + ") = " + sendData);
                    }

                    commandLineOutput(iCount);

                    Console.WriteLine("Waiting for a client...");

                    //IPEndPoint object will allow us to read datagrams sent from any source.
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    
                    try
                    {
                        udpClient.Client.ReceiveTimeout = TIMEOUT;

                        // Blocks until a message returns on this socket from a remote host or the timeout occurs.
                        Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                        string returnData = Encoding.ASCII.GetString(receiveBytes);

                        // Uses the IPEndPoint object to determine which of these two hosts responded.
                        Console.WriteLine("This is the message you received = \n" + returnData.ToString());

                        ///Processing on the received data
                        compareRecMessage(returnData.ToString());
                    }
                    catch
                    { }

                    ///Send out messages after every 15 sec, hence the sleep with the timeout period of 15 sec
                    Thread.Sleep(TIMEOUT);

                    ++iCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                udpClient.Close();
                udpClientB.Close();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                #region IO                
                Console.WriteLine("### Distance Vector Routing protocol implementation.");
                Console.WriteLine("Inputs:\nUDP Port # = " + args[0] + "\nInput file path = " + args[1]);
                int.TryParse(args[0], out iUdpPort);
                szInputFilePath = args[1];

                Program ob = new Program();

                ///Reading the input file
                szFlMap = ob.readingAndUpdatingInputFile(szInputFilePath);

                ///Printing the input file
                ob.printInputFile(szFlMap);
                #endregion

                #region Thread                
                Thread threadClient = new Thread(new ThreadStart(ob.sendReceiveFunction));
                threadClient.Start();
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
/*
 * Developed by: Tejaswi Konduri
 * Purpose: The utility is used to change the value of the edge between two specifies nodes...
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeValueChanger
{
    class Program
    {
        static double dValue = 0;
        static string szStartNode = "", szEndNode = "";

        static void updateFile(string szFileNameToUpdate, string szLastNode)
        {
            try
            {
                //Update the value inside the input file...
                String fileContents = "", currentLine = "";
                using (StreamReader sr = new StreamReader(szFileNameToUpdate))
                {
                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        if (currentLine.StartsWith(szLastNode))
                        {
                            string[] words = currentLine.Split(' ');
                            
                            currentLine = words[0] + " " + dValue.ToString() + " " + words[2] + " " + szLastNode;
                        }
                        fileContents += currentLine + "\n";
                    }
                }
                using (StreamWriter sw = new StreamWriter(szFileNameToUpdate))
                {
                    sw.Write(fileContents);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured : " + ex.Message);
            }
        }


        static void updateEdgeValue()
        {
            try
            {
                //Update file szStartNode
                updateFile(szStartNode + ".dat", szEndNode);

                //Update file szEndNode
                updateFile(szEndNode + ".dat", szStartNode);

                Console.WriteLine("\nThe values in the files have been updated...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured : " + ex.Message);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Node value changer");
                Console.WriteLine("Input format:<start-node> <end-node> <value>");
                Console.WriteLine("Example Input:a c 1");

                szStartNode = args[0];
                szEndNode = args[1];
                double.TryParse(args[2], out dValue);
                Console.WriteLine("\nUser entered Input:{0} {1} {2}", szStartNode, szEndNode, dValue);

                updateEdgeValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }
    }
}

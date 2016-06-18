using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS4DeltaCalc
{
    class FunctionEntry
    {
        
    }

    class Program
    {
        static void Main(string[] p_Args)
        {
            // TODO: Handle unnamed shit

            // Make sure we have the right amount of arguments
            if (p_Args.Length < 3)
            {
                Console.WriteLine("PS4DeltaCalc.exe <first input file> <second input file> <output file>");
                return;
            }

            // Get and check the file paths
            var s_FirstFilePath = p_Args[0];
            var s_SecondFilePath = p_Args[1];
            var s_OutputFilePath = p_Args[2];

            var s_Engine = new Deltas();
            s_Engine.ReadOriginalLog(s_FirstFilePath);
            s_Engine.ReadChangedLog(s_SecondFilePath);

            s_Engine.Compare(s_OutputFilePath);
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;

namespace ProcessNMTAlignments
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFile = null;
            string translatedFile = null;
            string outputFile = null;
            string alignmentDirectory = null;
            string alignmentFilePattern = "alignments_{0}.txt";
            string mosesFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o" && args.Length > i + 1)
                {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-s" && args.Length > i + 1)
                {
                    sourceFile = args[i + 1];
                }
                else if (args[i] == "-t" && args.Length > i + 1)
                {
                    translatedFile = args[i + 1];
                }
                else if (args[i] == "-a" && args.Length > i + 1)
                {
                    alignmentDirectory = args[i + 1];
                }
                else if (args[i] == "-p" && args.Length > i + 1)
                {
                    alignmentFilePattern = args[i + 1];
                }
                else if (args[i] == "-m" && args.Length > i + 1)
                {
                    mosesFile = args[i + 1];
                }
            }


            if (string.IsNullOrWhiteSpace(sourceFile)
                || string.IsNullOrWhiteSpace(translatedFile)
                || string.IsNullOrWhiteSpace(outputFile)
                || string.IsNullOrWhiteSpace(alignmentDirectory)
                || string.IsNullOrWhiteSpace(alignmentFilePattern)
                || !File.Exists(sourceFile)
                || !File.Exists(translatedFile)
                || !Directory.Exists(alignmentDirectory))
            {
                PrintUsage(true);
            }

            if (!alignmentDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())) alignmentDirectory += Path.DirectorySeparatorChar.ToString();
            List<string> sourceLines = new List<string>(File.ReadAllLines(sourceFile, Encoding.UTF8));
            List<string> translatedLines = new List<string>(File.ReadAllLines(translatedFile, Encoding.UTF8));
            
            StreamWriter swWithGaps = new StreamWriter(outputFile + ".with_gaps", false, new UTF8Encoding(false));
            swWithGaps.NewLine = "\n";
            StreamWriter swWithoutGaps = new StreamWriter(outputFile + ".no_gaps", false, new UTF8Encoding(false));
            swWithoutGaps.NewLine = "\n";

            StreamWriter swWithGapsWithLowConfTokens = new StreamWriter(outputFile + ".with_gaps.low_conf", false, new UTF8Encoding(false));
            swWithGapsWithLowConfTokens.NewLine = "\n";
            StreamWriter swWithoutGapsWithLowConfTokens = new StreamWriter(outputFile + ".no_gaps.low_conf", false, new UTF8Encoding(false));
            swWithoutGapsWithLowConfTokens.NewLine = "\n";

            NMTAlignmentProcessor nmt = new NMTAlignmentProcessor();
            nmt.ReadMSDDictionary(mosesFile);

            for (int i = 0; i < sourceLines.Count; i++)
            {
                string sourceSentence = sourceLines[i];
                string[] sourceTokens = sourceSentence.Split(NMTAlignmentProcessor.sep, StringSplitOptions.RemoveEmptyEntries);
                string translatedSentence = translatedLines[i];
                string[] translatedTokens = translatedSentence.Split(NMTAlignmentProcessor.sep, StringSplitOptions.RemoveEmptyEntries);
                Matrix<double> alignmentMatrix = nmt.ReadAlignmentFile(alignmentDirectory + string.Format(alignmentFilePattern, i));

                //First find the maximal source and target indices.

                //Possible algorithms:
                //1) Maximal source and target - without additional words that are not adjacent
                //2) Maximal source and target - with additional split word reordering
                List<NMTAlignmentElement> alignmentsWithGaps = nmt.GetMaxAlignments(sourceTokens, translatedTokens, alignmentMatrix);
                List<NMTAlignmentElement> alignmentsWithGapsWithLowConfTokens = nmt.GetMaxAlignments(sourceTokens, translatedTokens, alignmentMatrix, false, true);
                List<NMTAlignmentElement> alignmentsWithoutGaps = nmt.GetMaxAlignments(sourceTokens, translatedTokens, alignmentMatrix, true, false);
                List<NMTAlignmentElement> alignmentsWithoutGapsWithLowConfTokens = nmt.GetMaxAlignments(sourceTokens, translatedTokens, alignmentMatrix, true, true);
                PrintSentenceToFile(swWithGaps, alignmentsWithGaps, nmt);
                PrintSentenceToFile(swWithoutGaps, alignmentsWithoutGaps, nmt);

                PrintSentenceToFile(swWithGapsWithLowConfTokens, alignmentsWithGapsWithLowConfTokens, nmt);
                PrintSentenceToFile(swWithoutGapsWithLowConfTokens, alignmentsWithoutGapsWithLowConfTokens, nmt);


            }

            swWithGaps.Close();
            swWithoutGaps.Close();
            swWithGapsWithLowConfTokens.Close();
            swWithoutGapsWithLowConfTokens.Close();
        }

        

        private static void PrintSentenceToFile(StreamWriter sw, List<NMTAlignmentElement> alignments, NMTAlignmentProcessor nmt)
        {
            bool wasWritten = false;
            StringBuilder sbSrc = new StringBuilder();
            StringBuilder sbTrg = new StringBuilder();
            foreach (NMTAlignmentElement elem in alignments)
            {
                if (wasWritten && !string.IsNullOrWhiteSpace(elem.sourceString))
                {
                    sw.Write(" ");
                }
                else if (!string.IsNullOrWhiteSpace(elem.sourceString))
                {
                    wasWritten = true;
                }
                if (nmt.msdDictionary.Contains(elem.targetString))
                {
                    sw.Write(elem.sourceString);
                }
                else
                {
                    if (elem.sourceString.Length > 0)
                    {
                        sw.Write("<nmt translation=\"");
                        sw.Write(elem.targetString);
                        sw.Write("\"> ");
                        sw.Write(elem.sourceString);
                        sw.Write(" </nmt>");
                        sbTrg.Clear();
                        sbSrc.Clear();
                    }
                    else if (elem.targetString.Length > 0)
                    {
                        sw.Write("<nmt translation=\"");
                        sw.Write(elem.targetString);
                        sw.Write("\"> SRC_NULL </nmt>");
                        sbTrg.Clear();
                        sbSrc.Clear();
                    }
                }
            }
            sw.WriteLine();
        }

        private static void PrintUsage(bool addWarning = false)
        {
            if (addWarning)
            {
                Console.Error.WriteLine("ERROR: An important input parameter was not defined or an input file/directory was not accessible!");
                Console.Error.WriteLine();
            }
            Console.Error.WriteLine("Usage: mono ./ProcessNMTAlignments.exe [ARGS]");
            Console.Error.WriteLine("  where [ARGS] are:");
            Console.Error.WriteLine("    -s [File] - the source input file for translation (tokenised)");
            Console.Error.WriteLine("    -t [File] - the translated file (tokenised)");
            Console.Error.WriteLine("    -o [File] - the output file");
            Console.Error.WriteLine("    -a [Directory] - the directory of the NMT alignment files");
            Console.Error.WriteLine("    -p [Pattern] - the alignment file pattern (default: \"alignments_{0}.txt\")");
            Console.Error.WriteLine();
        }

    }
}